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
        private Form dragPreviewForm;
        private bool showDragPreview;
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
        private bool fMouseDownOnCloseBtn;
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
        private TabGroupState draggingGroup;
        private readonly EventHandler<TagVisualChangedEventArgs> tagVisualHandler;

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

        // New window creation event
        public event Action<IDLWrapper> OnTabDraggedToNewWindow;

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

            tagVisualHandler = TagManager_TagVisualChanged;
            try {
                TagManager.TagVisualChanged += tagVisualHandler;
            }
            catch { }

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
                return QTUtility.InNightMode ? ShellColors.Text : Color.Black;
            }

            if (normalized.A == 0 && (normalized.R != 0 || normalized.G != 0 || normalized.B != 0))
            {
                normalized = Color.FromArgb(255, normalized);
            }

            if (QTUtility.InNightMode)
            {
                normalized = Color.FromArgb(255, normalized);
                normalized = EnsureNightModeContrast(normalized, ShellColors.Default, ShellColors.Text);
            }

            return normalized;
        }

        private static Color EnsureNightModeContrast(Color color, Color background, Color fallback)
        {
            if (!QTUtility.InNightMode)
            {
                return color;
            }

            const double MinimumContrastRatio = 4.5d;

            Color opaqueColor = Color.FromArgb(255, color);
            Color opaqueBackground = Color.FromArgb(255, background);
            double contrast = CalculateContrastRatio(opaqueColor, opaqueBackground);
            if (contrast >= MinimumContrastRatio)
            {
                return opaqueColor;
            }

            Color target = fallback.IsEmpty ? Color.White : Color.FromArgb(255, fallback);

            double low = 0d;
            double high = 1d;
            Color result = target;

            for (int i = 0; i < 8; i++)
            {
                double mid = (low + high) / 2d;
                Color blended = BlendColors(opaqueColor, target, mid);
                contrast = CalculateContrastRatio(blended, opaqueBackground);

                if (contrast >= MinimumContrastRatio)
                {
                    result = blended;
                    high = mid;
                }
                else
                {
                    low = mid;
                }
            }

            return result;
        }

        private static Color BlendColors(Color from, Color to, double amount)
        {
            amount = Math.Min(1d, Math.Max(0d, amount));

            int r = (int)Math.Round(from.R + ((to.R - from.R) * amount));
            int g = (int)Math.Round(from.G + ((to.G - from.G) * amount));
            int b = (int)Math.Round(from.B + ((to.B - from.B) * amount));

            return Color.FromArgb(255, r, g, b);
        }

        private static double CalculateContrastRatio(Color first, Color second)
        {
            double luminance1 = GetRelativeLuminance(first);
            double luminance2 = GetRelativeLuminance(second);

            double lighter = Math.Max(luminance1, luminance2);
            double darker = Math.Min(luminance1, luminance2);

            return (lighter + 0.05d) / (darker + 0.05d);
        }

        private static double GetRelativeLuminance(Color color)
        {
            double r = color.R / 255d;
            double g = color.G / 255d;
            double b = color.B / 255d;

            r = r <= 0.03928d ? r / 12.92d : Math.Pow((r + 0.055d) / 1.055d, 2.4d);
            g = g <= 0.03928d ? g / 12.92d : Math.Pow((g + 0.055d) / 1.055d, 2.4d);
            b = b <= 0.03928d ? b / 12.92d : Math.Pow((b + 0.055d) / 1.055d, 2.4d);

            return (0.2126d * r) + (0.7152d * g) + (0.0722d * b);
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
            if(disposing && tagVisualHandler != null) {
                try {
                    TagManager.TagVisualChanged -= tagVisualHandler;
                }
                catch { }
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
            fMouseDownOnCloseBtn = false; // Reset close button tracking
            if(e.Button == MouseButtons.Left && Config.Tabs.ShowTabIslands && TryHandleGroupIndicatorClick(e.Location)) {
                return;
            }
            int num;
            QTabItem tabMouseOn = GetTabMouseOn(out num);
            if(tabMouseOn != null) {
                bool cancel = e.Button == MouseButtons.Right;
                if((!cancel && fDrawCloseButton) && HitTestOnButtons(tabMouseOn.TabBounds, e.Location, true, num == iSelectedIndex)) {
                    fMouseDownOnCloseBtn = true;
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
                // fMouseDownOnCloseBtn is already set correctly above if needed
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
            draggingGroup = null;
            PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            int num;
            if((e.Button & MouseButtons.Left) == MouseButtons.Left && (draggingTab != null || draggingGroup != null)) {
                if(!groupingDragActive) {
                    Rectangle dragRect = new Rectangle(
                        groupingDragOrigin.X - SystemInformation.DragSize.Width / 2,
                        groupingDragOrigin.Y - SystemInformation.DragSize.Height / 2,
                        SystemInformation.DragSize.Width,
                        SystemInformation.DragSize.Height);
                    if(!dragRect.Contains(e.Location)) {
                        groupingDragActive = true;
                        // Always create drag preview for tab dragging (both normal and group operations)
                        if(draggingTab != null) {
                            CreateDragPreview(draggingTab);
                        }
                        else if(draggingGroup != null) {
                            CreateGroupDragPreview(draggingGroup);
                        }
                    }
                }
                if(groupingDragActive) {
                    if(draggingGroup != null) {
                        // For group dragging, highlight valid drop zones
                        UpdateGroupDropTarget(GetGroupDropTarget(e.Location));
                    }
                    else {
                        UpdateGroupDropTarget(HitTestGroupSurface(e.Location));
                    }
                    // Always update drag preview position when dragging (for both tab and group operations)
                    UpdateDragPreview(PointToScreen(e.Location));
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
            TabGroupState droppedGroup = draggingGroup;
            bool wasGroupingDrag = groupingDragActive;
            TabGroupState dropTarget = groupDropTarget;
            draggingTab = null;
            draggingGroup = null;
            groupingDragActive = false;
            UpdateGroupDropTarget(null);
            DisposeDragPreview();
            if(fSuppressMouseUp) {
                fSuppressMouseUp = false;
                base.OnMouseUp(e);
            }
            else {
                int num;
                QTabItem tabMouseOn = GetTabMouseOn(out num);
                if(e.Button == MouseButtons.Left && wasGroupingDrag) {
                    if(droppedTab != null) {
                        HandleGroupDrop(droppedTab, dropTarget);
                    }
                    else if(droppedGroup != null) {
                        // Check if mouse is outside control bounds using screen coordinates
                        Point screenPos = PointToScreen(e.Location);
                        Rectangle screenBounds = RectangleToScreen(ClientRectangle);
                        if (!screenBounds.Contains(screenPos)) {
                            HandleGroupToNewWindow(droppedGroup);
                        } else {
                            HandleGroupReorder(droppedGroup, e.Location);
                        }
                    }
                }
                else if(e.Button == MouseButtons.Left && droppedGroup != null && !wasGroupingDrag) {
                    // This was a click without drag - toggle the group
                    ToggleGroup(droppedGroup.Name);
                }
                if(((fDrawCloseButton && (e.Button != MouseButtons.Right)) && ((CloseButtonClicked != null) && (tabMouseOn != null))) && (!tabMouseOn.TabLocked && fMouseDownOnCloseBtn && HitTestOnButtons(tabMouseOn.TabBounds, e.Location, true, num == iSelectedIndex))) {
                    if(e.Button == MouseButtons.Left) {
                        fMouseDownOnCloseBtn = false; // Reset tracking after close
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
                    if(Config.Tabs.ShowTabIslands) {
                        DrawGroupIndicators(e.Graphics);
                    }
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
                        // Find the last visible tab for positioning the plus button
                        Rectangle plusRect = Rectangle.Empty;
                        for(int i = tabPages.Count - 1; i >= 0; i--) {
                            Rectangle tabRect = GetItemRectangle(i);
                            if(!tabRect.IsEmpty && !tabPages[i].CollapsedByGroup) {
                                plusRect = tabRect;
                                break;
                            }
                        }
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
                        // Find the last visible tab for positioning the plus button
                        Rectangle plusButtonRect = Rectangle.Empty;
                        for(int k = tabPages.Count - 1; k >= 0; k--) {
                            QTabItem tab = tabPages[k];
                            if(!tab.CollapsedByGroup && !tab.TabBounds.IsEmpty) {
                                plusButtonRect = tab.TabBounds;
                                break;
                            }
                        }
                        if(!plusButtonRect.IsEmpty) {
                            DrawPlusButton(e.Graphics,plusButtonRect);
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
                tabs = new QTabItem[0];
            }
            IList<QTabItem> tabList = tabs as IList<QTabItem> ?? new List<QTabItem>(tabs);

            // Check if this assignment would split existing islands
            if (WouldSplitExistingIslands(tabList)) {
                // Don't allow the assignment - could optionally show a message here
                QTUtility.SoundPlay(); // Play error sound to indicate invalid operation
                return;
            }

            TabGroupState state;
            bool isNewGroup = !groupStates.TryGetValue(groupName, out state);
            if(isNewGroup) {
                state = new TabGroupState { Name = groupName, IsCollapsed = false }; // Ensure new groups are visible
                groupStates[groupName] = state;
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

            // Force immediate visual update for new/updated groups
            UpdateGroupIslandGeometry();
            Invalidate();
            Update(); // Ensure immediate repaint
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

                // Remove invalid tabs but maintain group membership for valid ones
                List<QTabItem> validTabs = new List<QTabItem>();
                foreach(QTabItem tab in state.Tabs) {
                    if(tab != null && tabPages.Contains(tab)) {
                        // Ensure the tab maintains its group assignment
                        tab.GroupKey = state.Name;
                        validTabs.Add(tab);
                    }
                }

                state.Tabs.Clear();
                state.Tabs.AddRange(validTabs);
                state.Tabs.Sort((a, b) => tabPages.IndexOf(a).CompareTo(tabPages.IndexOf(b)));
                ApplyGroupCollapseState(state);
                UpdateGroupAccent(state);
            }
        }

        private bool UpdateGroupAccent(TabGroupState state) {
            if(state == null) {
                return false;
            }
            Color accent;
            bool useTagColor = TryResolveSharedTagAccent(state, out accent);
            if(!useTagColor) {
                accent = ResolveGroupAccent(state.Name);
            }
            if(!state.AccentColor.IsEmpty && ColorsEqual(state.AccentColor, accent)) {
                return false;
            }
            state.AccentColor = accent;
            return true;
        }

        private static bool TryResolveSharedTagAccent(TabGroupState state, out Color accent) {
            accent = Color.Empty;
            if(state == null || state.Tabs == null || state.Tabs.Count == 0) {
                return false;
            }
            Color? candidate = null;
            foreach(QTabItem tab in state.Tabs) {
                if(tab == null) {
                    continue;
                }
                Color? tagColor = tab.TagTextColor;
                if(!tagColor.HasValue && !string.IsNullOrEmpty(tab.CurrentPath)) {
                    tagColor = TagManager.GetTagColorForPath(tab.CurrentPath);
                }
                if(!tagColor.HasValue) {
                    return false;
                }
                if(!candidate.HasValue) {
                    candidate = tagColor;
                    continue;
                }
                if(candidate.Value.ToArgb() != tagColor.Value.ToArgb()) {
                    return false;
                }
            }
            if(candidate.HasValue) {
                accent = candidate.Value;
                return true;
            }
            return false;
        }

        private static Color GetGroupAccentColor(TabGroupState state) {
            if(state != null && !state.AccentColor.IsEmpty) {
                return state.AccentColor;
            }
            // Check if the group has a custom island color set
            if(state != null && !string.IsNullOrEmpty(state.Name)) {
                Group group = GroupsManager.GetGroup(state.Name);
                if(group != null && group.IslandColor.HasValue) {
                    return group.IslandColor.Value;
                }
            }
            string name = state != null ? state.Name : null;
            return ResolveGroupAccent(name);
        }

        private static bool ColorsEqual(Color left, Color right) {
            return left.ToArgb() == right.ToArgb();
        }

        private void TagManager_TagVisualChanged(object sender, TagVisualChangedEventArgs e) {
            if(IsDisposed) {
                return;
            }
            if(InvokeRequired) {
                try {
                    BeginInvoke(new Action(() => HandleTagVisualChanged(e)));
                }
                catch { }
                return;
            }
            HandleTagVisualChanged(e);
        }

        private void HandleTagVisualChanged(TagVisualChangedEventArgs e) {
            if(IsDisposed) {
                return;
            }
            bool invalidate = false;
            foreach(TabGroupState state in groupStates.Values) {
                if(state == null || state.Tabs == null || state.Tabs.Count == 0) {
                    continue;
                }
                if(e != null && !e.RequiresFullRefresh) {
                    bool relevant = false;
                    foreach(QTabItem tab in state.Tabs) {
                        if(tab == null) {
                            continue;
                        }
                        string path = tab.CurrentPath;
                        if(string.IsNullOrEmpty(path)) {
                            continue;
                        }
                        if(e.AffectsPath(path)) {
                            relevant = true;
                            break;
                        }
                    }
                    if(!relevant) {
                        continue;
                    }
                }
                if(UpdateGroupAccent(state)) {
                    invalidate = true;
                }
            }
            if(invalidate) {
                Invalidate();
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
            // Force layout recalculation after collapse/expand
            UpdateGroupIslandGeometry();
            Invalidate();
            Update();
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
                Color accent = GetGroupAccentColor(state);
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

                        // Ensure all edges are properly visible
                        using(Pen thickPen = new Pen(borderColor, 1.5f)) {
                            // Draw all four edges to ensure visibility
                            g.DrawLine(thickPen, background.Left, background.Top, background.Right, background.Top); // Top
                            g.DrawLine(thickPen, background.Left, background.Bottom - 1, background.Right, background.Bottom - 1); // Bottom
                            g.DrawLine(thickPen, background.Left, background.Top, background.Left, background.Bottom); // Left
                            g.DrawLine(thickPen, background.Right - 1, background.Top, background.Right - 1, background.Bottom); // Right
                        }
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
                        int anchorRailHeight = Math.Max(anchor.Height - 4, 4);
                        int railX = anchor.Left + Math.Max((anchor.Width - GROUP_RAIL_WIDTH) / 2, 0);
                        state.RailBounds = new Rectangle(railX, anchor.Top + 2, GROUP_RAIL_WIDTH, anchorRailHeight);
                    }
                    else if(state.RailBounds.IsEmpty) {
                        state.RailBounds = Rectangle.Empty;
                    }
                    state.IndicatorBounds = state.RailBounds;
                    continue;
                }
                // Calculate rail position first to ensure proper alignment
                int railXBase = anchor.Width > 0
                        ? anchor.Left + Math.Max((anchor.Width - GROUP_RAIL_WIDTH) / 2, 0)
                        : union.Left - GROUP_RAIL_WIDTH - 4;
                railXBase = Math.Min(railXBase, union.Left - 2);
                int railHeight = Math.Max(union.Height - 4, 4);
                state.RailBounds = new Rectangle(railXBase, union.Top + 2, GROUP_RAIL_WIDTH, railHeight);

                // Calculate island bounds to align with rail top and bottom
                int islandLeft = Math.Max(railXBase + GROUP_RAIL_WIDTH, union.Left - GROUP_ISLAND_PADDING);
                int islandRight = union.Right + GROUP_ISLAND_PADDING;
                int islandTop = state.RailBounds.Top; // Align with rail top
                int islandBottom = state.RailBounds.Bottom; // Align with rail bottom
                state.IslandBounds = new Rectangle(islandLeft, islandTop, Math.Max(islandRight - islandLeft, 4), Math.Max(islandBottom - islandTop, 4));
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

                // Check if adding this tab would split existing islands
                if (WouldSplitExistingIslands(members)) {
                    // Don't allow adding the tab - play error sound for feedback
                    QTUtility.SoundPlay();
                    return;
                }

                AssignGroupTabs(state.Name, members);
            }
            // Ensure the group is visible when adding tabs
            if(state.IsCollapsed) {
                state.IsCollapsed = false;
                ApplyGroupCollapseState(state);
            }
            EnsureSelectionForCollapsedGroups();

            // Force immediate visual update
            UpdateGroupIslandGeometry();
            Invalidate();
            Update(); // Ensure immediate repaint
        }

        // Check if assigning tabs to a group would split existing islands
        private bool WouldSplitExistingIslands(IList<QTabItem> newGroupTabs) {
            if (newGroupTabs == null || newGroupTabs.Count == 0) {
                return false;
            }

            // Get indices of the tabs that would form the new group
            List<int> newGroupIndices = new List<int>();
            foreach (QTabItem tab in newGroupTabs) {
                if (tab != null && tabPages.Contains(tab)) {
                    int index = tabPages.IndexOf(tab);
                    if (index >= 0) {
                        newGroupIndices.Add(index);
                    }
                }
            }

            if (newGroupIndices.Count == 0) {
                return false;
            }

            newGroupIndices.Sort();

            // Check if the new group would split any existing islands
            foreach (var existingState in groupStates.Values) {
                if (existingState == null || existingState.Tabs == null || existingState.Tabs.Count <= 1) {
                    continue;
                }

                // Get indices of existing group tabs
                List<int> existingIndices = new List<int>();
                foreach (QTabItem existingTab in existingState.Tabs) {
                    if (existingTab != null && tabPages.Contains(existingTab) && !newGroupTabs.Contains(existingTab)) {
                        int index = tabPages.IndexOf(existingTab);
                        if (index >= 0) {
                            existingIndices.Add(index);
                        }
                    }
                }

                if (existingIndices.Count <= 1) {
                    continue; // Can't split a group with 1 or fewer remaining tabs
                }

                existingIndices.Sort();

                // Check if new group indices would split the existing group
                if (WouldIndicesSplitGroup(existingIndices, newGroupIndices)) {
                    return true;
                }
            }

            return false;
        }

        private bool WouldIndicesSplitGroup(List<int> existingGroupIndices, List<int> newGroupIndices) {
            if (existingGroupIndices.Count <= 1 || newGroupIndices.Count == 0) {
                return false;
            }

            // Check if any new group tabs would be inserted between existing group tabs
            int minExisting = existingGroupIndices.Min();
            int maxExisting = existingGroupIndices.Max();

            foreach (int newIndex in newGroupIndices) {
                if (newIndex > minExisting && newIndex < maxExisting) {
                    // Check if this new tab would actually split the existing group
                    // by being between two existing group tabs
                    for (int i = 0; i < existingGroupIndices.Count - 1; i++) {
                        if (newIndex > existingGroupIndices[i] && newIndex < existingGroupIndices[i + 1]) {
                            return true; // New tab would split existing group
                        }
                    }
                }
            }

            return false;
        }

        // Check if moving a tab would split an island group
        internal bool WouldSplitIslandGroup(int sourceIndex, int destIndex) {
            if (sourceIndex == destIndex || sourceIndex < 0 || destIndex < 0) {
                return false;
            }
            if (sourceIndex >= tabPages.Count || destIndex >= tabPages.Count) {
                return false;
            }

            QTabItem sourceTab = tabPages[sourceIndex];
            if (sourceTab == null) {
                return false;
            }

            // If source tab has no group, it can move anywhere without splitting groups
            string sourceGroupKey = sourceTab.GroupKey;
            if (string.IsNullOrEmpty(sourceGroupKey)) {
                // Check if inserting between island group members would split the group
                return WouldInsertBetweenIslandMembers(destIndex);
            }

            // If source tab is part of a group, check if moving it would split the group
            return WouldMoveBreakIslandContinuity(sourceIndex, destIndex, sourceGroupKey);
        }

        private bool WouldInsertBetweenIslandMembers(int destIndex) {
            // Allow inserting at position 0 (before everything)
            if (destIndex == 0) {
                return false;
            }

            // Allow inserting at the end
            if (destIndex >= tabPages.Count) {
                return false;
            }

            QTabItem leftTab = destIndex > 0 ? tabPages[destIndex - 1] : null;
            QTabItem rightTab = destIndex < tabPages.Count ? tabPages[destIndex] : null;

            // Need both adjacent tabs to check for splitting
            if (leftTab == null || rightTab == null) {
                return false;
            }

            // Check if we're inserting between two tabs of the same island group
            string leftGroup = leftTab.GroupKey;
            string rightGroup = rightTab.GroupKey;

            if (string.IsNullOrEmpty(leftGroup) || string.IsNullOrEmpty(rightGroup)) {
                return false;
            }

            // If both tabs belong to the same non-empty group, inserting between them would split the group
            return leftGroup == rightGroup;
        }

        private bool WouldMoveBreakIslandContinuity(int sourceIndex, int destIndex, string groupKey) {
            // Get all tabs in the same group
            var groupTabs = new List<int>();
            for (int i = 0; i < tabPages.Count; i++) {
                if (tabPages[i] != null && tabPages[i].GroupKey == groupKey) {
                    groupTabs.Add(i);
                }
            }

            if (groupTabs.Count <= 1) {
                return false; // Single tab or no group can't be split
            }

            // Check if the group is currently contiguous
            groupTabs.Sort();
            bool isContiguous = true;
            for (int i = 1; i < groupTabs.Count; i++) {
                if (groupTabs[i] != groupTabs[i - 1] + 1) {
                    isContiguous = false;
                    break;
                }
            }

            if (!isContiguous) {
                return false; // Group is already split, allow movement
            }

            // Simulate the move and check if group remains contiguous
            var newPositions = new List<int>(groupTabs);
            newPositions.Remove(sourceIndex);

            // Adjust destination index after removal
            int adjustedDestIndex = destIndex;
            if (sourceIndex < destIndex) {
                adjustedDestIndex--;
            }

            // Insert at new position
            if (adjustedDestIndex >= newPositions.Count) {
                newPositions.Add(adjustedDestIndex);
            } else {
                newPositions.Insert(FindInsertionPoint(newPositions, adjustedDestIndex), adjustedDestIndex);
            }

            // Check if new positions are contiguous
            newPositions.Sort();
            for (int i = 1; i < newPositions.Count; i++) {
                if (newPositions[i] != newPositions[i - 1] + 1) {
                    return true; // Would break continuity
                }
            }

            return false; // Would not break continuity
        }

        private int FindInsertionPoint(List<int> sortedList, int value) {
            for (int i = 0; i < sortedList.Count; i++) {
                if (sortedList[i] > value) {
                    return i;
                }
            }
            return sortedList.Count;
        }

        private void HandleGroupDrop(QTabItem tab, TabGroupState target) {
            if(tab == null) {
                return;
            }

            // If no target specified, check if we're dropping onto an island surface
            if(target == null) {
                Point mousePos = PointToClient(MousePosition);
                target = HitTestGroupSurface(mousePos);
            }

            if(target != null) {
                AddTabToGroup(target, tab);
            }
            else if(!string.IsNullOrEmpty(tab.GroupKey)) {
                RemoveTabFromGroups(tab);
                CleanupEmptyGroups();
                SyncGroupOrder();
                EnsureSelectionForCollapsedGroups();
                Invalidate();
            }
            else {
                // Handle regular tab reordering (non-group drops)
                Point mousePos = PointToClient(MousePosition);
                int dropIndex = GetDropIndex(mousePos);
                int currentIndex = tabPages.IndexOf(tab);

                // Check for invalid drop location (outside tab bar area)
                if (dropIndex == -1) {
                    // Dropped outside tab bar area - open in new window
                    try {
                        // Create new window with the tab's current location
                        if (tab.CurrentIDL != null && tab.CurrentIDL.Length > 0) {
                            using (IDLWrapper wrapper = new IDLWrapper(tab.CurrentIDL)) {
                                if (wrapper.Available) {
                                    // Notify parent to open new window (this will be handled by QTTabBarClass)
                                    if (OnTabDraggedToNewWindow != null) {
                                        OnTabDraggedToNewWindow(wrapper);
                                    }

                                    // Remove the tab from current window since it's moving to new window
                                    if (tabPages.Count > 1) {
                                        tabPages.Remove(tab);
                                        Invalidate();
                                    }
                                }
                            }
                        }
                        else {
                            // Fallback to path-based creation
                            using (IDLWrapper wrapper = new IDLWrapper(tab.CurrentPath)) {
                                if (wrapper.Available) {
                                    if (OnTabDraggedToNewWindow != null) {
                                        OnTabDraggedToNewWindow(wrapper);
                                    }
                                    if (tabPages.Count > 1) {
                                        tabPages.Remove(tab);
                                        Invalidate();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex) {
                        QTUtility2.MakeErrorLog(ex, "HandleGroupDrop new window creation");
                    }
                    return;
                }

                // Validate drop index bounds to prevent ArgumentOutOfRangeException
                if (currentIndex >= 0 && currentIndex < tabPages.Count &&
                    dropIndex >= 0 && dropIndex <= tabPages.Count &&
                    dropIndex != currentIndex) {

                    // Clamp dropIndex to valid range
                    dropIndex = Math.Max(0, Math.Min(dropIndex, tabPages.Count));

                    // Special handling for position 0 - always allow dropping before everything
                    if (dropIndex == 0) {
                        try {
                            tabPages.Relocate(currentIndex, dropIndex);
                        }
                        catch (ArgumentOutOfRangeException ex) {
                            QTUtility2.MakeErrorLog(ex, "HandleGroupDrop Relocate to 0");
                        }
                    }
                    else if (!WouldSplitIslandGroup(currentIndex, dropIndex)) {
                        try {
                            tabPages.Relocate(currentIndex, dropIndex);
                        }
                        catch (ArgumentOutOfRangeException ex) {
                            QTUtility2.MakeErrorLog(ex, "HandleGroupDrop Relocate general");
                        }
                    }
                }
            }
        }

        private int GetDropIndex(Point location) {
            // Determine the drop index based on mouse position
            // Return -1 if location is invalid/outside tab area

            if (tabPages.Count == 0) {
                return 0;
            }

            // Check if location is within reasonable bounds of the control
            Rectangle controlBounds = new Rectangle(0, 0, Width, Height);
            controlBounds.Inflate(100, 50); // Allow more tolerance for drag operations and new window creation
            if (!controlBounds.Contains(location)) {
                return -1; // Invalid drop location (triggers new window creation)
            }

            // Check if we're before the first tab (including island indicators)
            Rectangle firstTabRect = GetTabRect(0, false);
            if (!firstTabRect.IsEmpty) {
                // Check for island indicators before first tab
                int leftmostX = firstTabRect.Left;
                foreach (var state in groupStates.Values) {
                    if (state != null && !state.AnchorBounds.IsEmpty) {
                        Rectangle anchor = state.AnchorBounds;
                        if ((iMultipleType == 0) && fNeedToDrawUpDown) {
                            anchor.Offset(iScrollWidth, 0);
                        }
                        leftmostX = Math.Min(leftmostX, anchor.Left);
                    }
                }

                // If mouse is before the leftmost element, insert at position 0
                if (location.X < leftmostX + 10) { // Small buffer for easier targeting
                    return 0;
                }
            }

            // Check each tab to find insertion point
            for (int i = 0; i < tabPages.Count; i++) {
                Rectangle tabRect = GetTabRect(i, false);
                if (tabRect.IsEmpty) continue;

                if (location.X < tabRect.Left + tabRect.Width / 2) {
                    return i; // Insert before this tab
                }
            }

            // Make sure we don't return an invalid index
            return Math.Min(tabPages.Count, tabPages.Count);
        }

        private bool TryHandleGroupIndicatorClick(Point location) {
            UpdateGroupIslandGeometry();
            foreach(var state in groupStates.Values) {
                Rectangle railRect = state.RailBounds;
                Rectangle islandRect = state.IslandBounds;

                // Apply scroll offset for drawing
                if((iMultipleType == 0) && fNeedToDrawUpDown) {
                    if(!railRect.IsEmpty) {
                        railRect.Offset(iScrollWidth, 0);
                    }
                    if(!islandRect.IsEmpty) {
                        islandRect.Offset(iScrollWidth, 0);
                    }
                }

                bool hitRail = !railRect.IsEmpty && railRect.Contains(location);
                bool hitIsland = !state.IsCollapsed && !islandRect.IsEmpty && islandRect.Contains(location);

                if(hitRail || hitIsland) {
                    // Check if this is a rail click (for toggling) vs island drag
                    if(hitRail && !hitIsland) {
                        // Click directly on rail indicator - this will toggle if no drag occurs
                        draggingGroup = state;
                        groupingDragOrigin = location;
                        groupingDragActive = false;
                        return true;
                    } else if(hitIsland) {
                        // Click on expanded island area - this enables dragging the whole group
                        draggingGroup = state;
                        groupingDragOrigin = location;
                        groupingDragActive = false;
                        return true;
                    }
                }
            }
            return false;
        }

        private TabGroupState GetGroupDropTarget(Point location) {
            // For group drops, we need to find valid insertion points between other groups or tabs
            // Check if we're dropping on another group's island area
            foreach(var state in groupStates.Values) {
                if(state == null || state == draggingGroup) {
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
                // Check if dropping on island area (for merging) or rail area (for position indication)
                if((!island.IsEmpty && island.Contains(location)) || (!rail.IsEmpty && rail.Contains(location))) {
                    return state;
                }
            }
            // Return null if not dropping on another group (indicates reordering to empty space)
            return null;
        }

        private void HandleGroupReorder(TabGroupState group, Point dropLocation) {
            if(group == null || group.Tabs == null || group.Tabs.Count == 0) {
                return;
            }

            // Check if mouse is outside the control bounds - create new window if so
            Rectangle controlBounds = new Rectangle(0, 0, Width, Height);
            if (!controlBounds.Contains(dropLocation)) {
                HandleGroupToNewWindow(group);
                return;
            }

            // Get the drop index based on location
            int dropIndex = GetDropIndex(dropLocation);

            // Get all tabs in the group
            List<QTabItem> groupTabs = new List<QTabItem>(group.Tabs);
            groupTabs.RemoveAll(tab => tab == null || !tabPages.Contains(tab));

            if(groupTabs.Count == 0) {
                return;
            }

            // Sort by current position
            groupTabs.Sort((a, b) => tabPages.IndexOf(a).CompareTo(tabPages.IndexOf(b)));

            // Find the current range of the group
            int firstIndex = tabPages.IndexOf(groupTabs[0]);
            int lastIndex = tabPages.IndexOf(groupTabs[groupTabs.Count - 1]);

            // Don't move if dropping within the group's current range
            if(dropIndex >= firstIndex && dropIndex <= lastIndex + 1) {
                return;
            }

            // Store the group's original collapse state
            bool wasCollapsed = group.IsCollapsed;
            string groupName = group.Name;

            // Remove all group tabs from their current positions (in reverse order)
            for(int i = groupTabs.Count - 1; i >= 0; i--) {
                tabPages.Remove(groupTabs[i]);
            }

            // Adjust drop index if it was after removed tabs
            int adjustedDropIndex = dropIndex;
            if(dropIndex > firstIndex) {
                adjustedDropIndex -= groupTabs.Count;
            }

            // Ensure drop index is valid
            if(adjustedDropIndex < 0) {
                adjustedDropIndex = 0;
            }
            if(adjustedDropIndex > tabPages.Count) {
                adjustedDropIndex = tabPages.Count;
            }

            // Insert all group tabs at the new position
            for(int i = 0; i < groupTabs.Count; i++) {
                tabPages.Insert(adjustedDropIndex + i, groupTabs[i]);
            }

            // Restore the group state and ensure tabs maintain their group assignment
            TabGroupState restoredState;
            if(groupStates.TryGetValue(groupName, out restoredState)) {
                restoredState.IsCollapsed = wasCollapsed;
                restoredState.Tabs.Clear();
                restoredState.Tabs.AddRange(groupTabs);

                // Re-assign group keys to all tabs to ensure they stay grouped
                foreach(QTabItem tab in groupTabs) {
                    if(tab != null) {
                        tab.GroupKey = groupName;
                        tab.CollapsedByGroup = wasCollapsed;
                    }
                }

                // Apply the collapse state
                ApplyGroupCollapseState(restoredState);
            }

            // Update the selected index if it was affected
            if(iSelectedIndex >= 0 && iSelectedIndex < tabPages.Count) {
                QTabItem selectedTab = tabPages[iSelectedIndex];
                if(groupTabs.Contains(selectedTab)) {
                    iSelectedIndex = tabPages.IndexOf(selectedTab);
                }
            }

            // Force complete refresh to ensure group indicators are redrawn
            SyncGroupOrder();
            UpdateGroupIslandGeometry();
            EnsureSelectionForCollapsedGroups();
            Invalidate();
            Update(); // Force immediate repaint
        }

        private void HandleGroupToNewWindow(TabGroupState group) {
            if (group == null || group.Tabs == null || group.Tabs.Count == 0) {
                return;
            }

            try {
                // Get all tabs in the group that are still valid
                List<QTabItem> groupTabs = new List<QTabItem>(group.Tabs);
                groupTabs.RemoveAll(tab => tab == null || !tabPages.Contains(tab));

                if (groupTabs.Count == 0) {
                    return;
                }

                // Create paths for new window
                List<string> paths = new List<string>();
                foreach (QTabItem tab in groupTabs) {
                    if (tab != null && !string.IsNullOrEmpty(tab.CurrentPath)) {
                        paths.Add(tab.CurrentPath);
                    }
                }

                if (paths.Count == 0) {
                    return;
                }

                // Use the first tab's path to create the new window via event
                if (OnTabDraggedToNewWindow != null && paths.Count > 0) {
                    using (IDLWrapper wrapper = new IDLWrapper(paths[0])) {
                        if (wrapper.Available) {
                            // Store additional paths to be opened in the new window
                            if (paths.Count > 1) {
                                for (int i = 1; i < paths.Count; i++) {
                                    StaticReg.CreateWindowPaths.Add(paths[i]);
                                }
                            }

                            // Notify parent to create new window
                            OnTabDraggedToNewWindow(wrapper);

                            // Remove tabs from current window
                            foreach (QTabItem tab in groupTabs) {
                                if (tab != null && tabPages.Contains(tab)) {
                                    tabPages.Remove(tab);
                                }
                            }

                            // Clean up group state
                            if (groupStates.ContainsKey(group.Name)) {
                                groupStates.Remove(group.Name);
                            }

                            // Update display
                            SyncGroupOrder();
                            UpdateGroupIslandGeometry();
                            EnsureSelectionForCollapsedGroups();
                            Invalidate();
                        }
                    }
                }
            }
            catch (Exception ex) {
                QTUtility2.MakeErrorLog(ex, "HandleGroupToNewWindow");
            }
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
                // Validate indices
                if (indexSource < 0 || indexSource >= Count ||
                    indexDestination < 0 || indexDestination > Count) {
                    return; // Invalid indices
                }

                if (indexSource == indexDestination) {
                    return; // No move needed
                }

                // Check if this move would split an island group
                if (Owner.WouldSplitIslandGroup(indexSource, indexDestination)) {
                    return; // Don't allow moves that would split island groups
                }

                int selectedIndex = Owner.SelectedIndex;
                int num2 = (indexSource > indexDestination) ? indexSource : indexDestination;
                int num3 = (indexSource > indexDestination) ? indexDestination : indexSource;
                QTabItem item = base[indexSource];

                // Calculate the adjusted destination index after removal
                int adjustedDestination = indexDestination;
                if (indexSource < indexDestination) {
                    adjustedDestination--; // Destination shifts left after removal
                }

                base.Remove(item);

                // Ensure adjusted destination is still valid
                if (adjustedDestination > Count) {
                    adjustedDestination = Count;
                }

                base.Insert(adjustedDestination, item);
                if((num2 >= selectedIndex) && (selectedIndex >= num3)) {
                    if(num2 == selectedIndex) {
                        if(num2 == indexSource) {
                            Owner.SelectedIndex = adjustedDestination;
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
                            Owner.SelectedIndex = adjustedDestination;
                        }
                    }
                }
                Owner.OnTabsReordered();
                Owner.Refresh();
            }
        }

        private void CreateDragPreview(QTabItem tab) {
            if (tab == null || dragPreviewForm != null) {
                return;
            }

            try {
                // Create a bitmap of the tab
                Rectangle tabBounds = GetTabRect(tabPages.IndexOf(tab), true);
                if (tabBounds.Width <= 0 || tabBounds.Height <= 0) {
                    return;
                }

                Bitmap tabBitmap = new Bitmap(tabBounds.Width, tabBounds.Height);
                using (Graphics g = Graphics.FromImage(tabBitmap)) {
                    g.Clear(Color.Transparent);

                    // Draw the tab on the bitmap
                    Rectangle drawRect = new Rectangle(0, 0, tabBounds.Width, tabBounds.Height);
                    int tabIndex = tabPages.IndexOf(tab);
                    DrawTab(g, drawRect, tabIndex, null, true);
                }

                // Create translucent form
                dragPreviewForm = new Form {
                    FormBorderStyle = FormBorderStyle.None,
                    BackColor = Color.Magenta,
                    TransparencyKey = Color.Magenta,
                    TopMost = true,
                    ShowInTaskbar = false,
                    Size = tabBounds.Size,
                    Opacity = 0.7
                };

                // Set the bitmap as background
                dragPreviewForm.BackgroundImage = tabBitmap;
                dragPreviewForm.BackgroundImageLayout = ImageLayout.None;

                // Position at cursor
                Point cursorPos = Cursor.Position;
                dragPreviewForm.Location = new Point(
                    cursorPos.X - tabBounds.Width / 2,
                    cursorPos.Y - tabBounds.Height / 2);

                dragPreviewForm.Show();
                showDragPreview = true;
            }
            catch {
                DisposeDragPreview();
            }
        }

        private void CreateGroupDragPreview(TabGroupState groupState) {
            if (groupState == null || groupState.Tabs == null || groupState.Tabs.Count == 0 || dragPreviewForm != null) {
                return;
            }

            try {
                // Calculate bounds for the entire island (all tabs in the group)
                Rectangle totalBounds = Rectangle.Empty;
                List<Rectangle> tabBounds = new List<Rectangle>();

                foreach (QTabItem tab in groupState.Tabs) {
                    if (tab == null || !tabPages.Contains(tab)) continue;

                    int tabIndex = tabPages.IndexOf(tab);
                    Rectangle bounds = GetTabRect(tabIndex, true);
                    if (bounds.Width > 0 && bounds.Height > 0) {
                        tabBounds.Add(bounds);
                        if (totalBounds.IsEmpty) {
                            totalBounds = bounds;
                        } else {
                            totalBounds = Rectangle.Union(totalBounds, bounds);
                        }
                    }
                }

                if (totalBounds.Width <= 0 || totalBounds.Height <= 0 || tabBounds.Count == 0) {
                    return;
                }

                // Create bitmap for the island
                Bitmap islandBitmap = new Bitmap(totalBounds.Width, totalBounds.Height);
                using (Graphics g = Graphics.FromImage(islandBitmap)) {
                    g.Clear(Color.Transparent);

                    // Draw each tab in the group
                    foreach (QTabItem tab in groupState.Tabs) {
                        if (tab == null || !tabPages.Contains(tab)) continue;

                        int tabIndex = tabPages.IndexOf(tab);
                        Rectangle bounds = GetTabRect(tabIndex, true);
                        if (bounds.Width > 0 && bounds.Height > 0) {
                            Rectangle drawRect = new Rectangle(
                                bounds.X - totalBounds.X,
                                bounds.Y - totalBounds.Y,
                                bounds.Width,
                                bounds.Height);
                            DrawTab(g, drawRect, tabIndex, null, true);
                        }
                    }

                    // Draw group rail/indicator
                    Color accent = ResolveGroupAccent(groupState.Name);
                    Rectangle railRect = new Rectangle(0, totalBounds.Height - 6, totalBounds.Width, 6);
                    DrawGroupRail(g, railRect, accent, false, false);
                }

                // Create translucent form
                dragPreviewForm = new Form {
                    FormBorderStyle = FormBorderStyle.None,
                    BackColor = Color.Magenta,
                    TransparencyKey = Color.Magenta,
                    TopMost = true,
                    ShowInTaskbar = false,
                    Size = totalBounds.Size,
                    Opacity = 0.7
                };

                // Set the bitmap as background
                dragPreviewForm.BackgroundImage = islandBitmap;
                dragPreviewForm.BackgroundImageLayout = ImageLayout.None;

                // Position at cursor
                Point cursorPos = Cursor.Position;
                dragPreviewForm.Location = new Point(
                    cursorPos.X - totalBounds.Width / 2,
                    cursorPos.Y - totalBounds.Height / 2);

                dragPreviewForm.Show();
                showDragPreview = true;
            }
            catch {
                DisposeDragPreview();
            }
        }

        private void UpdateDragPreview(Point screenLocation) {
            if (dragPreviewForm != null && showDragPreview) {
                try {
                    dragPreviewForm.Location = new Point(
                        screenLocation.X - dragPreviewForm.Width / 2,
                        screenLocation.Y - dragPreviewForm.Height / 2);
                }
                catch {
                    DisposeDragPreview();
                }
            }
        }

        private void DisposeDragPreview() {
            showDragPreview = false;
            if (dragPreviewForm != null) {
                try {
                    if (dragPreviewForm.BackgroundImage != null) {
                        dragPreviewForm.BackgroundImage.Dispose();
                    }
                    dragPreviewForm.Close();
                    dragPreviewForm.Dispose();
                }
                catch { }
                finally {
                    dragPreviewForm = null;
                }
            }
        }
    }


}
