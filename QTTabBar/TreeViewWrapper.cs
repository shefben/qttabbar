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
        private readonly Dictionary<IntPtr, TagVisualInfo> tagInfoCache = new Dictionary<IntPtr, TagVisualInfo>();

        private struct TagVisualInfo {
            public bool HasPath;
            public bool HasTag;
            public Color? TextColor;
        }

        public TreeViewWrapper(IntPtr hwnd, INameSpaceTreeControl treeControl) {
            QTUtility2.log("TreeViewWrapper init");
            this.treeControl = treeControl;
            treeController = new NativeWindowController(hwnd);
            treeController.MessageCaptured += TreeControl_MessageCaptured;
            parentController = new NativeWindowController(PInvoke.GetParent(hwnd));
            parentController.MessageCaptured += ParentControl_MessageCaptured;
        }

        private void InvalidateTagInfoCache() {
            tagInfoCache.Clear();
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

        private TagVisualInfo GetTagInfo(IntPtr itemHandle, RECT itemRect) {
            TagVisualInfo info;
            if(itemHandle == IntPtr.Zero) {
                return default(TagVisualInfo);
            }
            if(tagInfoCache.TryGetValue(itemHandle, out info)) {
                return info;
            }
            info = default(TagVisualInfo);
            if(treeControl == null) {
                tagInfoCache[itemHandle] = info;
                return info;
            }
            IShellItem item = null;
            try {
                int height = itemRect.bottom - itemRect.top;
                int centerY = itemRect.top + (height > 0 ? height / 2 : 0);
                Point pt = new Point(Math.Max(itemRect.left + 4, itemRect.left), centerY);
                if(treeControl.HitTest(pt, out item) == 0 && item != null) {
                    IntPtr pidl;
                    if(PInvoke.SHGetIDListFromObject(item, out pidl) == 0 && pidl != IntPtr.Zero) {
                        using(IDLWrapper wrapper = new IDLWrapper(pidl)) {
                            string path = wrapper.Path;
                            if(!string.IsNullOrEmpty(path)) {
                                info.HasPath = true;
                                info.TextColor = TagManager.GetTagColorForPath(path);
                                info.HasTag = info.TextColor.HasValue || TagManager.HasTags(path);
                            }
                        }
                    }
                }
            }
            catch {
                info = default(TagVisualInfo);
            }
            finally {
                if(item != null) {
                    QTUtility2.log("ReleaseComObject item");
                    Marshal.ReleaseComObject(item);
                }
            }
            tagInfoCache[itemHandle] = info;
            return info;
        }

        private static bool ApplyTagTextColor(ref NMTVCUSTOMDRAW draw, TagVisualInfo info) {
            if(info.TextColor.HasValue) {
                draw.clrText = QTUtility2.MakeCOLORREF(info.TextColor.Value);
                return true;
            }
            if(TagManager.DimUntagged && info.HasPath && !info.HasTag) {
                draw.clrText = QTUtility2.MakeCOLORREF(Color.Gray);
                return true;
            }
            return false;
        }

        private bool HandleCustomDraw(ref Message msg) {
            try {
                NMTVCUSTOMDRAW draw = (NMTVCUSTOMDRAW)Marshal.PtrToStructure(msg.LParam, typeof(NMTVCUSTOMDRAW));
                switch(draw.nmcd.dwDrawStage) {
                    case CDDS.PREPAINT:
                        InvalidateTagInfoCache();
                        msg.Result = (IntPtr)CDRF.NOTIFYITEMDRAW;
                        return true;

                    case CDDS.ITEMPREPAINT:
                        msg.Result = (IntPtr)CDRF.DODEFAULT;
                        bool isSelected = (draw.nmcd.uItemState & 0x0001) != 0; // CDIS_SELECTED
                        if(!isSelected) {
                            TagVisualInfo info = GetTagInfo((IntPtr)draw.nmcd.dwItemSpec, draw.nmcd.rc);
                            if(ApplyTagTextColor(ref draw, info)) {
                                Marshal.StructureToPtr(draw, msg.LParam, false);
                            }
                        }
                        return true;
                }
            }
            catch {
            }
            return false;
        }

        private bool TreeControl_MessageCaptured(ref Message msg) {
            switch(msg.Msg) {
                case WM.USER:
                    QTUtility2.log("TreeViewWrapper TreeControl_MessageCaptured WM.USER");
                    fPreventSelChange = false;
                    InvalidateTagInfoCache();
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
                        InvalidateTagInfoCache();
                    }
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

        #region IDisposable Members

        public void RefreshTagColors() {
            InvalidateTagInfoCache();
            if(treeController != null && treeController.Handle != IntPtr.Zero) {
                PInvoke.InvalidateRect(treeController.Handle, IntPtr.Zero, true);
            }
        }

        public void Dispose() {
            if(fDisposed) return;
            if(treeControl != null) {
                QTUtility2.log("ReleaseComObject treeControl");
                Marshal.ReleaseComObject(treeControl);
                treeControl = null;
            }
            InvalidateTagInfoCache();
            fDisposed = true;
        }

        #endregion
    }
}
