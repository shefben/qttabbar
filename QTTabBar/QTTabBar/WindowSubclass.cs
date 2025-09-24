using QTTabBarLib.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QTTabBarLib
{
  internal sealed class WindowSubclass
  {
    private static volatile int idIncremental;
    private GCHandle gchSubClassProc;
    private IntPtr fp;
    private IntPtr ID;
    private WindowSubclass.SubclassingProcedure proc;
    public volatile bool Disabled;

    public WindowSubclass(IntPtr hwnd, WindowSubclass.SubclassingProcedure proc)
    {
      if (hwnd == IntPtr.Zero)
        throw new ArgumentNullException("hwnd is NULL.");
      this.proc = proc;
      this.ID = (IntPtr) WindowSubclass.idIncremental++;
      this.AssignHandle(hwnd);
    }

    public IntPtr Handle { get; private set; }

    private void AssignHandle(IntPtr hwnd)
    {
      SUBCLASSPROC d = new SUBCLASSPROC(this.subClassProcCore);
      this.gchSubClassProc = GCHandle.Alloc((object) d);
      this.fp = Marshal.GetFunctionPointerForDelegate(d); // <SUBCLASSPROC>
      if (PInvoke.SetWindowSubclass(hwnd, this.fp, this.ID, IntPtr.Zero)) {
        this.Handle = hwnd;
      } else {
        // Cleanup on failure to prevent memory leak
        if (this.gchSubClassProc.IsAllocated) {
          this.gchSubClassProc.Free();
        }
        throw new InvalidOperationException("Failed to subclass window");
      }
    }

    public void ReleaseHandle()
    {
      this.Disabled = true;
      this.proc = (WindowSubclass.SubclassingProcedure) null;

      if (this.Handle != IntPtr.Zero) {
        try {
          PInvoke.RemoveWindowSubclass(this.Handle, this.fp, this.ID);
        } catch (Exception ex) {
          System.Diagnostics.Debug.WriteLine("Failed to remove window subclass: " + ex.Message);
        }
        this.Handle = IntPtr.Zero;
      }

      if (this.gchSubClassProc.IsAllocated) {
        try {
          this.gchSubClassProc.Free();
        } catch (Exception ex) {
          System.Diagnostics.Debug.WriteLine("Failed to free GCHandle: " + ex.Message);
        }
      }
    }

    public void ReleaseHandleAsync()
    {
      this.Disabled = true;
      if (!(this.Handle != IntPtr.Zero))
        return;
      PInvoke.PostMessage(this.Handle, RegisteredMessage.Unsubclass, this.ID, IntPtr.Zero);
    }

    public void DefaultWindowProcedure(ref Message m)
    {
      try
      {
        if (!(this.Handle != IntPtr.Zero) || !(this.Handle == m.HWnd))
          return;
        m.Result = PInvoke.DefSubclassProc(m.HWnd, m.Msg, m.WParam, m.LParam);
      }
      catch (Exception ex)
      {
        QTUtility2.MakeErrorLog(ex);
      }
    }

    private IntPtr subClassProcCore(
      IntPtr hWnd,
      int uMsg,
      IntPtr wParam,
      IntPtr lParam,
      IntPtr uIdSubclass,
      IntPtr dwRefData)
    {
      try
      {
        if (this.ID != uIdSubclass || this.Handle != hWnd)
          return IntPtr.Zero;
        if (uMsg == 130)
        {
          this.ReleaseHandle();
          return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }
        if (!this.Disabled && this.proc != null)
        {
          Message msg = Message.Create(hWnd, uMsg, wParam, lParam);
          try
          {
            if (this.proc(ref msg))
              return msg.Result;
          }
          catch (Exception ex)
          {
            string optional = "msg: " + msg.Msg;
            QTUtility2.MakeErrorLog(ex, optional);
          }
        }
        if (uMsg == RegisteredMessage.Unsubclass)
        {
          if (wParam == this.ID)
          {
            this.ReleaseHandle();
            return IntPtr.Zero;
          }
        }
      }
      catch (Exception ex)
      {
        QTUtility2.MakeErrorLog(ex, "1");
      }
      try
      {
        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
      }
      catch (Exception ex)
      {
          QTUtility2.MakeErrorLog(ex, "2");
          return IntPtr.Zero;
      }
    }

    public delegate bool SubclassingProcedure(ref Message msg);
  }
}
