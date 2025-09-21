//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
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
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public sealed class QTabControl : Control 
    {
        private Bitmap bmpCloseBtn_Cold;
        private Bitmap bmpCloseBtn_ColdAlt;
        private Bitmap bmpCloseBtn_Hot;
        private Bitmap bmpCloseBtn_Pressed;
        private Bitmap bmpFolIconBG;
        private Bitmap bmpLocked;
        private SolidBrush brshActive;
        private SolidBrush brshInactv;
        private Color[] colorSet;
        private IContainer components;
        private QTabItem draggingTab;
        private bool fActiveTxtBold;
        private bool fAutoSubText;
        private bool fCloseBtnOnHover;
        private bool fDrawCloseButton;
        private bool fDrawFolderImg;
        private bool fDrawShadow;
        private bool fForceClassic;
        private bool fLimitSize;
        private bool fNeedToDrawUpDown;
        // Ƿť
        private bool fNeedPlusButton;
        private bool fNowMouseIsOnCloseBtn;
        private bool fNowMouseIsOnIcon;
        private bool fNowShowCloseBtnAlt;
        private bool fNowTabContextMenuStripShowing;
        private Font fnt_Underline;
        private Font fntBold;
        private Font fntBold_Underline;
        private Font fntDriveLetter;
        private Font fntSubText;
        private bool fOncePainted;
        internal const float FONTSIZE_DIFF = 0.75f;
        private bool fRedrawSuspended;
        private bool fShowSubDirTip;
        private bool fSubDirShown;
        private bool fSuppressDoubleClick;
        private bool fSuppressMouseUp;
        private QTabItem hotTab;
        private int iCurrentRow;
        private int iFocusedTabIndex = -1;
        private int iMultipleType;
        private int iPointedChanged_LastRaisedIndex = -2;
        private int iPseudoHotIndex = -1;
        private int iScrollClickedCount;
        private int iScrollWidth;
        private int iSelectedIndex;
        private int iTabIndexOfSubDirShown = -1;
        private int iTabMouseOnButtonsIndex = -1;
        private Size itemSize = new Size(100, 0x18);
        private int iToolTipIndex = -1;
        private int maxAllowedTabWidth = 10;
        private int minAllowedTabWidth = 10;
        private QTabItem selectedTabPage;
        private StringFormat sfTypoGraphic;
        private TabSizeMode sizeMode;
        private Padding sizingMargin;
        private Bitmap[] tabImages;
        private QTabCollection tabPages;
        private StringAlignment tabTextAlignment;
        private Timer timerSuppressDoubleClick;
        private ToolTip toolTip;
        private UpDown upDown;
        private const int UPDOWN_WIDTH = 0x24;
        private const int GROUP_INDICATOR_WIDTH = 12;
        private const int GROUP_INDICATOR_HEIGHT = 12;
        private const int GROUP_INDICATOR_SPACING = 4;
        private const int GROUP_RAIL_WIDTH = 6;
        private const int GROUP_ISLAND_PADDING = 6;
        private readonly Dictionary<string, TabGroupState> groupStates = new Dictionary<string, TabGroupState>(StringComparer.OrdinalIgnoreCase);
        private bool groupingDragActive;
        private Point groupingDragOrigin;
        private TabGroupState groupDropTarget;

        [ThreadStatic()]
        private static VisualStyleRenderer vsr_LHot;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_LNormal;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_LPressed;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_MHot;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_MNormal;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_MPressed;
        private static VisualStyleRenderer vsr_RHot;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_RNormal;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_RPressed;

        public event QTabCancelEventHandler CloseButtonClicked; // ر¼
        public event QTabCancelEventHandler Deselecting; 
        public event ItemDragEventHandler ItemDrag;
        public event QTabCancelEventHandler PointedTabChanged;
        public event QEventHandler RowCountChanged;
        public event EventHandler SelectedIndexChanged;
        public event QTabCancelEventHandler Selecting;
        public event QTabCancelEventHandler TabCountChanged;
        public event QTabCancelEventHandler TabIconMouseDown;
        // ɫť¼
        public event QTabCancelEventHandler PlusButtonClicked;

        public QTabControl() {
            fNeedPlusButton = Config.Tabs.NeedPlusButton;
            /*SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.SupportsTransparentBackColor | 
                     ControlStyles.ResizeRedraw | 
                     ControlStyles.UserPaint, true);*/
            
            // ControlStyles.UserPaint//ʹԶĻƷʽ
            // |ControlStyles.ResizeRedraw//ؼС仯ʱ»
            // |ControlStyles.SupportsTransparentBackColor//ؼ alpha С 255  BackColor ģ͸
            // | ControlStyles.AllPaintingInWmPaint//ؼԴϢ WM_ERASEBKGND Լ˸
            // | ControlStyles.OptimizedDoubleBuffer//ؼȻƵֱӻƵĻԼ˸
       
            // ʼ֮ǰлȡһΰģʽ
            QTUtility.InNightMode = QTUtility.getNightMode();

            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer 
                     | ControlStyles.ResizeRedraw//ؼС仯ʱ»
                     | ControlStyles.AllPaintingInWmPaint //ؼԴϢ WM_ERASEBKGND Լ˸
                     | ControlStyles.SupportsTransparentBackColor//ؼ alpha С 255  BackColor ģ͸
                     | ControlStyles.OptimizedDoubleBuffer //ؼȻƵֱӻƵĻԼ˸
            , value : true);

            /*this.SetStyle(ControlStyles.UserPaint |
                          ControlStyles.SupportsTransparentBackColor |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);*/
            
            components = new Container();
            tabPages = new QTabCollection(this);
            
            sfTypoGraphic = StringFormat.GenericTypographic;
            // MeasureTrailingSpaces ÿһнββո Ĭ£MeasureString صı߽ζųÿһнβĿո ô˱Աڲⶨʱոȥ
            // NoWrap ھøʽʱԶйܡ ݵǵǾʱָεгΪʱ˱ǡ
            sfTypoGraphic.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
            sfTypoGraphic.LineAlignment = StringAlignment.Far;  // StringAlignment.Center StringAlignment.Near StringAlignment.Far
            sfTypoGraphic.Trimming = StringTrimming.EllipsisCharacter;
            if (QTUtility.IsRTL)
            {
                this.sfTypoGraphic.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
            }

            /*if (QTUtility.InNightMode)
            {
                this.colorSet = new Color[]
                {
                    ShellColors.NightModeTextColor,
                    ShellColors.NightModeDisabledColor,
                    Config.Skin.TabTextHotColor,
                    ShellColors.NightModeTextShadow,
                     Config.Skin.TabShadInactiveColor,
                    ShellColors.NightModeColor
                };
            }
            else {
                colorSet = new Color[] 
                {
                    Config.Skin.TabTextActiveColor,
                    Config.Skin.TabTextInactiveColor,
                    Config.Skin.TabTextHotColor,
                    Config.Skin.TabShadActiveColor,
                    Config.Skin.TabShadInactiveColor,
                    Config.Skin.TabShadHotColor
                };
            }*/
            // brshActive = new SolidBrush(colorSet[0]);
            // brshInactv = new SolidBrush(colorSet[1]);
            // 䰵 by indiff dark mode
            /*brshActive = new SolidBrush(Config.Skin.TabTextActiveColor);  // ǩˢ
            brshInactv = new SolidBrush(Config.Skin.TabTextInactiveColor); // ǩǼˢ
            if (QTUtility.InNightMode)
            {
                BackColor = Config.Skin.TabShadActiveColor;
            }
            else
            {
                BackColor = Color.Transparent;
            }*/

            InitializeColors();
            this.BackColor = Color.Transparent;
            /*
            if (QTUtility.InNightMode)
            {
                // this.BackColor = SystemColors.ControlDarkDark;;
                this.BackColor = Color.Black;
            }
            else
            {
                this.BackColor = SystemColors.Window;
            }*/
            // ʱ֧˫¼
            timerSuppressDoubleClick = new Timer(components);
            timerSuppressDoubleClick.Interval = SystemInformation.DoubleClickTime + 100;
            timerSuppressDoubleClick.Tick += timerSuppressDoubleClick_Tick;
            if(VisualStyleRenderer.IsSupported) {
                InitializeRenderer();
            }
        }


        public  void InitializeColors()
        {
            Color activeColor = NormalizeTabTextColor(Config.Skin.TabTextActiveColor);
            Color inactiveColor = NormalizeTabTextColor(Config.Skin.TabTextInactiveColor);
            Color hotColor = NormalizeTabTextColor(Config.Skin.TabTextHotColor);

            if (QTUtility.InNightMode)
                this.colorSet = new Color[5]
                {
                    activeColor,
                    inactiveColor,
                    hotColor,
                    ShellColors.TextShadow,
                    ShellColors.Default,
                };
            else
                this.colorSet = new Color[5]
                {
                    activeColor,
                    inactiveColor,
                    hotColor,
                    Config.Skin.TabShadActiveColor,
                    Config.Skin.TabShadInactiveColor
                };
            if (brshActive == null)
            {
                brshActive = new SolidBrush(activeColor);
                brshInactv = new SolidBrush(inactiveColor);
            }
            else
            {
                brshActive.Color = activeColor;
                brshInactv.Color = inactiveColor;
            }
        }

        private static Color NormalizeTabTextColor(Color color)
        {
            Color normalized = color;
            if (normalized.IsEmpty)
            {
                return QTUtility.InNightMode ? ShellColors.NightModeTextColor : Color.Black;
            }

            if (normalized.A == 0 && (normalized.R != 0 || normalized.G != 0 || normalized.B != 0))
            {
                normalized = Color.FromArgb(255, normalized);
            }

            if (QTUtility.InNightMode)
            {
                if (normalized.A < 255)
                {
                    normalized = Color.FromArgb(255, normalized);
                }

                if (normalized.GetBrightness() < 0.65f)
                {
                    return ShellColors.NightModeTextColor;
                }
            }

            return normalized;
        }

        public static Color selectedColor(bool fSelected)
        {
            Color[] colorSet = new Color[5];
            if (QTUtility.InNightMode)
                colorSet = new Color[5]
                {
                    ShellColors.Text,
                    ShellColors.Disabled,
                    Config.Skin.TabTextActiveColor, // Config.TabHiliteColor,
                    ShellColors.TextShadow,
                    ShellColors.Default
                };
            else
                colorSet = new Color[5]
                {
                    Config.Skin.TabTextActiveColor,
                    Config.Skin.TabTextInactiveColor,
                    Config.Skin.TabTextActiveColor, // Config.TabHiliteColor,
                    Config.Skin.TabShadActiveColor,
                    Config.Skin.TabShadInactiveColor
                };
            if (fSelected)
            {
                return colorSet[0];
            }
            else
            {
                return colorSet[1];
            }
        }

        private bool CalculateItemRectangle() {
            int x = 0;
            int count = tabPages.Count;
            int height = itemSize.Height;
            List<QTabItem> visibleTabs = new List<QTabItem>();
            foreach(TabGroupState state in groupStates.Values) {
                state.AnchorBounds = Rectangle.Empty;
                state.IndicatorBounds = Rectangle.Empty;
            }
            if(sizeMode == TabSizeMode.Fixed) {
                for(int i = 0; i < count; i++) {
                    QTabItem tab = tabPages[i];
                    LayoutSingleRowTab(tab, ref x, height, itemSize.Width, visibleTabs);
                }
            }
            else {
                for(int i = 0; i < count; i++) {
                    QTabItem tab = tabPages[i];
                    int width = tab.TabBounds.Width;
                    if(fLimitSize) {
                        if(width > maxAllowedTabWidth) {
                            width = maxAllowedTabWidth;
                        }
                        if(width < minAllowedTabWidth) {
                            width = minAllowedTabWidth;
                        }
                    }
                    LayoutSingleRowTab(tab, ref x, height, width, visibleTabs);
                }
            }
            if(visibleTabs.Count > 0) {
                visibleTabs[0].Edge = Edges.Left;
                visibleTabs[visibleTabs.Count - 1].Edge = Edges.Right;
            }
            return (x > (Width - 0x24));
        }

        private void LayoutSingleRowTab(QTabItem tab, ref int x, int height, int width, List<QTabItem> visibleTabs) {
            bool isLeader;
            TabGroupState state;
            bool inGroup = TryGetGroupState(tab, out state, out isLeader);
            tab.Edge = 0;
            tab.Row = 0;
            tab.TabBounds = Rectangle.Empty;
            if(inGroup && isLeader) {
                ReserveGroupIndicator(state, ref x, 0, height);
            }
            if(inGroup && state.IsCollapsed) {
                return;
            }
            if(tab.CollapsedByGroup) {
                return;
            }
            if(width < 0) {
                width = 0;
            }
            tab.TabBounds = new Rectangle(x, 0, width, height);
            x += width;
            visibleTabs.Add(tab);
        }


        private void CalculateItemRectangle_MultiRows() {
            int x = 0;
            int count = tabPages.Count;
            int width = Width;
            int fixedWidth = itemSize.Width;
            int height = itemSize.Height;
            int rowStride = height - 3;
            int currentRow = 0;
            int selectedRow = 0;
            Dictionary<int, List<QTabItem>> rowTabs = new Dictionary<int, List<QTabItem>>();
            foreach(TabGroupState state in groupStates.Values) {
                state.AnchorBounds = Rectangle.Empty;
                state.IndicatorBounds = Rectangle.Empty;
            }
            for(int i = 0; i < count; i++) {
                QTabItem tab = tabPages[i];
                int tabWidth = (sizeMode == TabSizeMode.Fixed) ? fixedWidth : tab.TabBounds.Width;
                if(sizeMode != TabSizeMode.Fixed && fLimitSize) {
                    if(tabWidth > maxAllowedTabWidth) {
                        tabWidth = maxAllowedTabWidth;
                    }
                    if(tabWidth < minAllowedTabWidth) {
                        tabWidth = minAllowedTabWidth;
                    }
                }
                bool isLeader;
                TabGroupState state;
                bool inGroup = TryGetGroupState(tab, out state, out isLeader);
                bool collapsed = (inGroup && state.IsCollapsed) || tab.CollapsedByGroup;
                int indicatorWidth = (inGroup && isLeader) ? GROUP_INDICATOR_WIDTH + GROUP_INDICATOR_SPACING : 0;
                int requiredWidth = indicatorWidth + (collapsed ? 0 : tabWidth);
                if(requiredWidth > 0 && (x + requiredWidth) > width) {
                    currentRow++;
                    x = 0;
                }
                int y = rowStride * currentRow;
                if(inGroup && isLeader) {
                    ReserveGroupIndicator(state, ref x, y, height);
                }
                tab.Row = currentRow;
                tab.Edge = 0;
                tab.TabBounds = Rectangle.Empty;
                if(i == iSelectedIndex) {
                    selectedRow = currentRow;
                }
                if(collapsed) {
                    continue;
                }
                if(tabWidth < 0) {
                    tabWidth = 0;
                }
                tab.TabBounds = new Rectangle(x, y, tabWidth, height);
                x += tabWidth;
                List<QTabItem> rowList;
                if(!rowTabs.TryGetValue(currentRow, out rowList)) {
                    rowList = new List<QTabItem>();
                    rowTabs[currentRow] = rowList;
                }
                rowList.Add(tab);
            }
            int maxRowIndex = 0;
            bool hasRow = false;
            foreach(int row in rowTabs.Keys) {
                if(!hasRow || row > maxRowIndex) {
                    maxRowIndex = row;
                    hasRow = true;
                }
            }
            if(hasRow && iMultipleType == 1) {
                int shift = maxRowIndex - selectedRow;
                if(shift > 0) {
                    for(int i = 0; i < count; i++) {
                        QTabItem tab = tabPages[i];
                        Rectangle bounds = tab.TabBounds;
                        if(bounds.Width <= 0 && bounds.Height <= 0) {
                            if(tab.Row > selectedRow) {
                                tab.Row -= selectedRow + 1;
                            }
                            else {
                                tab.Row += shift;
                            }
                            continue;
                        }
                        if(tab.Row > selectedRow) {
                            tab.Row -= selectedRow + 1;
                            bounds.Y = tab.Row * rowStride;
                        }
                        else {
                            bounds.Y += shift * rowStride;
                            tab.Row += shift;
                        }
                        tab.TabBounds = bounds;
                    }
                    rowTabs.Clear();
                    for(int i = 0; i < count; i++) {
                        QTabItem tab = tabPages[i];
                        if(tab.TabBounds.Width <= 0 && tab.TabBounds.Height <= 0) {
                            continue;
                        }
                        List<QTabItem> rowList;
                        if(!rowTabs.TryGetValue(tab.Row, out rowList)) {
                            rowList = new List<QTabItem>();
                            rowTabs[tab.Row] = rowList;
                        }
                        rowList.Add(tab);
                    }
                    hasRow = false;
                    maxRowIndex = 0;
                    foreach(int row in rowTabs.Keys) {
                        if(!hasRow || row > maxRowIndex) {
                            maxRowIndex = row;
                            hasRow = true;
                        }
                    }
                }
            }
            foreach(List<QTabItem> row in rowTabs.Values) {
                row.Sort((a, b) => a.TabBounds.X.CompareTo(b.TabBounds.X));
                if(row.Count > 0) {
                    row[0].Edge = Edges.Left;
                    row[row.Count - 1].Edge = Edges.Right;
                }
            }
            int computedRow = hasRow ? maxRowIndex : 0;
            if(!hasRow && rowTabs.Count > 0) {
                foreach(int row in rowTabs.Keys) {
                    if(row > computedRow) {
                        computedRow = row;
                    }
                }
            }
            if(computedRow != iCurrentRow) {
                iCurrentRow = computedRow;
                if(RowCountChanged != null) {
                    RowCountChanged(this, new QEventArgs(iCurrentRow + 1));
                }
            }
        }


        /**
         * ǩл
         */
        private bool ChangeSelection(QTabItem tabToSelect, int index) {
            if(((Deselecting != null) && (this.iSelectedIndex > -1)) && (this.iSelectedIndex < tabPages.Count)) {
                QTabCancelEventArgs e = new QTabCancelEventArgs(tabPages[this.iSelectedIndex], this.iSelectedIndex, false, TabControlAction.Deselecting);
                Deselecting(this, e);
            }
            int curSelectedIndex = this.iSelectedIndex;
            QTabItem curSelectedTabPage = this.selectedTabPage;
            this.iSelectedIndex = index;
            this.selectedTabPage = tabToSelect;
            if(Selecting != null) {
                QTabCancelEventArgs args2 = new QTabCancelEventArgs(tabToSelect, index, false, TabControlAction.Selecting);
                Selecting(this, args2);
                if(args2.Cancel) {
                    this.iSelectedIndex = curSelectedIndex;
                    this.selectedTabPage = curSelectedTabPage;
                    return false;
                }
            }
            if(fNeedToDrawUpDown) {
                if((tabToSelect.TabBounds.X + iScrollWidth) < 0) {
                    iScrollWidth = -tabToSelect.TabBounds.X;
                    iScrollClickedCount = index;
                }
                else if((tabToSelect.TabBounds.X + iScrollWidth) > (Width - 0x24)) {
                    while((tabToSelect.TabBounds.Right + iScrollWidth) > Width) {
                        OnUpDownClicked(true, true);
                    }
                }
            }
            Refresh();
            if(SelectedIndexChanged != null) { // ѡıǩ仯 öӦ¼
                SelectedIndexChanged(this, new EventArgs());
            }
            iFocusedTabIndex = -1;
            return true;
        }

        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            if(brshActive != null) {
                brshActive.Dispose();
                brshActive = null;
            }
            if(brshInactv != null) {
                brshInactv.Dispose();
                brshInactv = null;
            }
            if(sfTypoGraphic != null) {
                sfTypoGraphic.Dispose();
                sfTypoGraphic = null;
            }
            if(bmpLocked != null) {
                bmpLocked.Dispose();
                bmpLocked = null;
            }
            if(bmpCloseBtn_Cold != null) {
                bmpCloseBtn_Cold.Dispose();
                bmpCloseBtn_Cold = null;
            }
            if(bmpCloseBtn_Hot != null) {
                bmpCloseBtn_Hot.Dispose();
                bmpCloseBtn_Hot = null;
            }
            if(bmpCloseBtn_Pressed != null) {
                bmpCloseBtn_Pressed.Dispose();
                bmpCloseBtn_Pressed = null;
            }
            if(bmpCloseBtn_ColdAlt != null) {
                bmpCloseBtn_ColdAlt.Dispose();
            }
            if(bmpFolIconBG != null) {
                bmpFolIconBG.Dispose();
                bmpFolIconBG = null;
            }
            if(fnt_Underline != null) {
                fnt_Underline.Dispose();
                fnt_Underline = null;
            }
            if(fntBold != null) {
                fntBold.Dispose();
                fntBold = null;
            }
            if(fntBold_Underline != null) {
                fntBold_Underline.Dispose();
                fntBold_Underline = null;
            }
            if(fntSubText != null) {
                fntSubText.Dispose();
                fntSubText = null;
            }
            if(fntDriveLetter != null) {
                fntDriveLetter.Dispose();
                fntDriveLetter = null;
            }
            foreach(QTabItem base2 in tabPages) {
                if(base2 != null) {
                    base2.OnClose();
                }
            }
            base.Dispose(disposing);
        }

        private void DrawBackground(Graphics g, bool bSelected, bool fHot, Rectangle rctItem, Edges edges, bool fVisualStyle, int index) {
            // add by indiff for dark mode
            Brush rectBrush = null;
            if (QTUtility.InNightMode)
            {
                // QTUtility2.log("QTabControl DrawBackground InNightMode ");
                rectBrush = new SolidBrush(Config.Skin.TabShadActiveColor);
                // Color light = Color.FromArgb(242, 242, 242);
                Color light = Color.FromArgb(122, 122, 122);
                // Color defaultColor = ShellColors.Default;
                // Color defaultColor2 = Color.FromArgb(240, 240, 240);
                // defaultColor = Color.Black;
                /*Graphic.FillRectangleRTL(g, 
                    QTUtility.InNightMode ?
                        (bSelected ? ShellColors.Light : ShellColors.Default) : 
                        (QTUtility.LaterThan10Beta17666 ? 
                            (bSelected ? ShellColors.Light : ShellColors.Default) :
                            Color.Black), 
                    rctItem, 
                    QTUtility.IsRTL);*/
                Graphic.FillRectangleRTL(g,
                    (bSelected ? light : Color.Black),
                    rctItem,
                    true);
            }
            else
            {
                QTUtility2.log("QTabControl DrawBackground NormanMode ");
                rectBrush = SystemBrushes.Control;
                g.FillRectangle(rectBrush, rctItem);
            }

            if(!fVisualStyle) {
               // g.FillRectangle(rectBrush, rctItem);
               /* 
                  g.FillRectangle(rectBrush, rctItem);
                  g.DrawRectangle(Pens.Black, new Rectangle(0, 0, rctItem.Width - 1, rctItem.Height - 1));
                  */
                int num = bSelected ? 0 : 1;
                if(tabImages == null) { // ͼƬΪ
                    // g.FillRectangle(rectBrush, rctItem);
                    g.DrawLine(SystemPens.ControlLightLight, 
                        new Point(rctItem.X + 2, rctItem.Y), 
                        new Point(((rctItem.X + rctItem.Width) - 2) - num, rctItem.Y));
                    g.DrawLine(SystemPens.ControlLightLight, 
                        new Point(rctItem.X + 2, rctItem.Y), 
                        new Point(rctItem.X, rctItem.Y + 2));
                    g.DrawLine(SystemPens.ControlLightLight, 
                        new Point(rctItem.X, rctItem.Y + 2), 
                        new Point(rctItem.X, (rctItem.Y + rctItem.Height) - 1));
                    g.DrawLine(SystemPens.ControlDarkDark, 
                        new Point((rctItem.X + rctItem.Width) - num, rctItem.Y + 2), 
                        new Point((rctItem.X + rctItem.Width) - num, (rctItem.Y + rctItem.Height) - 1));
                    g.DrawLine(SystemPens.ControlDark, 
                        new Point(((rctItem.X + rctItem.Width) - num) - 1, rctItem.Y + 1), 
                        new Point(((rctItem.X + rctItem.Width) - num) - 1, (rctItem.Y + rctItem.Height) - 1));
                    g.DrawLine(SystemPens.ControlDarkDark, 
                        new Point(((rctItem.X + rctItem.Width) - num) - 1, rctItem.Y + 1), 
                        new Point((rctItem.X + rctItem.Width) - num, rctItem.Y + 2));
                    if(bSelected) {
                        // QTUtility2.log("DrawBackground g.DrawLine bSelected");
                        Pen pen = new Pen(colorSet[2], 2f);
                        g.DrawLine(pen, 
                            new Point(rctItem.X, (rctItem.Y + rctItem.Height) - 1), 
                            new Point((rctItem.X + rctItem.Width) + 1,  (rctItem.Y + rctItem.Height) - 1));
                        pen.Dispose();
                    }
                }  else {  // ͼƬΪ
                    Bitmap bitmap;
                    if(bSelected) {
                        // QTUtility2.log("tabImages[0] ");
                        bitmap = tabImages[0];
                    }
                    else if(fHot || (iPseudoHotIndex == index)) {
                        // QTUtility2.log("tabImages[2] ");
                        bitmap = tabImages[2];
                    }
                    else {
                        // QTUtility2.log("tabImages[1] ");
                        bitmap = tabImages[1];
                    }
                    if(bitmap != null) { // ͼƬΪ
                                int left = sizingMargin.Left;
                                int top = sizingMargin.Top;
                                int right = sizingMargin.Right;
                                int bottom = sizingMargin.Bottom;
                                int vertical = sizingMargin.Vertical;
                                int horizontal = sizingMargin.Horizontal;
                                Rectangle[] rectangleArray = new Rectangle[]
                                {
                                    new Rectangle(rctItem.X, rctItem.Y, left, top), 
                                    new Rectangle(rctItem.X + left, rctItem.Y, rctItem.Width - horizontal, top), 
                                    new Rectangle(rctItem.Right - right, rctItem.Y, right, top), 
                                    new Rectangle(rctItem.X, rctItem.Y + top, left, rctItem.Height - vertical), 
                                    new Rectangle(rctItem.X + left, rctItem.Y + top, rctItem.Width - horizontal, rctItem.Height - vertical), 
                                    new Rectangle(rctItem.Right - right, rctItem.Y + top, right, rctItem.Height - vertical), 
                                    new Rectangle(rctItem.X, rctItem.Bottom - bottom, left, bottom), 
                                    new Rectangle(rctItem.X + left, rctItem.Bottom - bottom, rctItem.Width - horizontal, bottom), 
                                    new Rectangle(rctItem.Right - right, rctItem.Bottom - bottom, right, bottom)
                                };
                                Rectangle[] rectangleArray2 = new Rectangle[9];
                                // QTUtility2.log("ͼƬ 9 ");
                                int width = bitmap.Width;
                                int height = bitmap.Height;

                                // QTUtility2.log("ͼƬ  " + width + " ͼƬ߶  " + height);
                                rectangleArray2[0] = new Rectangle(0, 0, left, top);
                                rectangleArray2[1] = new Rectangle(left, 0, width - horizontal, top);
                                rectangleArray2[2] = new Rectangle(width - right, 0, right, top);
                                rectangleArray2[3] = new Rectangle(0, top, left, height - vertical);
                                rectangleArray2[4] = new Rectangle(left, top, width - horizontal, height - vertical);
                                rectangleArray2[5] = new Rectangle(width - right, top, right, height - vertical);
                                rectangleArray2[6] = new Rectangle(0, height - bottom, left, bottom);
                                rectangleArray2[7] = new Rectangle(left, height - bottom, width - horizontal, bottom);
                                rectangleArray2[8] = new Rectangle(width - right, height - bottom, right, bottom);
                                for (int i = 0; i < 9; i++)
                                {
                                    g.DrawImage(bitmap, rectangleArray[i], rectangleArray2[i], GraphicsUnit.Pixel);
                                }
                                // QTUtility2.log("drawbackground by image end");
                                // bitmap.Dispose(); // ﵼͼƬ
                    }
                }
            } // !fVisualStyle
            else {
                VisualStyleRenderer renderer;
                if(!bSelected) {
                    // ѡ renderer
                    if(!fHot && (iPseudoHotIndex != index)) {
                        Edges edges4 = edges;
                        if(edges4 == Edges.Left) {
                            renderer = vsr_LNormal;
                        }
                        else if(edges4 == Edges.Right) {
                            renderer = vsr_RNormal;
                        }
                        else {
                            renderer = vsr_MNormal;
                        }
                    }
                    else {
                        Edges edges3 = edges;
                        if(edges3 == Edges.Left) {
                            renderer = vsr_LHot;
                        }
                        else if(edges3 == Edges.Right) {
                            renderer = vsr_RHot;
                        }
                        else {
                            renderer = vsr_MHot;
                        }
                    }
                } //  !bSelected
                else {
                    Edges edges2 = edges;
                    if(edges2 == Edges.Left) {
                        renderer = vsr_LPressed;
                    }
                    else if(edges2 == Edges.Right) {
                        renderer = vsr_RPressed;
                    }
                    else {
                        renderer = vsr_MPressed;
                    }
                    // QTUtility2.log("DrawBackground renderer.DrawBackground1");
                    if (!QTUtility.InNightMode)
                    {
                        renderer.DrawBackground(g, rctItem);
                    }
                    return;
                }
                // QTUtility2.log("DrawBackground renderer.DrawBackground2");
                if (!QTUtility.InNightMode)
                {
                    renderer.DrawBackground(g, rctItem);
                }
            }
        }

        private static void DrawDriveLetter(Graphics g, string str, Font fnt, Rectangle rctFldImg, bool fSelected) {
            Rectangle layoutRectangle = new Rectangle(rctFldImg.X + 7, rctFldImg.Y + 6, 0x10, 0x10);
            using(SolidBrush brush = 
                        new SolidBrush( 
                                /*QTUtility2.MakeModColor(fSelected ? 
                                    Config.Skin.TabShadActiveColor : 
                                    Config.Skin.TabShadInactiveColor
                                    )*/
                                QTUtility2.MakeModColor(selectedColor(fSelected))
                        )
                   ) {
                Rectangle rectangle2 = layoutRectangle;
                rectangle2.Offset(1, 0);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(-2, 0);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(1, -1);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(0, 2);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(1, 0);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(0, -2);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(-2, 0);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(0, 2);
                g.DrawString(str, fnt, brush, rectangle2);
                // dark mode brshActive.Color
                // brush.Color = fSelected ? Config.Skin.TabTextActiveColor : Config.Skin.TabTextInactiveColor;
                brush.Color = selectedColor(fSelected);
                g.DrawString(str, fnt, brush, layoutRectangle);
            }
        }

        // 43 bug
        /*
         * 
            Message ---
            δõʵ
            HelpLink ---

            Source ---
            QTTabBar

            StackTrace ---
                QTTabBarLib.QTabControl.DrawTab(Graphics g, Rectangle itemRct, Int32 index, QTabItem tabHot, Boolean fVisualStyle)
                QTTabBarLib.QTabControl.OnPaint_MultipleRow(PaintEventArgs e)
            TargetSite ---
            Void DrawTab(System.Drawing.Graphics, System.Drawing.Rectangle, Int32, QTTabBarLib.QTabItem, Boolean)
         
             Message ---
            ΧΪǸֵСڼϴС
                       : index
            HelpLink ---

            Source ---
            mscorlib
            StackTrace ---
                        System.Collections.ArrayList.get_Item(Int32 index)
                        System.Windows.Forms.ImageList.ImageCollection.IndexOfKey(String key)
                        System.Windows.Forms.ImageList.ImageCollection.ContainsKey(String key)
                        QTTabBarLib.QTabControl.DrawTab(Graphics g, Rectangle itemRct, Int32 index, QTabItem tabHot, Boolean fVisualStyle)
*/
        // ָ߿ڻƵǰӾʽԪصıͼ
        private void DrawTab(Graphics g, Rectangle itemRct, int index, QTabItem tabHot, bool fVisualStyle) {
            try
            {
                Rectangle textRect; // ı
                Rectangle rctItem = textRect = itemRct; // ǩ
                // ΧΪǸֵСڼϴС
                QTabItem baseTabItem = tabPages[index]; // ǰıǩ
                bool bSelected = iSelectedIndex == index; // Ƿѡ
                bool fHot = baseTabItem == tabHot; // Ƿδȵǩ
                textRect.X += 2; // xƫ 2 
                if(bSelected) {
                    rctItem.Width += 4; // ѡȼӿ 4 
                }
                else {
                    rctItem.X += 2;  // ѡ ǩxƫ 2 
                    rctItem.Y += 2;  // ѡ ǩyƫ 2 
                    rctItem.Height -= 2;  // ѡ ǩ߶Ȼ 2 
                    // textRect.Y += 2; // ѡ ıyƫ 2 
                }
                DrawBackground(g, bSelected, fHot, rctItem, baseTabItem.Edge, fVisualStyle, index);
                int tabPosYHalfTabHeight = (rctItem.Height - 0x10) / 2; // ǩY 10 صһ
                // QTUtility2.log("draw folder image " + fDrawFolderImg +  " baseTabItem.ImageKey " + baseTabItem.ImageKey );
                // жǷʹͼƬ
                if(fDrawFolderImg && QTUtility.ImageListGlobal.Images.ContainsKey(baseTabItem.ImageKey)) {
                    // ͼƬ 0x10 -> 16
                    Rectangle imgRect = new Rectangle(
                        rctItem.X + (bSelected ? 7 : 5), 
                        rctItem.Y + tabPosYHalfTabHeight, 
                        0x10, 
                        0x10); // 16 ߶  * 16 
                    textRect.X += 0x18;
                    textRect.Width -= 0x18; // 24
                    if((fNowMouseIsOnIcon && (iTabMouseOnButtonsIndex == index)) || (iTabIndexOfSubDirShown == index)) {
                        if(fSubDirShown && (iTabIndexOfSubDirShown == index)) {
                            imgRect.X++;
                            imgRect.Y++;
                        }
                        if(bmpFolIconBG == null) {
                            bmpFolIconBG = Resources_Image.imgFolIconBG;
                        }
                        g.DrawImage(bmpFolIconBG, new Rectangle(imgRect.X - 2, imgRect.Y - 2, imgRect.Width + 4, imgRect.Height + 4));
                    }
					// ƱͼƬ
                    g.DrawImage(QTUtility.ImageListGlobal.Images[baseTabItem.ImageKey], imgRect);
					// жǷͼ
                    if(Config.Tabs.ShowDriveLetters) {
                        string pathInitial = baseTabItem.PathInitial;
                        if(pathInitial.Length > 0) {
                            DrawDriveLetter(g, pathInitial, fntDriveLetter, imgRect, bSelected);
                        }
                    }
                }
                else {
                    textRect.X += 4;
                    textRect.Width -= 4;
                }
                if(baseTabItem.TabLocked) { // ͼƬ
                    Rectangle lockRect = new Rectangle(
                        rctItem.X + (bSelected ? 6 : 4),  // ѡƫ 6 ءѡƫ 4 
                        rctItem.Y + tabPosYHalfTabHeight,  // YΪǩһ߶
                        9, 
                        11); // 9 * 11
                    if(fDrawFolderImg) { // ļͼƬ
                        lockRect.X += 9;   //  X ƫ 9 
                        lockRect.Y += 5;   //  Y ƫ 9 
                    }
                    else {
                        lockRect.Y += 2; //  X ƫ 2 
                        textRect.X += 10;//  Y ƫ 10 
                        textRect.Width -= 10;  // ȼ10
                    }
                    if(bmpLocked == null) {
                        bmpLocked = Resources_Image.imgLocked;
                    }
                    g.DrawImage(bmpLocked, lockRect);
                }
                bool isComment = baseTabItem.Comment.Length > 0;
                if((fDrawCloseButton && !fCloseBtnOnHover) && !fNowShowCloseBtnAlt) {
                    textRect.Width -= 15;
                }
                float textWidth = isComment ? 
                    ((baseTabItem.TitleTextSize.Width + baseTabItem.SubTitleTextSize.Width) + 4f) : 
                    (baseTabItem.TitleTextSize.Width + 2f);

                // ǩYƫΪ ı߶- ı߶  һ
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0.993 2022/10/1 16:57:52  Config.Skin.TabHeight 35
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0 2022/10/1 16:57:52  textRect.Height 35
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0 2022/10/1 16:57:52  baseTabItem.TitleTextSize.Height 20
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0 2022/10/1 16:57:52  textRect.X 26
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0 2022/10/1 16:57:52  textRect.Y 0
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0 2022/10/1 16:57:52  textPosX 53.5
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0.994 2022/10/1 16:57:52  textPosY 2.5
                // QTUtility2.log(" Config.Skin.TabHeight " + Config.Skin.TabHeight);
                // QTUtility2.log(" textRect.Height " + textRect.Height);
                // QTUtility2.log(" baseTabItem.TitleTextSize.Height " + baseTabItem.TitleTextSize.Height);
                // QTUtility2.log(" textRect.X " + textRect.X);
                // QTUtility2.log(" textRect.Y " + textRect.Y);
                // QTUtility2.log(" textPosX " + ((tabTextAlignment == StringAlignment.Center)
                //     ? Math.Max(((textRect.Width - textWidth) / 2f), 0f) :
                //     0f));
                // QTUtility2.log(" textPosY " + Math.Max(((textRect.Height - baseTabItem.TitleTextSize.Height) / 2f) - 5, 0f));
                // float textPosY = Math.Max(((textRect.Height - baseTabItem.TitleTextSize.Height) / 2f) - 5 , 0f);
                // float textPosY = 0;
                // Ϊʾ
                float textPosY = -(textRect.Height - baseTabItem.TitleTextSize.Height) / 2;
                // float textPosY = 5f;
                // ǩıƫֵ
                float textPosX = (tabTextAlignment == StringAlignment.Center)
                              ? Math.Max(((textRect.Width - textWidth) / 2f), 0f) :
                              0f; 
                RectangleF textRct = new RectangleF(
                                            textRect.X + textPosX, 
                                            textRect.Y + textPosY,
                                            Math.Min((baseTabItem.TitleTextSize.Width + 2f), (textRect.Width - textPosX)), 
                                            textRect.Height);
                // Ӱ dark mode
                Color textColor = baseTabItem.TagTextColor ?? (bSelected ? colorSet[0] : colorSet[1]);
                Color shadowColor = baseTabItem.TagTextColor.HasValue ? ControlPaint.Dark(textColor) : (bSelected ? colorSet[3] : colorSet[4]);
                Font textFont = (bSelected && fActiveTxtBold) ?
                            (baseTabItem.Underline ? fntBold_Underline : fntBold) :
                            (baseTabItem.Underline ? fnt_Underline : Font);
                SolidBrush overrideBrush = null;
                try {
                    if(fDrawShadow) {
                        DrawTextWithShadow(g,
                            baseTabItem.Text,
                            textColor,
                            shadowColor,
                            textFont,
                            textRct,
                            sfTypoGraphic);
                    }
                    else {
                        Brush mainBrush;
                        if(baseTabItem.TagTextColor.HasValue) {
                            overrideBrush = new SolidBrush(textColor);
                            mainBrush = overrideBrush;
                        }
                        else {
                            mainBrush = bSelected ? brshActive : brshInactv;
                        }
                        g.DrawString(baseTabItem.Text, textFont, mainBrush, textRct, sfTypoGraphic);
                    }
                    if(iFocusedTabIndex == index) {
                        Rectangle rectangle = rctItem;
                        rectangle.Inflate(-2, -1);
                        rectangle.Y++;
                        rectangle.Width--;
                        ControlPaint.DrawFocusRectangle(g, rectangle);
                    }
                    if(isComment && (textRect.Width > baseTabItem.TitleTextSize.Width)) {
                        float posY = Math.Max(((textRect.Height - baseTabItem.SubTitleTextSize.Height) / 2f), 0f);
                        posY = textRect.Y  - posY;
                        RectangleF drawStrRectF = new RectangleF(
                            textRct.Right,
                            posY,
                            Math.Min(
                                (baseTabItem.SubTitleTextSize.Width + 2f),
                                (textRect.Width - ((baseTabItem.TitleTextSize.Width + textPosX) + 4f))
                            ),
                            textRect.Height);
                        if(fDrawShadow) {
                            DrawTextWithShadow(g,
                                (fAutoSubText ? "@" : ":") + baseTabItem.Comment,
                                baseTabItem.TagTextColor ?? (bSelected ? colorSet[0] : colorSet[1]),
                                baseTabItem.TagTextColor.HasValue ? ControlPaint.Dark(textColor) : (bSelected ? colorSet[3] : colorSet[4]),
                                fntSubText,
                                drawStrRectF,
                                sfTypoGraphic);
                        }
                        else {
                            Brush commentBrush = overrideBrush ?? brshInactv;
                            g.DrawString((fAutoSubText ? "@" : ":") + baseTabItem.Comment,
                                fntSubText,
                                commentBrush,
                                drawStrRectF,
                                sfTypoGraphic);
                        }
                    }
                }
                finally {
                    if(overrideBrush != null) {
                        overrideBrush.Dispose();
                    }
                }
                if(fDrawCloseButton && (!fCloseBtnOnHover || fHot)) {
                    Rectangle closeButtonRectangle = GetCloseButtonRectangle(baseTabItem.TabBounds, bSelected);
                    if(fNowMouseIsOnCloseBtn && (iTabMouseOnButtonsIndex == index)) {
                        if(MouseButtons == MouseButtons.Left) {
                            if(bmpCloseBtn_Pressed == null) {
                                bmpCloseBtn_Pressed = Resources_Image.imgCloseButton_Press;
                            }
                            g.DrawImage(bmpCloseBtn_Pressed, closeButtonRectangle);
                        }
                        else {
                            if(bmpCloseBtn_Hot == null) {
                                bmpCloseBtn_Hot = Resources_Image.imgCloseButton_Hot;
                            }
                            g.DrawImage(bmpCloseBtn_Hot, closeButtonRectangle);
                        }
                    }
                    else if(fNowShowCloseBtnAlt || fCloseBtnOnHover) {
                        if(bmpCloseBtn_ColdAlt == null) {
                            bmpCloseBtn_ColdAlt = Resources_Image.imgCloseButton_ColdAlt;
                        }
                        g.DrawImage(bmpCloseBtn_ColdAlt, closeButtonRectangle);
                    }
                    else {
                        if(bmpCloseBtn_Cold == null) {
                            bmpCloseBtn_Cold = Resources_Image.imgCloseButton_Cold;
                        }
                        g.DrawImage(bmpCloseBtn_Cold, closeButtonRectangle);
                    }
                }
            }
            catch (Exception e)
            {
                QTUtility2.MakeErrorLog(e, "DrawTab");
            }
        }

        private static void DrawTextWithShadow(Graphics g, string txt, Color clrTxt, Color clrShdw, Font fnt, RectangleF rct, StringFormat sf) {
            RectangleF layoutRectangle = rct;
            RectangleF ef2 = rct;
            RectangleF ef3 = rct;
            layoutRectangle.Offset(1f, 1f);
            ef2.Offset(2f, 0f);
            ef3.Offset(1f, 2f);
            Color color = Color.FromArgb(0xc0, clrShdw);
            Color color2 = Color.FromArgb(0x80, clrShdw);
            using(SolidBrush brush = new SolidBrush(Color.FromArgb(0x40, clrShdw))) {
                g.DrawString(txt, fnt, brush, ef3, sf);
                brush.Color = color2;
                g.DrawString(txt, fnt, brush, ef2, sf);
                brush.Color = color;
                g.DrawString(txt, fnt, brush, layoutRectangle, sf);
                brush.Color = clrTxt;
                g.DrawString(txt, fnt, brush, rct, sf);
            }
        }

        public bool FocusNextTab(bool fBack, bool fEntered, bool fEnd) {
            if(tabPages.Count <= 0) {
                return false;
            }
            if(fEntered) {
                iFocusedTabIndex = fBack ? (tabPages.Count - 1) : 0;
                SetPseudoHotIndex(iFocusedTabIndex);
                return true;
            }
            if((fBack && (iFocusedTabIndex == 0)) || (!fBack && (iFocusedTabIndex == (tabPages.Count - 1)))) {
                iFocusedTabIndex = -1;
                return false;
            }
            if(fEnd) {
                iFocusedTabIndex = fBack ? 0 : (tabPages.Count - 1);
            }
            else {
                iFocusedTabIndex += fBack ? -1 : 1;
                if(iFocusedTabIndex < 0) {
                    iFocusedTabIndex = tabPages.Count - 1;
                }
            }
            SetPseudoHotIndex(iFocusedTabIndex);
            return true;
        }

        private Rectangle GetCloseButtonRectangle(Rectangle rctTab, bool fSelected) {
            int num = ((itemSize.Height - 15) / 2) + 1;
            if(!fSelected) {
                num += 2;
            }
            if((iMultipleType == 0) && fNeedToDrawUpDown) {
                rctTab.X += iScrollWidth;
            }
            return new Rectangle(rctTab.Right - 0x11, rctTab.Top + num, 15, 15);
        }

        public int GetFocusedTabIndex() {
            return iFocusedTabIndex;
        }

        private Rectangle GetFolderIconRectangle(Rectangle rctTab, bool fSelected) {
            int num = (rctTab.Height - 0x10) / 2;
            if(!fSelected) {
                num += 2;
            }
            if((iMultipleType == 0) && fNeedToDrawUpDown) {
                rctTab.X += iScrollWidth;
            }
            return new Rectangle(rctTab.X + (fSelected ? 5 : 3), (rctTab.Y + num) - 2, 20, 20);
        }

        private Rectangle GetItemRectangle(int index) {
            if((index < 0) || (index >= tabPages.Count)) {
                return Rectangle.Empty;
            }
            QTabItem tab = tabPages[index];
            if(tab.CollapsedByGroup) {
                return Rectangle.Empty;
            }
            Rectangle tabBounds = tab.TabBounds;
            if(fNeedToDrawUpDown) {
                tabBounds.X += iScrollWidth;
            }
            return tabBounds;
        }

        private Rectangle GetItemRectWithInflation(int index) {
            Rectangle tabBounds = GetItemRectangle(index);
            if(tabBounds.IsEmpty) {
                return tabBounds;
            }
            if(index == iSelectedIndex) {
                tabBounds.Inflate(4, 0);
            }
            return tabBounds;
        }

        /**
         * ȡıǩ
         * bug ֻһǩʱ򣬵ǩհ״ʶΪǩ
         */
        public QTabItem GetTabMouseOn() {
            if (this == null || this.IsDisposed)
            {
                 if (tabPages.Count == 1)
                 {
                     Point pp = PointToClient(MousePosition);
                     if (((upDown != null) && upDown.Visible) && upDown.Bounds.Contains(pp))
                     {
                         return null;
                     }
                     QTUtility2.log(" return tabPage[0] 1");
                     return tabPages[0];
                 }
                 return null;
            }
            Point pt = PointToClient(MousePosition);
            if (((upDown != null) && upDown.Visible) && upDown.Bounds.Contains(pt))
            {
                return null;
            }

            // ǩֻһĻ
            if (tabPages.Count == 1) {
                 if (tabPages[0].TabBounds.Contains(pt))
                 {
                     QTUtility2.log("contains pt return tabPage[0] 2");
                     return tabPages[0];
                 }
                 return null;
            }

            QTabItem base2 = null;
            QTabItem base3 = null;
            for(int i = 0; i < tabPages.Count; i++) {
                Rectangle rect = GetItemRectWithInflation(i);
                if(rect.IsEmpty) {
                    continue;
                }
                if(rect.Contains(pt)) {
                    if(base2 == null) {
                        base2 = tabPages[i];
                        if(iMultipleType == 0) {
                            return base2;
                        }
                    }
                    else {
                        base3 = tabPages[i];
                        break;
                    }
                }
            }
            if((base3 != null) && (base2.Row <= base3.Row)) {
                return base3;
            }
            return base2;
        }

        public QTabItem GetTabMouseOn(out int index) {
            Point pt = PointToClient(MousePosition);
            QTabItem base2 = null;
            QTabItem base3 = null;
            int num = -1;
            int num2 = -1;
            for(int i = 0; i < tabPages.Count; i++) {
                Rectangle rect = GetItemRectWithInflation(i);
                if(rect.IsEmpty) {
                    continue;
                }
                if(rect.Contains(pt)) {
                    if(base2 == null) {
                        base2 = tabPages[i];
                        num = i;
                        if(iMultipleType == 0) {
                            index = i;
                            return base2;
                        }
                    }
                    else {
                        base3 = tabPages[i];
                        num2 = i;
                        break;
                    }
                }
            }
            if(base3 != null) {
                if(base2.Row > base3.Row) {
                    index = num;
                    return base2;
                }
                index = num2;
                return base3;
            }
            index = num;
            return base2;
        }

        public Rectangle GetTabRect(QTabItem tab) {
            Rectangle tabBounds = tab.TabBounds;
            if(fNeedToDrawUpDown) {
                tabBounds.X += iScrollWidth;
            }
            return tabBounds;
        }

        public Rectangle GetTabRect(int index, bool fInflation) {
            if((index <= -1) || (index >= tabPages.Count)) {
                throw new ArgumentOutOfRangeException("index," + index, "index is out of range.");
            }
            if(fInflation) {
                return GetItemRectWithInflation(index);
            }
            return GetItemRectangle(index);
        }

        private bool HitTestOnButtons(Rectangle rctTab, Point pntClient, bool fCloseButton, bool fSelected) {
            if(fCloseButton) {
                return GetCloseButtonRectangle(rctTab, fSelected).Contains(pntClient);
            }
            return GetFolderIconRectangle(rctTab, fSelected).Contains(pntClient);
        }

        private void InitializeRenderer() {
            vsr_LPressed = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemLeftEdge.Pressed);
            vsr_RPressed = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemRightEdge.Pressed);
            vsr_MPressed = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItem.Pressed);
            vsr_LNormal = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemLeftEdge.Normal);
            vsr_RNormal = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemRightEdge.Normal);
            vsr_MNormal = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItem.Normal);
            vsr_LHot = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItem.Hot);
            vsr_RHot = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemRightEdge.Hot);
            vsr_MHot = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItem.Hot);
        }

        private void InvalidateTabsOnMouseMove(QTabItem tabPage, int index, Point pnt) {
            iTabMouseOnButtonsIndex = index;
            if(tabPage != hotTab) {
                hotTab = tabPage;
                if((tabPage != null) && !tabPage.TabLocked) {
                    bool fSelected = index == iSelectedIndex;
                    if(fDrawCloseButton) {
                        fNowMouseIsOnCloseBtn = HitTestOnButtons(tabPage.TabBounds, pnt, true, fSelected);
                    }
                    if(fDrawFolderImg && fShowSubDirTip) {
                        fNowMouseIsOnIcon = HitTestOnButtons(tabPage.TabBounds, pnt, false, fSelected);
                    }
                }
                else {
                    fNowMouseIsOnCloseBtn = false;
                    fNowMouseIsOnIcon = false;
                }
                PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
            }
            else if(tabPage != null) {
                bool flag2 = index == iSelectedIndex;
                bool flag3 = false;
                if(fDrawCloseButton) {
                    bool flag4 = HitTestOnButtons(tabPage.TabBounds, pnt, true, flag2);
                    if(fNowMouseIsOnCloseBtn ^ flag4) {
                        fNowMouseIsOnCloseBtn = flag4 && !tabPage.TabLocked;
                        flag3 = true;
                    }
                }
                if(fDrawFolderImg && fShowSubDirTip) {
                    bool flag5 = HitTestOnButtons(tabPage.TabBounds, pnt, false, flag2);
                    if(fNowMouseIsOnIcon ^ flag5) {
                        fNowMouseIsOnIcon = flag5;
                        flag3 = true;
                    }
                }
                if(flag3) {
                    PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
                }
            }
        }

        protected override void OnLostFocus(EventArgs e) {
            iFocusedTabIndex = -1;
            if(iPseudoHotIndex != -1) {
                SetPseudoHotIndex(-1);
            }
            base.OnLostFocus(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e) {
            if(!fSuppressDoubleClick) {
                int num;
                QTabItem tabMouseOn = GetTabMouseOn(out num);
                if(((!fDrawCloseButton || (tabMouseOn == null)) || !HitTestOnButtons(tabMouseOn.TabBounds, e.Location, true, num == iSelectedIndex)) && ((!fDrawFolderImg || !fShowSubDirTip) || ((tabMouseOn == null) || !HitTestOnButtons(tabMouseOn.TabBounds, e.Location, false, num == iSelectedIndex)))) {
                    base.OnMouseDoubleClick(e);
                    fSuppressMouseUp = true;
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            if(e.Button == MouseButtons.Left && TryHandleGroupIndicatorClick(e.Location)) {
                return;
            }
            int num;
            QTabItem tabMouseOn = GetTabMouseOn(out num);
            if(tabMouseOn != null) {
                bool cancel = e.Button == MouseButtons.Right;
                if((!cancel && fDrawCloseButton) && HitTestOnButtons(tabMouseOn.TabBounds, e.Location, true, num == iSelectedIndex)) {
                    PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
                    return;
                }
                if((fNowMouseIsOnIcon && HitTestOnButtons(tabMouseOn.TabBounds, e.Location, false, num == iSelectedIndex)) && (TabIconMouseDown != null)) {
                    if((e.Button == MouseButtons.Left) || cancel) {
                        iTabIndexOfSubDirShown = num;
                        int tabPageIndex = 0;
                        if((iMultipleType == 0) && fNeedToDrawUpDown) {
                            tabPageIndex = iScrollWidth;
                        }
                        TabIconMouseDown(this, new QTabCancelEventArgs(tabMouseOn, tabPageIndex, cancel, TabControlAction.Selecting));
                        PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
                    }
                    return;
                }
                if(e.Button == MouseButtons.Left) {
                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Left, ModifierKeys);
                    if(!Config.Mouse.TabActions.ContainsKey(chord) && SelectTab(tabMouseOn)) {
                        fSuppressDoubleClick = true;
                        timerSuppressDoubleClick.Enabled = true;
                    }
                }
            }
            draggingTab = tabMouseOn;
            if(e.Button == MouseButtons.Left) {
                groupingDragOrigin = e.Location;
                groupingDragActive = false;
                UpdateGroupDropTarget(null);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseLeave(EventArgs e) {
            iToolTipIndex = -1;
            if(toolTip != null) {
                toolTip.Active = false;
            }
            iPointedChanged_LastRaisedIndex = -2;
            if((PointedTabChanged != null) && (hotTab != null)) {
                PointedTabChanged(null, new QTabCancelEventArgs(null, -1, false, TabControlAction.Deselecting));
            }
            hotTab = null;
            fNowMouseIsOnCloseBtn = fNowMouseIsOnIcon = false;
            if(groupingDragActive) {
                UpdateGroupDropTarget(null);
                groupingDragActive = false;
            }
            PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            int num;
            if((e.Button & MouseButtons.Left) == MouseButtons.Left && draggingTab != null) {
                if(!groupingDragActive) {
                    Rectangle dragRect = new Rectangle(
                        groupingDragOrigin.X - SystemInformation.DragSize.Width / 2,
                        groupingDragOrigin.Y - SystemInformation.DragSize.Height / 2,
                        SystemInformation.DragSize.Width,
                        SystemInformation.DragSize.Height);
                    if(!dragRect.Contains(e.Location)) {
                        groupingDragActive = true;
                    }
                }
                if(groupingDragActive) {
                    UpdateGroupDropTarget(HitTestGroupSurface(e.Location));
                }
            }
            else if(groupingDragActive) {
                UpdateGroupDropTarget(null);
            }
            if(((e.Button == MouseButtons.Right) && !Parent.RectangleToScreen(Bounds).Contains(MousePosition)) && ((ItemDrag != null) && (draggingTab != null))) {
                ItemDrag(this, new ItemDragEventArgs(e.Button, draggingTab));
            }
            QTabItem tabMouseOn = GetTabMouseOn(out num);
            InvalidateTabsOnMouseMove(tabMouseOn, num, e.Location);
            if((PointedTabChanged != null) && (num != iPointedChanged_LastRaisedIndex)) {
                if(tabMouseOn != null) {
                    iPointedChanged_LastRaisedIndex = num;
                    PointedTabChanged(this, new QTabCancelEventArgs(tabMouseOn, num, false, TabControlAction.Selecting));
                }
                else if(iPointedChanged_LastRaisedIndex != -2) {
                    iPointedChanged_LastRaisedIndex = -1;
                    PointedTabChanged(this, new QTabCancelEventArgs(null, -1, false, TabControlAction.Deselecting));
                }
            }
            if(tabMouseOn != null) {
                if(((iToolTipIndex != num) && IsHandleCreated) && !string.IsNullOrEmpty(tabMouseOn.ToolTipText)) {
                    if(toolTip == null) {
                        toolTip = new ToolTip(components) { ShowAlways = true };
                    }
                    else {
                        toolTip.Active = false;
                    }
                    string toolTipText = tabMouseOn.ToolTipText;
                    string str2 = tabMouseOn.ShellToolTip;
                    if(!string.IsNullOrEmpty(str2)) {
                        toolTipText = toolTipText + "\r\n" + str2;
                    }
                    iToolTipIndex = num;
                    toolTip.SetToolTip(this, toolTipText);
                    toolTip.Active = true;
                }
            }
            else {
                iToolTipIndex = -1;
                if(toolTip != null) {
                    toolTip.Active = false;
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            QTabItem droppedTab = draggingTab;
            bool wasGroupingDrag = groupingDragActive;
            TabGroupState dropTarget = groupDropTarget;
            draggingTab = null;
            groupingDragActive = false;
            UpdateGroupDropTarget(null);
            if(fSuppressMouseUp) {
                fSuppressMouseUp = false;
                base.OnMouseUp(e);
            }
            else {
                int num;
                QTabItem tabMouseOn = GetTabMouseOn(out num);
                if(e.Button == MouseButtons.Left && wasGroupingDrag && droppedTab != null) {
                    HandleGroupDrop(droppedTab, dropTarget);
                }
                if(((fDrawCloseButton && (e.Button != MouseButtons.Right)) && ((CloseButtonClicked != null) && (tabMouseOn != null))) && (!tabMouseOn.TabLocked && HitTestOnButtons(tabMouseOn.TabBounds, e.Location, true, num == iSelectedIndex))) {
                    if(e.Button == MouseButtons.Left) {
                        iTabMouseOnButtonsIndex = -1;
                        QTabCancelEventArgs args = new QTabCancelEventArgs(tabMouseOn, num, false, TabControlAction.Deselected);
                        CloseButtonClicked(this, args);
                        if(args.Cancel) {
                            PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
                        }
                    }
                } else if ( (fNeedPlusButton && (e.Button != MouseButtons.Right)) && ((PlusButtonClicked != null) && tabMouseOn == null && IsPlusButton(e) ) ) {
                    PlusButtonClicked(this, null);
                }
                else {
                    base.OnMouseUp(e);
                }
            }
        }

        private bool IsPlusButton(MouseEventArgs e)
        {
            if (newRect != null && newRect.Contains( e.Location ))
            {
                return true;
            }
            return false;
        }

        protected override void OnPaint(PaintEventArgs e) {
            fOncePainted = true;
            if(iMultipleType != 0) {
                OnPaint_MultipleRow(e);
            }
            else {
                fNeedToDrawUpDown = CalculateItemRectangle();
                try {
                    QTabItem tabMouseOn = GetTabMouseOn();
                    bool fVisualStyle = !fForceClassic && VisualStyleRenderer.IsSupported;
                    if(fVisualStyle && (vsr_LPressed == null)) {
                        InitializeRenderer();
                    }
                    for(int i = 0; i < tabPages.Count; i++) {
                        if(i == iSelectedIndex) {
                            continue;
                        }
                        if(tabPages[i].CollapsedByGroup) {
                            continue;
                        }
                        Rectangle rect = GetItemRectangle(i);
                        if(rect.IsEmpty) {
                            continue;
                        }
                        DrawTab(e.Graphics, rect, i, tabMouseOn, fVisualStyle);
                    }
                    if((tabPages.Count > 0) && (iSelectedIndex > -1) && !tabPages[iSelectedIndex].CollapsedByGroup) {
                        Rectangle selectedRect = GetItemRectangle(iSelectedIndex);
                        if(!selectedRect.IsEmpty) {
                            DrawTab(e.Graphics, selectedRect, iSelectedIndex, tabMouseOn, fVisualStyle);
                        }
                    }
                    if((fNeedToDrawUpDown && (iSelectedIndex < tabPages.Count)) && ((iSelectedIndex > -1) && (GetItemRectangle(iSelectedIndex).X != 0))) {
                        e.Graphics.FillRectangle(SystemBrushes.Control, new Rectangle(0, 0, 2, e.ClipRectangle.Height));
                    }

                    if (fNeedPlusButton)
                    {
                        Rectangle plusRect = GetItemRectangle(tabPages.Count - 1);
                        if(!plusRect.IsEmpty) {
                            DrawPlusButton(e.Graphics, plusRect);
                        }
                    }

                    DrawGroupIndicators(e.Graphics);
                    ShowUpDown(fNeedToDrawUpDown);
                }
                catch(Exception exception) {
                    QTUtility2.MakeErrorLog(exception);
                }
            }
        }

        private RectangleF newRect;
        /**
         * ɫť
         */
        private void DrawPlusButton(Graphics g,Rectangle drawRect)
        {
            // Create string to draw.
            String drawString = "+";

            // Create font and brush.
            Font drawFont = new Font("Arial", 16, FontStyle.Bold);

            Color color = Color.Blue;
            if (QTUtility.InNightMode)
            {
                color = Color.White;
            }
            SolidBrush drawBrush = new SolidBrush(color);

            // Create rectangle for drawing.
            // int defaultDpi = DpiManager.DefaultDpi;
            //  new PointF((float)defaultDpi / 96f, (float)defaultDpi / 96f);
            newRect = new RectangleF(
                drawRect.X + drawRect.Width + 3 , 
                drawRect.Y + drawRect.Height / 2 - 13, 
                drawRect.Width / 2, 
                drawRect.Height );
            // QTUtility2.MakeErrorLog( "x:" + (drawRect.X + drawRect.Width) + ",y:" + (drawRect.Y + drawRect.Height / 2 - 10) + ",width:" + (drawRect.Width / 2) + ",height:" + (drawRect.Height));
            //  new Rectangle(num, 0, PLUSBUTTON_WIDTH, ScaledTabHeight).TranslateClient(num2, IsRightToLeft);
            // Draw rectangle to screen.
            // Pen blackPen = new Pen(Color.Blue);
            //g.DrawRectangle(blackPen, x, y, width, height);
            // Draw string to screen.
            g.DrawString(drawString, drawFont, drawBrush, newRect );
        }

        private void OnPaint_MultipleRow(PaintEventArgs e) {
            CalculateItemRectangle_MultiRows();
            try {
                DrawGroupIndicators(e.Graphics);
                QTabItem tabMouseOn = GetTabMouseOn();
                bool fVisualStyle = !fForceClassic && VisualStyleRenderer.IsSupported;
                if(fVisualStyle && (vsr_LPressed == null)) {
                    InitializeRenderer();
                }
                bool flag2 = false;
                for(int i = 0; i < (iCurrentRow + 1); i++) {
                    for(int j = 0; j < tabPages.Count; j++) {
                        QTabItem base3 = tabPages[j];
                        if(base3.Row == i) {
                            if(base3.CollapsedByGroup || base3.TabBounds.IsEmpty) {
                                continue;
                            }
                            if(j != iSelectedIndex) {
                                DrawTab(e.Graphics, base3.TabBounds, j, tabMouseOn, fVisualStyle);
                            }
                            else {
                                flag2 = true;
                            }
                        }
                    }
                    if(flag2 && !tabPages[iSelectedIndex].CollapsedByGroup && !tabPages[iSelectedIndex].TabBounds.IsEmpty) {
                        DrawTab(e.Graphics, tabPages[iSelectedIndex].TabBounds, iSelectedIndex, tabMouseOn, fVisualStyle);
                        flag2 = false;
                    }

                    if (fNeedPlusButton)
                    {
                        if (tabPages.Count > 0)
                        {
                            Rectangle plusButtonRect = tabPages[tabPages.Count - 1].TabBounds;
                            if(!plusButtonRect.IsEmpty) {
                                DrawPlusButton(e.Graphics,plusButtonRect);
                            }
                        }
                    }
                }
                ShowUpDown(false);
            }
            catch(Exception exception) {
                QTUtility2.MakeErrorLog(exception);
            }
        }

        private void OnTabPageAdded(QTabItem tabPage, int index) {
            if(index == 0) {
                selectedTabPage = tabPage;
            }
            if(TabCountChanged != null) {
                TabCountChanged(this, new QTabCancelEventArgs(tabPage, index, false, TabControlAction.Selected));
            }
        }

        private void OnTabPageInserted(QTabItem tabPage, int index) {
            if(index <= iSelectedIndex) {
                iSelectedIndex++;
            }
            if(TabCountChanged != null) {
                TabCountChanged(this, new QTabCancelEventArgs(tabPage, index, false, TabControlAction.Selected));
            }
        }

        private void OnTabPageRemoved(QTabItem tabPage, int index) {
            if(!Disposing && (index != -1)) {
                if(index == iSelectedIndex) {
                    iSelectedIndex = -1;
                }
                else if(index < iSelectedIndex) {
                    iSelectedIndex--;
                }
                if(TabCountChanged != null) {
                    TabCountChanged(this, new QTabCancelEventArgs(tabPage, index, false, TabControlAction.Deselected));
                }
            }
            RemoveTabFromGroups(tabPage);
            CleanupEmptyGroups();
            EnsureSelectionForCollapsedGroups();
        }

        private void OnUpDownClicked(bool dir, bool lockPaint) {
            int num = Width - 0x24;
            if((!dir || ((tabPages[tabPages.Count - 1].TabBounds.Right + iScrollWidth) >= num)) && (dir || ((tabPages[0].TabBounds.Left + iScrollWidth) != 0))) {
                iScrollClickedCount += dir ? 1 : -1;
                if(iScrollClickedCount > (tabPages.Count - 1)) {
                    iScrollClickedCount = tabPages.Count - 1;
                }
                else if(iScrollClickedCount < 0) {
                    iScrollClickedCount = 0;
                }
                else {
                    iScrollWidth = -tabPages[iScrollClickedCount].TabBounds.X;
                    if(!lockPaint) {
                        Invalidate();
                    }
                }
            }
        }

        public bool PerformFocusedFolderIconClick(bool fParent) {
            if(((TabIconMouseDown == null) || !Focused) || ((-1 >= iFocusedTabIndex) || (iFocusedTabIndex >= tabPages.Count))) {
                return false;
            }
            iTabIndexOfSubDirShown = iFocusedTabIndex;
            QTabItem tabPage = tabPages[iFocusedTabIndex];
            int tabPageIndex = 0;
            if((iMultipleType == 0) && fNeedToDrawUpDown) {
                tabPageIndex = iScrollWidth;
            }
            TabIconMouseDown(this, new QTabCancelEventArgs(tabPage, tabPageIndex, fParent, TabControlAction.Selecting));
            PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
            return true;
        }

        public override void Refresh() {
            if(!fRedrawSuspended) {
                base.Refresh();
            }
        }

        public void RefreshFolderImage() {
            iTabMouseOnButtonsIndex = -1;
            fNowMouseIsOnIcon = false;
            PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
        }

        public void RefreshOptions(bool fInit) {
            if(fInit) {
                if(Config.Tabs.MultipleTabRows) {
                    iMultipleType = Config.Tabs.ActiveTabOnBottomRow ? 1 : 2;
                }
                fDrawFolderImg = Config.Tabs.ShowFolderIcon;
            }
            else {
                colorSet = new Color[] {
                    Config.Skin.TabTextActiveColor,
                    Config.Skin.TabTextInactiveColor,
                    Config.Skin.TabTextHotColor,
                    Config.Skin.TabShadActiveColor,
                    Config.Skin.TabShadInactiveColor,
                    Config.Skin.TabShadHotColor
                };
                brshActive.Color = colorSet[0];
                brshInactv.Color = colorSet[1];
            }
            if(Config.Skin.FixedWidthTabs) {
                sizeMode = TabSizeMode.Fixed;
                fLimitSize = false;
            }
            else {
                sizeMode = TabSizeMode.Normal;
                fLimitSize = true; // Config.LimitedWidthTabs;
            }
            if((Config.Skin.TabMaxWidth >= Config.Skin.TabMinWidth) && (Config.Skin.TabMinWidth > 9)) {
                maxAllowedTabWidth = Config.Skin.TabMaxWidth;
                minAllowedTabWidth = Config.Skin.TabMinWidth;
            }
            itemSize = new Size(maxAllowedTabWidth, Config.Skin.TabHeight);
            fActiveTxtBold = Config.Skin.ActiveTabInBold;
            fForceClassic = Config.Skin.UseTabSkin;
            SetFont(Config.Skin.TabTextFont);
            sizingMargin = Config.Skin.TabSizeMargin + new Padding(0, 0, 1, 1);
            if(Config.Skin.UseTabSkin && Config.Skin.TabImageFile.Length > 0) {
                SetTabImages(QTTabBarClass.CreateTabImage());
            }
            else {
                SetTabImages(null);
            }
            // жϱǩıǷ  
            tabTextAlignment = Config.Skin.TabTextCentered ? StringAlignment.Center : StringAlignment.Near;
            fDrawShadow = Config.Skin.TabTitleShadows;
            fDrawCloseButton = Config.Tabs.ShowCloseButtons && !Config.Tabs.CloseBtnsWithAlt;
            fCloseBtnOnHover = Config.Tabs.CloseBtnsOnHover;
            fShowSubDirTip = Config.Tabs.ShowSubDirTipOnTab;
            if(!fInit) {
                if(fDrawFolderImg != Config.Tabs.ShowFolderIcon) {
                    fDrawFolderImg = Config.Tabs.ShowFolderIcon;
                    if(fDrawFolderImg) {
                        foreach(QTabItem base2 in TabPages) {
                            base2.ImageKey = base2.ImageKey;
                        }
                    }
                    else {
                        fNowMouseIsOnIcon = false;
                    }
                }
                if(fAutoSubText && !Config.Tabs.RenameAmbTabs) {
                    foreach(QTabItem item in TabPages) {
                        item.Comment = string.Empty;
                        item.RefreshRectangle();
                    }
                    Refresh();
                }
                else if(!fAutoSubText && Config.Tabs.RenameAmbTabs) {
                    QTabItem.CheckSubTexts(this);
                }   
            }
            fAutoSubText = Config.Tabs.RenameAmbTabs;
        }

        public bool SelectFocusedTab() {
            if((Focused && (-1 < iFocusedTabIndex)) && (iFocusedTabIndex < tabPages.Count)) {
                SelectedIndex = iFocusedTabIndex;
                return true;
            }
            return false;
        }

        public bool SelectTab(QTabItem tabPage) {
            int index = tabPages.IndexOf(tabPage);
            if(index == -1) {
                throw new ArgumentException("arg was not found.");
            }
            return (((index != -1) && (selectedTabPage != tabPage)) && ChangeSelection(tabPage, index));
        }

        public void SelectTab(int index) {
            if((index <= -1) || (index >= tabPages.Count)) {
                throw new ArgumentOutOfRangeException("index," + index, "index is out of range.");
            }
            QTabItem tabToSelect = tabPages[index];
            if(selectedTabPage != tabToSelect) {
                ChangeSelection(tabToSelect, index);
            }
            else {
                iSelectedIndex = index;
            }
        }

        public void SelectTabDirectly(QTabItem tabPage) {
            int index = tabPages.IndexOf(tabPage);
            selectedTabPage = tabPage;
            SelectedIndex = index;
        }

        public void SetContextMenuState(bool fShow) {
            fNowTabContextMenuStripShowing = fShow;
        }

        private void SetFont(Font fnt) {
            Font = fnt;
            if(fntBold != null) {
                fntBold.Dispose();
            }
            fntBold = Font;
            try
            {
                fntBold = new Font(Font, FontStyle.Bold);
            }
            catch (Exception e)
            {
                QTUtility2.MakeErrorLog(e, "SetFont fntBold");

            }
            if(fnt_Underline != null) {
                fnt_Underline.Dispose();
            }
            fnt_Underline = Font;
            try
            {
                fnt_Underline = new Font(Font, FontStyle.Underline);
            }
            catch (Exception e)
            {
                QTUtility2.MakeErrorLog(e, "SetFont fnt_Underline");

            }
            if(fntBold_Underline != null) {
                fntBold_Underline.Dispose();
            }
            fntBold_Underline = fntBold;
            try
            {
                fntBold_Underline = new Font(fntBold, FontStyle.Underline);
            }
            catch  (Exception e)
            {
                QTUtility2.MakeErrorLog(e, "SetFont fntBold_Underline");

            }
            if(fntSubText != null) {
                fntSubText.Dispose();
            }
            float sizeInPoints = Font.SizeInPoints;
            fntSubText = Font;
            try
            {
                fntSubText = new Font(Font.FontFamily, (sizeInPoints > 8.25f) ? (sizeInPoints - 0.75f) : sizeInPoints);
            }
            catch (Exception e)
            {
                QTUtility2.MakeErrorLog(e, "SetFont sizeInPoints");

            }
            if(fntDriveLetter != null) {
                fntDriveLetter.Dispose();
            }
            fntDriveLetter = Font;
            try
            {
                fntDriveLetter = new Font(Font.FontFamily, 8.25f);
            }
            catch (Exception e)
            {
                QTUtility2.MakeErrorLog(e, "SetFont 8.25f");

            }
            QTabItem.TabFont = Font;
        }

        public void SetPseudoHotIndex(int index) {
            int iPseudoHotIndex = this.iPseudoHotIndex;
            this.iPseudoHotIndex = index;
            if((iPseudoHotIndex > -1) && (iPseudoHotIndex < TabCount)) {
                Invalidate(GetTabRect(iPseudoHotIndex, true));
            }
            if((this.iPseudoHotIndex > -1) && (this.iPseudoHotIndex < TabCount)) {
                Invalidate(GetTabRect(this.iPseudoHotIndex, true));
            }
            Update();
        }

        public void SetRedraw(bool bRedraw) {
            if(bRedraw && fRedrawSuspended) {
                base.Refresh();
            }
            fRedrawSuspended = !bRedraw;
        }

        public void SetSubDirTipShown(bool fShown) {
            if(!fShown) {
                iTabIndexOfSubDirShown = -1;
            }
            fSubDirShown = fShown;
        }

        private void SetTabImages(Bitmap[] bmps) {
            if((bmps != null) && (bmps.Length == 3)) {
                if(tabImages == null) {
                    tabImages = bmps;
                }
                else if((tabImages[0] != null) && (tabImages[1] != null)) {
                    Bitmap bitmap = tabImages[0];
                    Bitmap bitmap2 = tabImages[1];
                    Bitmap bitmap3 = tabImages[2];
                    tabImages[0] = bmps[0];
                    tabImages[1] = bmps[1];
                    tabImages[2] = bmps[2];
                    bitmap.Dispose();
                    bitmap2.Dispose();
                    bitmap3.Dispose();
                }
                else {
                    tabImages = bmps;
                }
            }
            else if(((tabImages != null) && (tabImages[0] != null)) && ((tabImages[1] != null) && (tabImages[2] != null))) {
                Bitmap bitmap4 = tabImages[0];
                Bitmap bitmap5 = tabImages[1];
                Bitmap bitmap6 = tabImages[2];
                tabImages = null;
                bitmap4.Dispose();
                bitmap5.Dispose();
                bitmap6.Dispose();
            }
        }

        public int SetTabRowType(int iType) {
            iMultipleType = iType;
            if(iType != 0) {
                fNeedToDrawUpDown = false;
                return (iCurrentRow + 1);
            }
            return 1;
        }

        public void ShowCloseButton(bool fShow) {
            fDrawCloseButton = fNowShowCloseBtnAlt = fShow;
            Invalidate();
        }

        private void ShowUpDown(bool fShow) {
            if(fShow) {
                if(upDown == null) {
                    upDown = new UpDown();
                    upDown.Anchor = AnchorStyles.Right;
                    upDown.ValueChanged += upDown_ValueChanged;
                    Controls.Add(upDown);
                }
                upDown.Location = new Point(Width - 0x24, 0);
                upDown.Visible = true;
                upDown.BringToFront();
            }
            else if((upDown != null) && upDown.Visible) {
                upDown.Visible = false;
            }
        }

        private void timerSuppressDoubleClick_Tick(object sender, EventArgs e) {
            timerSuppressDoubleClick.Enabled = false;
            fSuppressDoubleClick = false;
        }

        private void upDown_ValueChanged(object sender, QEventArgs e) {
            OnUpDownClicked(e.Direction == ArrowDirection.Right, false);
        }

        protected override void WndProc(ref Message m) {
            QTabItem tabMouseOn;
            int num;
            int msg = m.Msg;
            switch(msg) {
                case WM.SETCURSOR:
                    if(fSubDirShown || fNowTabContextMenuStripShowing) {
                        uint num4 = ((uint)((long)m.LParam)) & 0xffff;
                        uint num5 = (((uint)((long)m.LParam)) >> 0x10) & 0xffff;
                        if((num4 == 1) && (num5 == 0x200)) {
                            tabMouseOn = GetTabMouseOn(out num);
                            InvalidateTabsOnMouseMove(tabMouseOn, num, PointToClient(MousePosition));
                            m.Result = (IntPtr)1;
                            return;
                        }
                    }
                    break;

                case WM.MOUSEACTIVATE: {
                        if(!fSubDirShown || (TabIconMouseDown == null)) {
                            break;
                        }
                        int num2 = (((int)((long)m.LParam)) >> 0x10) & 0xffff;
                        if(num2 == 0x207) {
                            break;
                        }
                        bool cancel = num2 == 0x204;
                        m.Result = (IntPtr)4;
                        tabMouseOn = GetTabMouseOn(out num);
                        if(((tabMouseOn == null) || (num == iTabIndexOfSubDirShown)) || !HitTestOnButtons(tabMouseOn.TabBounds, PointToClient(MousePosition), false, num == iSelectedIndex)) {
                            TabIconMouseDown(this, new QTabCancelEventArgs(null, -1, false, TabControlAction.Deselected));
                            return;
                        }
                        int tabPageIndex = 0;
                        if((iMultipleType == 0) && fNeedToDrawUpDown) {
                            tabPageIndex = iScrollWidth;
                        }
                        TabIconMouseDown(this, new QTabCancelEventArgs(tabMouseOn, tabPageIndex, cancel, TabControlAction.Selecting));
                        if(fSubDirShown) {
                            iTabIndexOfSubDirShown = num;
                        }
                        else {
                            iTabIndexOfSubDirShown = -1;
                        }
                        fNowMouseIsOnIcon = true;
                        iTabMouseOnButtonsIndex = num;
                        PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
                        return;
                    }

                case WM.ERASEBKGND:
                    if(!fRedrawSuspended) {
                        break;
                    }
                    m.Result = (IntPtr)1;
                    return;

                default:
                    if(msg != WM.CONTEXTMENU) {
                        break;
                    }
                    if((QTUtility2.GET_X_LPARAM(m.LParam) != -1) || (QTUtility2.GET_Y_LPARAM(m.LParam) != -1)) {
                        tabMouseOn = GetTabMouseOn(out num);
                        if(tabMouseOn == null) {
                            PInvoke.SendMessage(Parent.Handle, 0x7b, m.WParam, m.LParam);
                            return;
                        }
                        if(!fShowSubDirTip || !HitTestOnButtons(tabMouseOn.TabBounds, PointToClient(MousePosition), false, num == iSelectedIndex)) {
                            break;
                        }
                    }
                    return;
            }
            base.WndProc(ref m);
        }

        public bool AutoSubText {
            get {
                return fAutoSubText;
            }
        }

        protected override bool CanEnableIme {
            get {
                return false;
            }
        }

        public bool DrawFolderImage {
            get {
                return fDrawFolderImg;
            }
        }

        public bool EnableCloseButton {
            get {
                return fDrawCloseButton;
            }
            set {
                fDrawCloseButton = value;
            }
        }

        public bool OncePainted {
            get {
                return fOncePainted;
            }
        }

        public int SelectedIndex {
            get {
                return iSelectedIndex;
            }
            set {
                SelectTab(value);
            }
        }

        public QTabItem SelectedTab {
            get {
                return tabPages[iSelectedIndex];
            }
        }

        public bool TabCloseButtonOnAlt {
            get {
                return fNowShowCloseBtnAlt;
            }
        }

        public bool TabCloseButtonOnHover {
            get {
                return fCloseBtnOnHover;
            }
        }

        public int TabCount {
            get {
                return tabPages.Count;
            }
        }

        public int TabOffset {
            get {
                if((iMultipleType == 0) && fNeedToDrawUpDown) {
                    return iScrollWidth;
                }
                return 0;
            }
        }

        public QTabCollection TabPages {
            get {
                return tabPages;
            }
        }

        private bool TryGetGroupState(QTabItem tab, out TabGroupState state, out bool isLeader) {
            state = null;
            isLeader = false;
            if(tab == null) {
                return false;
            }
            string groupKey = tab.GroupKey;
            if(string.IsNullOrEmpty(groupKey)) {
                return false;
            }
            TabGroupState resolved;
            if(!groupStates.TryGetValue(groupKey, out resolved) || resolved == null || resolved.Tabs == null || resolved.Tabs.Count == 0) {
                return false;
            }
            if(!resolved.Tabs.Contains(tab)) {
                return false;
            }
            state = resolved;
            isLeader = ReferenceEquals(resolved.Tabs[0], tab);
            return true;
        }

        private void ReserveGroupIndicator(TabGroupState state, ref int x, int y, int height) {
            if(state == null) {
                return;
            }
            int width = GROUP_INDICATOR_WIDTH + GROUP_INDICATOR_SPACING;
            state.AnchorBounds = new Rectangle(x, y, width, height);
            int railHeight = Math.Max(height - 4, 4);
            int railX = x + Math.Max((width - GROUP_RAIL_WIDTH) / 2, 0);
            state.RailBounds = new Rectangle(railX, y + 2, GROUP_RAIL_WIDTH, railHeight);
            state.IndicatorBounds = state.RailBounds;
            x += width;
        }

        private int GetNextVisibleIndex(int start) {
            for(int i = start; i < tabPages.Count; i++) {
                if(!tabPages[i].CollapsedByGroup) {
                    return i;
                }
            }
            return -1;
        }

        private int GetPrevVisibleIndex(int start) {
            for(int i = start; i >= 0; i--) {
                if(!tabPages[i].CollapsedByGroup) {
                    return i;
                }
            }
            return -1;
        }

        internal void AssignGroupTabs(string groupName, IList<QTabItem> tabs) {
            if(string.IsNullOrEmpty(groupName)) {
                return;
            }
            if(tabs == null) {
                tabs = Array.Empty<QTabItem>();
            }
            IList<QTabItem> tabList = tabs as IList<QTabItem> ?? new List<QTabItem>(tabs);
            TabGroupState state;
            if(!groupStates.TryGetValue(groupName, out state)) {
                state = new TabGroupState { Name = groupName };
                state.AccentColor = ResolveGroupAccent(groupName);
                groupStates[groupName] = state;
            }
            else if(state.AccentColor.IsEmpty) {
                state.AccentColor = ResolveGroupAccent(groupName);
            }
            foreach(var other in groupStates.Values) {
                if(other == state) {
                    continue;
                }
                for(int i = other.Tabs.Count - 1; i >= 0; i--) {
                    QTabItem tab = other.Tabs[i];
                    if(tab == null || !tabPages.Contains(tab) || tabList.Contains(tab)) {
                        other.Tabs.RemoveAt(i);
                        if(tab != null) {
                            tab.GroupKey = null;
                            tab.CollapsedByGroup = false;
                        }
                    }
                }
            }
            state.Tabs.Clear();
            state.AnchorBounds = Rectangle.Empty;
            state.IndicatorBounds = Rectangle.Empty;
            foreach(QTabItem tab in tabList) {
                if(tab == null || !tabPages.Contains(tab)) {
                    continue;
                }
                RemoveTabFromGroups(tab);
                state.Tabs.Add(tab);
                tab.GroupKey = groupName;
            }
            CleanupEmptyGroups();
            SyncGroupOrder();
            EnsureSelectionForCollapsedGroups();
            Invalidate();
        }

        private void RemoveTabFromGroups(QTabItem tab) {
            if(tab == null) {
                return;
            }
            foreach(var state in groupStates.Values) {
                state.Tabs.Remove(tab);
            }
            tab.GroupKey = null;
            tab.CollapsedByGroup = false;
        }

        private void CleanupEmptyGroups() {
            List<string> empty = new List<string>();
            foreach(KeyValuePair<string, TabGroupState> pair in groupStates) {
                TabGroupState state = pair.Value;
                if(state == null) {
                    continue;
                }
                state.Tabs.RemoveAll(tab => tab == null || !tabPages.Contains(tab));
                state.RailBounds = Rectangle.Empty;
                state.IslandBounds = Rectangle.Empty;
                state.IndicatorBounds = Rectangle.Empty;
                state.DropHighlighted = false;
                if(state.Tabs.Count == 0) {
                    empty.Add(pair.Key);
                }
            }
            foreach(string key in empty) {
                groupStates.Remove(key);
            }
        }

        private void SyncGroupOrder() {
            if(groupStates.Count == 0) {
                return;
            }
            foreach(TabGroupState state in groupStates.Values) {
                if(state == null) {
                    continue;
                }
                state.Tabs.RemoveAll(tab => tab == null || !tabPages.Contains(tab));
                state.Tabs.Sort((a, b) => tabPages.IndexOf(a).CompareTo(tabPages.IndexOf(b)));
                ApplyGroupCollapseState(state);
            }
        }

        internal void OnTabsReordered() {
            SyncGroupOrder();
            EnsureSelectionForCollapsedGroups();
            Invalidate();
        }

        private void ApplyGroupCollapseState(TabGroupState state) {
            if(state == null) {
                return;
            }
            foreach(QTabItem tab in state.Tabs) {
                if(tab != null) {
                    tab.CollapsedByGroup = state.IsCollapsed;
                }
            }
        }

        private void EnsureSelectionForCollapsedGroups() {
            if(iSelectedIndex < 0 || iSelectedIndex >= tabPages.Count) {
                return;
            }
            if(!tabPages[iSelectedIndex].CollapsedByGroup) {
                return;
            }
            int newIndex = GetNextVisibleIndex(iSelectedIndex + 1);
            if(newIndex == -1) {
                newIndex = GetPrevVisibleIndex(iSelectedIndex - 1);
            }
            if(newIndex != -1) {
                SelectedIndex = newIndex;
            }
        }

        private void ToggleGroup(string groupName) {
            TabGroupState state;
            if(!groupStates.TryGetValue(groupName, out state)) {
                return;
            }
            state.IsCollapsed = !state.IsCollapsed;
            ApplyGroupCollapseState(state);
            EnsureSelectionForCollapsedGroups();
            UpdateGroupDropTarget(null);
            Invalidate();
        }

        private void UpdateGroupIndicators() {
            UpdateGroupIslandGeometry();
        }

        private void DrawGroupIndicators(Graphics g) {
            DrawGroupIslands(g);
        }

        private static Color ResolveGroupAccent(string groupName) {
            if(string.IsNullOrEmpty(groupName)) {
                return Color.SteelBlue;
            }
            unchecked {
                int hash = 17;
                foreach(char c in groupName) {
                    hash = hash * 31 + c;
                }
                double hue = (hash & 0x7fffffff) % 360;
                return ColorFromHsl(hue / 360.0, 0.55, 0.45);
            }
        }

        private static Color ColorFromHsl(double h, double s, double l) {
            double r = l;
            double g = l;
            double b = l;
            if(s != 0) {
                double q = l < 0.5 ? l * (1 + s) : (l + s - l * s);
                double p = 2 * l - q;
                r = HueToRgb(p, q, h + 1.0 / 3.0);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0 / 3.0);
            }
            return Color.FromArgb(255, (int)Math.Round(r * 255), (int)Math.Round(g * 255), (int)Math.Round(b * 255));
        }

        private static double HueToRgb(double p, double q, double t) {
            if(t < 0) {
                t += 1;
            }
            if(t > 1) {
                t -= 1;
            }
            if(t < 1.0 / 6.0) {
                return p + (q - p) * 6 * t;
            }
            if(t < 1.0 / 2.0) {
                return q;
            }
            if(t < 2.0 / 3.0) {
                return p + (q - p) * (2.0 / 3.0 - t) * 6;
            }
            return p;
        }

        private void DrawGroupIslands(Graphics g) {
            UpdateGroupIslandGeometry();
            foreach(var state in groupStates.Values) {
                if(state == null) {
                    continue;
                }
                Rectangle island = state.IslandBounds;
                Rectangle rail = state.RailBounds;
                if((iMultipleType == 0) && fNeedToDrawUpDown) {
                    if(!island.IsEmpty) {
                        island.Offset(iScrollWidth, 0);
                    }
                    if(!rail.IsEmpty) {
                        rail.Offset(iScrollWidth, 0);
                    }
                }
                Color accent = state.AccentColor.IsEmpty ? ResolveGroupAccent(state.Name) : state.AccentColor;
                if(!state.IsCollapsed && !island.IsEmpty) {
                    Rectangle background = island;
                    background.Width = Math.Max(background.Width, 4);
                    background.Height = Math.Max(background.Height, 4);
                    Color fillColor = Color.FromArgb(state.DropHighlighted ? 96 : 48, accent);
                    Color borderColor = Color.FromArgb(160, accent);
                    using(SolidBrush brush = new SolidBrush(fillColor))
                    using(Pen pen = new Pen(borderColor, 1f)) {
                        g.FillRectangle(brush, background);
                        g.DrawRectangle(pen, background);
                    }
                }
                if(rail.Width <= 0 || rail.Height <= 0) {
                    continue;
                }
                if(rail.Right < 0 || rail.Left > Width) {
                    continue;
                }
                DrawGroupRail(g, rail, accent, state.IsCollapsed, state.DropHighlighted);
            }
        }

        private void DrawGroupRail(Graphics g, Rectangle rail, Color accent, bool collapsed, bool highlighted) {
            Color fill = highlighted ? ControlPaint.LightLight(accent) : accent;
            using(SolidBrush brush = new SolidBrush(fill))
            using(Pen pen = new Pen(ControlPaint.Dark(fill))) {
                g.FillRectangle(brush, rail);
                g.DrawRectangle(pen, rail);
            }
            int inset = Math.Max(2, rail.Width / 2);
            Point[] glyph;
            if(collapsed) {
                glyph = new[] {
                    new Point(rail.Left + inset, rail.Top + rail.Height / 2),
                    new Point(rail.Right - inset, rail.Top + inset),
                    new Point(rail.Right - inset, rail.Bottom - inset)
                };
            }
            else {
                glyph = new[] {
                    new Point(rail.Right - inset, rail.Top + rail.Height / 2),
                    new Point(rail.Left + inset, rail.Top + inset),
                    new Point(rail.Left + inset, rail.Bottom - inset)
                };
            }
            using(SolidBrush glyphBrush = new SolidBrush(Color.White)) {
                g.FillPolygon(glyphBrush, glyph);
            }
        }

        private void UpdateGroupIslandGeometry() {
            foreach(var state in groupStates.Values) {
                if(state == null) {
                    continue;
                }
                Rectangle anchor = state.AnchorBounds;
                Rectangle union = Rectangle.Empty;
                foreach(QTabItem tab in state.Tabs) {
                    if(tab == null || tab.CollapsedByGroup) {
                        continue;
                    }
                    Rectangle rect = tab.TabBounds;
                    if(rect.IsEmpty) {
                        continue;
                    }
                    union = union.IsEmpty ? rect : Rectangle.Union(union, rect);
                }
                if(union.IsEmpty) {
                    state.IslandBounds = Rectangle.Empty;
                    if(anchor.Width > 0 && anchor.Height > 0) {
                        int railHeight = Math.Max(anchor.Height - 4, 4);
                        int railX = anchor.Left + Math.Max((anchor.Width - GROUP_RAIL_WIDTH) / 2, 0);
                        state.RailBounds = new Rectangle(railX, anchor.Top + 2, GROUP_RAIL_WIDTH, railHeight);
                    }
                    else if(state.RailBounds.IsEmpty) {
                        state.RailBounds = Rectangle.Empty;
                    }
                    state.IndicatorBounds = state.RailBounds;
                    continue;
                }
                int islandLeft = Math.Min(anchor.Left, union.Left) - GROUP_ISLAND_PADDING;
                int islandRight = union.Right + GROUP_ISLAND_PADDING;
                int islandTop = union.Top + 1;
                int islandBottom = union.Bottom - 1;
                state.IslandBounds = new Rectangle(islandLeft, islandTop, Math.Max(islandRight - islandLeft, 4), Math.Max(islandBottom - islandTop, 4));
                int railXBase = anchor.Width > 0
                        ? anchor.Left + Math.Max((anchor.Width - GROUP_RAIL_WIDTH) / 2, 0)
                        : union.Left - GROUP_RAIL_WIDTH - 4;
                railXBase = Math.Min(railXBase, union.Left - 2);
                int railHeight = Math.Max(union.Height - 4, 4);
                state.RailBounds = new Rectangle(railXBase, union.Top + 2, GROUP_RAIL_WIDTH, railHeight);
                state.IndicatorBounds = state.RailBounds;
            }
        }

        private TabGroupState HitTestGroupSurface(Point location) {
            foreach(var state in groupStates.Values) {
                if(state == null) {
                    continue;
                }
                Rectangle island = state.IslandBounds;
                Rectangle rail = state.RailBounds;
                if((iMultipleType == 0) && fNeedToDrawUpDown) {
                    if(!island.IsEmpty) {
                        island.Offset(iScrollWidth, 0);
                    }
                    if(!rail.IsEmpty) {
                        rail.Offset(iScrollWidth, 0);
                    }
                }
                if(!state.IsCollapsed && !island.IsEmpty && island.Contains(location)) {
                    return state;
                }
                if(!rail.IsEmpty && rail.Contains(location)) {
                    return state;
                }
            }
            return null;
        }

        private void UpdateGroupDropTarget(TabGroupState target) {
            if(groupDropTarget == target) {
                return;
            }
            if(groupDropTarget != null) {
                groupDropTarget.DropHighlighted = false;
            }
            groupDropTarget = target;
            if(groupDropTarget != null) {
                groupDropTarget.DropHighlighted = true;
            }
            Invalidate();
        }

        private void AddTabToGroup(TabGroupState state, QTabItem tab) {
            if(state == null || tab == null) {
                return;
            }
            List<QTabItem> members = state.Tabs.Where(t => t != null && tabPages.Contains(t)).ToList();
            if(!members.Contains(tab)) {
                members.Add(tab);
                AssignGroupTabs(state.Name, members);
            }
            if(state.IsCollapsed) {
                state.IsCollapsed = false;
                ApplyGroupCollapseState(state);
            }
            EnsureSelectionForCollapsedGroups();
            Invalidate();
        }

        private void HandleGroupDrop(QTabItem tab, TabGroupState target) {
            if(tab == null) {
                return;
            }
            if(target != null) {
                AddTabToGroup(target, tab);
            }
            else if(!string.IsNullOrEmpty(tab.GroupKey)) {
                RemoveTabFromGroups(tab);
                CleanupEmptyGroups();
                EnsureSelectionForCollapsedGroups();
                Invalidate();
            }
        }

        private bool TryHandleGroupIndicatorClick(Point location) {
            UpdateGroupIslandGeometry();
            foreach(var state in groupStates.Values) {
                Rectangle rect = state.RailBounds;
                if(rect.Width <= 0 || rect.Height <= 0) {
                    continue;
                }
                if((iMultipleType == 0) && fNeedToDrawUpDown) {
                    rect.Offset(iScrollWidth, 0);
                }
                if(rect.Contains(location)) {
                    ToggleGroup(state.Name);
                    return true;
                }
            }
            return false;
        }

        

        

        

        

        

        

        private sealed class TabGroupState {
            public string Name;
            public List<QTabItem> Tabs = new List<QTabItem>();
            public bool IsCollapsed;
            public Rectangle AnchorBounds;
            public Rectangle IndicatorBounds;
            public Rectangle IslandBounds;
            public Rectangle RailBounds;
            public Color AccentColor;
            public bool DropHighlighted;
        }

        public sealed class QTabCollection : List<QTabItem> {
            private QTabControl Owner;

            public QTabCollection(QTabControl owner) {
                Owner = owner;
            }

            new public void Add(QTabItem tabPage) {
                base.Add(tabPage);
                Owner.OnTabPageAdded(tabPage, Count - 1);
                Owner.Refresh();
            }

            new public void Insert(int index, QTabItem tabPage) {
                base.Insert(index, tabPage);
                Owner.OnTabPageInserted(tabPage, index);
                Owner.Refresh();
            }

            new public bool Remove(QTabItem tabPage) {
                int index = IndexOf(tabPage);
                Owner.OnTabPageRemoved(tabPage, index);
                bool flag = base.Remove(tabPage);
                Owner.Refresh();
                return flag;
            }

            public void Relocate(int indexSource, int indexDestination) {
                int selectedIndex = Owner.SelectedIndex;
                int num2 = (indexSource > indexDestination) ? indexSource : indexDestination;
                int num3 = (indexSource > indexDestination) ? indexDestination : indexSource;
                QTabItem item = base[indexSource];
                base.Remove(item);
                base.Insert(indexDestination, item);
                if((num2 >= selectedIndex) && (selectedIndex >= num3)) {
                    if(num2 == selectedIndex) {
                        if(num2 == indexSource) {
                            Owner.SelectedIndex = indexDestination;
                        }
                        else {
                            Owner.SelectedIndex--;
                        }
                    }
                    else if((num3 < selectedIndex) && (selectedIndex < num2)) {
                        if(num2 == indexSource) {
                            Owner.SelectedIndex++;
                        }
                        else {
                            Owner.SelectedIndex--;
                        }
                    }
                    else if(num3 == selectedIndex) {
                        if(num2 == indexSource) {
                            Owner.SelectedIndex++;
                        }
                        else {
                            Owner.SelectedIndex = indexDestination;
                        }
                    }
                }
                Owner.OnTabsReordered();
                Owner.Refresh();
            }
        }
    }

  
}
