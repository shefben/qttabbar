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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal class ExtendedSysListView32 : ExtendedListViewCommon {

        private static SolidBrush sbAlternate;
        private static SolidBrush sbTagBack;
        private struct TagVisualInfo {
            public bool HasTag;
            public Color? TextColor;
            public string Path;
            public int VisualHash;
        }
        private Dictionary<int, TagVisualInfo> tagInfoCache;
        private NativeWindowController EditController;
        private List<int> lstColumnFMT;
        private bool fListViewHasFocus;
        private readonly Dictionary<int, SolidBrush> compareBrushCache = new Dictionary<int, SolidBrush>();
        private int lastCompareItemIndex = -1;
        private bool lastCompareHasStyle;
        private DiffVisualStyle lastCompareStyle = new DiffVisualStyle(Color.Empty, Color.Empty);
        private int iListViewItemState;
        private int iHotItem = -1;


        internal ExtendedSysListView32(ShellBrowserEx ShellBrowser, IntPtr hwndShellView, IntPtr hwndListView, IntPtr hwndSubDirTipMessageReflect)
                : base(ShellBrowser, hwndShellView, hwndListView, hwndSubDirTipMessageReflect) {
            SetStyleFlags();
            try {
                TagManager.TagVisualChanged += TagManager_TagVisualChanged;
            }
            catch { }
        }

        private int CorrectHotItem(int iItem) {
            if(QTUtility.IsXP && iItem == -1 && ShellBrowser.ViewMode == FVM.DETAILS && ShellBrowser.GetItemCount() > 0) {
                RECT rect = GetItemRect(0, LVIR.LABEL);
                Point mousePosition = Control.MousePosition;
                PInvoke.ScreenToClient(Handle, ref mousePosition);
                int minX = Math.Min(rect.left, rect.right);
                int maxX = Math.Max(rect.left, rect.right);
                if(minX <= mousePosition.X && mousePosition.X <= maxX) {
                    iItem = HitTest(new Point(minX + 2, mousePosition.Y), false);
                }
            }
            return iItem;
        }

        private bool EditController_MessageCaptured(ref Message msg) {
            // QTUtility2.debugMessage(msg);
            if(msg.Msg == 0xb1 /* EM_SETSEL */ && msg.WParam.ToInt32() != -1) {
                msg.LParam = EditController.OptionalHandle;
                EditController.MessageCaptured -= EditController_MessageCaptured;
            }
            return false;
        }

        protected override bool OnShellViewNotify(NMHDR nmhdr, ref Message msg) {
            // Process WM.NOTIFY.  These are all notifications from the 
            // SysListView32 control.  We will not get ANY of these on 
            // Windows 7, which means every single one of them has to 
            // have an alternative somewhere for the ItemsView control,
            // or it's not going to happen.
            switch(nmhdr.code) {
                case -12: // NM_CUSTOMDRAW
                    // This is for drawing alternating row colors.  I doubt
                    // very much we'll find an alternative for this...
                    return HandleCustomDraw(ref msg);

                case LVN.ITEMCHANGED: {
                        // There are two things happening here.
                        // 1. Notify plugins of selection changing: Handled through 
                        //    undocumented WM_USER+163 message
                        // 2. Redraw for Full Row Select: Not happening
                    /*
                        // TODO
                     
                        IntPtr ptr;
                        if(InstanceManager.TryGetButtonBarHandle(this.hwndExplorer, out ptr)) {
                            QTUtility2.SendCOPYDATASTRUCT(ptr, (IntPtr)13, null, (IntPtr)GetItemCount());
                        }
                     */
                        QTUtility2.log("LVN.ITEMCHANGED");
                        bool flag = !QTUtility.IsXP && Config.Tweaks.ToggleFullRowSelect;
                        NMLISTVIEW nmlistview2 = (NMLISTVIEW)Marshal.PtrToStructure(msg.LParam, typeof(NMLISTVIEW));
                        if(nmlistview2.uChanged == 8 /*LVIF_STATE*/) {
                            uint newSelected = nmlistview2.uNewState & LVIS.SELECTED;
                            uint oldSelected = nmlistview2.uOldState & LVIS.SELECTED;
                            uint newDrophilited = nmlistview2.uNewState & LVIS.DROPHILITED;
                            uint oldDrophilited = nmlistview2.uOldState & LVIS.DROPHILITED;
                            uint newCut = nmlistview2.uNewState & LVIS.CUT;
                            uint oldCut = nmlistview2.uOldState & LVIS.CUT;
                            if(flag) {
                                if (nmlistview2.iItem != -1 && 
                                    ((newSelected != oldSelected) || 
                                     (newDrophilited != oldDrophilited) || 
                                     (newCut != oldCut)) &&
                                    ShellBrowser.ViewMode == FVM.DETAILS)
                                {
                                    QTUtility2.log("LVN.ITEMCHANGED nmlistview2.iItem " + nmlistview2.iItem);
                                    PInvoke.SendMessage(nmlistview2.hdr.hwndFrom, LVM.REDRAWITEMS, (IntPtr)nmlistview2.iItem, (IntPtr)nmlistview2.iItem);
                                }
                            }
                            if(newSelected != oldSelected) {
                                QTUtility2.log("newSelected != oldSelected  OnSelectionChanged " );
                                OnSelectionChanged(ref msg);
                            }
                        }
                        break;
                    }

                case LVN.INSERTITEM:
                case LVN.DELETEITEM:
                    // Handled through undocumented WM_USER+174 message
                    if(Config.Tips.ShowSubDirTips) {
                        HideSubDirTip(1);
                    }
                    if(Config.Tweaks.AlternateRowColors && (ShellBrowser.ViewMode == FVM.DETAILS)) {
                        PInvoke.InvalidateRect(nmhdr.hwndFrom, IntPtr.Zero, true);
                    }
                    InvalidateTagInfoCache();
                    ShellViewController.DefWndProc(ref msg);
                    OnItemCountChanged();
                    return true;

                case LVN.BEGINDRAG:
                    // This won't be necessary it seems.  On Windows 7, when you
                    // start to drag, a MOUSELEAVE message is sent, which hides
                    // the SubDirTip anyway.
                    ShellViewController.DefWndProc(ref msg);
                    HideSubDirTip(0xff);
                    return true;

                case LVN.ITEMACTIVATE: {
                    // Handled by catching Double Clicks and Enter keys.  Ugh...
                    NMITEMACTIVATE nmitemactivate = (NMITEMACTIVATE)Marshal.PtrToStructure(msg.LParam, typeof(NMITEMACTIVATE));
                    Keys modKeys =
                        (((nmitemactivate.uKeyFlags & 1) == 1) ? Keys.Alt : Keys.None) |
                        (((nmitemactivate.uKeyFlags & 2) == 2) ? Keys.Control : Keys.None) |
                        (((nmitemactivate.uKeyFlags & 4) == 4) ? Keys.Shift : Keys.None);
                    if(OnSelectionActivated(modKeys)) return true;
                    break;
                }

                case LVN.ODSTATECHANGED:
                    // FullRowSelect doesn't look possible anyway, so whatever.
                    if(!QTUtility.IsXP && Config.Tweaks.ToggleFullRowSelect) {
                        NMLVODSTATECHANGE nmlvodstatechange = (NMLVODSTATECHANGE)Marshal.PtrToStructure(msg.LParam, typeof(NMLVODSTATECHANGE));
                        if(((nmlvodstatechange.uNewState & 2) == 2) && (ShellBrowser.ViewMode == FVM.DETAILS)) {
                            PInvoke.SendMessage(nmlvodstatechange.hdr.hwndFrom, LVM.REDRAWITEMS, (IntPtr)nmlvodstatechange.iFrom, (IntPtr)nmlvodstatechange.iTo);
                        }
                    }
                    break;

                case LVN.HOTTRACK:
                    // Handled through WM_MOUSEMOVE.
                    if(Config.Tips.ShowTooltipPreviews || Config.Tips.ShowSubDirTips) {
                        NMLISTVIEW nmlistview = (NMLISTVIEW)Marshal.PtrToStructure(msg.LParam, typeof(NMLISTVIEW));
                        int iItem = CorrectHotItem(nmlistview.iItem);
                        if(iHotItem != iItem) {
                            OnHotItemChanged(iItem);
                            iHotItem = iItem;
                        }
                    }
                    break;

                case LVN.KEYDOWN: {
                    // Handled through WM_KEYDOWN.
                    NMLVKEYDOWN nmlvkeydown = (NMLVKEYDOWN)Marshal.PtrToStructure(msg.LParam, typeof(NMLVKEYDOWN));
                    if(OnKeyDown((Keys)nmlvkeydown.wVKey)) {
                        msg.Result = (IntPtr)1;
                        return true;
                    }
                    else {
                        return false;
                    }                        
                }
                    
                case LVN.GETINFOTIP: {
                    // Handled through WM_NOTIFY / TTN_NEEDTEXT
                    NMLVGETINFOTIP nmlvgetinfotip = (NMLVGETINFOTIP)Marshal.PtrToStructure(msg.LParam, typeof(NMLVGETINFOTIP));
                    return OnGetInfoTip(nmlvgetinfotip.iItem, GetHotItem() != nmlvgetinfotip.iItem); // TODO there's got to be a better way.
                }

                case LVN.BEGINLABELEDIT:
                    // This is just for file renaming, which there's no need to
                    // mess with in Windows 7.
                    ShellViewController.DefWndProc(ref msg);
                    if(QTUtility.IsXP && Config.Tweaks.KillExtWhileRenaming) {
                        NMLVDISPINFO nmlvdispinfo = (NMLVDISPINFO)Marshal.PtrToStructure(msg.LParam, typeof(NMLVDISPINFO));
                        if(nmlvdispinfo.item.lParam != IntPtr.Zero) {
                            using(IDLWrapper idl = ShellBrowser.ILAppend(nmlvdispinfo.item.lParam)) {
                                OnFileRename(idl);
                            }
                        }
                    }
                    break;

                case LVN.ENDLABELEDIT: {
                    // TODO
                    NMLVDISPINFO nmlvdispinfo2 = (NMLVDISPINFO)Marshal.PtrToStructure(msg.LParam, typeof(NMLVDISPINFO));
                    OnEndLabelEdit(nmlvdispinfo2.item);
                    InvalidateTagInfoCache();
                    break;
                }
            }
            return false;
        }

        private void SetStyleFlags()
        {
            if (ShellBrowser == null) return;  // qt desktop tool ���ÿ�ָ������ https://www.yuque.com/indiff/lc0r1g/kqgkr0
            if(ShellBrowser.ViewMode != FVM.DETAILS) return;
            uint flags = 0;
            if(Config.Tweaks.DetailsGridLines) {
                flags |= LVS_EX.GRIDLINES;
            }
            else {
                flags &= ~LVS_EX.GRIDLINES;
            }
            if(Config.Tweaks.ToggleFullRowSelect ^ !QTUtility.IsXP) {
                flags |= LVS_EX.FULLROWSELECT;
            }
            else {
                flags &= ~LVS_EX.FULLROWSELECT;
            }
            const uint mask = LVS_EX.GRIDLINES | LVS_EX.FULLROWSELECT;
            PInvoke.SendMessage(Handle, LVM.SETEXTENDEDLISTVIEWSTYLE, (IntPtr)mask, (IntPtr)flags);
        }

        protected override IntPtr GetEditControl() {
            return PInvoke.SendMessage(Handle, LVM.GETEDITCONTROL, IntPtr.Zero, IntPtr.Zero);
        }

        protected override Rectangle GetFocusedItemRect() {
            if(HasFocus()) {
                int code = ShellBrowser.ViewMode == FVM.DETAILS ? LVIR.LABEL : LVIR.BOUNDS;
                return GetItemRect(ShellBrowser.GetFocusedIndex(), code).ToRectangle();
            }
            return new Rectangle(0, 0, 0, 0);
        }

        public override int GetHotItem() {
            return CorrectHotItem(base.GetHotItem());
        }

        protected override Point GetSubDirTipPoint(bool fByKey) {
            int iItem = fByKey ? ShellBrowser.GetFocusedIndex() : GetHotItem();
            int x, y;
            Point ret;
            RECT rect;
            switch(ShellBrowser.ViewMode) {
                case FVM.DETAILS:
                    rect = GetItemRect(iItem, LVIR.LABEL);
                    x = rect.right;
                    y = rect.top;
                    y += (rect.bottom - y)/2;
                    ret = new Point(x - 19, y - 7);
                    break;

                case FVM.SMALLICON:
                    rect = GetItemRect(iItem);
                    x = rect.right;
                    y = rect.top;
                    x -= (rect.bottom - y) / 2;
                    y += (rect.bottom - y) / 2;
                    ret = new Point(x - 9, y - 7);
                    break;

                case FVM.CONTENT:
                case FVM.TILE:
                    rect = GetItemRect(iItem, LVIR.ICON);
                    y = rect.bottom;
                    x = rect.right;
                    ret = new Point(x - 16, y - 16);
                    break;

                case FVM.THUMBSTRIP:
                case FVM.THUMBNAIL:
                    rect = GetItemRect(iItem, LVIR.ICON);
                    if(QTUtility.IsXP) rect.right -= 13;
                    y = rect.bottom;
                    x = rect.right;
                    ret = new Point(x - 16, y - 16);
                    break;

                case FVM.ICON:
                    rect = GetItemRect(iItem, LVIR.ICON);
                    if(QTUtility.IsXP) {
                        int num3 = (int)PInvoke.SendMessage(Handle, LVM.GETITEMSPACING, IntPtr.Zero, IntPtr.Zero);
                        Size iconSize = SystemInformation.IconSize;
                        rect.right = ((rect.left + (((num3 & 0xffff) - iconSize.Width) / 2)) + iconSize.Width) + 8;
                        rect.bottom = (rect.top + iconSize.Height) + 6;
                    }
                    y = rect.bottom;
                    x = rect.right;
                    ret = new Point(x - 16, y - 16);
                    break;

                case FVM.LIST:
                    if(QTUtility.IsXP) {
                        rect = GetItemRect(iItem, LVIR.ICON);
                        using(SafePtr pszText = new SafePtr(520)) {
                            LVITEM structure = new LVITEM {
                                pszText = pszText,
                                cchTextMax = 260,
                                iItem = iItem,
                                mask = 1
                            };
                            PInvoke.SendMessage(Handle, LVM.GETITEM, IntPtr.Zero, ref structure);
                            int num4 = (int)PInvoke.SendMessage(Handle, LVM.GETSTRINGWIDTH, IntPtr.Zero, pszText);
                            num4 += 20;
                            rect.right += num4;
                            rect.top += 2;
                            rect.bottom += 2;                            
                        }
                    }
                    else {
                        rect = GetItemRect(iItem, LVIR.LABEL);
                    }                    
                    y = rect.bottom;
                    x = rect.right;
                    ret = new Point(x - 16, y - 16);
                    break;

                default:
                    rect = GetItemRect(iItem);
                    y = rect.bottom;
                    x = rect.right;
                    ret = new Point(x - 16, y - 16);
                    break;

            }
            PInvoke.ClientToScreen(Handle, ref ret);
            return ret;
        }
        // ʹ�ü�ͷ��ʱ����ѡ���ļ���
        protected override bool HandleCursorLoop(Keys key) {
            int focusedIdx = ShellBrowser.GetFocusedIndex();
            int itemCount = ShellBrowser.GetItemCount();
            int selectMe = -1;
            FVM viewMode = ShellBrowser.ViewMode;
            if(viewMode == FVM.TILE && QTUtility.IsXP) {
                viewMode = FVM.ICON;
            }
            switch(viewMode) {
                case FVM.CONTENT:
                case FVM.DETAILS:
                case FVM.TILE:
                    if(key == Keys.Up && focusedIdx == 0) {
                        selectMe = itemCount - 1;
                    }
                    else if(key == Keys.Down && focusedIdx == itemCount - 1) {
                        selectMe = 0;
                    }
                    break;

                case FVM.ICON:
                case FVM.SMALLICON:
                case FVM.THUMBNAIL:
                case FVM.LIST:
                    Keys KeyNextItem, KeyPrevItem, KeyNextPage, KeyPrevPage;
                    IntPtr MsgNextPage, MsgPrevPage;
                    if(viewMode == FVM.LIST) {
                        KeyNextItem = Keys.Down;
                        KeyPrevItem = Keys.Up;
                        KeyNextPage = Keys.Right;
                        KeyPrevPage = Keys.Left;
                        MsgNextPage = (IntPtr)LVNI.TORIGHT;
                        MsgPrevPage = (IntPtr)LVNI.TOLEFT;
                    }
                    else {
                        KeyNextItem = Keys.Right;
                        KeyPrevItem = Keys.Left;
                        KeyNextPage = Keys.Down;
                        KeyPrevPage = Keys.Up;
                        MsgNextPage = (IntPtr)LVNI.BELOW;
                        MsgPrevPage = (IntPtr)LVNI.ABOVE;
                    }

                    int nextPageIdx = (int)PInvoke.SendMessage(Handle, LVM.GETNEXTITEM, (IntPtr)focusedIdx, MsgNextPage);
                    if(nextPageIdx == -1 || nextPageIdx == focusedIdx) {
                        nextPageIdx = (int)PInvoke.SendMessage(Handle, LVM.GETNEXTITEM, (IntPtr)focusedIdx, MsgPrevPage);
                    }
                    else if(QTUtility.IsXP) {
                        int testIdx = (int)PInvoke.SendMessage(Handle, LVM.GETNEXTITEM, (IntPtr)nextPageIdx, MsgPrevPage);
                        if(testIdx != focusedIdx) {
                            nextPageIdx = (int)PInvoke.SendMessage(Handle, LVM.GETNEXTITEM, (IntPtr)focusedIdx, MsgPrevPage);
                        }
                    }
                    if(nextPageIdx == -1 || nextPageIdx == focusedIdx) {
                        if(key == KeyNextItem) {
                            if(focusedIdx == itemCount - 1) {
                                selectMe = 0;
                            }
                            else {
                                RECT thisRect = GetItemRect(focusedIdx);
                                RECT nextRect = GetItemRect(focusedIdx + 1);
                                if(viewMode == FVM.LIST) {
                                    if(nextRect.top < thisRect.top) selectMe = 0;
                                }
                                else if(nextRect.left < thisRect.left) {
                                    selectMe = 0;
                                }
                            }
                        }
                        else if(key == KeyPrevItem && focusedIdx == 0) {
                            selectMe = itemCount - 1;
                        }
                        else if(key == KeyNextPage || key == KeyPrevPage) {
                            if(QTUtility.IsXP) {
                                return true;
                            }
                        }
                    }
                    else {
                        int pageCount = Math.Abs(focusedIdx - nextPageIdx);
                        int page = focusedIdx % pageCount;
                        if(key == KeyNextItem && (page == pageCount - 1 || focusedIdx == itemCount - 1)) {
                            selectMe = focusedIdx - page;
                        }
                        else if(key == KeyPrevItem && page == 0) {
                            selectMe = Math.Min(focusedIdx + pageCount - 1, itemCount - 1);
                        }
                        else if(key == KeyNextPage && focusedIdx + pageCount >= itemCount) {
                            selectMe = page;
                        }
                        else if(key == KeyPrevPage && focusedIdx < pageCount) {
                            int x = itemCount - focusedIdx - 1;
                            selectMe = x - x % pageCount + focusedIdx;
                        }
                    }
                    break;

            }

            if(selectMe >= 0) {
                SetRedraw(false);
                ShellBrowser.SelectItem(selectMe);
                PInvoke.SendMessage(Handle, LVM.REDRAWITEMS, (IntPtr)focusedIdx, (IntPtr)focusedIdx);
                SetRedraw(true);
                return true;
            }
            else {
                return false;
            }
        }

        // �����Զ������
        private bool HandleCustomDraw(ref Message msg) {
            // TODO this needs to be cleaned
            if(ShellBrowser.ViewMode == FVM.DETAILS) {
                bool enableAlternating = Config.Tweaks.AlternateRowColors;
                NMLVCUSTOMDRAW structure = (NMLVCUSTOMDRAW)Marshal.PtrToStructure(msg.LParam, typeof(NMLVCUSTOMDRAW));
                int dwItemSpec = 0;
                if((ulong)structure.nmcd.dwItemSpec < Int32.MaxValue) {
                    dwItemSpec = (int)structure.nmcd.dwItemSpec;
                }
                switch(structure.nmcd.dwDrawStage) {
                    case CDDS.SUBITEM | CDDS.ITEMPREPAINT:
                        iListViewItemState = (int)PInvoke.SendMessage(
                                ListViewController.Handle, 
                                LVM.GETITEMSTATE, 
                                structure.nmcd.dwItemSpec,
                                (IntPtr)(LVIS.FOCUSED | LVIS.SELECTED | LVIS.DROPHILITED));

                        if(!QTUtility.IsXP) {
                            int num4 = (lstColumnFMT != null && structure.iSubItem < lstColumnFMT.Count) ? lstColumnFMT[structure.iSubItem] : 0;
                            int itemIndex = (int)structure.nmcd.dwItemSpec;
                            TagVisualInfo tagInfo = GetTagInfo(itemIndex);
                            bool isTagged = tagInfo.HasTag;
                            Color? tagColor = tagInfo.TextColor;
                            bool isSelectedState = (iListViewItemState & (LVIS.SELECTED | LVIS.DROPHILITED)) != 0;

                            Color baseBackground = SystemColors.Window;
                            Color baseForeground = SystemColors.WindowText;
                            try {
                                if(enableAlternating && Config.Tweaks != null) {
                                    baseBackground = Config.Tweaks.AltRowBackgroundColor;
                                    baseForeground = Config.Tweaks.AltRowForegroundColor;
                                }
                            } catch { }

                            structure.clrTextBk = QTUtility2.MakeCOLORREF(baseBackground);
                            structure.clrText = QTUtility2.MakeCOLORREF(baseForeground);
                            if(!isSelectedState) {
                                // Color the second file red (index 1) in the main column
                                if (structure.iSubItem == 0 && itemIndex == 1) {
                                    structure.clrText = QTUtility2.MakeCOLORREF(Color.Red);
                                }
                                else if(tagColor.HasValue) {
                                    structure.clrText = QTUtility2.MakeCOLORREF(tagColor.Value);
                                }
                                else if(TagManager.DimUntagged && !isTagged) {
                                    structure.clrText = QTUtility2.MakeCOLORREF(Color.Gray);
                                }
                                if(TagManager.HighlightTagged && isTagged) {
                                    structure.clrTextBk = QTUtility2.MakeCOLORREF(GetTagBackgroundColor(tagColor));
                                }
                            }
                            Marshal.StructureToPtr(structure, msg.LParam, false);
                            bool drawingHotItem = (dwItemSpec == GetHotItem());

                            bool fullRowSel = true;
                            try {
                                if(Config.Tweaks != null) {
                                    fullRowSel = !Config.Tweaks.ToggleFullRowSelect;
                                }
                            } catch { }

                            msg.Result = (IntPtr)(CDRF.NEWFONT);
                            if(structure.iSubItem == 0 && !drawingHotItem) {
                                if(iListViewItemState == 0 && (num4 & 0x600) != 0) {
                                    msg.Result = (IntPtr)(CDRF.NEWFONT | CDRF.NOTIFYPOSTPAINT);
                                }
                                else if(iListViewItemState == LVIS.FOCUSED && !fullRowSel) {
                                    msg.Result = (IntPtr)(CDRF.NEWFONT | CDRF.NOTIFYPOSTPAINT);
                                }
                            }

                            if(structure.iSubItem > 0 && (!fullRowSel || !drawingHotItem)) {
                                if(!fullRowSel || (iListViewItemState & (LVIS.SELECTED | LVIS.DROPHILITED)) == 0) {
                                    using(Graphics graphics = Graphics.FromHdc(structure.nmcd.hdc)) {
                                        Color alternateColor = enableAlternating ? Config.Tweaks.AltRowBackgroundColor : SystemColors.Window;
                                        if(sbAlternate == null || sbAlternate.Color != alternateColor) {
                                            if(sbAlternate != null) {
                                                sbAlternate.Dispose();
                                            }
                                            sbAlternate = new SolidBrush(alternateColor);
                                        }
                                        graphics.FillRectangle(sbAlternate, structure.nmcd.rc.ToRectangle());
                                    }
                                }
                            }
                        }
                        else {
                            msg.Result = (IntPtr)(CDRF.NOTIFYPOSTPAINT);
                        }
                        return true;

                    case CDDS.SUBITEM | CDDS.ITEMPOSTPAINT: {
                            RECT rc = structure.nmcd.rc;
                            if(QTUtility.IsXP) {
                                rc = PInvoke.ListView_GetItemRect(ListViewController.Handle, dwItemSpec, structure.iSubItem, 2);
                            }
                            else {
                                rc.left += 0x10;
                            }
                            Color alternateColor = enableAlternating ? Config.Tweaks.AltRowBackgroundColor : SystemColors.Window;
                            bool flag4 = false;
                            bool flag5 = Config.Tweaks.DetailsGridLines;
                            bool flag6 = Config.Tweaks.ToggleFullRowSelect ^ !QTUtility.IsXP;
                            bool flag7 = false;
                            if(QTUtility.IsXP && QTUtility.fSingleClick) {
                                flag7 = (dwItemSpec == GetHotItem());
                            }
                            LVITEM lvitem = new LVITEM();
                            IntPtr pszTextPtr = IntPtr.Zero;
                            IntPtr structPtr = IntPtr.Zero;
                            IntPtr zero = IntPtr.Zero;
                            IntPtr hgdiobj = IntPtr.Zero;
                            try {
                                pszTextPtr = Marshal.AllocHGlobal(520);
                                lvitem.pszText = pszTextPtr;
                                lvitem.cchTextMax = 260;
                                lvitem.iSubItem = structure.iSubItem;
                                lvitem.iItem = dwItemSpec;
                                lvitem.mask = 1;
                                structPtr = Marshal.AllocHGlobal(Marshal.SizeOf(lvitem));
                                Marshal.StructureToPtr(lvitem, structPtr, false);
                            PInvoke.SendMessage(ListViewController.Handle, LVM.GETITEM, IntPtr.Zero, structPtr);
                            if(sbAlternate == null || sbAlternate.Color != alternateColor) {
                                if(sbAlternate != null) {
                                    sbAlternate.Dispose();
                                }
                                sbAlternate = new SolidBrush(alternateColor);
                            }
                            using(Graphics graphics2 = Graphics.FromHdc(structure.nmcd.hdc)) {
                                Rectangle rect = rc.ToRectangle();
                                string folderPath = GetCurrentFolderPath();
                                if(flag5) {
                                    rect = new Rectangle(rc.left + 1, rc.top, rc.Width - 1, rc.Height - 1);
                                }
                                bool applyCompareStyle = false;
                                DiffVisualStyle compareStyle = new DiffVisualStyle(Color.Empty, Color.Empty);
                                if (structure.iSubItem == 0) {
                                    string itemName = Marshal.PtrToStringUni(lvitem.pszText);
                                    DiffVisualStyle style;
                                    if (CompareOverlayManager.TryGetDetailedStyle(folderPath, itemName, out style)) {
                                        lastCompareItemIndex = dwItemSpec;
                                        lastCompareHasStyle = true;
                                        lastCompareStyle = style;
                                    }
                                    else {
                                        lastCompareItemIndex = dwItemSpec;
                                        lastCompareHasStyle = false;
                                    }
                                }
                                if (lastCompareHasStyle && dwItemSpec == lastCompareItemIndex) {
                                    compareStyle = lastCompareStyle;
                                    applyCompareStyle = true;
                                }
                                else if (structure.iSubItem != 0 && dwItemSpec != lastCompareItemIndex) {
                                    lastCompareHasStyle = false;
                                }

                                bool isSelected = (iListViewItemState & LVIS.SELECTED) == LVIS.SELECTED;
                                if (applyCompareStyle && !isSelected) {
                                    graphics2.FillRectangle(GetCompareBrush(compareStyle.Background), rect);
                                }
                                else if (TagManager.HighlightTagged) {
                                    TagVisualInfo tagInfo = GetTagInfo(dwItemSpec);
                                    if (tagInfo.HasTag) {
                                        Color highlightColor = GetTagBackgroundColor(tagInfo.TextColor);
                                        if (sbTagBack == null || sbTagBack.Color != highlightColor) {
                                            if (sbTagBack != null) {
                                                sbTagBack.Dispose();
                                            }
                                            sbTagBack = new SolidBrush(highlightColor);
                                        }
                                        graphics2.FillRectangle(sbTagBack, rect);
                                    }
                                    else {
                                        graphics2.FillRectangle(sbAlternate, rect);
                                    }
                                }
                                else {
                                    Brush backgroundBrush = enableAlternating ? (Brush)sbAlternate : SystemBrushes.Window;
                                    graphics2.FillRectangle(backgroundBrush, rect);
                                }
                                if(QTUtility.IsXP && ((structure.iSubItem == 0) || flag6)) {
                                    flag4 = (iListViewItemState & 8) == 8;
                                    if((iListViewItemState != 0) && (((iListViewItemState == 1) && fListViewHasFocus) || (iListViewItemState != 1))) {
                                        int width;
                                        if(flag6) {
                                            width = rc.Width;
                                        }
                                        else {
                                            width = 8 + ((int)PInvoke.SendMessage(ListViewController.Handle, LVM.GETSTRINGWIDTH, IntPtr.Zero, lvitem.pszText));
                                            if(width > rc.Width) {
                                                width = rc.Width;
                                            }
                                        }
                                        Rectangle rectangle2 = new Rectangle(rc.left, rc.top, width, flag5 ? (rc.Height - 1) : rc.Height);
                                        if(((iListViewItemState & 2) == 2) || flag4) {
                                            if(flag4) {
                                                graphics2.FillRectangle(SystemBrushes.Highlight, rectangle2);
                                            }
                                            else if(QTUtility.fSingleClick && flag7) {
                                                graphics2.FillRectangle(fListViewHasFocus ? SystemBrushes.HotTrack : SystemBrushes.Control, rectangle2);
                                            }
                                            else {
                                                graphics2.FillRectangle(fListViewHasFocus ? SystemBrushes.Highlight : SystemBrushes.Control, rectangle2);
                                            }
                                        }
                                        if((fListViewHasFocus && ((iListViewItemState & 1) == 1)) && !flag6) {
                                            ControlPaint.DrawFocusRectangle(graphics2, rectangle2);
                                        }
                                    }
                                }
                                if(!QTUtility.IsXP && ((iListViewItemState & 1) == 1)) {
                                    int num6 = rc.Width;
                                    if(!flag6) {
                                        num6 = 4 + ((int)PInvoke.SendMessage(ListViewController.Handle, LVM.GETSTRINGWIDTH, IntPtr.Zero, lvitem.pszText));
                                        if(num6 > rc.Width) {
                                            num6 = rc.Width;
                                        }
                                    }
                                    Rectangle rectangle = new Rectangle(rc.left + 1, rc.top + 1, num6, flag5 ? (rc.Height - 2) : (rc.Height - 1));
                                    ControlPaint.DrawFocusRectangle(graphics2, rectangle);
                                }
                                // Compare overlay stripe on subitem 0
                                try {
                                    if (structure.iSubItem == 0 && CompareOverlayManager.IsActive) {
                                        string name = Marshal.PtrToStringUni(lvitem.pszText);
                                        DiffMark mark = CompareOverlayManager.GetMark(folderPath, name);
                                        if (mark != DiffMark.None) {
                                            Color c = Color.Transparent;
                                            if (mark == DiffMark.Added) c = Color.LightGreen; else if (mark == DiffMark.Removed) c = Color.IndianRed; else if (mark == DiffMark.Changed) c = Color.Orange;
                                            using (var b = new SolidBrush(c)) {
                                                Rectangle stripe = new Rectangle(rect.Left + 1, rect.Top + 1, 4, rect.Height - 2);
                                                graphics2.FillRectangle(b, stripe);
                                            }
                                        }
                                    }
                                } catch { }
                            }
                            if(QTUtility.IsXP && QTUtility.fSingleClick) {
                                LOGFONT logfont;
                                zero = PInvoke.GetCurrentObject(structure.nmcd.hdc, 6);
                                PInvoke.GetObject(zero, Marshal.SizeOf(typeof(LOGFONT)), out logfont);
                                if((structure.iSubItem == 0) || flag6) {
                                    logfont.lfUnderline = ((QTUtility.iIconUnderLineVal == 3) || flag7) ? ((byte)1) : ((byte)0);
                                }
                                else {
                                    logfont.lfUnderline = 0;
                                }
                                hgdiobj = PInvoke.CreateFontIndirect(ref logfont);
                                PInvoke.SelectObject(structure.nmcd.hdc, hgdiobj);
                            }
                            PInvoke.SetBkMode(structure.nmcd.hdc, 1);
                            int dwDTFormat = 0x8824;
                            if(QTUtility.IsRTL ? ((lstColumnFMT[structure.iSubItem] & 1) == 0) : ((lstColumnFMT[structure.iSubItem] & 1) == 1)) {
                                if(QTUtility.IsRTL) {
                                    dwDTFormat &= -3;
                                }
                                else {
                                    dwDTFormat |= 2;
                                }
                                rc.right -= 6;
                            }
                            else if(structure.iSubItem == 0) {
                                rc.left += 2;
                                rc.right -= 2;
                            }
                            else {
                                rc.left += 6;
                            }
                            Color textColor = Config.Tweaks.AltRowForegroundColor;
                            if ((iListViewItemState & LVIS.SELECTED) == LVIS.SELECTED) {
                                textColor = (fListViewHasFocus || flag4) ? SystemColors.HighlightText : SystemColors.WindowText;
                            }
                            else if (lastCompareHasStyle && dwItemSpec == lastCompareItemIndex) {
                                textColor = lastCompareStyle.Foreground;
                            }
                            else {
                                // Color the second file red (index 1) in the main column
                                if (structure.iSubItem == 0 && dwItemSpec == 1) {
                                    textColor = Color.Red;
                                }
                                else {
                                    // Apply tag colors for unselected items
                                    TagVisualInfo tagInfo = GetTagInfo(dwItemSpec);
                                    if (tagInfo.HasTag && tagInfo.TextColor.HasValue) {
                                        textColor = tagInfo.TextColor.Value;
                                    }
                                    else if (TagManager.DimUntagged && !tagInfo.HasTag) {
                                        textColor = Color.Gray;
                                    }
                                }
                            }
                            PInvoke.SetTextColor(structure.nmcd.hdc, QTUtility2.MakeCOLORREF(textColor));
                            PInvoke.DrawTextExW(structure.nmcd.hdc, lvitem.pszText, -1, ref rc, dwDTFormat, IntPtr.Zero);
                            }
                            catch(Exception ex) {
                                QTUtility2.MakeErrorLog(ex, "ExtendedSysListView32.HandleCustomDraw - Memory operation failed");
                            }
                            finally {
                                if(pszTextPtr != IntPtr.Zero) Marshal.FreeHGlobal(pszTextPtr);
                                if(structPtr != IntPtr.Zero) Marshal.FreeHGlobal(structPtr);
                            }
                            msg.Result = IntPtr.Zero;
                            if(zero != IntPtr.Zero) {
                                PInvoke.SelectObject(structure.nmcd.hdc, zero);
                            }
                            if(hgdiobj != IntPtr.Zero) {
                                PInvoke.DeleteObject(hgdiobj);
                            }
                            return true;
                        }
                    case CDDS.ITEMPREPAINT:
                        if((dwItemSpec % 2) == 0) {
                            msg.Result = (IntPtr)0x20;
                            return true;
                        }
                        msg.Result = IntPtr.Zero;
                        return false;

                    case CDDS.PREPAINT: {
                            HDITEM hditem = new HDITEM();
                            hditem.mask = 4;
                            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(hditem));
                            Marshal.StructureToPtr(hditem, ptr, false);
                            IntPtr hWnd = PInvoke.SendMessage(ListViewController.Handle, LVM.GETHEADER, IntPtr.Zero, IntPtr.Zero);
                            int num2 = (int)PInvoke.SendMessage(hWnd, 0x1200, IntPtr.Zero, IntPtr.Zero);
                            if(lstColumnFMT == null) {
                                lstColumnFMT = new List<int>();
                            }
                            else {
                                lstColumnFMT.Clear();
                            }
                            for(int i = 0; i < num2; i++) {
                                PInvoke.SendMessage(hWnd, 0x120b, (IntPtr)i, ptr);
                                hditem = (HDITEM)Marshal.PtrToStructure(ptr, typeof(HDITEM));
                                lstColumnFMT.Add(hditem.fmt);
                            }
                            Marshal.FreeHGlobal(ptr);
                            fListViewHasFocus = ListViewController.Handle == PInvoke.GetFocus();
                            msg.Result = (IntPtr)0x20;
                            return true;
                        }
                }
            }
            return false;
        }

        private void OnFileRename(IDLWrapper idl) {
            if(!idl.Available || idl.IsFileSystemFolder) return;
            string path = idl.Path;
            if(File.Exists(path)) {
                string extension = Path.GetExtension(path);
                if(!string.IsNullOrEmpty(extension) && extension.PathEquals(".lnk")) {
                    return;
                }
            }
            IntPtr hWnd = GetEditControl();
            if(hWnd == IntPtr.Zero) return;

            using(SafePtr lParam = new SafePtr(520)) {
                if((int)PInvoke.SendMessage(hWnd, WM.GETTEXT, (IntPtr)260, lParam) <= 0) return;
                string str3 = Marshal.PtrToStringUni(lParam);
                if(str3.Length > 2) {
                    int num = str3.LastIndexOf(".");
                    if(num > 0) {
                        // Explorer will send the EM_SETSEL message to select the
                        // entire filename.  We will intercept this message and
                        // change the params to select only the part before the
                        // extension.
                        EditController = new NativeWindowController(hWnd);
                        EditController.OptionalHandle = (IntPtr)num;
                        EditController.MessageCaptured += EditController_MessageCaptured;
                    }
                }
            }
        }

        public override void RefreshTagColors() {
            InvalidateTagInfoCache();
            base.RefreshTagColors();
            if(ListViewController != null && ListViewController.Handle != IntPtr.Zero) {
                // Force immediate invalidation and redraw
                PInvoke.InvalidateRect(ListViewController.Handle, IntPtr.Zero, true);
                PInvoke.UpdateWindow(ListViewController.Handle);

                // Also redraw items to ensure immediate visual update
                try {
                    int itemCount = ShellBrowser.GetItemCount();
                    if (itemCount > 0) {
                        PInvoke.SendMessage(ListViewController.Handle, LVM.REDRAWITEMS, IntPtr.Zero, (IntPtr)(itemCount - 1));
                    }
                }
                catch { }
            }
        }

        private TagVisualInfo GetTagInfo(int index) {
            if(index < 0) {
                return default(TagVisualInfo);
            }
            if(tagInfoCache == null) {
                tagInfoCache = new Dictionary<int, TagVisualInfo>();
            }
            TagVisualInfo info;
            if(tagInfoCache.TryGetValue(index, out info)) {
                return info;
            }
            info = default(TagVisualInfo);
            try {
                using(IDLWrapper wrapper = ShellBrowser.GetItem(index)) {
                    string path = wrapper != null ? wrapper.Path : null;
                    info.Path = path;
                    if(!string.IsNullOrEmpty(path)) {
                        TagVisualState state = TagManager.GetVisualState(path);
                        info.TextColor = state.ForegroundColor;
                        info.HasTag = state.HasTag;
                        info.VisualHash = state.VisualHash;
                    }
                }
            }
            catch {
                info = default(TagVisualInfo);
            }
            tagInfoCache[index] = info;
            return info;
        }

        private static Color GetTagBackgroundColor(Color? tagColor) {
            Color baseColor = tagColor ?? Color.LightGoldenrodYellow;
            return ControlPaint.LightLight(baseColor);
        }

        private void TagManager_TagVisualChanged(object sender, TagVisualChangedEventArgs e) {
            if(e == null) {
                e = TagVisualChangedEventArgs.Global;
            }
            bool requireRefresh = e.RequiresFullRefresh;
            if(tagInfoCache != null) {
                if(e.RequiresFullRefresh) {
                    if(tagInfoCache.Count > 0) {
                        tagInfoCache.Clear();
                    }
                }
                else {
                    List<int> toRemove = null;
                    foreach(KeyValuePair<int, TagVisualInfo> entry in tagInfoCache) {
                        string cachedPath = entry.Value.Path;
                        if(string.IsNullOrEmpty(cachedPath)) {
                            continue;
                        }
                        if(e.AffectsPath(cachedPath)) {
                            if(toRemove == null) {
                                toRemove = new List<int>();
                            }
                            toRemove.Add(entry.Key);
                        }
                    }
                    if(toRemove != null) {
                        foreach(int key in toRemove) {
                            tagInfoCache.Remove(key);
                        }
                        requireRefresh = true;
                    }
                }
            }
            if(requireRefresh && ListViewController != null && ListViewController.Handle != IntPtr.Zero) {
                PInvoke.InvalidateRect(ListViewController.Handle, IntPtr.Zero, true);
            }
        }

        private void InvalidateTagInfoCache() {
            if(tagInfoCache != null) {
                tagInfoCache.Clear();
            }
        }

        public override void Dispose(bool fDisposing) {
            if(fDisposing) {
                try {
                    TagManager.TagVisualChanged -= TagManager_TagVisualChanged;
                }
                catch { }
            }
            base.Dispose(fDisposing);
        }

        private SolidBrush GetCompareBrush(Color color) {
            int key = color.ToArgb();
            SolidBrush brush;
            if (!compareBrushCache.TryGetValue(key, out brush) || brush == null || brush.Color != color) {
                brush = new SolidBrush(color);
                compareBrushCache[key] = brush;
            }
            return brush;
        }

        private RECT GetItemRect(int iItem, int LVIRCode = LVIR.BOUNDS) {
            RECT rect = new RECT {left = LVIRCode};
            PInvoke.SendMessage(Handle, LVM.GETITEMRECT, (IntPtr)iItem, ref rect);
            return rect;
        }

        private string GetCurrentFolderPath() {
            try { using (IDLWrapper w = ShellBrowser.GetShellPath()) { return w.Available ? w.Path : string.Empty; } } catch { return string.Empty; }
        }
        private bool IsTagged(int index) {
            return GetTagInfo(index).HasTag;
        }

        public override int HitTest(Point pt, bool screenCoords) {
            if(screenCoords) {
                PInvoke.ScreenToClient(ListViewController.Handle, ref pt);
            }
            LVHITTESTINFO structure = new LVHITTESTINFO {pt = pt};
            int num = (int)PInvoke.SendMessage(ListViewController.Handle, LVM.HITTEST, IntPtr.Zero, ref structure);
            return num;
        }

        public override bool HotItemIsSelected() {
            // TODO: I don't think HOTITEM means what you think it does.
            int hot = (int)PInvoke.SendMessage(ListViewController.Handle, LVM.GETHOTITEM, IntPtr.Zero, IntPtr.Zero);
            if(hot == -1) return false;
            int state = (int)PInvoke.SendMessage(ListViewController.Handle, LVM.GETITEMSTATE, (IntPtr)hot, (IntPtr)LVIS.SELECTED);
            return ((state & LVIS.SELECTED) != 0);
        }

        public override bool IsTrackingItemName() {
            if(ShellBrowser.ViewMode == FVM.DETAILS) return true;
            if(ShellBrowser.GetItemCount() == 0) return false;
            RECT rect = PInvoke.ListView_GetItemRect(ListViewController.Handle, 0, 0, 2);
            Point mousePosition = Control.MousePosition;
            PInvoke.MapWindowPoints(IntPtr.Zero, ListViewController.Handle, ref mousePosition, 1);
            return (Math.Min(rect.left, rect.right) <= mousePosition.X) && (mousePosition.X <= Math.Max(rect.left, rect.right));
        }

        protected override bool ListViewController_MessageCaptured(ref Message msg) {
            if(base.ListViewController_MessageCaptured(ref msg)) {
                return true;
            }

            switch(msg.Msg) {
                // Style flags are reset when the view is changed.
                case LVM.SETVIEW:
                    SetStyleFlags();
                    break;

                // On Vista/7, we don't get a LVM.SETVIEW, but we do
                // get this.
                case WM.SETREDRAW:
                    if(msg.WParam != IntPtr.Zero) {
                        SetStyleFlags();
                    }
                    break;

            }
            return false;
        }

        public override bool PointIsBackground(Point pt, bool screenCoords) {
            if(screenCoords) {
                PInvoke.ScreenToClient(ListViewController.Handle, ref pt);
            }
            LVHITTESTINFO structure = new LVHITTESTINFO {pt = pt};
            if(QTUtility.IsXP) {
                return -1 == (int)PInvoke.SendMessage(ListViewController.Handle, LVM.HITTEST, IntPtr.Zero, ref structure);
            }
            else {
                PInvoke.SendMessage(ListViewController.Handle, LVM.HITTEST, (IntPtr)(-1), ref structure);
                return structure.flags == 1 /* LVHT_NOWHERE */;
            }
        }
    }
}

