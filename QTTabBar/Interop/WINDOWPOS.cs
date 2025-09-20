using System;

#pragma warning disable CS0649
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
#pragma warning restore CS0649

