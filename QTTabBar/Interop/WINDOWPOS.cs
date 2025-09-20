using System;

#pragma warning disable 649
namespace QTTabBarLib.Interop
{
  internal struct WINDOWPOS
  {
    public IntPtr hwnd;
    public IntPtr hwndInsertAfter;
    public int x;
    public int y;
    public int cx;
    public int cy;
    public SWP flags;
  }
}
#pragma warning restore 649

