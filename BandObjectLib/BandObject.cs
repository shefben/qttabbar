//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2002-2022  Pavel Zolnikov, Quizo, Paul Accisano, indiff
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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SHDocVw;


namespace BandObjectLib {
    /// <summary>
    /// https://docs.microsoft.com/en-us/windows/win32/shell/band-objects
    /// https://docs.microsoft.com/en-us/previous-versions/windows/desktop/legacy/cc144099(v=vs.85)?redirectedfrom=MSDN
    /// https://docs.microsoft.com/zh-cn/cpp/mfc/rebar-controls-and-bands?view=msvc-160
    /// </summary>
    public class BandObject : 
        UserControl, 
        IDeskBand, 
        IDockingWindow, 
        IInputObject, 
        IObjectWithSite, 
        IOleWindow,
        IPersistStream
        // , IDpiAwareObject
    {
        /***
         *
         *
         *    // ��ֱ��Դ��������	CATID_InfoBand
            // ˮƽ��Դ��������	CATID_CommBand
            // ����	CATID_DeskBand
         *
         *
         */
        private Size minSize = new Size(16, 26);
        private Size maxSize = new Size(-1, -1);

        protected IInputObjectSite BandObjectSite;
        protected WebBrowserClass Explorer;
        protected bool fClosedDW;
        protected bool fFinalRelease;
        protected IntPtr ReBarHandle;
        private RebarBreakFixer RebarSubclass;
        private IAsyncResult result;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        protected const int S_OK = 0;
        protected const int S_FALSE = 1;
        protected const int E_NOTIMPL = -2147467263;	// _HRESULT_TYPEDEF_(0x80004001L)
        protected const int E_FAIL = -2147467259;    // _HRESULT_TYPEDEF_(0x80004005L)

        public static string ProcessName = Process.GetCurrentProcess().ProcessName;

        internal static bool HostedNotByExplorer = (ProcessName != "explorer");

        // �ж��Ƿ�������־��������Ϊfalse�� ��������. Ĭ���ǹرյģ��ڳ���ѡ�����������������
        // public static bool ENABLE_LOGGER = true;

        // We must subclass the rebar in order to fix a certain bug in 
        // Windows 7.
        internal sealed class RebarBreakFixer : NativeWindow {
            private readonly BandObject parent;
            public bool MonitorSetInfo { get; set; }
            public bool Enabled { get; set; }

            public RebarBreakFixer(IntPtr hwnd, BandObject parent) {
                this.parent = parent;
                Enabled = true;
                MonitorSetInfo = true;
                AssignHandle(hwnd);
            }

            protected override void WndProc(ref Message m) {
                // bandLog("WndProc");
                if(!Enabled) {
                    base.WndProc(ref m);
                    return;
                }

                // When the bars are first loaded, they will always have 
                // RBBS_BREAK set.  Catch RB_SETBANDINFO to fix this.
                if(m.Msg == RB.SETBANDINFO) {
                    if(MonitorSetInfo && m.LParam != IntPtr.Zero) {
                        try {
                            Util2.bandLog("msg SETBANDINFO");
                            REBARBANDINFO pInfo = (REBARBANDINFO)Marshal.PtrToStructure(m.LParam, typeof(REBARBANDINFO));
                            if(pInfo.hwndChild == parent.Handle && (pInfo.fMask & RBBIM.STYLE) != 0) {
                                // Ask the bar if we actually want a break.
                                if(parent.ShouldHaveBreak()) {
                                    pInfo.fStyle |= RBBS.BREAK;
                                }
                                else {
                                    pInfo.fStyle &= ~RBBS.BREAK;
                                }
                                Marshal.StructureToPtr(pInfo, m.LParam, false);
                            }
                        }
                        catch(Exception ex) {
                            Util2.MakeErrorLog(ex, "BandObject.WndProc - Marshal operation failed");
                        }
                    }
                }
                // Whenever a band is deleted, the RBBS_BREAKs come back!
                // Catch RB_DELETEBAND to fix it.
                else if(m.Msg == RB.DELETEBAND) {
                    Util2.bandLog("msg DELETEBAND");
                    int del = (int)m.WParam;
                    
                    // Look for our band
                    int n = parent.ActiveRebarCount();
                    for(int i = 0; i < n; ++i) {
                        REBARBANDINFO info = parent.GetRebarBand(i, RBBIM.STYLE | RBBIM.CHILD);
                        if(info.hwndChild == parent.Handle) {
                            // Call the WndProc to let the deletion fall 
                            // through, with the break status safely saved
                            // in the info variable.
                            base.WndProc(ref m);

                            // If *we're* the one being deleted, no need to do
                            // anything else.
                            if(i == del) {
                                return;
                            }
                                
                            // Otherwise, our style has been messed with.
                            // Set it back to what it was.
                            info.cbSize = Marshal.SizeOf(info);
                            info.fMask = RBBIM.STYLE;
                            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(info));
                            Marshal.StructureToPtr(info, ptr, false);
                            bool reset = MonitorSetInfo;
                            MonitorSetInfo = false;
                            SendMessage(parent.ReBarHandle, RB.SETBANDINFO, (IntPtr)i, ptr);
                            MonitorSetInfo = reset;
                            Marshal.FreeHGlobal(ptr);

                            // Return without calling WndProc twice!
                            return;
                        }
                    }
                }
                base.WndProc(ref m);
            }
        }

        private int ActiveRebarCount() {
            return (int)SendMessage(ReBarHandle, RB.GETBANDCOUNT, IntPtr.Zero, IntPtr.Zero);
        }

        // Determines if the DeskBand is preceded by a break.
        protected bool BandHasBreak() {
            int n = ActiveRebarCount();
            for(int i = 0; i < n; ++i) {
                REBARBANDINFO info = GetRebarBand(i, RBBIM.STYLE | RBBIM.CHILD);
                if(info.hwndChild == Handle) {
                    return (info.fStyle & RBBS.BREAK) != 0;
                }
            }
            return true;
        }
        // virtual �ؼ��������޸ķ��������ԡ����������¼���������ʹ���ǿ������������б���д��
        public virtual void CloseDW(uint dwReserved) {
            Util2.bandLog("CloseDW");
            fClosedDW = true;
            ShowDW(false);
            Dispose(true);
            if(Explorer != null) {
                // Util2.bandLog("ReleaseComObject Explorer");
                Marshal.ReleaseComObject(Explorer);
                Explorer = null;
            }
            if(BandObjectSite != null) {
                Marshal.ReleaseComObject(BandObjectSite);
                BandObjectSite = null;
            }
            if(RebarSubclass != null) {
                RebarSubclass.Enabled = false;
                RebarSubclass = null;
            }
        }

        public virtual void ContextSensitiveHelp(bool fEnterMode) {
        }




        private int bandID;
        private bool fVertical;

        public int BandID
        {
            get
            {
                return this.bandID;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dwBandID">��������Ĵ��ı�ʶ���� �����Ҫ������������Ա�����ֵ��</param>
        /// <param name="dwViewMode">���������ͼģʽ�� ����ֵ֮һ��</param>
        /// DBIF_VIEWMODE_NORMAL
        /// ��״��������ˮƽ������ʾ��
        ///
        /// DBIF_VIEWMODE_VERTICAL
        ///     ��״������ʾ�ڴ�ֱ���С�
        ///
        /// DBIF_VIEWMODE_FLOATING
        ///     ��״�������ڸ���������ʾ��
        ///
        /// DBIF_VIEWMODE_TRANSPARENT
        ///     ��״��������͸��������ʾ��
        /// <param name="pdbi">���������Ϣ</param>
        /// ptMinSize
        /// 
        /// ���ͣ� POINTL
        /// 
        /// ���մ��������С��С�� POINTL �ṹ�� ��С������ POINTL �ṹ�� x ��Ա�и�������С�߶��� y ��Ա�и�����
        /// 
        /// ptMaxSize
        /// 
        /// ���ͣ� POINTL
        /// 
        /// һ�� POINTL �ṹ�������մ����������С�� POINTL �ṹ�� y ��Ա���ṩ�����߶ȣ�x ��Ա�������ԡ� ���������������߶�û�����ƣ�Ӧʹ�� (LONG) -1��
        /// 
        /// ptIntegral
        /// 
        /// ���ͣ� POINTL
        /// 
        /// һ�� POINTL �ṹ�������մ�С��������ֵ (����) �����е����˴�����Ĵ�С�� ��ֱ����ֵ�� POINTL �ṹ�� y ��Ա�� ������x ��Ա�������ԡ�
        /// 
        /// dwModeFlags ��Ա������� DBIMF_VARIABLEHEIGHT ��־;���򣬽����� ptIntegral��
        /// 
        /// ptActual
        /// 
        /// ���ͣ� POINTL
        /// 
        /// ���մ�����������С�� POINTL �ṹ�� ��������� POINTL �ṹ�� x ��Ա�и���������߶��� y ��Ա�и����� ����������ʹ����Щֵ�����޷���֤���δ�СΪ�˴�С��
        /// 
        /// wszTitle[256]
        /// 
        /// ���ͣ� WCHAR[256]
        /// 
        /// ���մ������ WCHAR ��������
        /// 
        /// dwModeFlags
        /// 
        /// ���ͣ�DWORD
        /// 
        /// һ�� ֵ����ֵ����һ��ָ�� band ����Ĳ���ģʽ�ı�־�� ����һ������ֵ��
        /// 
        /// DBIMF_NORMAL
        /// ��ʹ��Ĭ�����ԡ� ����ģʽ��־�޸Ĵ˱�־��
        /// 
        /// DBIMF_FIXED
        /// Windows XP �����߰汾�� ������Ĵ�С��λ�ù̶��� ʹ�ô˱�־ʱ�������ڴ���������ʾ��С�����ֱ���
        /// 
        /// DBIMF_FIXEDBMP
        /// Windows XP �����߰汾�� band ����ʹ�ù̶�λͼ (.bmp) �ļ���Ϊ�䱳���� ��ע�⣬������������¶�֧�ֱ�������˼�ʹ�����˴˱�־��Ҳ�����޷�����λͼ��
        /// 
        /// DBIMF_VARIABLEHEIGHT
        /// ���Ը��Ĵ�����ĸ߶ȡ� ptIntegral ��Ա���������������С�Ĳ���ֵ��
        /// 
        /// DBIMF_UNDELETEABLE
        /// Windows XP �����߰汾�� �޷����ֶ�������ɾ�� band ����
        /// 
        /// DBIMF_DEBOSSED
        /// �������԰��ݵ������ʾ��
        /// 
        /// DBIMF_BKCOLOR
        /// ʹ�� crBkgnd ��ָ���ı���ɫ��ʾ����
        /// 
        /// DBIMF_USECHEVRON
        /// Windows XP �����߰汾�� ����޷���ʾ (����������С�� ptActual�������ʾ V �Σ���ָʾ�и���Ŀ���ѡ� ���� V ��ʱ����ʾ��Щѡ�
        /// 
        /// DBIMF_BREAK
        /// Windows XP �����߰汾�� �ֶӶ�����ʾ�ڴ������е������С�
        /// 
        /// DBIMF_ADDTOFRONT
        /// Windows XP �����߰汾�� band �������ֶ������еĵ�һ������
        /// 
        /// DBIMF_TOPALIGN
        /// Windows XP �����߰汾�� band ������ʾ���ֶ������Ķ������С�
        /// 
        /// DBIMF_NOGRIPPER
        /// Windows Vista �����߰汾�� ������ʾ��С�����ֱ����������û��ƶ������������Ĵ�С��
        /// 
        /// DBIMF_ALWAYSGRIPPER
        /// Windows Vista �����߰汾�� ʼ����ʾ�����û��ƶ��ֶӶ����������С�Ĵ�С�ֱ�����ʹ�ô�������������Ψһ��һ����
        /// 
        /// DBIMF_NOMARGINS
        /// Windows Vista �����߰汾�� ������Ӧ��ʾ�߾ࡣ
        /// 
        /// crBkgnd
        /// 
        /// ���ͣ� COLORREF
        /// 
        /// ���մ��ı���ɫ�� COLORREF �ṹ�� dwModeFlags ��Ա������� DBIMF_BKCOLOR ��־;���򣬽����� crBkgnd��
        public virtual void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO pdbi) {
            this.bandID = (int) dwBandID;
            this.fVertical = dwViewMode == 1U;

            if((pdbi.dwMask & DBIM.ACTUAL) != 0) {
                pdbi.ptActual.X = Size.Width;
                pdbi.ptActual.Y = Size.Height;
            }
            if((pdbi.dwMask & DBIM.INTEGRAL) != 0) {
                pdbi.ptIntegral.X = -1;
                pdbi.ptIntegral.Y = -1;
            }
            if((pdbi.dwMask & DBIM.MAXSIZE) != 0) {
                pdbi.ptMaxSize.X = pdbi.ptMaxSize.Y = -1;
            }
            if((pdbi.dwMask & DBIM.MINSIZE) != 0) {
                pdbi.ptMinSize.X = MinSize.Width;
                pdbi.ptMinSize.Y = MinSize.Height;
            }
            if((pdbi.dwMask & DBIM.MODEFLAGS) != 0) {
                pdbi.dwModeFlags = DBIMF.NORMAL;
            }
            if((pdbi.dwMask & DBIM.BKCOLOR) != 0) {
                pdbi.dwMask &= ~DBIM.BKCOLOR;
            }
            if((pdbi.dwMask & DBIM.TITLE) != 0) {
                pdbi.wszTitle = null;
            }
        }

        private REBARBANDINFO GetRebarBand(int idx, int fMask) {
            Util2.bandLog("GetRebarBand");
            REBARBANDINFO info = new REBARBANDINFO();
            info.cbSize = Marshal.SizeOf(info);
            info.fMask = fMask;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(info));
            Marshal.StructureToPtr(info, ptr, false);
            SendMessage(ReBarHandle, RB.GETBANDINFO, (IntPtr)idx, ptr);
            info = (REBARBANDINFO)Marshal.PtrToStructure(ptr, typeof(REBARBANDINFO));
            Marshal.FreeHGlobal(ptr);
            return info;
        }

        public virtual int GetSite(ref Guid riid, out object ppvSite) {
            try
            {
                if (this.BandObjectSite != null)
                {
                    ppvSite = BandObjectSite;
                    // ppvSite = Marshal.GetIUnknownForObject((object)this.BandObjectSite);
                    return 0;
                }
            }
            catch 
            {
            }
            // ppvSite = IntPtr.Zero;
            ppvSite = null;
            return -2147467259;
        }

        public virtual void GetWindow(out IntPtr phwnd) {
            if (BandObject.HostedNotByExplorer)
            {
                phwnd = IntPtr.Zero;
                return ;
            }
            phwnd = Handle;
        }

        public virtual int HasFocusIO() {
            if(!ContainsFocus) {
                return 1;
            }
            return 0;
        }

        protected virtual void OnExplorerAttached() {
            Util2.bandLog("BandObject");
        }

        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
            if((!fClosedDW && (BandObjectSite != null)) && IsHandleCreated) {
                Util2.bandLog("OnGotFocus");
                BandObjectSite.OnFocusChangeIS(this, 1);
            }
        }

        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);
            if((!fClosedDW && (BandObjectSite != null)) && (ActiveControl == null)) {
                Util2.bandLog("OnLostFocus");
                BandObjectSite.OnFocusChangeIS(this, 0);
            }
        }

        /// <summary>
        /// ֪ͨͣ�����ڶ����ܵı߿�ռ��Ѹ��ġ�Ϊ����Ӧ�˷�����IDockingWindow ʵ�ֱ������ SetBorderSpaceDW����ʹ����Ҫ�߿�ռ����Ҫ���ġ�
        /// </summary>
        /// <param name="prcBorder"></param>
        /// <param name="punkToolbarSite"></param>
        /// <param name="fReserved"></param>
        public virtual void ResizeBorderDW(IntPtr prcBorder, object punkToolbarSite, bool fReserved) {
        }

        // Override this to set whether the DeskBand has a break when it is 
        // first displayed
        protected virtual bool ShouldHaveBreak() {
            return true;
        }

        public virtual int SetSite(object pUnkSite)
        {
            /*if(Process.GetCurrentProcess().ProcessName == "iexplore") {
                Marshal.ThrowExceptionForHR(E_FAIL);
                Util2.bandLog("Marshal.ThrowExceptionForHR");

            }*/

            if (pUnkSite == null)
            {
                if (BandObjectSite != null )
                {
                    Marshal.ReleaseComObject(BandObjectSite);
                    Util2.bandLog("Marshal.ReleaseComObject BandObjectSite");
                    this.BandObjectSite = null;
                }
                if (Explorer != null)
                {
                    Marshal.ReleaseComObject(Explorer);
                    Explorer = null;
                    Util2.bandLog("Marshal.ReleaseComObject Explorer");
                }
            } else if (pUnkSite != null)
            {
                BandObjectSite = pUnkSite as IInputObjectSite;
                try {
                    object obj2;
                    ((_IServiceProvider)BandObjectSite).QueryService(
                        ExplorerGUIDs.IID_IWebBrowserApp, 
                        ExplorerGUIDs.IID_IUnknown, 
                        out obj2);
                    Util2.bandLog("BandObjectSite.QueryService");
                    Explorer = (WebBrowserClass)Marshal.CreateWrapperOfType(obj2 as IWebBrowser, typeof(WebBrowserClass));
                    Util2.bandLog("Marshal.CreateWrapperOfType");
                    OnExplorerAttached();
                    Util2.bandLog("OnExplorerAttached");
                }
                catch  (COMException exception) { // exception
                    Util2.MakeErrorLog(exception, "QueryService CreateWrapperOfType");
                }
            }
            try {
                IOleWindow window = pUnkSite as IOleWindow;
                if(window != null) {
                    window.GetWindow(out ReBarHandle);
                }
            }
            catch (Exception e) // exc
            {
                Util2.MakeErrorLog(e, "BandObject SetSite");
               //  logger.Log(exc);
            }
            return 0;
        }

        public virtual void ShowDW(bool fShow) {
            if(ReBarHandle != IntPtr.Zero && Environment.OSVersion.Version.Major > 5) {
                if(RebarSubclass == null) {
                    RebarSubclass = new RebarBreakFixer(ReBarHandle, this);
                }

                RebarSubclass.MonitorSetInfo = true;
                if(result == null || result.IsCompleted) {    
                    result = BeginInvoke(new UnsetInfoDelegate(UnsetInfo));
                }
            }
            Visible = fShow;
        }

        public virtual int TranslateAcceleratorIO(ref MSG msg) {
            if(((msg.message == 0x100) && ((msg.wParam == ((IntPtr)9L)) || (msg.wParam == ((IntPtr)0x75L)))) && SelectNextControl(ActiveControl, (ModifierKeys & Keys.Shift) != Keys.Shift, true, false, false)) {
                return 0;
            }
            return 1;
        }

        public virtual void UIActivateIO(int fActivate, ref MSG msg) {
            if(fActivate != 0) {
                Control nextControl = GetNextControl(this, true);
                if(nextControl != null) {
                    nextControl.Select();
                }
                Focus();
            }
        }

        private delegate void UnsetInfoDelegate();

        private void UnsetInfo() {
            if(RebarSubclass != null) {
                RebarSubclass.MonitorSetInfo = false;
            }
        }

        public Size MinSize {
            get {
                return minSize;
            }
            set {
                minSize = value;
            }
        }

        protected Size MaxSize
        {
            get
            {
                return this.maxSize;
            }
            set
            {
                this.maxSize = value;
            }
        }

        public virtual void GetClassID(out Guid pClassID) {
            pClassID = Guid.Empty;
        }

        public virtual int IsDirty() {
            return 0;
        }

        public virtual void IPersistStreamLoad(object pStm) {
        }

        public virtual void Save(IntPtr pStm, bool fClearDirty) {
        }

        public virtual int GetSizeMax(out ulong pcbSize) {
            const int E_NOTIMPL = -2147467263;
            pcbSize = 0;
            return E_NOTIMPL;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // BandObject
            // 
            this.ForeColor = System.Drawing.Color.Black;
            this.Name = "BandObject";
            this.ResumeLayout(false);
        }

        public int Dpi { get; private set; } 

        public float Scaling
        {
            get
            {
               return (float) this.Dpi / 96f;
            }
        } 

        /*
        public void NotifyDpiChanged(int oldDpi, int dpiNew)
        {
            Util2.bandLog("BandObject NotifyDpiChanged oldDpi " + oldDpi + " dpiNew " + dpiNew);
            this.Dpi = dpiNew;
            Action<Control> act = (Action<Control>) null;
            act = (Action<Control>)(
                control =>
                {
                    for (var i = 0; i < control.Controls.Count; i++)
                    {
                        var cc = (Control) control.Controls[i];

                        if (cc is IDpiAwareObject)
                            ((IDpiAwareObject)cc).NotifyDpiChanged(oldDpi, dpiNew);
                        act(cc);
                    }
                }
            );
            act((Control) this);
            this.OnDpiChanged(oldDpi, dpiNew);
        }*/


        protected virtual void OnDpiChanged(int oldDpi, int newDpi)
        {
        }

        /*#region �������� by indiff
        public  void RefreshRebarBand()
        {
            // REBARBANDINFO* lParam = stackalloc REBARBANDINFO[1];
            REBARBANDINFO lParam = new REBARBANDINFO();
            // lParam.cbSize = sizeof(REBARBANDINFO);
            lParam.cbSize = Marshal.SizeOf(lParam);
            lParam.fMask = 32;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(lParam));
            Marshal.StructureToPtr(lParam, ptr, false);

            int wParam = (int)PInvoke.SendMessage(this.ReBarHandle, 1040, this.BandID, 0);
            if (wParam == -1)
                return;
            // PInvoke.SendMessage(this.ReBarHandle, 1052, (IntPtr)wParam, ptr);
            SendMessage(ReBarHandle, 1052, (IntPtr)wParam, ptr);
            // PInvoke.SendMessage(this.Handle, RB.SETBANDINFOW, (void*)wParam, ref structure);
            lParam.cyChild = this.fVertical ? this.Width : this.Height;
            lParam.cyMinChild = this.fVertical ? this.Width : this.Height;
            PInvoke.SendMessage(this.ReBarHandle, 1035, (IntPtr)wParam, ptr);
            lParam = (REBARBANDINFO)Marshal.PtrToStructure(ptr, typeof(REBARBANDINFO));
            Marshal.FreeHGlobal(ptr);
        }
        #endregion*/

        /*protected override void OnPaintBackground(PaintEventArgs e)
        {

        }*/
    }

    internal class Util2
    {
        private static readonly bool ENABLE_LOGGER = false;


        public static void bandLog(string optional)
        {
            if (ENABLE_LOGGER)
                bandLog("bandLog", optional);
        }

        

        public static void err(string optional)
        {
            if (ENABLE_LOGGER)
                bandLog("err", optional);

        }

        private static void writeStr(string path, StringBuilder formatLogLine)
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(formatLogLine);
            }
        }

        public static void bandLog(string level, string optional)
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appdataQT = Path.Combine(appdata, "QTTabBar");
            if (!Directory.Exists(appdataQT))
            {
                Directory.CreateDirectory(appdataQT);
            }

            Process process = Process.GetCurrentProcess();
            int managedThreadId = Thread.CurrentThread.ManagedThreadId;

            string path = Path.Combine(appdataQT, "bandLog.log");
            var formatLogLine = new StringBuilder();
            formatLogLine
                .Append("[")
                .Append(level)
                .Append("]");
            if (process != null)
            {
                formatLogLine
                    .Append(" PID:")
                    .Append(process.Id);
            }
            formatLogLine
                .Append(" TID:")
                .Append(managedThreadId);
            formatLogLine
                .Append(" ")
                .Append(DateTime.Now.ToString())
                .Append(" ")
                .Append(optional);
            writeStr(path, formatLogLine);
        }

        internal static void MakeErrorLog(Exception ex, string optional = null)
        {
            try
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appdataQT = Path.Combine(appdata, "QTTabBar");
                if (!Directory.Exists(appdataQT))
                {
                    Directory.CreateDirectory(appdataQT);
                }
                // string path = Path.Combine(appdataQT, "QTTabBarBandObject.bandLog");
                string path = Path.Combine(appdataQT, "QTTabBarBandObjectException.log");
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(DateTime.Now.ToString());
                    writer.WriteLine(".NET �汾: " + Environment.Version);
                    writer.WriteLine("����ϵͳ�汾: " + Environment.OSVersion.Version);
                    //writer.WriteLine("QT �汾: " + MakeVersionString());
                    if (!String.IsNullOrEmpty(optional))
                    {
                        writer.WriteLine("������Ϣ: " + optional);
                    }
                    if (ex == null)
                    {
                        writer.WriteLine("Exception: None");
                        writer.WriteLine(Environment.StackTrace);
                    }
                    else
                    {
                        // writer.WriteLine(ex.ToString());

                        writer.WriteLine("\nMessage ---\n{0}", ex.Message);
                        writer.WriteLine(
                            "\nHelpLink ---\n{0}", ex.HelpLink);
                        writer.WriteLine("\nSource ---\n{0}", ex.Source);
                        writer.WriteLine(
                            "\nStackTrace ---\n{0}", ex.StackTrace);
                        writer.WriteLine(
                            "\nTargetSite ---\n{0}", ex.TargetSite);


                    }
                    writer.WriteLine("--------------");
                    writer.WriteLine();
                    writer.Close();
                }
                // SystemSounds.Exclamation.Play();
            }
            catch
            {
            }
        }
    }
}
