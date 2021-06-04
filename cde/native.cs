using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace cde
{
  static unsafe class Native
  {
    [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
    static extern int memcmp(byte[] b1, byte[] b2, long count);
    internal static bool Equals(byte[] a, byte[] b)
    {
      if (a == b) return true;
      if (a.Length != b.Length) return false;
      return memcmp(a, b, b.Length) == 0;
    }
    internal static IEnumerable<(T a, T b, T c)> TriShopper<T>(this IEnumerable<T> a)
    {
      T x = default, y = default; int i = 0;
      foreach (var p in a) { switch (i++) { case 0: x = p; break; case 1: y = p; break; default: yield return (x, y, p); i = 0; break; } }
    }
  }

}
