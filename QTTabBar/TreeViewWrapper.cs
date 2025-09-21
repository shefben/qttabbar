//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    class TreeViewWrapper : IDisposable {
        public delegate void TreeViewMiddleClickedHandler(IShellItem item);
        public event FolderClickedHandler TreeViewClicked;

        private bool fDisposed;
        private INameSpaceTreeControl treeControl;
        private NativeWindowController treeController;
        private NativeWindowController parentController;
        private bool fPreventSelChange;
        private readonly Dictionary<IntPtr, Color?> tagColorCache = new Dictionary<IntPtr, Color?>();

        public TreeViewWrapper(IntPtr hwnd, INameSpaceTreeControl treeControl) {
            QTUtility2.log("TreeViewWrapper init");
            this.treeControl = treeControl;
            treeController = new NativeWindowController(hwnd);
            treeController.MessageCaptured += TreeControl_MessageCaptured;
            parentController = new NativeWindowController(PInvoke.GetParent(hwnd));
            parentController.MessageCaptured += ParentControl_MessageCaptured;
        }

        private bool HandleClick(Point pt, Keys modifierKeys, bool middle) {
            QTUtility2.log("TreeViewWrapper HandleClick");
            IShellItem item = null;
            try {
                TVHITTESTINFO structure = new TVHITTESTINFO { pt = pt };
                IntPtr wParam = PInvoke.SendMessage(treeController.Handle, 0x1111, IntPtr.Zero, ref structure);
                if(wParam != IntPtr.Zero) {
                    if((structure.flags & 0x10) == 0 && (structure.flags & 0x80) == 0) {
                        treeControl.HitTest(pt, out item);
                        if(item != null) {
                            IntPtr pidl;
                            if(PInvoke.SHGetIDListFromObject(item, out pidl) == 0) {
                                using(IDLWrapper wrapper = new IDLWrapper(pidl)) {
                                    return TreeViewClicked(wrapper, modifierKeys, middle);
                                }
                            }
                        }
                    }
                }
            }
            finally {
                if(item != null) {
                    QTUtility2.log("ReleaseComObject item");
                    Marshal.ReleaseComObject(item);
                }
            }
            return false;
        }

        private bool TreeControl_MessageCaptured(ref Message msg) {
            switch(msg.Msg) {
                case WM.USER:
                    QTUtility2.log("TreeViewWrapper TreeControl_MessageCaptured WM.USER");
                    fPreventSelChange = false;
                    break;

                case WM.MBUTTONUP:
                    if (treeControl != null && TreeViewClicked != null) {
                        QTUtility2.log("TreeViewWrapper TreeControl_MessageCaptured MBUTTONUP");
                        HandleClick(QTUtility2.PointFromLPARAM(msg.LParam), Control.ModifierKeys, true);
                    }
                    break;

                case WM.DESTROY:
                    if(treeControl != null)
                    {
                        QTUtility2.log("TreeViewWrapper TreeControl_MessageCaptured DESTROY");
                        Marshal.ReleaseComObject(treeControl);
                        treeControl = null;
                    }
                    tagColorCache.Clear();
                    break;
            }
            return false;
        }

        private bool ParentControl_MessageCaptured(ref Message msg) {
            if(msg.Msg == WM.NOTIFY) {

                NMHDR nmhdr = (NMHDR)Marshal.PtrToStructure(msg.LParam, typeof(NMHDR));
                switch(nmhdr.code) {
                    case -12: /* NM_CUSTOMDRAW */
                        if(HandleCustomDraw(ref msg)) {
                            return true;
                        }
                        break;

                    case -2: /* NM_CLICK */
                        if(Control.ModifierKeys != Keys.None) {
                            QTUtility2.log("TreeViewWrapper ParentControl_MessageCaptured WM.NOTIFY NM_CLICK");
                            Point pt = Control.MousePosition;
                            PInvoke.ScreenToClient(nmhdr.hwndFrom, ref pt);
                            if(HandleClick(pt, Control.ModifierKeys, false)) {
                                fPreventSelChange = true;
                                PInvoke.PostMessage(nmhdr.hwndFrom, WM.USER, IntPtr.Zero, IntPtr.Zero);
                                return true;                                
                            }
                        }
                        break;

                    case -450: /* TVN_SELECTIONCHANGING */
                        QTUtility2.log("TreeViewWrapper ParentControl_MessageCaptured WM.NOTIFY TVN_SELECTIONCHANGING");
                        if(fPreventSelChange) {
                            msg.Result = (IntPtr)1;
                            return true;
                        }
                        break;
                }
            }
            return false;
        }

        private bool HandleCustomDraw(ref Message msg) {
            NMTVCUSTOMDRAW draw = (NMTVCUSTOMDRAW)Marshal.PtrToStructure(msg.LParam, typeof(NMTVCUSTOMDRAW));
            switch(draw.nmcd.dwDrawStage) {
                case CDDS.PREPAINT:
                    msg.Result = (IntPtr)CDRF.NOTIFYITEMDRAW;
                    return true;

                case CDDS.ITEMPREPAINT:
                    Color? textColor = ResolveTagColor(draw);
                    if(textColor.HasValue) {
                        draw.clrText = QTUtility2.MakeCOLORREF(textColor.Value);
                        Marshal.StructureToPtr(draw, msg.LParam, false);
                        msg.Result = (IntPtr)CDRF.NEWFONT;
                        return true;
                    }
                    break;
            }
            msg.Result = IntPtr.Zero;
            return false;
        }

        private Color? ResolveTagColor(NMTVCUSTOMDRAW draw) {
            IntPtr handle = draw.nmcd.dwItemSpec;
            Color? cached;
            if(tagColorCache.TryGetValue(handle, out cached)) {
                return cached;
            }
            Color? color = null;
            string path = TryGetPathFromDraw(draw);
            if(!string.IsNullOrEmpty(path)) {
                color = TagManager.GetTagColorForPath(path);
            }
            tagColorCache[handle] = color;
            return color;
        }

        private string TryGetPathFromDraw(NMTVCUSTOMDRAW draw) {
            if(treeControl == null) {
                return null;
            }
            Point pt = new Point((draw.nmcd.rc.left + draw.nmcd.rc.right) / 2, (draw.nmcd.rc.top + draw.nmcd.rc.bottom) / 2);
            IShellItem item = null;
            try {
                if(treeControl.HitTest(ref pt, out item) == 0 && item != null) {
                    IntPtr pidl;
                    if(PInvoke.SHGetIDListFromObject(item, out pidl) == 0) {
                        try {
                            using(IDLWrapper wrapper = new IDLWrapper(pidl)) {
                                if(wrapper.Available && wrapper.HasPath) {
                                    return wrapper.Path;
                                }
                            }
                        }
                        finally {
                            if(pidl != IntPtr.Zero) {
                                PInvoke.CoTaskMemFree(pidl);
                            }
                        }
                    }
                }
            }
            catch { }
            finally {
                if(item != null) {
                    Marshal.ReleaseComObject(item);
                }
            }
            return null;
        }

        internal void RefreshTagColors() {
            tagColorCache.Clear();
            if(treeController != null && treeController.Handle != IntPtr.Zero) {
                PInvoke.InvalidateRect(treeController.Handle, IntPtr.Zero, true);
            }
        }

        #region IDisposable Members

        public void Dispose() {
            if(fDisposed) return;
            if(treeControl != null) {
                QTUtility2.log("ReleaseComObject treeControl");
                Marshal.ReleaseComObject(treeControl);
                treeControl = null;
            }
            fDisposed = true;
        }

        #endregion
    }
}
