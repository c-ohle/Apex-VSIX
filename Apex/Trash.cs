using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Apex.CDX;
//using System.Runtime.InteropServices.ComTypes;

namespace Apex
{
#if(false)
    static int[] caladj(ushort[] a)
    {
      var b = new int[a.Length];
      var dict = new System.Collections.Generic.Dictionary<(int, int), int>(a.Length);
      for (int i = 0; i < a.Length; i++) dict[(a[i], a[i + (i % 3 == 2 ? -2 : 1)])] = i;
      for (int i = 0; i < a.Length; i++) b[i] = dict.TryGetValue((a[i + (i % 3 == 2 ? -2 : 1)], a[i]), out var k) ? k : -1;
      return b;
    }
    static void testmesh(CSG.IMesh mesh)
    {
      var vv = mesh.GetVertices();
      var ii = mesh.GetIndices();
      var ad = caladj(ii); var ade = ad.Count(p => p == -1); if (ade != 0) { }
      var ee = new CSG.Rational.Plane[ii.Length / 3];
      for (int i = 0, k = 0; i < ee.Length; i++, k += 3) ee[i] = CSG.Rational.Plane.FromPoints(vv[ii[k]], vv[ii[k + 1]], vv[ii[k + 2]]);
      int wrong = 0;
      for (int i = 0; i < ii.Length; i++)
      {
        var e1 = ee[i / 3];
        var e2 = ee[ad[i] / 3];
        if (e1 != -e2) continue;
        wrong++;
      }
      if (wrong != 0) { }
    }

  static void MeshRound2(ref float3[] pp, ref int[] ii)
  {
    var ad = new int[ii.Length];
    var dict = new System.Collections.Generic.Dictionary<(int, int), int>(ii.Length);
    for (int i = 0; i < ii.Length; i++) dict[(ii[i], ii[i + (i % 3 == 2 ? -2 : 1)])] = i;
    for (int i = 0; i < ii.Length; i++) ad[i] = dict.TryGetValue((ii[i + (i % 3 == 2 ? -2 : 1)], ii[i]), out var k) ? k : -1;
    var ee = new float4[ii.Length / 3];
    for (int i = 0, k = 0; i < ee.Length; i++, k += 3) ee[i] = PlaneFromPoints(pp[ii[k]], pp[ii[k + 1]], pp[ii[k + 2]]);
  
    int wrong = 0; //var join = new System.Collections.Generic.List<int>(); 
    for (int i = 0; i < ii.Length; i++)
    {
      if (i > ad[i]) continue;
      var e1 = ee[i / 3]; var e2 = ee[ad[i] / 3];
      if (e1 != -e2) continue;
      wrong++;
      //var i1 = i / 3 * 3;
      //var i2 = ad[i] / 3 * 3;
      //var a1 = (pp[ii[i1 + 1]] - pp[ii[i1]] ^ pp[ii[i1 + 2]] - pp[ii[i1]]).Length;
      //var a2 = (pp[ii[i2 + 1]] - pp[ii[i2]] ^ pp[ii[i2 + 2]] - pp[ii[i2]]).Length;
      ref var p1 = ref pp[ii[i]]; ref var p2 = ref pp[ii[i + (i % 3 == 2 ? -2 : 1)]];
      //var dp = (p2 - p1).Length;
      p1 = p2;
    }
    if (wrong == 0) return;
  
    var newii = new List<int>();
    var ptdict = new Dictionary<float3, int>(pp.Length);
    for (int i = 0; i < ii.Length; i++)
    {
      if (!ptdict.TryGetValue(pp[ii[i]], out var k)) ptdict.Add(pp[ii[i]], k = ptdict.Count);
      newii.Add(k); if (i % 3 != 2) continue; var x = newii.Count - 3;
      if (newii[x] != newii[x + 1] && newii[x + 1] != newii[x + 2] && newii[x + 2] != newii[x]) continue;
      newii.RemoveRange(x, 3);
    }
    pp = ptdict.Keys.ToArray();
    ii = newii.ToArray();
  
  }
#endif
#if (false)
  public unsafe static class Rational
  {
    public struct Number : IEquatable<Number>, IComparable<Number>
    {
      public static bool DecimalCasts = true; //compatibility
      public string ToString(int flags, int digits)
      {
        var ct = getroot(); var ps = ct[-1]; ct[-1] += 1000; //debug
        uint* num = push(ct) - 1, den = &num[num[0] + 2];
        var nn = (num[0] > den[-1] ? num[0] : den[-1]) + 2;
        var p1 = ct + (ct[-1] + 1); ct[-1] = ps; var p2 = p1 + nn + 1; var p3 = p2 + nn;
        var ss = (char*)(p3 + nn); int ns = 0, ab; long ten = 0xA00000001;
        for (int i = 0; i <= num[0]; i++) p1[i - 1] = num[i]; div(p1, den, p2);
        if (this.num < 0) ss[ns++] = '-';
        if (p2[-1] == 1 && p2[0] == 0) ss[ns++] = '0';
        else
        {
          for (ab = ns; p2[-1] > 1 || p2[0] != 0;) { div(p2, ((uint*)&ten) + 1, p3); ss[ns++] = (char)('0' + *p2); var t = p2; p2 = p3; p3 = t; }
          for (int i = ab, k = ns - 1; i < k; i++, k--) { var t = ss[i]; ss[i] = ss[k]; ss[k] = t; }
        }
        for (int x = 0; p1[-1] > 1 || p1[0] != 0; x++)
        {
          if (x == digits) { if ((flags & 2) != 0) ss[ns++] = '…'; break; }
          if (x == 0) ss[ns++] = '.'; Debug.Assert(p1[-1] + 2 <= nn);
          mul(p1, ((uint*)&ten) + 1, p2); Debug.Assert(p2[-1] < nn);
          div(p2, den, p1); var c = (char)('0' + p1[0]);
          if ((flags & 1) != 0) //reps
          {
            var pp = (uint*)(ss + (digits + ns - x));
            for (int j = 0; j < x; j++, pp += pp[0] + 1)
            {
              if (ss[ab = ns - x + j] != c) continue;
              int i = 0; for (; i <= pp[0] && pp[i] == p2[i - 1]; i++) ; if (i <= pp[0]) continue;
              for (i = ns++; i > ab; i--) ss[i] = ss[i - 1]; ss[ab] = '\''; return new string(ss, 0, ns);
            }
            for (int i = 0; i <= p2[-1]; i++) pp[i] = p2[i - 1];
          }
          ss[ns++] = c; var t = p1; p1 = p2; p2 = t;
        }
        return new string(ss, 0, ns);
      }
      public override string ToString()
      {
        return ToString(0x3, 63);
      }
      public static Number Parse(char* p, int n)
      {
        Number a = 0, b = a, e = 1, f = 10; //3.42'56
        for (int i = n - 1, c; i >= 0; i--)
        {
          if ((c = p[i]) >= '0' && c <= '9') { if (c != '0') a += (c - '0') * e; e *= f; continue; }
          if (c == '-') { a = -a; continue; }
          if (c == '.') { b = e; continue; }
          if (c == '\'') { a = a * e / (e - 1); continue; }
          if (c == '/') return Parse(p, i) / a;
          if ((c | 0x20) == 'e') { Debug.Assert(a.bits == null); return Parse(p, i) * Pow(10, a.num); }
        }
        if (b.num != 0) a /= b;
        return a;
      }
      public static Number Parse(string s)
      {
        fixed (char* p = s) return Parse(p, s.Length);
      }
      public override int GetHashCode()
      {
        var h = 0u; if (bits != null) for (int i = 0; i < bits.Length; h = ((h << 7) | (bits[i] >> 25)) ^ bits[i], i++) ;
        return (int)(h ^ num ^ den);
      }
      public override bool Equals(object obj)
      {
        return obj is Number && Equals((Number)obj);
      }
      public bool Equals(Number b)
      {
        if (num != b.num || den != b.den) return false;
        if (bits == b.bits) return true;
        if (bits == null || b.bits == null || bits.Length != b.bits.Length) return false;
        for (int i = 0, n = bits.Length; i < n; i++) if (bits[i] != b.bits[i]) return false;
        return true;
      }
      public int CompareTo(Number b)
      {
        int sa = (num >> 31) - (-num >> 31), sb = (b.num >> 31) - (-b.num >> 31);
        if (sa != sb) return sa > sb ? +1 : -1; if (sa == 0) return 0;
        var ct = getroot(); var ps = ct[-1];
        uint* s = push(ct), t = b.push(ct);
        uint* u = ct + (ct[-1] + 1), v; ct[-1] = ps;
        mul(s, t + (t[-1] + 1), u);
        mul(t, s + (s[-1] + 1), v = u + (u[-1] + 1));
        return cmp(u, v) * sa;
      }
      public int Sign
      {
        get { return (num >> 31) - (-num >> 31); }
      }
      public static Number Abs(Number a)
      {
        if (a.num < 0) a.num = -a.num; return a;
      }
      public static Number Min(Number a, Number b)
      {
        return a.CompareTo(b) < 0 ? a : b;
      }
      public static Number Max(Number a, Number b)
      {
        return a.CompareTo(b) > 0 ? a : b;
      }
      public static Number Pow(Number a, int e)
      {
        var b = (Number)1; if (e == 0) return b;
        for (var n = e > 0 ? e : -e; ; n >>= 1, a *= a)
        {
          if ((n & 1) != 0) b *= a;
          if (n == 1) break;
        }
        if (e < 0) b = 1 / b; return b;
      }
      public static implicit operator Number(int v)
      {
        return new Number { num = v };
      }
      public static implicit operator Number(float v)
      {
        if (DecimalCasts) return (decimal)v;
        var bits = *(uint*)&v; if ((bits & 0x7fffffff) == 0) return 0;
        var man = bits & 0x7FFFFF; var exp = (int)(bits >> 23) & 0xFF;
        man |= 0x800000; exp -= 150;
        var ct = getroot(); var r = ct + ct[-1];
        r[0] = 1; r[1] = man; if (exp > 0) shl(r + 1, exp); var p = r + r[0] + 1;
        p[0] = p[1] = 1; if (exp < 0) shl(p + 1, -exp);
        return new Number(ct, r, v < 0);
      }
      public static implicit operator Number(double v)
      {
        if (DecimalCasts) return (decimal)v;
        var bits = *(ulong*)&v; if ((bits & 0x7FFFFFFFFFFFFFFF) == 0) return 0;
        var man = bits & 0x000FFFFFFFFFFFFF; var exp = (int)(bits >> 52) & 0x7FF;
        man |= 0x0010000000000000; exp -= 1075;
        var ct = getroot(); var r = ct + ct[-1];
        r[0] = (r[2] = ((uint*)&man)[1]) != 0 ? 2u : 1u; r[1] = ((uint*)&man)[0];
        if (exp > 0) shl(r + 1, exp); var p = r + r[0] + 1;
        p[0] = p[1] = 1; if (exp < 0) shl(p + 1, -exp);
        return new Number(ct, r, v < 0);
      }
      public static implicit operator Number(decimal v)
      {
        var n = (uint*)&v; //if ((((ulon g*)n)[0] & ~0x80000000) == 0 && ((ulong*)n)[1] <= 0x7fffffff) { int t = (int)n[2]; if ((n[0] & 0x80000000) != 0) t = -t; return t; }//small ints
        var l = 1M; var d = (uint*)&l; var z = n[0]; var e = (z >> 16) & 0xff; //0-28
        for (var i = 10UL; ; i = i * i) { if ((e & 1) != 0) l *= i; if ((e >>= 1) == 0) break; } //pow(10, e)
        var ct = getroot(); var r = ct + ct[-1];
        r[1] = n[2]; r[2] = n[3]; r[3] = n[1]; r[0] = r[3] != 0 ? 3u : r[2] != 0 ? 2u : 1; var s = r + (r[0] + 1);
        s[1] = d[2]; s[2] = d[3]; s[3] = d[1]; s[0] = s[3] != 0 ? 3u : s[2] != 0 ? 2u : 1; if (r[0] == 1 && r[1] == 0) return 0;
        return new Number(ct, r, z >> 31 != 0);
      }
      public static explicit operator float(Number v)
      {
        return (float)(double)v;
      }
      public static explicit operator double(Number v)
      {
        if (v.bits == null) return (double)v.num / (v.den + 1);
        var ct = getroot(); var ps = ct[-1];
        uint* s = v.push(ct) - 1, t = s + (s[0] + 1); ct[-1] = ps;
        var u1 = s[0] != 1 ? *(ulong*)&s[s[0] - 1] : (ulong)s[s[0]] << 32;
        var u2 = t[0] != 1 ? *(ulong*)&t[t[0] - 1] : (ulong)t[t[0]] << 32;
        var d = (double)u1 / u2; if (s[0] != t[0]) d *= Math.Pow(2, ((int)s[0] - (int)t[0]) << 5);
        return v.num < 0 ? -d : d;
      }
      public static explicit operator decimal(Number v)
      {
        //return (decimal)(double)v;
        var ct = getroot(); var ps = ct[-1];
        uint* s = v.push(ct) - 1, t = s + (s[0] + 1), p; ct[-1] = ps;
        var d1 = (int)(s[0] << 5) - chz(s[s[0]]) - 96; if ((d1 = d1 > 0 ? d1 : 0) != 0) shr(s + 1, d1);
        var d2 = (int)(t[0] << 5) - chz(t[t[0]]) - 96; if ((d2 = d2 > 0 ? d2 : 0) != 0) shr(t + 1, d2);
        decimal b; (p = (uint*)&b)[2] = t[1]; if (t[0] > 1) { p[3] = t[2]; if (t[0] > 2) p[1] = t[3]; }
        decimal a; (p = (uint*)&a)[2] = s[1]; if (s[0] > 1) { p[3] = s[2]; if (s[0] > 2) p[1] = s[3]; }
        a /= b; if (d1 != d2) a *= (decimal)Math.Pow(2, d1 - d2); if (v.num < 0) p[0] |= (1u << 31); return a;
      }
      public static bool operator ==(Number a, Number b)
      {
        return a.CompareTo(b) == 0; //a.Equals(b);
      }
      public static bool operator !=(Number a, Number b)
      {
        return a.CompareTo(b) != 0;
      }
      public static bool operator <=(Number a, Number b)
      {
        return a.CompareTo(b) <= 0;
      }
      public static bool operator >=(Number a, Number b)
      {
        return a.CompareTo(b) >= 0;
      }
      public static bool operator <(Number a, Number b)
      {
        return a.CompareTo(b) < 0;
      }
      public static bool operator >(Number a, Number b)
      {
        return a.CompareTo(b) > 0;
      }
      public static Number operator -(Number a)
      {
        a.num = -a.num; return a;
      }
      public static Number operator +(Number a, Number b)
      {
        if (a.num == 0) return b;
        if (b.num == 0) return a;
        var ct = getroot(); var ps = ct[-1];
        uint* s = a.push(ct), t = b.push(ct); int si;
        uint* u = ct + (ct[-1] + 1), v;
        mul(s, t + (t[-1] + 1), u);
        mul(t, s + (s[-1] + 1), v = u + (u[-1] + 1));
        if (a.num > 0 == b.num > 0) { add(u, v, u); si = a.num > 0 ? +1 : -1; }
        else
        {
          if ((si = cmp(u, v)) == 0) { ct[-1] = ps; return 0; }
          sub(si > 0 ? u : v, si > 0 ? v : u, u); si = a.num > 0 == si > 0 ? +1 : -1;
        }
        mul(s + (s[-1] + 1), t + (t[-1] + 1), u + (u[-1] + 1));
        ct[-1] = ps; return new Number(ct, u - 1, si < 0);
      }
      public static Number operator -(Number a, Number b)
      {
        b.num = -b.num; return a + b;
      }
      public static Number operator *(Number a, Number b)
      {
        if (a.num == 0 || b.num == 0) return 0;
        var ct = getroot(); var ps = ct[-1];
        uint* s = a.push(ct), t = b.push(ct), r = ct + (ct[-1] + 1);
        mul(s, t, r); mul(s + (s[-1] + 1), t + (t[-1] + 1), r + (r[-1] + 1));
        ct[-1] = ps; return new Number(ct, r - 1, a.num > 0 != b.num > 0);
      }
      public static Number operator /(Number a, Number b)
      {
        if (a.num == 0) return 0;
        if (b.num == 0) throw new DivideByZeroException();
        var ct = getroot(); var ps = ct[-1];
        uint* s = a.push(ct), t = b.push(ct), r = ct + (ct[-1] + 1);
        mul(s, t + (t[-1] + 1), r); mul(s + (s[-1] + 1), t, r + (r[-1] + 1));
        ct[-1] = ps; return new Number(ct, r - 1, a.num > 0 != b.num > 0);
      }
      public static Number operator >>(Number a, int b)
      {
        if (a.num == 0 || b == 0) return a;
        var ct = getroot(); var ps = ct[-1]; uint* s = a.push(ct), r = ct + (ct[-1] + 1);
        for (int i = -1; i < s[-1]; i++) r[i] = s[i]; ct[-1] = ps;
        if (b > 0) { shr(r, b); if (*(long*)(r - 1) == 1) return 0; } else shl(r, -b);
        var t = r + (r[-1] + 1); s += s[-1] + 1; for (int i = -1; i < s[-1]; i++) t[i] = s[i];
        return new Number(ct, r - 1, a.num < 0);
      }
      public static Number operator <<(Number a, int b)
      {
        return a >> -b;
      }
      public static Number operator |(Mach a, Number b)
      {
        var ct = getroot(); Debug.Assert(ct[-2] != 0); ct[-2]--;
        if (b.num != 0) b = new Number(null, b.push(ct) - 1, b.num < 0); ct[-1] = a.p; return b;
      }
      public static int operator ^(Mach a, Number b)
      {
        var ct = getroot(); Debug.Assert(ct[-2] != 0); ct[-2]--; ct[-1] = a.p; return b.Sign;
      }
      public struct Mach : IDisposable
      {
        internal uint p;
        public static implicit operator Mach(int v)
        {
          var ct = getroot(); ct[-2]++; return new Mach { p = ct[-1] };
        }
        public void Dispose()
        {
          var ct = getroot(); ct[-1] = p; Debug.Assert(ct[-2] != 0); ct[-2]--;
        }
      }
      int num; uint den; uint[] bits; static uint[] unull = new uint[0];
      Number(uint* ct, uint* r, bool neg)
      {
        var n = r[0] + r[r[0] + 1]; var mach = ct != null ? ct[-2] : 0;
        if (mach == 0)
        {
          if (*(long*)(r + (r[0] + 1)) != 0x100000001)
          {
            var t = r + n + 2; for (int i = 0; i < n + 2; i++) t[i] = r[i];
            var s = gcd(t + 1, t + (t[0] + 2));
            if (*(long*)(s - 1) != 0x100000001)
            {
              t += n + 2; for (int i = 0; i < n + 2; i++) t[i] = r[i]; var k = t + (t[0] + 2);
              div(t + 1, s, r + 1); div(k, s, r + (r[0] + 2)); n = r[0] + r[r[0] + 1];
            }
          }
        }
        if (n == 2 && r[1] < 0x80000000) { num = (int)r[1]; if (neg) num = -num; den = r[r[0] + 2] - 1; bits = null; return; }
        num = neg ? -1 : +1;
        if (mach > 0) { den = (uint)(r - ct); bits = unull; ct[-1] = den + n + 2; return; }
        den = r[0]; bits = new uint[n]; for (uint i = 0; i < n; i++) bits[i] = r[i + (i < den ? 1 : 2)];
      }
      uint* push(uint* ct)
      {
        if (bits == unull) return ct + den + 1; var p = ct + (ct[-1] + 1);
        if (bits == null) { p[-1] = p[1] = 1; p[0] = (uint)(num < 0 ? -num : num); p[2] = den + 1; ct[-1] += 4; return p; }
        var n = (uint)bits.Length; p[p[-1] = den] = n - den;
        for (uint i = 0; i < n; i++) p[i + (i < den ? 0 : 1)] = bits[i]; ct[-1] += n + 2;
        return p;
      }
      static int cmp(uint* a, uint* b)
      {
        if (a[-1] != b[-1]) return a[-1] > b[-1] ? +1 : -1;
        for (var i = a[-1]; i-- != 0;) if (a[i] != b[i]) return a[i] > b[i] ? +1 : -1; return 0;
      }
      static void add(uint* a, uint* b, uint* r)
      {
        if (a[-1] < b[-1]) { var t = a; a = b; b = t; }
        uint c = 0, i = 0, na = a[-1], nb = b[-1];
        for (; i < nb; i++) { var u = (ulong)a[i] + b[i] + c; r[i] = ((uint*)&u)[0]; c = ((uint*)&u)[1]; }
        for (; i < na; i++) { var u = (ulong)a[i] + c; /*  */ r[i] = ((uint*)&u)[0]; c = ((uint*)&u)[1]; }
        r[-1] = na; if (c != 0) r[r[-1]++] = c;
      }
      static void sub(uint* a, uint* b, uint* r)
      {
        uint c = 0, i = 0, na = a[-1], nb = b[-1]; Debug.Assert(na >= nb);
        for (; i < nb; i++) { var u = (ulong)a[i] - b[i] - c; r[i] = ((uint*)&u)[0]; c = (uint)-((int*)&u)[1]; }
        for (; i < na; i++) { var u = (ulong)a[i] /*  */ - c; r[i] = ((uint*)&u)[0]; c = (uint)-((int*)&u)[1]; }
        for (; i > 1 && r[i - 1] == 0; i--) ; r[-1] = i; Debug.Assert(c == 0);
      }
      static void mul(uint* a, uint* b, uint* r)
      {
        uint na = a[-1], nb = b[-1];
        if (na == 1)
        {
          if (nb == 1) { *(ulong*)r = (ulong)a[0] * b[0]; r[-1] = r[1] != 0 ? 2u : 1; return; }
          if (a[0] == 1) { for (int i = -1; i < nb; i++) r[i] = b[i]; return; }
        }
        if (nb == 1 && b[0] == 1) { for (int i = -1; i < na; i++) r[i] = a[i]; return; }
        uint nr = na + nb - 1; for (uint i = 0; i < nr; i++) r[i] = 0;
        for (uint i = na, k, c; i-- != 0;)
        {
          for (k = c = 0; k < nb; k++) { var t = (ulong)b[k] * a[i] + r[i + k] + c; r[i + k] = (uint)t; c = (uint)(t >> 32); }
          if (c == 0) continue;
          for (k = i + nb; c != 0 && k < nr; k++) { var t = (ulong)r[k] + c; r[k] = (uint)t; c = (uint)(t >> 32); }
          if (c == 0) continue; r[nr++] = c;
        }
        r[-1] = nr;
      }
      static void shl(uint* p, int c)
      {
        var s = c & 31; uint d = (uint)c >> 5, n = p[-1]; p[-1] = p[n] = 0;
        for (int i = (int)n; i >= 0; i--) p[i + d] = (p[i] << s) | (uint)((ulong)p[i - 1] >> (32 - s));
        for (int i = 0; i < d; i++) p[i] = 0;
        n += d; p[-1] = p[n] != 0 ? n + 1 : n;
      }
      static void shr(uint* p, int c)
      {
        int s = c & 31; uint k = (uint)c >> 5, i = 0, n = p[-1];
        for (; k + 1 < n; i++, k++) p[i] = (p[k] >> s) | (uint)((ulong)p[k + 1] << (32 - s));
        if (k < n) p[i++] = p[k] >> s;
        if (i != 0 && p[i - 1] == 0) i--; if (i == 0) p[i++] = 0; p[-1] = i;
      }
      static uint* gcd(uint* a, uint* b)
      {
        int shift = 0;
        if (a[0] == 0 || b[0] == 0)
        {
          int i1 = 0; for (; a[i1] == 0; i1++) ; i1 = clz(a[i1]) + (i1 << 5); if (i1 != 0) shr(a, i1);
          int i2 = 0; for (; b[i2] == 0; i2++) ; i2 = clz(b[i2]) + (i2 << 5); if (i2 != 0) shr(b, i2); shift = i1 < i2 ? i1 : i2;
        }
        for (; ; )
        {
          if (cmp(a, b) < 0) { var t = a; a = b; b = t; }
          uint max = a[-1], min = b[-1];
          if (min == 1)
          {
            if (max != 1)
            {
              if (b[0] == 0) break;
              ulong u = 0; for (var i = a[-1]; i-- != 0; u = (u << 32) | a[i], u %= b[0]) ;
              a[-1] = 1; if (u == 0) { a[0] = b[0]; break; }
              a[0] = (uint)u;
            }
            uint xa = a[0], xb = b[0]; for (; (xa > xb ? xa %= xb : xb %= xa) != 0;) ; a[0] = xa | xb; break;
          }
          if (max == 2)
          {
            var xa = a[-1] == 2 ? *(ulong*)a : a[0];
            var xb = b[-1] == 2 ? *(ulong*)b : b[0];
            for (; (xa > xb ? xa %= xb : xb %= xa) != 0;) ;
            *(ulong*)a = xa | xb; a[-1] = a[1] != 0 ? 2u : 1u; break;
          }
          if (min <= max - 2) { div(a, b, null); continue; }
          ulong uu1 = a[-1] >= max ? ((ulong)a[max - 1] << 32) | a[max - 2] : a[-1] == max - 1 ? a[max - 2] : 0;
          ulong uu2 = b[-1] >= max ? ((ulong)b[max - 1] << 32) | b[max - 2] : b[-1] == max - 1 ? b[max - 2] : 0;
          int cbit = chz(uu1 | uu2);
          if (cbit > 0)
          {
            uu1 = (uu1 << cbit) | (a[max - 3] >> (32 - cbit));
            uu2 = (uu2 << cbit) | (b[max - 3] >> (32 - cbit));
          }
          if (uu1 < uu2) { var t1 = uu1; uu1 = uu2; uu2 = t1; var t2 = a; a = b; b = t2; }
          if (uu1 == 0xffffffffffffffff || uu2 == 0xffffffffffffffff) { uu1 >>= 1; uu2 >>= 1; }
          if (uu1 == uu2) { sub(a, b, a); continue; }
          if ((uu2 >> 32) == 0) { div(a, b, null); continue; }
          uint ma = 1, mb = 0, mc = 0, md = 1;
          for (; ; )
          {
            uint uQuo = 1; ulong uuNew = uu1 - uu2;
            for (; uuNew >= uu2 && uQuo < 32; uuNew -= uu2, uQuo++) ;
            if (uuNew >= uu2)
            {
              ulong uuQuo = uu1 / uu2; if (uuQuo > 0xffffffff) break;
              uQuo = (uint)uuQuo; uuNew = uu1 - uQuo * uu2;
            }
            ulong uuAdNew = ma + (ulong)uQuo * mc;
            ulong uuBcNew = mb + (ulong)uQuo * md;
            if (uuAdNew > 0x7FFFFFFF || uuBcNew > 0x7FFFFFFF) break;
            if (uuNew < uuBcNew || uuNew + uuAdNew > uu2 - mc) break;
            ma = (uint)uuAdNew; mb = (uint)uuBcNew;
            uu1 = uuNew; if (uu1 <= mb) break;
            uQuo = 1; uuNew = uu2 - uu1;
            for (; uuNew >= uu1 && uQuo < 32; uuNew -= uu1, uQuo++) ;
            if (uuNew >= uu1)
            {
              ulong uuQuo = uu2 / uu1; if (uuQuo > 0xffffffff) break;
              uQuo = (uint)uuQuo; uuNew = uu2 - uQuo * uu1;
            }
            uuAdNew = md + (ulong)uQuo * mb;
            uuBcNew = mc + (ulong)uQuo * ma;
            if (uuAdNew > 0x7FFFFFFF || uuBcNew > 0x7FFFFFFF) break;
            if (uuNew < uuBcNew || uuNew + uuAdNew > uu1 - mb) break;
            md = (uint)uuAdNew; mc = (uint)uuBcNew;
            uu2 = uuNew; if (uu2 <= mc) break;
          }
          if (mb == 0) { if (uu1 / 2 >= uu2) div(a, b, null); else sub(a, b, a); continue; }
          int c1 = 0, c2 = 0; b[-1] = a[-1] = min;
          for (int iu = 0; iu < min; iu++)
          {
            uint u1 = a[iu], u2 = b[iu];
            long nn1 = (long)u1 * ma - (long)u2 * mb + c1; a[iu] = (uint)nn1; c1 = (int)(nn1 >> 32);
            long nn2 = (long)u2 * md - (long)u1 * mc + c2; b[iu] = (uint)nn2; c2 = (int)(nn2 >> 32);
          }
          while (a[-1] > 1 && a[a[-1] - 1] == 0) a[-1]--;
          while (b[-1] > 1 && b[b[-1] - 1] == 0) b[-1]--;
        }
        if (shift != 0) shl(a, shift);
        return a;
      }
      static void div(uint* a, uint* b, uint* m)
      {
        int na = (int)a[-1], nb = (int)b[-1];
        if (na < nb) { if (m == null) return; m[-1] = 1; m[0] = 0; return; }
        if (nb == 1)
        {
          ulong uu = 0, ub = b[0];
          for (int i = na; i-- != 0;) { uu = ((ulong)(uint)uu << 32) | a[i]; if (m != null) m[i] = (uint)(uu / ub); uu %= ub; }
          a[-1] = 1; a[0] = (uint)uu; if (m == null) return;
          for (; na > 1 && m[na - 1] == 0; na--) ; m[-1] = (uint)na; return;
        }
        if (nb == 2 && na == 2)
        {
          if (m != null) *(ulong*)m = *(ulong*)a / *(ulong*)b; *(ulong*)a %= *(ulong*)b;
          if (a[na - 1] == 0) a[-1] = 1; if (m == null) return;
          if (m[nb - 1] == 0) nb = 1; m[-1] = (uint)nb; return;
        }
        int diff = na - nb, nc = diff;
        for (int i = na - 1; ; i--)
        {
          if (i < diff) { nc++; break; }
          if (b[i - diff] != a[i]) { if (b[i - diff] < a[i]) nc++; break; }
        }
        if (nc == 0) { a[-1] = (uint)na; if (m == null) return; m[-1] = 1; m[0] = 0; return; }
        uint uden = b[nb - 1], unex = nb > 1 ? b[nb - 2] : 0;
        int shl = chz(uden), shr = 32 - shl;
        if (shl > 0)
        {
          uden = (uden << shl) | (unex >> shr); unex <<= shl;
          if (nb > 2) unex |= b[nb - 3] >> shr;
        }
        for (int i = nc; --i >= 0;)
        {
          uint hi = i + nb < na ? a[i + nb] : 0;
          ulong uu = ((ulong)hi << 32) | a[i + nb - 1];
          uint un = i + nb - 2 >= 0 ? a[i + nb - 2] : 0;
          if (shl > 0)
          {
            uu = (uu << shl) | (un >> shr); un <<= shl;
            if (i + nb >= 3) un |= a[i + nb - 3] >> shr;
          }
          ulong quo = uu / uden, rem = (uint)(uu % uden);
          if (quo > 0xffffffff) { rem += uden * (quo - 0xffffffff); quo = 0xffffffff; }
          while (rem <= 0xffffffff && quo * unex > (((ulong)(uint)rem << 32) | un)) { quo--; rem += uden; }
          if (quo > 0)
          {
            ulong bor = 0;
            for (int k = 0; k < nb; k++)
            {
              bor += b[k] * quo; uint sub = (uint)bor;
              bor >>= 32; if (a[i + k] < sub) bor++;
              a[i + k] -= sub;
            }
            if (hi < bor)
            {
              uint c = 0;
              for (int k = 0; k < nb; k++)
              {
                ulong t = (ulong)a[i + k] + b[k] + c;
                a[i + k] = (uint)t; c = (uint)(t >> 32);
              }
              quo--;
            }
            na = i + nb;
          }
          if (m != null) m[i] = (uint)quo;
        }
        for (; na > 1 && a[na - 1] == 0; na--) ; a[-1] = (uint)na; if (m == null) return;
        for (; nc > 1 && m[nc - 1] == 0; nc--) ; m[-1] = (uint)nc; return;
      }
      static int chz(ulong u)
      {
        return (u & 0xFFFFFFFF00000000) == 0 ? 32 + chz((uint)u) : chz((uint)(u >> 32));
      }
      static int chz(uint u)
      {
        int c = 0;
        if ((u & 0xFFFF0000) == 0) { c += 0x10; u <<= 0x10; }
        if ((u & 0xFF000000) == 0) { c += 0x08; u <<= 0x08; }
        if ((u & 0xF0000000) == 0) { c += 0x04; u <<= 0x04; }
        if ((u & 0xC0000000) == 0) { c += 0x02; u <<= 0x02; }
        if ((u & 0x80000000) == 0) { c += 0x01; }
        return c;
      }
      static int clz(uint u)
      {
        int c = 0;
        if ((u & 0x0000FFFF) == 0) { c += 0x10; u >>= 0x10; }
        if ((u & 0x000000FF) == 0) { c += 0x08; u >>= 0x08; }
        if ((u & 0x0000000F) == 0) { c += 0x04; u >>= 0x04; }
        if ((u & 0x00000003) == 0) { c += 0x02; u >>= 0x02; }
        if ((u & 0x00000001) == 0) { c += 0x01; }
        return c;
      }
      [ThreadStatic]
      static uint* threadroot;
      static readonly uint* mainroot = reserve();
      static uint* reserve() { var p = stackalloc uint[100000]; return p + 2; }
      static uint* getroot()
      {
        var p = mainroot; if (&p < p + 200000) return p;
        p = threadroot; if (p != null) return p;
        return threadroot = reserve();
      }
    }

    public struct Vector2 : IEquatable<Vector2>
    {
      public Number X, Y;
      public override string ToString()
      {
        return string.Format("{0}; {1}", X.ToString(), Y.ToString());
      }
      public override int GetHashCode()
      {
        var h1 = (uint)X.GetHashCode();
        var h2 = (uint)Y.GetHashCode();
        h2 = ((h2 << 7) | (h1 >> 25)) ^ h1;
        h1 = ((h1 << 7) | (h2 >> 25)) ^ h2;
        return (int)h1;
      }
      public bool Equals(Vector2 v)
      {
        return X.Equals(v.X) && Y.Equals(v.Y);
      }
      public override bool Equals(object p)
      {
        return p is Vector2 && Equals((Vector2)p);
      }
      public Vector2(Number x, Number y)
      {
        X = x; Y = y;
      }
      public Number this[int i]
      {
        get { return i == 0 ? X : Y; }
        set { if (i == 0) X = value; else Y = value; }
      }
      public void Normalize()
      {
        int i = Number.Abs(Y) > Number.Abs(X) ? 1 : 0;
        var l = this[i]; if (l.Sign < 0) l = -l; X /= l; Y /= l;
      }
      public static implicit operator Vector2(float2 p)
      {
        Vector2 b; b.X = p.x; b.Y = p.y; return b;
      }
      public static explicit operator float2(Vector2 p)
      {
        float2 b;
        b.x = (float)(double)p.X;
        b.y = (float)(double)p.Y;
        return b;
      }
      public static bool operator ==(Vector2 a, Vector2 b)
      {
        return a.X.Equals(b.X) && a.Y.Equals(b.Y);
      }
      public static bool operator !=(Vector2 a, Vector2 b)
      {
        return !a.X.Equals(b.X) || !a.Y.Equals(b.Y);
      }
      public static Vector2 operator -(Vector2 v)
      {
        v.X = -v.X; v.Y = -v.Y; return v;
      }
      public static Vector2 operator +(Vector2 a, Vector2 b)
      {
        a.X += b.X; a.Y += b.Y; return a;
      }
      public static Vector2 operator -(Vector2 a, Vector2 b)
      {
        a.X -= b.X; a.Y -= b.Y; return a;
      }
      public static Vector2 operator *(Vector2 v, Number f)
      {
        v.X *= f; v.Y *= f; return v;
      }
      public static Vector2 operator /(Vector2 v, Number f)
      {
        v.X /= f; v.Y /= f; return v;
      }
      public static Vector2 operator ~(Vector2 v)
      {
        Vector2 b; b.X = -v.Y; b.Y = v.X; return b;
      }
      public static Number Dot(Vector2 a)
      {
        return 0 | a.X * a.X + a.Y * a.Y;
      }
      public static Number Dot(Vector2 a, Vector2 b)
      {
        return 0 | a.X * b.X + a.Y * b.Y;
      }
      public static Number Ccw(Vector2 a, Vector2 b)
      {
        return 0 | a.X * b.Y - a.Y * b.X;
      }
      public static int DotSign(Vector2 a, Vector2 b)
      {
        return 0 ^ a.X * b.X + a.Y * b.Y;
      }
      public static int CcwSign(Vector2 a, Vector2 b)
      {
        return 0 ^ a.X * b.Y - a.Y * b.X;
      }
      public static Vector2 SinCos(double a)
      {
        Vector2 v;
        v.X = Math.Round(Math.Cos(a), 15);
        v.Y = Math.Round(Math.Sin(a), 15); return v;
        //const double epsilon = 0.001 * Math.PI / 180; // 0.1% of a degree
        //Vector2 v; a = Math.IEEERemainder(a, Math.PI * 2);
        //if (a > -epsilon && a < epsilon) { v.Y = 1; v.X = 0; }
        //else if (a > +Math.PI / 2 - epsilon && a < +Math.PI / 2 + epsilon) { v.Y = 0; v.X = 1; }
        //else if (a < -Math.PI + epsilon || a > Math.PI - epsilon) { v.Y = -1; v.X = 0; }
        //else if (a > -Math.PI / 2 - epsilon && a < -Math.PI / 2 + epsilon) { v.Y = 0; v.X = -1; }
        //else { v.Y = Math.Cos(a); v.X = Math.Sin(a); }
        //return v;
      }
    }

    public struct Vector3 : IEquatable<Vector3>, IComparable<Vector3>
    {
      public Number X, Y, Z;
      public override string ToString()
      {
        return string.Format("{0}; {1}; {2}", X.ToString(), Y.ToString(), Z.ToString());
      }
      public override int GetHashCode()
      {
        var h1 = (uint)X.GetHashCode();
        var h2 = (uint)Y.GetHashCode();
        var h3 = (uint)Z.GetHashCode();
        h2 = ((h2 << 7) | (h3 >> 25)) ^ h3;
        h1 = ((h1 << 7) | (h2 >> 25)) ^ h2;
        return (int)h1;
      }
      public bool Equals(Vector3 v)
      {
        return X.Equals(v.X) && Y.Equals(v.Y) && Z.Equals(v.Z);
      }
      public int CompareTo(Vector3 b)
      {
        int i;
        if ((i = X.CompareTo(b.X)) != 0) return i;
        if ((i = Y.CompareTo(b.Y)) != 0) return i;
        if ((i = Z.CompareTo(b.Z)) != 0) return i;
        //if (!X.Equals(b.X)) return 0 ^ X - b.X;
        //if (!Y.Equals(b.Y)) return 0 ^ Y - b.Y;
        //if (!Z.Equals(b.Z)) return 0 ^ Z - b.Z;
        return 0;
      }
      public override bool Equals(object p)
      {
        return p is Vector3 && Equals((Vector3)p);
      }
      public Vector3(Number x, Number y, Number z)
      {
        X = x; Y = y; Z = z;
      }
      public Number this[int i]
      {
        get { return i == 0 ? X : i == 1 ? Y : Z; }
        set { if (i == 0) X = value; else if (i == 1) Y = value; else Z = value; }
      }
      public static implicit operator Vector3(float3 p)
      {
        Vector3 b; b.X = p.x; b.Y = p.y; b.Z = p.z; return b;
      }
      public static explicit operator float3(Vector3 p)
      {
        float3 b;
        b.x = (float)(double)p.X;
        b.y = (float)(double)p.Y;
        b.z = (float)(double)p.Z;
        return b;
      }
      public static implicit operator Vector3(Vector2 p)
      {
        Vector3 b; b.X = p.X; b.Y = p.Y; b.Z = 0; return b;
      }
      public static explicit operator Vector2(Vector3 p)
      {
        Vector2 b; b.X = p.X; b.Y = p.Y; return b;
      }
      public static bool operator ==(Vector3 a, Vector3 b)
      {
        return a.X.Equals(b.X) && a.Y.Equals(b.Y) && a.Z.Equals(b.Z);
      }
      public static bool operator !=(Vector3 a, Vector3 b)
      {
        return !a.X.Equals(b.X) || !a.Y.Equals(b.Y) || !a.Z.Equals(b.Z);
      }
      public static Vector3 operator -(Vector3 v)
      {
        v.X = -v.X; v.Y = -v.Y; v.Z = -v.Z; return v;
      }
      public static Vector3 operator +(Vector3 a, Vector3 b)
      {
        a.X += b.X; a.Y += b.Y; a.Z += b.Z; return a;
      }
      public static Vector3 operator -(Vector3 a, Vector3 b)
      {
        a.X -= b.X; a.Y -= b.Y; a.Z -= b.Z; return a;
      }
      public static Vector3 operator *(Vector3 v, Number f)
      {
        v.X *= f; v.Y *= f; v.Z *= f; return v;
      }
      public static Vector3 operator /(Vector3 v, Number f)
      {
        v.X /= f; v.Y /= f; v.Z /= f; return v;
      }
      public static Number Dot(Vector3 a)
      {
        return 0 | a.X * a.X + a.Y * a.Y + a.Z * a.Z;
      }
      public static Number Dot(Vector3 a, Vector3 b)
      {
        return 0 | a.X * b.X + a.Y * b.Y + a.Z * b.Z;
      }
      public static Vector3 Ccw(Vector3 a, Vector3 b)
      {
        Vector3 c;
        c.X = 0 | a.Y * b.Z - a.Z * b.Y;
        c.Y = 0 | a.Z * b.X - a.X * b.Z;
        c.Z = 0 | a.X * b.Y - a.Y * b.X;
        return c;
      }
      public static int LongAxis(ref Vector3 v)
      {
        int i = 0;
        if (Number.Abs(v.Y) > Number.Abs(v.X)) i = 1;
        if (Number.Abs(v.Z) > Number.Abs(i == 0 ? v.X : v.Y)) i = 2;
        return i;
      }
      public static int ShortAxis(ref Vector3 v)
      {
        int i = 0;
        if (Number.Abs(v.Y) < Number.Abs(v.X)) i = 1;
        if (Number.Abs(v.Z) < Number.Abs(i == 0 ? v.X : v.Y)) i = 2;
        return i;
      }
      public static bool Inline(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3)
      {
        //if ((0 ^ ((p2.X - p1.X) * (p3.Y - p2.Y) - (p2.Y - p1.Y) * (p3.X - p2.X))) != 0) return false;
        //if ((0 ^ ((p2.Y - p1.Y) * (p3.Z - p2.Z) - (p2.Z - p1.Z) * (p3.Y - p2.Y))) != 0) return false;
        //if ((0 ^ ((p2.Z - p1.Z) * (p3.X - p2.X) - (p2.X - p1.X) * (p3.Z - p2.Z))) != 0) return false;
        //return true;
        using (Number.Mach mach = 2)
        {
          var ax = p2.X - p1.X; var bx = p3.X - p2.X;
          var ay = p2.Y - p1.Y; var by = p3.Y - p2.Y;
          if ((ax * by - ay * bx).Sign != 0) return false;
          var az = p2.Z - p1.Z; var bz = p3.Z - p2.Z;
          if ((ay * bz - az * by).Sign != 0) return false;
          if ((az * bx - ax * bz).Sign != 0) return false;
          return true;
        }
      }
      public void Normalize()
      {
        var l = this[Vector3.LongAxis(ref this)];
        if (l.Sign < 0) l = -l; X /= l; Y /= l; Z /= l;
      }
    }

    public struct Plane : IEquatable<Plane>, IComparable<Plane>
    {
      public Vector3 N; public Number D;
      public override string ToString()
      {
        return string.Format("{0}; {1}; {2}; {3}", N.X.ToString(), N.Y.ToString(), N.Z.ToString(), D.ToString());
      }
      public override int GetHashCode()
      {
        //unchecked
        //{
        //  int hash = (int)2166136261;
        //  hash = hash * 16777619 ^ N.GetHashCode();
        //  hash = hash * 16777619 ^ D.GetHashCode();
        //  return hash;
        //}
        //int hash = 17;
        //hash = hash * 23 ^ N.GetHashCode();
        //hash = hash * 23 ^ D.GetHashCode();
        //return hash;
        var h1 = (uint)N.GetHashCode();
        var h2 = (uint)D.GetHashCode();
        h2 = ((h2 << 7) | (h1 >> 25)) ^ h1;
        h1 = ((h1 << 7) | (h2 >> 25)) ^ h2;
        return (int)h1;
      }
      public override bool Equals(object p)
      {
        return p is Plane && Equals((Plane)p);
      }
      public bool Equals(Plane p)
      {
        return N.Equals(p.N) && D.Equals(p.D);
      }
      public int CompareTo(Plane b)
      {
        var i = N.CompareTo(b.N); if (i != 0) return i;
        return D.CompareTo(b.D);
        //if (!D.Equals(b.D)) return 0 ^ D - b.D; return 0;
      }
      public static bool operator ==(Plane a, Plane b)
      {
        return a.Equals(b);
      }
      public static bool operator !=(Plane a, Plane b)
      {
        return !a.Equals(b);
      }
      //public static implicit operator Plane(codxcs.Plane p)
      //{
      //  Plane b; b.N = p.N; b.D = p.D; return b;
      //}
      //public static explicit operator codxcs.Plane(Plane p)
      //{
      //  codxcs.Plane b; b.N = (codxcs.Vector3)p.N; b.D = (float)(double)p.D; return b;
      //}

      public static Plane FromPointNormal(ref Vector3 p, ref Vector3 n)
      {
        //Plane v; v.N = n; v.D = -(0 | p.X * n.X + p.Y * n.Y + p.Z * n.Z); return v;
        using (Number.Mach mach = 2) { Plane e; e.N = n; e.D = -(p.X * n.X + p.Y * n.Y + p.Z * n.Z); e.Normalize(); return e; }
      }
      public static Plane FromPointNormal(Vector3 p, Vector3 n)
      {
        return FromPointNormal(ref p, ref n);
      }
      public static Plane FromPoints(ref Vector3 a, ref Vector3 b, ref Vector3 c)
      {
        //Vector3 u; u.X = c.X - b.X; u.Y = c.Y - b.Y; u.Z = c.Z - b.Z;
        //Vector3 v; v.X = a.X - b.X; v.Y = a.Y - b.Y; v.Z = a.Z - b.Z;
        //return FromPointNormal(a, Vector3.Ccw(u, v));
        using (Number.Mach mach = 2)
        {
          var ux = c.X - b.X; var uy = c.Y - b.Y; var uz = c.Z - b.Z;
          var vx = a.X - b.X; var vy = a.Y - b.Y; var vz = a.Z - b.Z; Plane e;
          e.N.X = uy * vz - uz * vy;
          e.N.Y = uz * vx - ux * vz;
          e.N.Z = ux * vy - uy * vx;
          e.D = -(a.X * e.N.X + a.Y * e.N.Y + a.Z * e.N.Z);
          e.Normalize(); return e;
        }
      }
      public static Plane FromPoints(Vector3 a, Vector3 b, Vector3 c)
      {
        return FromPoints(ref a, ref b, ref c);
      }
      public static Plane FromPointsNormal(ref Vector3 a, ref Vector3 b, ref Vector3 n)
      {
        var c = a + n; return FromPoints(ref a, ref b, ref c);
      }

      public Number Dot(ref Vector3 p)
      {
        return 0 | N.X * p.X + N.Y * p.Y + N.Z * p.Z;
      }
      public Number DotCoord(ref Vector3 p)
      {
        return 0 | N.X * p.X + N.Y * p.Y + N.Z * p.Z + D;
      }
      public int DotSign(ref Vector3 p)
      {
        return 0 ^ N.X * p.X + N.Y * p.Y + N.Z * p.Z;
      }
      public int DotCoordSign(ref Vector3 p)
      {
        return 0 ^ N.X * p.X + N.Y * p.Y + N.Z * p.Z + D;
      }
      public static Plane operator -(Plane p)
      {
        p.N.X = -p.N.X; p.N.Y = -p.N.Y; p.N.Z = -p.N.Z; p.D = -p.D; return p;
      }
      public void Normalize()
      {
        var l = Number.Abs(N[Vector3.LongAxis(ref N)]);
        N.X = 0 | N.X / l;
        N.Y = 0 | N.Y / l;
        N.Z = 0 | N.Z / l; D = 0 | D / l;
      }

      //Vector3 intersect1(ref Vector3 a, ref Vector3 b)
      //{
      //  var u = 0 | N.X * a.X + N.Y * a.Y + N.Z * a.Z;
      //  var v = 0 | N.X * b.X + N.Y * b.Y + N.Z * b.Z;
      //  var w = 0 | (u + D) / (u - v); Vector3 r;
      //  r.X = 0 | a.X + (b.X - a.X) * w;
      //  r.Y = 0 | a.Y + (b.Y - a.Y) * w;
      //  r.Z = 0 | a.Z + (b.Z - a.Z) * w; return r;
      //}
      //Vector3 intersect2(ref Vector3 a, ref Vector3 b)
      //{
      //  using (Number.Mach mach = 2)
      //  {
      //    var u = N.X * a.X + N.Y * a.Y + N.Z * a.Z;
      //    var v = N.X * b.X + N.Y * b.Y + N.Z * b.Z;
      //    var w = (u + D) / (u - v); Vector3 r;
      //    r.X = 0 | a.X + (b.X - a.X) * w;
      //    r.Y = 0 | a.Y + (b.Y - a.Y) * w;
      //    r.Z = 0 | a.Z + (b.Z - a.Z) * w; return r;
      //  }
      //}
      //public static void Test()
      //{
      //  var ar = new Archive(System.IO.File.ReadAllBytes("d:\\test.x"));
      //  var plane = ar.Read<Plane>();
      //  var a = ar.Read<Vector3>();
      //  var b = ar.Read<Vector3>();
      //  var p1 = plane.intersect1(ref a, ref b);
      //  var p2 = plane.intersect2(ref a, ref b);
      //  if (p1 != p2) { } else { }
      //}


      public Vector3 Intersect(ref Vector3 a, ref Vector3 b)
      {
        using (Number.Mach mach = 2)
        {
          var u = N.X * a.X + N.Y * a.Y + N.Z * a.Z;
          var v = N.X * b.X + N.Y * b.Y + N.Z * b.Z;
          var w = (u + D) / (u - v); Vector3 r;
          r.X = 0 | a.X + (b.X - a.X) * w;
          r.Y = 0 | a.Y + (b.Y - a.Y) * w;
          r.Z = 0 | a.Z + (b.Z - a.Z) * w; return r;
        }
        //var u = 0 | N.X * a.X + N.Y * a.Y + N.Z * a.Z;
        //var v = 0 | N.X * b.X + N.Y * b.Y + N.Z * b.Z;
        //var w = 0 | (u + D) / (u - v); Vector3 r;
        //r.X = 0 | a.X + (b.X - a.X) * w;
        //r.Y = 0 | a.Y + (b.Y - a.Y) * w;
        //r.Z = 0 | a.Z + (b.Z - a.Z) * w; return r;
      }
      public static Plane Transform(Plane p, Matrix m)
      {
        m = m.Inverse(); var n = p.N; var d = p.D;
        p.N.X = 0 | n.X * m.M11 + n.Y * m.M12 + n.Z * m.M13; // + d * m.M14;
        p.N.Y = 0 | n.X * m.M21 + n.Y * m.M22 + n.Z * m.M23; // + d * m.M24;
        p.N.Z = 0 | n.X * m.M31 + n.Y * m.M32 + n.Z * m.M33; // + d * m.M34;
        p.D = 0 | n.X * m.M41 + n.Y * m.M42 + n.Z * m.M43 + d; // * m.M44;
        return p;
      }
      public static Vector3 Intersect(ref Plane a, ref Plane b, ref Plane c)
      {
        var d = -Vector3.Dot(Vector3.Ccw(a.N, b.N), c.N);
        return (Vector3.Ccw(b.N, c.N) * a.D + Vector3.Ccw(c.N, a.N) * b.D + Vector3.Ccw(a.N, b.N) * c.D) / d;
      }
      public static bool Intersect(ref Plane a, ref Plane b, ref Plane c, out Vector3 result)
      {
        var d = -Vector3.Dot(Vector3.Ccw(a.N, b.N), c.N);
        if (d.Sign == 0) { result = new Vector3(); return false; }
        result = (Vector3.Ccw(b.N, c.N) * a.D + Vector3.Ccw(c.N, a.N) * b.D + Vector3.Ccw(a.N, b.N) * c.D) / d;
        return true;
      }
      public static Vector3 AxisPoint(ref Plane p)
      {
        var i = Vector3.LongAxis(ref p.N);
        return new Vector3(i == 0 ? -p.D / p.N.X : 0, i == 1 ? -p.D / p.N.Y : 0, i == 2 ? -p.D / p.N.Z : 0);
      }
    }

    public struct Matrix
    {
      public Number M11, M12, M13;
      public Number M21, M22, M23;
      public Number M31, M32, M33;
      public Number M41, M42, M43;
      public static implicit operator Matrix(float4x3 p)
      {
        Matrix b;
        b.M11 = p._11;
        b.M12 = p._12;
        b.M13 = p._13;
        b.M21 = p._21;
        b.M22 = p._22;
        b.M23 = p._23;
        b.M31 = p._31;
        b.M32 = p._32;
        b.M33 = p._33;
        b.M41 = p._41;
        b.M42 = p._42;
        b.M43 = p._43;
        return b;
      }
      //public static implicit operator Matrix34(Matrix p)
      //{
      //  return (codxcs.Matrix34)p;
      //}
      public void Transform(ref Vector3 p)
      {
        var x = 0 | M11 * p.X + M21 * p.Y + M31 * p.Z + M41;
        var y = 0 | M12 * p.X + M22 * p.Y + M32 * p.Z + M42;
        var z = 0 | M13 * p.X + M23 * p.Y + M33 * p.Z + M43;
        p.X = x; p.Y = y; p.Z = z;
      }
      public static Matrix Multiply(ref Matrix a, ref Matrix b)
      {
        Number x = a.M11, y = a.M12, z = a.M13; Matrix r;
        r.M11 = 0 | b.M11 * x + b.M21 * y + b.M31 * z;
        r.M12 = 0 | b.M12 * x + b.M22 * y + b.M32 * z;
        r.M13 = 0 | b.M13 * x + b.M23 * y + b.M33 * z; x = a.M21; y = a.M22; z = a.M23;
        r.M21 = 0 | b.M11 * x + b.M21 * y + b.M31 * z;
        r.M22 = 0 | b.M12 * x + b.M22 * y + b.M32 * z;
        r.M23 = 0 | b.M13 * x + b.M23 * y + b.M33 * z; x = a.M31; y = a.M32; z = a.M33;
        r.M31 = 0 | b.M11 * x + b.M21 * y + b.M31 * z;
        r.M32 = 0 | b.M12 * x + b.M22 * y + b.M32 * z;
        r.M33 = 0 | b.M13 * x + b.M23 * y + b.M33 * z; x = a.M41; y = a.M42; z = a.M43;
        r.M41 = 0 | b.M11 * x + b.M21 * y + b.M31 * z + b.M41;
        r.M42 = 0 | b.M12 * x + b.M22 * y + b.M32 * z + b.M42;
        r.M43 = 0 | b.M13 * x + b.M23 * y + b.M33 * z + b.M43; return r;
      }
      public Matrix Inverse()
      {
        using (Number.Mach mach = 2)
        {
          var b0 = M31 * M42 - M32 * M41;
          var b1 = M31 * M43 - M33 * M41;
          var b3 = M32 * M43 - M33 * M42;
          var d1 = M22 * M33 + M23 * -M32;
          var d2 = M21 * M33 + M23 * -M31;
          var d3 = M21 * M32 + M22 * -M31;
          var d4 = M21 * b3 + M22 * -b1 + M23 * b0;
          var de = 1 / (M11 * d1 - M12 * d2 + M13 * d3);
          var a0 = M11 * M22 - M12 * M21;
          var a1 = M11 * M23 - M13 * M21;
          var a3 = M12 * M23 - M13 * M22;
          var d5 = M12 * M33 + M13 * -M32;
          var d6 = M11 * M33 + M13 * -M31;
          var d7 = M11 * M32 + M12 * -M31;
          var d8 = M11 * b3 + M12 * -b1 + M13 * b0;
          var d9 = M41 * a3 + M42 * -a1 + M43 * a0;
          Matrix m;
          m.M11 = 0 | d1 * de; m.M12 = 0 | -d5 * de; m.M13 = 0 | a3 * de;
          m.M21 = 0 | -d2 * de; m.M22 = 0 | d6 * de; m.M23 = 0 | -a1 * de;
          m.M31 = 0 | d3 * de; m.M32 = 0 | -d7 * de; m.M33 = 0 | a0 * de;
          m.M41 = 0 | -d4 * de; m.M42 = 0 | d8 * de; m.M43 = 0 | -d9 * de;
          return m;
        }
      }
      public static Matrix operator *(Matrix a, Matrix b)
      {
        return Multiply(ref a, ref b);
      }
      public static Matrix Identity()
      {
        return new Matrix { M11 = 1, M22 = 1, M33 = 1 };
      }
      public static Matrix Translation(Number x, Number y, Number z)
      {
        return new Matrix { M11 = 1, M22 = 1, M33 = 1, M41 = x, M42 = y, M43 = z };
      }
      public static Matrix Scaling(Number x, Number y, Number z)
      {
        return new Matrix { M11 = x, M22 = y, M33 = z };
      }
      public static Matrix RotationX(double a)
      {
        var sc = Vector2.SinCos(a);
        return new Matrix { M11 = 1, M22 = sc.Y, M23 = sc.X, M32 = -sc.X, M33 = sc.Y };
        //Number s = Math.Sin(a), c = Math.Cos(a);
        //return new Matrix34 { M11 = 1, M22 = c, M23 = s, M32 = -s, M33 = c };
      }
      public static Matrix RotationY(double a)
      {
        var sc = Vector2.SinCos(a);
        return new Matrix { M11 = sc.Y, M13 = -sc.X, M22 = 1, M31 = sc.X, M33 = sc.Y };
        //Number s = Math.Sin(a), c = Math.Cos(a);
        //return new Matrix34 { M11 = c, M13 = -s, M22 = 1, M31 = s, M33 = c };
      }
      public static Matrix RotationZ(double a)
      {
        var sc = Vector2.SinCos(a);
        return new Matrix { M11 = sc.Y, M12 = sc.X, M21 = -sc.X, M22 = sc.Y, M33 = 1 };
        //Number s = Math.Sin(a), c = Math.Cos(a);
        //return new Matrix34 { M11 = c, M12 = s, M21 = -s, M22 = c, M33 = 1 };
      }
      public Vector3 this[int i]
      {
        get
        {
          Vector3 p;
          switch (i)
          {
            case 00: p.X = M11; p.Y = M12; p.Z = M13; break;
            case 01: p.X = M21; p.Y = M22; p.Z = M23; break;
            case 02: p.X = M31; p.Y = M32; p.Z = M33; break;
            default: p.X = M41; p.Y = M42; p.Z = M43; break;
          }
          return p;
        }
        set
        {
          switch (i)
          {
            case 0: M11 = value.X; M12 = value.Y; M13 = value.Z; break;
            case 1: M21 = value.X; M22 = value.Y; M23 = value.Z; break;
            case 2: M31 = value.X; M32 = value.Y; M33 = value.Z; break;
            case 3: M41 = value.X; M42 = value.Y; M43 = value.Z; break;
          }
        }
      }
    }

    public class Polyhedron : IEquatable<Polyhedron>
    {
      Vector3[] points; ushort[] indices;

      private Polyhedron() { }
      private Polyhedron(List<Vector3> pp, List<ushort> ii)
      {
        points = pp.ToArray(); indices = ii.ToArray();
        //var test = closed(indices, indices.Length);
      }
      public bool Equals(Polyhedron b)
      {
        if (indices == b.indices && points == b.points) return true;
        if (indices.Length != b.indices.Length || points.Length != b.points.Length) return false;
        fixed (ushort* x = indices, y = b.indices) if (memcmp(x, y, (void*)(indices.Length << 1)) != 0) return false;
        for (int i = 0, n = points.Length; i < n; i++) if (points[i].Equals(b.points[i])) return false;
        return true; //indices = b.indices; points = b.points; return true;
      }
      [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
      internal static extern int memcmp(void* a, void* b, void* n);

      public IList<Vector3> Points
      {
        get { return new ReadOnlyCollection<Vector3>(points); }
      }
      public IList<ushort> Indices
      {
        get { return new ReadOnlyCollection<ushort>(indices); }
      }
      public IList<Plane> Planes
      {
        get { return new ReadOnlyCollection<Plane>(getplanes(points, indices, indices.Length, null, 0)); }
      }
      public IList<int> PlaneMapping
      {
        get { return new ReadOnlyCollection<int>(getplanmap(indices, indices.Length, null, 0)); }
      }
      public IList<int> Adjacencies
      {
        get { return new ReadOnlyCollection<int>(getadj(indices, indices.Length, null, 0)); }
      }
      public IList<Vector3> BoundingBox
      {
        get
        {
          var c = points.Length; var b = new Vector3[c != 0 ? 2 : 0];
          if (c != 0)
          {
            b[0] = b[1] = points[0];
            for (int i = 1; i < points.Length; i++)
              for (int t = 0; t < 3; t++)
              {
                var v = points[i][t];
                if (b[0][t] > v) b[0][t] = v;
                if (b[1][t] < v) b[1][t] = v;
              }
          }
          return new ReadOnlyCollection<Vector3>(b);
        }
      }

      //public IList<int> Edges
      //{
      //  get
      //  {
      //    var map = PlaneMapping;
      //    return new ReadOnlyCollection<int>(Adjacencies.
      //      Select((a, i) => a == -1 || (a < i && map[a / 3] != map[i / 3]) ? indices[i] | (indices[mod(i, 1)] << 16) : 0).
      //      Where(l => l != 0).ToArray());
      //  }
      //}
      //public IList<int> PointAdjacencies
      //{
      //  get { return new ReadOnlyCollection<int>(Adjacencies.Select((i, k) => indices[mod(i != -1 ? i : k, 2)]).ToArray()); }
      //}
      //public IList<int> Dx11Adjacencies
      //{
      //  get { return new ReadOnlyCollection<int>(Adjacencies.Select((i, k) => k | (indices[mod(i != -1 ? i : k, 2)] << 16)).ToArray()); }
      //}

      public bool Closed
      {
        get { return indices.Length == 0 || decode(indices, 0) != 0; }
      }
      public bool Normalized
      {
        get { return indices.Length == 0 || decode(indices, 0) == 2; }
      }
      public int EulerNumber //https://en.wikipedia.org/wiki/Euler_characteristic
      {
        get { return points.Length - indices.Length / 2 + indices.Length / 3; }
      }

      public static Polyhedron Create(float3[] pp, int[] ii)
      {
        var tess = gettess();
        var ppp = tess.GetHashSet<Vector3>(0);
        var iii = tess.GetList<ushort>(0);
        for (int i = 0, ni = ii.Length; i < ni; i++) iii.Add((ushort)ppp.Add(pp[ii[i]]));
        retess(ppp, iii, 1); return new Polyhedron(ppp, iii);
      }
      public static Polyhedron Create(INode a)
      {
        float3* pp; var np = a.GetBufferPtr(BUFFER.POINTBUFFER, (void**)&pp) / sizeof(float3);
        ushort* ii; var ni = a.GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&ii) / sizeof(ushort);
        var tess = gettess(); //todo: codxcs.Vector3 cache -> upcast
        var ppp = tess.GetHashSet<Vector3>(0);
        var iii = tess.GetList<ushort>(0);
        for (int i = 0; i < ni; i++) iii.Add((ushort)ppp.Add(pp[ii[i]]));
        retess(ppp, iii, 1); return new Polyhedron(ppp, iii);
      }
      //public static Polyhedron Create2(INode a)
      //{
      //  float3* pp; var np = a.GetBufferPtr(BUFFER.POINTBUFFER, (void**)&pp) / sizeof(float3);
      //  ushort* ii; var ni = a.GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&ii) / sizeof(ushort);
      //  var vv = new Vector3[np]; for (int i = 0; i < np; i++) vv[i] = pp[i];
      //  var r = new Polyhedron(); r.points = vv;
      //  fixed (void* p = r.indices = new ushort[ni]) Native.memcpy(p, ii, (void*)(ni * sizeof(ushort)));
      //  return r;
      //}
      struct d3 { internal double x, y, z; }
      public CSG.IMesh ToIMesh()
      {
        var m = CSG.Factory.CreateMesh();
        var pp = points.Select(p => new d3 { x = (double)p.X, y = (double)p.Y, z = (double)p.Z }).ToArray();
        fixed (ushort* ii = indices)
        fixed (d3* tt = pp)
          m.Update(new CSG.Variant(&tt->x, 3, pp.Length), new CSG.Variant(ii, 1, indices.Length));
        return m;
      }

      public static Polyhedron operator +(Polyhedron a, Vector3 offset)
      {
        var b = new Polyhedron { points = (Vector3[])a.points.Clone(), indices = (ushort[])a.indices.Clone() };
        var p = b.points; for (int i = 0; i < p.Length; i++) p[i] += offset; return b;
      }
      public static Polyhedron operator +(Polyhedron a, Matrix m)
      {
        var b = new Polyhedron { points = (Vector3[])a.points.Clone(), indices = (ushort[])a.indices.Clone() };
        var p = b.points; for (int i = 0; i < p.Length; i++) m.Transform(ref p[i]); return b;
      }
      public static Polyhedron operator +(Polyhedron a, Polyhedron b)
      {
        var c = union(a, b, false);

        var ii = c.indices; var ni = ii.Length;
        var kk = stackalloc int[3 + (ni << 1)]; kk += 3;
        for (int i = 0; i < ni; i += 3)
        {
          xor(kk, ii[i + 1], ii[i + 0]);
          xor(kk, ii[i + 2], ii[i + 1]);
          xor(kk, ii[i + 0], ii[i + 2]);
        }
        if (kk[-3] != 0) encode(ii, 0);

        return c;
      }
      public static Polyhedron operator -(Polyhedron a, Polyhedron b)
      {
        return sub(a, b);
      }
      public static Polyhedron operator -(Polyhedron a, Plane halfspace)
      {
        return cut(a, halfspace);
      }
      public static Polyhedron operator &(Polyhedron a, Polyhedron b)
      {
        return a - (a ^ b);
      }
      public static Polyhedron operator |(Polyhedron a, Polyhedron b)
      {
        return union(a, b, true);
      }
      public static Polyhedron operator ^(Polyhedron a, Polyhedron b)
      {
        return union(a - b, b - a, false);
      }

      public static Polyhedron Tesselate(Vector2[] points, int[] counts = null)
      {
        var tess = gettess(); tess.boundary = true; tess.normal = new Vector3(0, 0, 1); tess.winding = Winding.Positive;
        tess.BeginPolygon(); var nc = counts != null ? counts.Length : 1;
        for (int i = 0, k = 0; i < nc; i++)
        {
          tess.BeginContour(); var c = counts != null ? counts[i] : points.Length;
          for (int t = 0; t < c; t++) { var p = (Vector3)points[k + t]; tess.AddVertex(ref p); }
          tess.EndContour(); k += c;
        }
        tess.EndPolygon();
        tesscontours(tess);
        var ii = tess.indices.p; for (int i = 0, ni = tess.indices.n; i < ni; i += 3) encode(ii, i, 0);
        return new Polyhedron(tess.vertices, tess.indices);
      }
      public static Polyhedron Tesselate(Vector3[] points, int[] counts = null)
      {
        var n = new Vector3();
        for (int i = 0, c = counts != null ? counts[0] : points.Length, j = c - 1; i < c; j = i++)
        {
          var a = points[i]; var b = points[j];
          n.X = 0 | n.X + (a.Y + b.Y) * (a.Z - b.Z);
          n.Y = 0 | n.Y + (a.Z + b.Z) * (a.X - b.X);
          n.Z = 0 | n.Z + (a.X + b.X) * (a.Y - b.Y);
        }
        var nc = counts != null ? counts.Length : 1;
        var tess = gettess(); tess.boundary = true; tess.normal = n; tess.winding = Winding.Positive;
        tess.BeginPolygon();
        for (int i = 0, k = 0; i < nc; i++)
        {
          tess.BeginContour(); var c = counts != null ? counts[i] : points.Length;
          for (int t = 0; t < c; t++) { var p = points[k + t]; tess.AddVertex(ref p); }
          tess.EndContour(); k += c;
        }
        tess.EndPolygon();
        tesscontours(tess);
        var ii = tess.indices.p; for (int i = 0, ni = tess.indices.n; i < ni; i += 3) encode(ii, i, 0);
        return new Polyhedron(tess.vertices, tess.indices);
      }

      public static Polyhedron Skeleton(Vector2[][][] data)
      {
        Number zmax = 0x7fff; var z1 = zmax; for (int i = 0; i < data.Length; i++) z1 = Number.Min(z1, data[i][0][1].X);

        var tess = gettess();
        var points = tess.GetHashSet<Vector3>(0);
        var planes = tess.GetHashSet<Plane>(0);
        var collector = tess.GetList<int>(0);
        var normals = tess.GetList<Vector3>(0);
        var buffer = tess.GetList<__m128>(0);

        tess.boundary = true; tess.winding = Winding.Positive; tess.normal = new Vector3(0, 0, 1);
        for (; ; )
        {
          for (int i = 0; i < buffer.n; i++)
          {
            var c = buffer.p[i].c; var a = data[c & 0xfff][(c >> 12) & 0xfff];
            var l = (c >> 24) + 1; if (l >= a.Length || a[l].X != z1) continue;
            buffer.p[i].b = -1; buffer.p[i].c = (c & 0xffffff) | (l << 24);
          }
          for (int i = 0; i < data.Length; i++)
          {
            var a = data[i]; if (a[0][1].X != z1) continue;
            var n = a.Length; buffer.Capacity(buffer.n + n);
            for (int j = 0, t = buffer.n; j < n; j++)
              buffer.p[buffer.n++] = new __m128 { a = points.Add(new Vector3(a[j][0].X, a[j][0].Y, z1)), b = -1, c = i | (j << 12) | (1 << 24), d = t + (j + 1) % n };
          }
          tess.BeginPolygon();
          for (int i = 0, a; (a = i) < buffer.n;)
          {
            tess.BeginContour();
            do tess.AddVertex(ref points.p[buffer.p[i].a]); while (buffer.p[i++].d != a);
            tess.EndContour();
          }
          tess.EndPolygon();
          tess.Flatten();
          buffer.Capacity(buffer.n + tess.vertices.n); int bn = 0;
          for (int i = 0, u = 0, c; i < tess.indices.n; i++, u += c)
          {
            c = tess.indices.p[i]; var at = bn;
            for (int j = 0; j < c; j++)
            {
              var a = points.Add(tess.vertices.p[u + j]);
              int t = 0; for (; t < buffer.n && buffer.p[t].a != a; t++) ;
              buffer.p[buffer.n + bn++] = t == buffer.n ? new __m128 { a = a, b = -1, c = -1, d = -1 } : buffer.p[t];
            }
            for (int k = 0, dn = bn - at, j; k < dn; k++)
            {
              int i1 = at + k, i2 = at + (k + 1) % dn;
              if (buffer.p[buffer.n + i1].d != -1 && buffer.p[buffer.p[buffer.n + i1].d].a == buffer.p[buffer.n + i2].a) continue;
              var p1 = (Vector2)points.p[buffer.p[buffer.n + i1].a];
              var p2 = (Vector2)points.p[buffer.p[buffer.n + i2].a]; var pv = p2 - p1;
              for (j = 0; j < buffer.n; j++)
              {
                int t1 = buffer.p[j].a, t2 = buffer.p[buffer.p[j].d].a; if (t1 == t2) continue;
                var o1 = (Vector2)points.p[t1];
                var o2 = (Vector2)points.p[t2]; var ov = o2 - o1;
                if (Vector2.CcwSign(ov, pv) != 0) continue;
                if (Vector2.DotSign(ov, pv) <= 0) continue;
                using (Number.Mach mach = 2)
                { //0 & 
                  var f = Number.Abs(ov.X) > Number.Abs(ov.Y) ? (p2.X - o1.X) / ov.X : (p2.Y - o1.Y) / ov.Y;
                  if (f.Sign <= 0 || f.CompareTo(1) > 0) continue;
                }
                using (Number.Mach mach = 2) if ((ov.X * (p1.Y - o1.Y)).CompareTo(ov.Y * (p1.X - o1.X)) != 0) continue;
                //if ((0 ^ (ov.X * (p1.Y - o1.Y) - ov.Y * (p1.X - o1.X))).Sign != 0) continue;
                buffer.p[buffer.n + i1].b = buffer.p[j].b;
                buffer.p[buffer.n + i1].c = buffer.p[j].c;
                break;
              }
              if (j == buffer.n) return new Polyhedron { points = new Vector3[0], indices = new ushort[0] }; //Empty();
            }
            for (int k = at + 1; k < bn; k++) buffer.p[buffer.n + k - 1].d = k; buffer.p[buffer.n + bn - 1].d = at;
          }
          Array.Copy(buffer.p, buffer.n, buffer.p, 0, buffer.n = bn);
          for (int i = 0; i < buffer.n; i++)
          {
            if (buffer.p[i].b != -1) continue;
            var i1 = buffer.p[i].a; var i2 = buffer.p[buffer.p[i].d].a;
            var a = points.p[i1];
            var v = (Vector2)a - (Vector2)points.p[i2]; v.Normalize();
            var c = buffer.p[i].c;
            var w = data[c & 0xfff][(c >> 12) & 0xfff][c >> 24].Y;
            var h = Math.Tan((90 - (double)w) * (Math.PI / 180));
            var l = Math.Sqrt((double)(v.X * v.X + v.Y * v.Y));
            var b = new Vector3(-v.Y, v.X, h * l);
            var p = Plane.FromPointNormal(ref a, ref b);
            buffer.p[i].b = planes.Add(ref p);
          }
          normals.Capacity(buffer.n);
          for (int i = 0; i < buffer.n; i++)
          {
            var k = buffer.p[i].d;
            var v = Vector3.Ccw(planes.p[buffer.p[i].b].N, planes.p[buffer.p[k].b].N);
            normals.p[k] = v.Z.Sign > 0 ? v : -v; Debug.Assert(v.Z.Sign != 0);
          }
          var z2 = zmax;
          for (int i = 0; i < data.Length; i++)
          {
            var a = data[i];
            for (int j = 0; j < a.Length; j++)
            {
              var b = a[j];
              for (int k = 1; k < b.Length; k++)
              {
                if (b[k].X <= z1) continue;
                if (b[k].X < z2) { z2 = b[k].X; break; }
              }
            }
          }
          for (int i = 0; i < buffer.n; i++)
          {
            var p = points.p[buffer.p[i].a]; var v = normals.p[i];
            for (int k = 0, j; k < buffer.n; k++)
            {
              if (k == i) continue;
              if ((j = buffer.p[k].d) == i) continue;
              var e = planes.p[buffer.p[k].b];
              var d = e.Dot(ref v); if (d.Sign == 0) continue;
              var f = e.DotCoord(ref p) / d;
              var z = p.Z - v.Z * f; if (z >= z2 || z <= z1) continue;
              var t = new Vector2(p.X - v.X * f, p.Y - v.Y * f);
              if (Vector2.CcwSign(t - (Vector2)points.p[buffer.p[k].a], (Vector2)normals.p[k]) < 0) continue;
              if (Vector2.CcwSign(t - (Vector2)points.p[buffer.p[j].a], (Vector2)normals.p[j]) > 0) continue;
              z2 = z;
            }
          }
          for (int i = 0; i < buffer.n; i++)
          {
            var p = points.p[buffer.p[i].a]; var n = normals.p[i];
            buffer.p[i].a |= points.Add(new Vector3(
              0 | p.X + n.X * z2 / n.Z - n.X * p.Z / n.Z,
              0 | p.Y + n.Y * z2 / n.Z - n.Y * p.Z / n.Z, z2)) << 16;
          }
          for (int i = 0; i < buffer.n; i++)
          {
            var q = buffer.p[i]; if (planes.p[q.b].N.Z.Sign == 0) continue;
            //collector.Add(q.b, q.a, buffer.p[q.d].a);
            collector.Add(q.b); collector.Add(q.a); collector.Add(buffer.p[q.d].a);
          }
          if (z2 == zmax) break;
          for (int i = 0; i < buffer.n; i++) buffer.p[i].a >>= 16; z1 = z2;
        }
        normals.Capacity(points.Count); points.CopyTo(normals.p, 0); points.Clear();
        var indices = tess.GetList<ushort>(1);
        Parallel.For(0, planes.Count, i =>
        {
          var t = gettess(); t.normal = planes[i].N; t.boundary = true; t.winding = Winding.Positive;
          t.BeginPolygon();
          for (int k = 0; k < collector.n; k += 3)
          {
            if (collector.p[k] != i) continue;
            t.BeginContour();
            t.AddVertex(ref normals.p[collector.p[k + 1] & 0xffff]);
            t.AddVertex(ref normals.p[collector.p[k + 2] & 0xffff]);
            t.AddVertex(ref normals.p[collector.p[k + 2] >> 16]);
            t.AddVertex(ref normals.p[collector.p[k + 1] >> 16]);
            t.EndContour();
          }
          t.EndPolygon();
          tesscontours(t);
          lock (tess) capture(t, points, indices);
        });
        encode(indices.p, 0); return new Polyhedron(points, indices);
      }

      public Polyhedron Clone()
      {
        return new Polyhedron { points = (Vector3[])points.Clone(), indices = (ushort[])indices.Clone() };
      }
      public void Transform(Matrix m)
      {
        for (int i = 0; i < points.Length; i++) m.Transform(ref points[i]);
        if (Normalized) encode(indices, 1);
      }

      public static Polyhedron BooleanAnd(Polyhedron a, Polyhedron b)
      {
        return a & b;
      }
      public static Polyhedron BooleanOr(Polyhedron a, Polyhedron b)
      {
        return a | b;
      }
      public static Polyhedron BooleanXOr(Polyhedron a, Polyhedron b)
      {
        return a ^ b;
      }
      public static Polyhedron Subtract(Polyhedron a, Polyhedron b)
      {
        return a - b;
      }
      public static Polyhedron Cut(Polyhedron a, Plane halfspace)
      {
        return a - halfspace;
      }

      public Polyhedron Extrusion(Vector3 dir)
      {
        var ii = indices; var ni = indices.Length; if (ni == 0) return this;
        var tess = gettess();
        var none = dir.X.Sign == 0 && dir.Y.Sign == 0 && dir.Z.Sign == 0;
        if (!Closed)
        {
          ii = none ? new ushort[ni << 1] : tess.GetArray<ushort>(3, ni << 1);
          Array.Copy(indices, 0, ii, 0, ni); encode(ii, 1); Array.Copy(ii, 0, ii, ni, ni);
          for (int i = 0, d; i < ni; i += 3) { d = decode(ii, i); var t = ii[i + 1]; ii[i + 1] = ii[i + 2]; ii[i + 2] = t; encode(ii, i, d); }
          ni <<= 1;
          if (none) return new Polyhedron { points = (Vector3[])points.Clone(), indices = ii };
        }
        if (none) return this;
        var ad = getadj(ii, ni, tess, 2);
        var ne = planecount(ii, ni);
        var ee = tess.GetArray<int>(1, ne + ni / 3);
        for (int i = 0, k = 0, t = ne; i < ni; i += 3) { if (decode(ii, i) != 0) ee[k++] = i; ee[t++] = k - 1; }
        Parallel.For(0, ne, i =>
        {
          using (Number.Mach mach = 2)
          {
            var a = points[ii[ee[i]]]; var b = points[ii[ee[i] + 1]]; var c = points[ii[ee[i] + 2]];
            var ux = c.X - b.X; var uy = c.Y - b.Y; var uz = c.Z - b.Z;
            var vx = a.X - b.X; var vy = a.Y - b.Y; var vz = a.Z - b.Z;
            ee[i] = (dir.X * (uy * vz - uz * vy) + dir.Y * (uz * vx - ux * vz) + dir.Z * (ux * vy - uy * vx)).Sign;
          }
        });
        var pp = tess.GetHashSet<Vector3>(0);
        var tt = tess.GetList<ushort>(0); int fl = 1;
        for (int i = 0, d = 0, s = 0, t, k; i < ni; i += 3)
        {
          if (decode(ii, i) != 0) if ((s = ee[d++]) == 0) fl = 0;
          if (s >= 0) { for (t = 0; t < 3; t++) tt.Add((ushort)pp.Add(points[ii[i + t]] + dir)); continue; }
          for (t = 0, k = tt.n; t < 3; t++) tt.Add((ushort)pp.Add(ref points[ii[i + t]]));
          for (t = 0; t < 3; t++)
          {
            if (ee[ee[ne + ad[i + t] / 3]] < 0) continue;
            var i1 = tt.p[k + t];
            var i2 = tt.p[k + (t + 1) % 3];
            var i3 = pp.Add(pp.p[i1] + dir);
            var i4 = pp.Add(pp.p[i2] + dir);
            tt.Add(i1, i3, i4);
            tt.Add(i4, i2, i1);
          }
        }
        if (dir.Z == -0x7fff) for (int i = 0; i < pp.n; i++) if (pp.p[i].Z < -0x3fff) pp.p[i].Z = -0x3fff; //simple as possible cut for infinty values, 30% more speed
        retess(pp, tt, fl); return new Polyhedron(pp, tt);
      }

      public Polyhedron Select(IEnumerable<int> faces)
      {
        var tess = gettess();
        var pp = tess.GetHashSet<Vector3>(0);
        var ii = tess.GetList<ushort>(0);
        foreach (var i in faces) for (int t = 0, k = i * 3; t < 3; t++, k++) ii.Add((ushort)pp.Add(ref this.points[this.indices[k]]));
        sort(tess, pp.p, ii.p, ii.n); encode(ii.p, 0); return new Polyhedron(pp, ii);
      }

      public void Normalize()
      {
        if (Normalized) return;
        if (!Closed) return;

        var tess = gettess();
        var ne = planecount(indices, indices.Length);
        var ee = getplanes(points, indices, indices.Length, tess, 0);
        var ii = getplanmap(indices, indices.Length, tess, 0);

        var np = points.Length; if (np > 0xffff) throw new OverflowException(); //todo: big sort2, decimal?
        var ni = indices.Length; var nm = ni / 3;
        var kk = tess.GetArray<int>(1, Math.Max(np, ne));

        for (int i = 0; i < np; i++) kk[i] = i;
        Array.Sort(points, kk, 0, np); remap(kk, np);
        for (int i = 0; i < ni; i++) indices[i] = (ushort)kk[indices[i]];

        for (int i = 0; i < ne; i++) kk[i] = i;
        Array.Sort(ee, kk, 0, ne); remap(kk, ne);
        for (int i = 0; i < nm; i++) ii[i] = kk[ii[i]];

        sort2(tess, indices, ii, nm, 2);
      }

      static void sort(Tesselator tess, Vector3[] pp, ushort[] ii, int ni)
      {
        var ppp = tess.GetHashSet<Plane>(0);
        var iii = tess.GetList<int>(0); //Debug.Assert(iii.p != ii);
        addplanes(pp, ii, ni, ppp, iii, 1);
        sort1(tess, ii, iii.p, iii.n, 1);
      }

      struct u3 { ushort a, b, c; }
      static void sort1(Tesselator tess, ushort[] ii, int[] kk, int nk, int code)
      {
        var uu = tess.GetArray<u3>(0, nk); var n = (void*)(nk * 6);
        fixed (ushort* s = ii) fixed (u3* d = uu)
        {
          Native.memcpy(d, s, n);
          Array.Sort(kk, uu, 0, nk);
          Native.memcpy(s, d, n);
        }
        for (int i = 0, k = 0, d = -1; k < nk; encode(ii, i, d != kk[k] ? code : 0), d = kk[k++], i += 3) ;
      }
      static void sort2(Tesselator tess, ushort[] ii, int[] kk, int nk, int code)
      {
        var uu = tess.GetArray<ulong>(0, nk); ulong t; var p = (ushort*)&t;
        for (int i = 0, k = 0; k < nk; k++, i += 3)
        {
          p[0] = (ushort)ii[i + 0];
          p[1] = (ushort)ii[i + 1];
          p[2] = (ushort)ii[i + 2];
          p[3] = (ushort)kk[k];
          uu[k] = t;
        }
        Array.Sort(uu, 0, nk);
        for (int i = 0, k = 0, d = -1; k < nk; k++, i += 3)
        {
          t = uu[k];
          ii[i + 0] = p[0];
          ii[i + 1] = p[1];
          ii[i + 2] = p[2];
          encode(ii, i, d != p[3] ? code : 0); d = p[3];
        }
      }
      static void remap(int[] ii, int ni)
      {
        var kk = stackalloc int[ni];
        for (int i = 0; i < ni; i++) kk[ii[i]] = i;
        Marshal.Copy((IntPtr)kk, ii, 0, ni);
      }

      static int planecount(ushort[] ii, int ni)
      {
        int c = 0; for (int i = 0; i < ni; i += 3) if (i == 0 || decode(ii, i) != 0) c++; return c;
      }
      static Plane[] getplanes(Vector3[] pp, ushort[] ii, int ni, Tesselator tess, int slot)
      {
        var c = planecount(ii, ni); var ee = tess != null ? tess.GetArray<Plane>(slot, c) : new Plane[c];
        var tt = stackalloc int[c];
        for (int i = 3, k = 0; i < ni; i += 3) if (decode(ii, i) != 0) tt[++k] = i;
        Parallel.For(0, c, i => ee[i] = Plane.FromPoints(ref pp[ii[tt[i]]], ref pp[ii[tt[i] + 1]], ref pp[ii[tt[i] + 2]]));
        return ee;
      }
      static int[] getadj(ushort[] ii, int ni, Tesselator tess, int slot)
      {
        var aa = tess != null ? tess.GetArray<int>(slot, ni) : new int[ni];
        var kk = stackalloc int[3 + (ni << 1)]; kk += 3;
        for (int i = 0, k; i < ni; i++)
        {
          k = ii[i + 0] | (ii[mod(i, 1)] << 16);
          k = xorval(kk, k, i); if (k == -1) continue;
          aa[i] = k; aa[k] = i;
        }
        if (kk[-3] != 0) for (int i = 0; i < kk[-1]; i += 2) if (kk[i] != -1) aa[kk[i + 1]] = -1;
        return aa;
      }
      static int[] getplanmap(ushort[] ii, int ni, Tesselator tess, int slot)
      {
        ni = ni / 3; var tt = tess != null ? tess.GetArray<int>(slot, ni) : new int[ni];
        for (int i = 0, k = 0; i < ni; i++) { if (i != 0 && decode(ii, i * 3) != 0) k++; tt[i] = k; }
        return tt;
      }

      static Polyhedron cut(Polyhedron a, Plane plane)
      {
        //var test = closed(a.indices, a.indices.Length);
        var la = Vector3.LongAxis(ref plane.N);
        var p0 = new Vector3(
          la == 0 ? -plane.D / plane.N.X : 0,
          la == 1 ? -plane.D / plane.N.Y : 0,
          la == 2 ? -plane.D / plane.N.Z : 0);
        var sa = Vector3.ShortAxis(ref plane.N);
        var tv = sa == 0 ?
          new Vector3(plane.N.X, plane.N.Z, -plane.N.Y) : sa == 1 ?
          new Vector3(plane.N.Z, plane.N.Y, -plane.N.X) :
          new Vector3(plane.N.Y, -plane.N.X, plane.N.Z); //Debug.WriteLine(sa + " " + codxcs.Vector3.Length((codxcs.Vector3)tv));
        var v1 = Vector3.Ccw(tv, plane.N); //v1.Normalize();
        var v2 = Vector3.Ccw(v1, plane.N); var l = (Number)2000000000;// 0x7fff;
        var b = new Polyhedron
        {
          points = new Vector3[] { p0 - v1 * l, p0 + (v1 + v2) * l, p0 + (v1 - v2) * l, p0 + plane.N * l },
          indices = new ushort[] { 1, 0, 2, 3, 0, 1, 3, 1, 2, 2, 0, 3 } //encode 1
        };
        return a - b;
      }
      static Polyhedron union(Polyhedron a, Polyhedron b, bool check)
      {
        var tess = gettess();
        var pp = tess.GetHashSet<Vector3>(0);
        var ii = tess.GetList<ushort>(0);
        { var vv = a.points; var tt = a.indices; for (int i = 0; i < a.indices.Length; i++) ii.Add((ushort)pp.Add(ref vv[tt[i]])); }
        { var vv = b.points; var tt = b.indices; for (int i = 0; i < b.indices.Length; i++) ii.Add((ushort)pp.Add(ref vv[tt[i]])); }
        if (check) retess(pp, ii, 1); else sort(tess, pp.p, ii.p, ii.n); return new Polyhedron(pp, ii);
      }
      static Polyhedron sub(Polyhedron a, Polyhedron b)
      {
        var ac = a.Closed; Debug.Assert(b.Closed);
        var tess = gettess();
        var pp = tess.GetHashSet<Vector3>(0);
        var ii = tess.GetList<ushort>(2);
        var pa = tess.GetList<int>(0);
        var pb = tess.GetList<int>(1);
        var ee = tess.GetHashSet<Plane>(0);
        a.addplanes(ee, pa, 1);
        if (ac) b.addplanes(ee, pb, 2);
        Parallel.For(0, ee.Count, e =>
        {
          var p = ee.p[e]; var t = gettess(); t.normal = p.N; t.boundary = true;
          t.winding = ac ? Winding.AbsGeqTwo : Winding.Positive;
          t.BeginPolygon();
          if (ac)
          {
            a.addcontour(t, pa, e, 1);
            b.addcontour(t, pb, e, 2);
            a.cutcontour(t, ref p, 0);
            b.cutcontour(t, ref p, 1);
          }
          else
          {
            a.addcontour(t, pa, e, 1);
            b.cutcontour(t, ref p, 1);
          }
          t.EndPolygon();
          t.winding = Winding.Positive;
          tesscontours(t);
          lock (tess) capture(t, pp, ii);
        });
        if (ac) covariance(pp.p, pp.n, ii); else encode(ii.p, 0); //!closed
        return new Polyhedron(pp, ii);
      }

      static void retess(List<Vector3> pp, List<ushort> ii, int fl)
      {
        var tess = gettess();
        var planes = tess.GetHashSet<Plane>(0);
        var planei = tess.GetList<int>(2);
        addplanes(pp.p, ii.p, ii.n, planes, planei, 1);

        if ((fl & 1) != 0 && Parallel.For(0, planes.n, (e, state) =>
        {
          var p = planes.p[e]; var n = -p;
          var t = gettess(); t.normal = p.N; t.boundary = true; t.winding = Winding.AbsGeqTwo;
          t.BeginPolygon();
          if (cutcontour(t, pp.p, pp.n, ii.p, ii.n, ref n, 0)) addcontour(t, pp.p, ii.p, ii.n, planei, e, 2);
          t.EndPolygon(); if (t.indices.n != 0) state.Stop();
        }).IsCompleted) { sort1(tess, ii.p, planei.p, planei.n, 1); return; }

        var ppp = tess.GetHashSet<Vector3>(1);
        var iii = tess.GetList<ushort>(1);
        Parallel.For(0, planes.n, e =>
        {
          var p = planes.p[e]; var n = -p;
          var t = gettess(); t.normal = p.N; t.boundary = true; t.winding = Winding.Positive;
          t.BeginPolygon();
          addcontour(t, pp.p, ii.p, ii.n, planei, e, 1);
          var i = planes.IndexOf(ref n); if (i != -1) addcontour(t, pp.p, ii.p, ii.n, planei, i, 1);
          t.EndPolygon();
          t.BeginPolygon();
          t.AddContours(); cutcontour(t, pp.p, pp.n, ii.p, ii.n, ref n, 0);
          t.EndPolygon();
          tesscontours(t);
          lock (tess) capture(t, ppp, iii);
        });
        covariance(ppp.p, ppp.n, iii);
        pp.Capacity(pp.n = ppp.n); if (pp.n != 0) Array.Copy(ppp.p, pp.p, pp.n);
        ii.Capacity(ii.n = iii.n); if (ii.n != 0) Array.Copy(iii.p, ii.p, ii.n);
      }

      static void covariance(Vector3[] pp, int np, List<ushort> iii)
      {
        var ii = iii.p; var ni = iii.n;
        var kk = stackalloc int[3 + (ni << 1)]; kk += 3;
        for (int i = 0; i < ni; i += 3)
        {
          xor(kk, ii[i + 1], ii[i + 0]);
          xor(kk, ii[i + 2], ii[i + 1]);
          xor(kk, ii[i + 0], ii[i + 2]);
        }
        if (kk[-3] == 0) return;
        for (int t, u = 0, i3 = 0; ;)
        {
          pack(kk); var count = kk[-3];
          for (int i = 0; i < kk[-1]; i += 2)
          {
            if (kk[i] == -1) continue;
            int i1 = kk[i], i2 = kk[i + 1];
            var p1 = pp[i1]; var p2 = pp[i2];
            for (int k = 0; k < np; k++)
            {
              if (k == i1 || k == i2) continue;
              var p3 = pp[k]; if (!Vector3.Inline(ref p1, ref p2, ref p3)) continue;
              using (Number.Mach mach = 2)
              {
                var f = p2.X != p1.X ? (p3.X - p1.X) / (p2.X - p1.X) : p2.Y != p1.Y ? (p3.Y - p1.Y) / (p2.Y - p1.Y) : (p3.Z - p1.Z) / (p2.Z - p1.Z);
                if (f.Sign <= 0 || f.CompareTo(1) >= 0) continue;
              }
              //var f = 0 & (p2.X != p1.X ? (p3.X - p1.X) / (p2.X - p1.X) : p2.Y != p1.Y ? (p3.Y - p1.Y) / (p2.Y - p1.Y) : (p3.Z - p1.Z) / (p2.Z - p1.Z));
              //if (f != 1) continue;
              for (t = 0; t < ni; t += 3)
              {
                for (u = 0; u < 3 && !(ii[t + u] == i2 && ii[t + (u + 1) % 3] == i1); u++) ;
                if (u == 3) continue;
                if ((i3 = ii[t + (u + 2) % 3]) == k) continue; //todo: check this case //for (int j = t + 3; j < ni; j += 3) for (int v = 0; v < 3; v++) if (ii[t + v] == i2 && ii[t + (v + 1) % 3] == i1) { }
                break;
              }
              if (t == ni) throw new Exception(); //Invalid Polyhedron with openings
                                                  //ii[t + (u + 1) % 3] = k; iii.Add(k, i1, i3); ii = iii.p; ni = iii.n;

              var c = decode(ii, t); ii[t + (u + 1) % 3] = (ushort)k; encode(ii, t, c); t += 3; //keep encoding
              iii.Capacity(iii.n = ni = ni + 3); ii = iii.p; Array.Copy(ii, t, ii, t + 3, ni - t - 3);
              ii[t + 0] = (ushort)k; ii[t + 1] = (ushort)i1; ii[t + 2] = (ushort)i3; encode(ii, t, 0);

              xor(kk, i2, i1); xor(kk, i1, k); xor(kk, k, i2);
              var nc = kk[-3]; if (nc == 0) return;
              break;
            }
          }
          if (kk[-3] == count) throw new Exception("Invalid Polyhedron"); //openings
        }
      }

      static void addplanes(Vector3[] pp, ushort[] ii, int ni, HashSet<Plane> planes, List<int> index, int f)
      {
        index.Capacity(index.n = ni /= 3); var kk = index.p;
        Parallel.For(0, ni, i =>
        {
          var t = i * 3;
          var p = Plane.FromPoints(ref pp[ii[t]], ref pp[ii[t + f]], ref pp[ii[t + (f ^ 3)]]);
          kk[i] = planes.AddLock(ref p);
        });
      }
      void addplanes(HashSet<Plane> planes, List<int> index, int f)
      {
        if (indices.Length == 0) return;
        var nk = indices.Length / 3; index.Capacity(index.n = nk); var ii = index.p;
        var np = planecount(indices, indices.Length); var kk = stackalloc int[np];
        for (int k = 0, t = 0; k < nk; k++) if (decode(indices, k * 3) != 0) kk[t++] = k;
        Parallel.For(0, np, k =>
        {
          var t = (k = kk[k]) * 3;
          var p = Plane.FromPoints(ref points[indices[t]], ref points[indices[t + f]], ref points[indices[t + (f ^ 3)]]);
          var e = planes.AddLock(ref p);
          ii[k++] = e; for (t += 3; t < indices.Length && decode(indices, t) == 0; t += 3) ii[k++] = e;
        });
      }

      static void addcontour(Tesselator tess, Vector3[] pp, ushort[] ii, int ni, List<int> planes, int e, int f)
      {
        int l1 = 0, l2 = 0; for (int l = 0; l < planes.n; l++) if (planes.p[l] == e) { if (l2 == 0) l1 = l; l2 = l + 1; }
        var kk = stackalloc int[3 + (((l2 - l1) * 3) << 1)]; kk += 3;
        for (int l = l1; l < l2; l++)
        {
          if (planes.p[l] != e) continue;
          int i1 = l * 3, i2 = i1 + f, i3 = i1 + (f ^ 3);
          xor(kk, ii[i1], ii[i2]); xor(kk, ii[i2], ii[i3]); xor(kk, ii[i3], ii[i1]);
        }
        for (int i = 0, k; i < kk[-1]; i += 2)
        {
          if (kk[i] == -1) continue;
          int u = kk[i], v = kk[i + 1];
          tess.BeginContour();
          for (tess.AddVertex(ref pp[u]); u != v;)
          {
            for (k = i + 2; kk[k] != v; k += 2) ;
            tess.AddVertex(ref pp[v]);
            v = kk[k + 1]; kk[k] = -1; //if (k == i + 2) i = k;
          }
          tess.EndContour();
        }
      }
      void addcontour(Tesselator tess, List<int> planes, int e, int f)
      {
        addcontour(tess, points, indices, indices.Length, planes, e, f);
      }

      static bool cutcontour(Tesselator tess, Vector3[] pp, int np, ushort[] ii, int ni, ref Plane plane, int fl)
      {
        int f1 = 0, f2 = 3;
        var ff = stackalloc int[np + ni / 3 * 2];
        for (int i = 0; i < np; i++)
        {
          var f = plane.DotCoordSign(ref pp[i]) + 1;
          ff[i] = f = (f | (f >> 1)) ^ 1; f1 |= f; f2 &= f;
        }
        if (f1 == 2) return false;
        if (f2 == 1) return false; bool cuts = false;
        var nk = 0; var kk = ff + np;
        float3 tmp; var tt = (int*)&tmp;
        for (int i = 0, r; i < ni; i += 3)
        {
          for (int j = 0; j < 3; j++) tt[j] = ff[ii[i + j]];
          if (((tt[0] | tt[1] | tt[2]) != 3) && (tt[0] + tt[1] + tt[2]) != 1) continue;
          for (r = 0; tt[r] != 1; r++) ;
          for (int l = 0; l < 3; l++)
          {
            var u = (r + l + 0) % 3; if (tt[u] == 0) { u = ii[i + u]; kk[nk++] = u | (u << 16); continue; }
            var v = (r + l + 1) % 3; if (tt[u] == tt[v] || tt[v] == 0) continue;
            u = ii[i + u]; v = ii[i + v]; if (u < v) { var t = u; u = v; v = t; }
            kk[nk++] = u | (v << 16);
          }
          Debug.Assert((nk & 1) == 0);
        }
        if ((fl & 1) == 1) for (int k1 = 0, k2 = nk - 1; k1 < k2; k1++, k2--) { var t = kk[k1]; kk[k1] = kk[k2]; kk[k2] = t; }
        for (int i = 0; i < nk; i += 2)
        {
          int k1 = kk[i + 1]; if (k1 == -1) continue;
          tess.BeginContour();
          for (int k2 = kk[i], k; ;)
          {
            int u = k2 & 0xffff, v = k2 >> 16; cuts = true;
            var p = u != v ? plane.Intersect(ref pp[u], ref pp[v]) : pp[u]; //32%
            tess.AddVertex(ref p); if (k2 == k1) break;
            for (k = i + 2; k < nk && kk[k + 1] != k2; k += 2) ;
            if (k == nk) { tess.EndContour(); throw new Exception(); } //!Closed
            kk[k + 1] = -1; k2 = kk[k];
          }
          tess.EndContour();
        }
        return cuts;
      }
      bool cutcontour(Tesselator tess, ref Plane plane, int fl)//, codxcs.Vector3[] ppest)
      {
        return cutcontour(tess, points, points.Length, indices, indices.Length, ref plane, fl);//, ppest);
      }

      //static List<codxcs.Vector3> getptsest(Tesselator tess, Vector3[] pp, int np, int slot)
      //{
      //  var tt = tess.GetList<codxcs.Vector3>(slot); tt.Capacity(tt.n = np);
      //  //Parallel.For(0, np, i => tt.p[i] = (codxcs.Vector3)pp[i]);
      //  for (int i = 0; i < np; i++) tt.p[i] = (codxcs.Vector3)pp[i];
      //  return tt;
      //}

      static int decode(ushort[] p, int i)
      {
        int l; return p[i + 2] < p[i + (l = p[i + 1] < p[i + 0] ? 1 : 0)] ? 2 : l;
      }
      static void encode(ushort[] p, int i, int v)
      {
        int l = p[i + 2] < p[i + (l = p[i + 1] < p[i + 0] ? 1 : 0)] ? 2 : l; if (l == v) return;
        ushort a = p[i + l], b = p[i + (l + 1) % 3], c = p[i + (l + 2) % 3];
        p[i + v] = a; p[i + (v + 1) % 3] = b; p[i + (v + 2) % 3] = c;
      }
      static void encode(ushort[] p, int v)
      {
        if (p != null && p.Length >= 3) encode(p, 0, v);
      }
      static void capture(Tesselator tess, HashSet<Vector3> ppp, List<ushort> iii)
      {
        var ni = tess.indices.n; if (ni == 0) return;
        var ii = tess.indices.p; var vv = tess.vertices.p;
        var nt = iii.n; iii.Capacity(iii.n += ni); var tt = iii.p;
        for (int i = 0; i < ni; i++) tt[nt + i] = (ushort)ppp.Add(ref vv[ii[i]]);
        encode(tt, nt, 1); for (int i = 3; i < ni; i += 3) encode(tt, nt + i, 0);
      }

      static void xor(int* kk, int[] ii, int ni)
      {
        for (int i = 0; i < ni; i += 3)
        {
          xor(kk, ii[i + 0], ii[i + 1]);
          xor(kk, ii[i + 1], ii[i + 2]);
          xor(kk, ii[i + 2], ii[i + 0]);
        }
      }
      static void xor(int* kk, int a, int b)
      {
        int i = 0; var n = kk[-1];
        for (; i < n && (kk[i] != b || kk[i + 1] != a); i += 2) ;
        if (i < n) { kk[i] = -1; kk[i + 1] = kk[-2]; kk[-2] = i + 1; kk[-3]--; return; }
        if ((i = kk[-2]) != 0) kk[-2] = kk[i--];
        else { i = n; kk[-1] += 2; }
        kk[i] = a; kk[i + 1] = b; kk[-3]++;
      }
      static void pack(int* kk)
      {
        int n = 0; //if (kk[-1] >> 1 == kk[-3]) return;
        for (int i = 0; ; i += 2)
        {
          if (kk[i] == -1) continue;
          kk[n++] = kk[i]; kk[n++] = kk[i + 1];
          if (n >> 1 == kk[-3]) break;
        }
        kk[-1] = n; kk[-2] = 0;
      }
      static int xorval(int* kk, int ab, int v)
      {
        int i = 0; var n = kk[-1];
        for (; i < n && kk[i] != ab; i += 2) ;
        if (i < n) { v = kk[i + 1]; kk[i] = -1; kk[i + 1] = kk[-2]; kk[-2] = i + 1; kk[-3]--; return v; }
        if ((i = kk[-2]) != 0) kk[-2] = kk[i--];
        else { i = n; kk[-1] += 2; }
        kk[i] = (ab << 16) | (ab >> 16); kk[i + 1] = v; kk[-3]++; return -1;
      }
      //static int freeedges(int[] ii, int ni)
      //{
      //  var kk = stackalloc int[3 + (ni << 1)]; kk += 3;
      //  for (int i = 0; i < ni; i += 3)
      //  {
      //    xor(kk, ii[i + 1], ii[i + 0]);
      //    xor(kk, ii[i + 2], ii[i + 1]);
      //    xor(kk, ii[i + 0], ii[i + 2]);
      //  }
      //  return kk[-3];
      //}

      static void tesscontours(Tesselator tess)
      {
        tess.boundary = false; if (tess.indices.n == 0) return;
        tess.BeginPolygon(); tess.AddContours(); tess.EndPolygon();
        tess.CorrectJunctions();
      }
      static void edgefill(HashSet<Vector3> points, List<ushort> indices, int ni, int i1, int i2, int sign, List<int> bipsl)
      {
        var a1 = indices.p[i1]; var b1 = indices.p[ni + i1];
        var a2 = indices.p[i2]; var b2 = indices.p[ni + i2];
        var x1 = find(indices.p, ni, a2, a1);
        if (x1 != -1)
        {
          var c1 = indices.p[ni + x1];
          var c2 = indices.p[ni + mod(x1, 1)]; if (c1 == b2) return;
          //var si = thetrasign(points, b2, b1, c2, a1) * sign;
          var si = Plane.FromPoints(ref points.p[b2], ref points.p[b1], ref points.p[c2]).DotCoordSign(ref points.p[a1]) * sign;
          if (si == -1)
          {
            if (a1 > a2) return;
            var d1 = points.Add(crosscut(ref points.p[a1], ref points.p[b1], ref points.p[c2]));
            var d2 = points.Add(crosscut(ref points.p[a2], ref points.p[c1], ref points.p[b2]));
            indices.Add(b1, d2, b2); //side left
            indices.Add(d2, b1, d1);
            indices.Add(c1, d2, c2); //side right
            indices.Add(c2, d2, d1);
            bipsl.Add(a1 | (indices.n << 16));
            indices.Add(a1, c2, b1); //tent  
            indices.Add(d1, b1, c2);
            bipsl.Add(a2 | (indices.n << 16));
            indices.Add(a2, b2, c1); //tent  
            indices.Add(d2, c1, b2);
            return;
          }
        }
        indices.Add(a1, b2, b1);
        indices.Add(b2, a1, a2);
      }
      static Vector3 crosscut(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3)
      {
        var e1 = Plane.FromPoints(ref p1, ref p2, ref p3);
        var e2 = Plane.FromPointNormal(p2, p2 - p1);
        var e3 = Plane.FromPointNormal(p3, p3 - p1);
        return Plane.Intersect(ref e1, ref e2, ref e3);
      }
      static void invert(ushort[] p, int i, int n)
      {
        for (n += i; i < n; i += 3) { var t = p[i + 1]; p[i + 1] = p[i + 2]; p[i + 2] = t; }
      }
      static int find(ushort[] ii, int ni, int a, int b)
      {
        for (int i = 0; i < ni; i++) if (ii[i] == a && ii[mod(i, 1)] == b) return i;
        return -1;
      }
      static int mod(int i, int k) { var r = i % 3; return i - r + (r + k) % 3; }

      struct __m128 { public int a, b, c, d; }

    }

    class List<T>
    {
      internal int n; internal T[] p;
      public void Capacity(int c) { if (p == null || p.Length < c) Array.Resize(ref p, ((c >> 8) + 1) << 8); }
      public int Count { get { return n; } }
      public T this[int i]
      {
        get { Debug.Assert((uint)i < (uint)n); return p[i]; }
      }
      public void Add(T v)
      {
        Capacity(n + 1); p[n++] = v;
      }
      public void Add(ref T v)
      {
        Capacity(n + 1); p[n++] = v;
      }
      public T[] ToArray()
      {
        var a = new T[n]; if (n != 0) Array.Copy(p, 0, a, 0, n); return a;
      }
      public void CopyTo(T[] a, int offs)
      {
        for (int i = 0; i < n; i++) a[offs++] = p[i];
      }
    }

    class HashSet<T> : List<T> where T : IEquatable<T>
    {
      int[] buckets; int count; //static IEqualityComparer<T> comparer = EqualityComparer<T>.Default;
      public HashSet()
      {
        buckets = new int[count = 1103];
      }
      public void Clear()
      {
        if (base.n != 0) Array.Clear(buckets, base.n = 0, count = 1103);
      }
      public int IndexOf(ref T v)
      {
        int t = unchecked((int)((uint)v.GetHashCode() % 1103));
        for (int i = buckets[t]; i != 0; i >>= 16, i = i != 1 ? buckets[i] : 0) if (v.Equals(p[i & 0xffff])) return i & 0xffff;
        //if (comparer.Equals(p[i & 0xffff], v)) return i & 0xffff;
        return -1;
      }
      int Add(ref T v, int t)
      {
        int i;
        if ((i = buckets[t]) != 0)
        {
          for (; i != 0; i >>= 16, i = i != 1 ? buckets[i] : 0) if (v.Equals(p[i & 0xffff])) return i & 0xffff;
          //if ( comparer.Equals(p[i & 0xffff], v)) return i & 0xffff;
          if (count == buckets.Length) Array.Resize(ref buckets, ((count >> 8) + 1) << 8);
          buckets[count] = buckets[t]; i = count++;
        }
        else i = 1; Debug.Assert(i < 0x7fff && base.n < 0xffff);
        buckets[t] = base.n | (i << 16); base.Add(ref v); return base.n - 1;
      }
      public new int Add(ref T v) { return Add(ref v, unchecked((int)((uint)v.GetHashCode() % 1103))); }
      public new int Add(T v) { return Add(ref v, unchecked((int)((uint)v.GetHashCode() % 1103))); }
      public int AddLock(ref T v)
      {
        int t = unchecked((int)((uint)v.GetHashCode() % 1103));
        lock (this) return Add(ref v, t);
      }
    }

    static void Add(this List<ushort> ii, int i1, int i2, int i3)
    {
      Debug.Assert(!(i1 == i2 || i2 == i3 || i3 == i1));// if (i1 == i2 || i2 == i3 || i3 == i1) { }
      ii.Capacity(ii.n + 3); ii.p[ii.n] = (ushort)i1; ii.p[ii.n + 1] = (ushort)i2; ii.p[ii.n + 2] = (ushort)i3; ii.n += 3;
    }

    [ThreadStatic]
    static WeakReference weaktess;
    static Tesselator gettess()
    {
      var tess = weaktess != null ? weaktess.Target as Tesselator : null;
      if (tess == null) weaktess = new WeakReference(tess = new Tesselator()); //Debug.WriteLine("new Tesselator " + Thread.CurrentThread.ManagedThreadId ); }
      return tess;
    }
    enum Winding { EvenOdd, NonZero, Positive, Negative, AbsGeqTwo }
    class Tesselator
    {
#if (false)
      public Tesselator()
      {
        //nodehead = new Node();
        //nodehead.prev = nodehead;
        //nodehead.next = nodehead;
        //vqkeys = new Vertex[vqmax = 32];
        //vhnodes = new int[(vhmax = 128) + 1]; vhnodes[1] = 1;
        //vhhandles = new Elem[vhmax + 1];
        //Debug.WriteLine("Tesselator() " + threadid);
      }
      int threadid = Thread.CurrentThread.ManagedThreadId;
      ~Tesselator()
      {
        Debug.WriteLine("~Tesselator() " + threadid);
        Debug.WriteLine("vqkeys " + vqkeys.Length);
        Debug.WriteLine("vhnodes " + vhnodes.Length);
        Debug.WriteLine("vhhandles " + vhhandles.Length);
        Debug.WriteLine("vqorder " + vqorder.Length);
        { int n = 0; for (var p = eCache; p != null; p = p.next, n++); Debug.WriteLine("eCache " + n); }
        { int n = 0; for (var p = vCache; p != null; p = p.next, n++); Debug.WriteLine("vCache " + n); }
        { int n = 0; for (var p = fCache; p != null; p = p.next, n++); Debug.WriteLine("fCache " + n); }
        { int n = 0; for (var p = rCache; p != null; p = p.next, n++); Debug.WriteLine("rCache " + n); }
        { int n = 0; for (var p = nCache; p != null; p = p.next, n++); Debug.WriteLine("nCache " + n); }
        Debug.WriteLine("--------------");
      }
#endif
      public Winding winding;
      public bool boundary;
      public Vector3 normal;
      public List<Vector3> vertices;
      public List<ushort> indices;

      public void BeginPolygon()
      {
        if (nodehead == null)
        {
          nodehead = new Node();
          nodehead.prev = nodehead;
          nodehead.next = nodehead;
          vqkeys = new Vertex[vqmax = 32];
          vhnodes = new int[(vhmax = 128) + 1]; vhnodes[1] = 1;
          vhhandles = new Elem[vhmax + 1];
          vertices = new List<Vector3>();
          indices = new List<ushort>();
        }
        Debug.Assert(vHead == null && edge == null);
        vHead = newVertex();
        fHead = newFace();
        var pair = CreateEdgePair();
        eHead = pair.e;
        eHeadSym = pair.eSym;
        vHead.next = vHead.prev = vHead;
        fHead.next = fHead.prev = fHead;
        eHead.next = eHead;
        eHead.Sym = eHeadSym;
        eHeadSym.next = eHeadSym;
        eHeadSym.Sym = eHead;
      }
      public void BeginContour()
      {
        Debug.Assert(vHead != null && edge == null);
      }
      public void AddVertex(ref Vector3 p)
      {
        if (edge == null) { edge = MakeEdge(); Splice(edge, edge.Sym); }
        else { SplitEdge(edge); edge = edge.Lnext; }
        edge.Org.coords = p; //edge.Org.id = 0;
        edge.winding = 1; edge.Sym.winding = -1;
      }
      public void EndContour()
      {
        edge = null; Debug.Assert(vHead != null);
      }
      public void EndPolygon()
      {
        Debug.Assert(vHead != null && edge == null);

        int i = Vector3.LongAxis(ref normal);
        var n = normal[i].Sign < 0;
        for (var v = vHead.next; v != vHead; v = v.next)
        {
          switch (i)
          {
            case 0: v.x = v.coords.Y; v.y = v.coords.Z; break;
            case 1: v.x = v.coords.Z; v.y = v.coords.X; break;
            case 2: v.x = v.coords.X; v.y = v.coords.Y; break;
          }
          if (n) v.y = -v.y;
        }

        ComputeInterior();
        if (boundary) SetWindingNumber(1, true); else TessellateInterior(); Check();
        vertices.n = indices.n = 0;
        if (boundary) OutputContours(); else OutputPolymesh();

        { var t = fHead.next; fHead.next = fCache; fCache = t; fHead = null; }
        { var t = vHead.next; vHead.next = vCache; vCache = t; vHead = null; }
        { var t = eHead.next; eHead.next = eCache; eCache = t; eHead = null; }
        Debug.Assert(vqsize == 0);
      }
      public void AddContours()
      {
        var vv = vertices.p;
        var ii = indices.p; var ni = indices.n;
        for (int j = 0, l = 0; j < indices.n; j++)
        {
          BeginContour(); var c = ii[j];
          for (int k = 0; k < c; k++)
          {
            if (Vector3.Inline(ref vv[l + ((k + c - 1) % c)], ref vv[l + k], ref vv[l + ((k + 1) % c)])) continue;
            AddVertex(ref vv[l + k]);
          }
          EndContour(); l += c;
        }
      }
      public void Flatten()
      {
        var vv = vertices.p; var nv = vertices.n;
        var ii = indices.p; var ni = indices.n;
        var nt = 0; var tt = stackalloc int[ni + nv];
        for (int i = 0, k = 0, c; i < ni; i++, k += c)
        {
          c = ii[i];
          for (int j = 0; j < c; j++)
          {
            if (Vector3.Inline(ref vv[k + (j + c - 1) % c], ref vv[k + j], ref vv[k + (j + 1) % c])) continue;
            tt[nt++] = k + j;
          }
          tt[nt++] = -1;
        }
        if (nt == nv + ni) return;
        ni = nv = 0;
        for (int i = 0, c = 0; i < nt; i++)
        {
          if (tt[i] == -1) { if (c != 0) ii[ni++] = (ushort)c; c = 0; continue; }
          if (tt[i] != nv) vv[nv] = vv[tt[i]]; nv++; c++;
        }
        indices.n = ni; vertices.n = nv;
      }
      public void CorrectJunctions()
      {
        var vv = vertices.p; var nv = vertices.n;
        var ii = indices.p; var ni = indices.n;
        for (int i = 0, t; i < ni; i += 3)
        {
          var p1 = vv[ii[i + 0]]; var p2 = vv[ii[i + 1]]; var p3 = vv[ii[i + 2]];
          if (!Vector3.Inline(ref p1, ref p2, ref p3)) continue;
          using (Number.Mach mach = 2)
          {
            var f = 0 | (p2.X != p1.X ? (p3.X - p1.X) / (p2.X - p1.X) : p2.Y != p1.Y ? (p3.Y - p1.Y) / (p2.Y - p1.Y) : (p3.Z - p1.Z) / (p2.Z - p1.Z));
            t = f.Sign < 0 ? 1 : f.CompareTo(1) > 0 ? 2 : 0;
          }
          //t = 0 & (p2.X != p1.X ? (p3.X - p1.X) / (p2.X - p1.X) : p2.Y != p1.Y ? (p3.Y - p1.Y) / (p2.Y - p1.Y) : (p3.Z - p1.Z) / (p2.Z - p1.Z));
          //t = t < 0 ? 1 : t > 2 ? 2 : 0;
          int i1 = i + t, i2 = i + (t + 1) % 3, i3 = i + (t + 2) % 3;
          for (int k1 = 0; k1 < ni; k1++)
          {
            if (ii[k1] != ii[i2]) continue; var k2 = k1 / 3 * 3 + (k1 + 1) % 3;
            if (ii[k2] != ii[i1]) continue; var k3 = k1 / 3 * 3 + (k1 + 2) % 3;
            ii[k2] = ii[i3]; ii[i2] = ii[k3]; i = Math.Min(k1, i) - 3; break;
          }
        }
      }

  #region shared buffer
      Dictionary<KeyValuePair<Type, int>, object> buffers;
      internal T GetBuffer<T>(int slot) where T : new()
      {
        if (buffers == null) buffers = new Dictionary<KeyValuePair<Type, int>, object>(); //16
        var k = new KeyValuePair<Type, int>(typeof(T), slot);
        object p; if (!buffers.TryGetValue(k, out p)) buffers.Add(k, p = new T());
        return (T)p;
      }
      internal T[] GetArray<T>(int slot, int n)
      {
        var a = GetBuffer<List<T>>(slot); a.Capacity(a.n = n); return a.p;
      }
      internal List<T> GetList<T>(int slot)
      {
        var a = GetBuffer<List<T>>(slot); a.n = 0; return a;
      }
      internal HashSet<T> GetHashSet<T>(int slot) where T : IEquatable<T>
      {
        var a = GetBuffer<HashSet<T>>(slot); a.Clear(); return a;
      }
  #endregion

      Edge eCache; Vertex vCache; Face fCache; Region rCache; Node nCache;
      Edge newEdge()
      {
        var p = eCache; if (p == null) return new Edge();
        eCache = p.next; p.next = null;
        p.Sym = p.Onext = p.Lnext = null; //p.pair.e = p.pair.eSym = null;
        p.Org = null; p.Lface = null; p.activeRegion = null; p.winding = 0; return p;
      }
      Vertex newVertex()
      {
        var p = vCache; if (p == null) return new Vertex();
        vCache = p.next; p.prev = p.next = null; p.anEdge = null; p.pqHandle = p.n = 0;
        return p;
      }
      Face newFace()
      {
        var p = fCache; if (p == null) return new Face();
        fCache = p.next;
        p.prev = p.next = p.trail = null;
        p.anEdge = null; p.n = 0; p.marked = p.inside = false;
        return p;
      }
      Region newRegion()
      {
        var p = rCache; if (p == null) return new Region();
        rCache = p.next; p.next = null;
        p.eUp = null; p.nodeUp = null;
        p.windingNumber = 0; p.inside = p.sentinel = p.dirty = p.fixUpperEdge = false;
        return p;
      }

      Edge edge; Vertex evt;

      static readonly Number SentinelCoord = decimal.MaxValue; //1e18m;

      void TessellateMonoRegion(Face face)
      {
        var up = face.anEdge;
        Debug.Assert(up.Lnext != up && up.Lnext.Lnext != up);
        for (; VertLeq(up.Sym.Org, up.Org); up = up.Onext.Sym) ;
        for (; VertLeq(up.Org, up.Sym.Org); up = up.Lnext) ;
        var lo = up.Onext.Sym;
        while (up.Lnext != lo)
        {
          if (VertLeq(up.Sym.Org, lo.Org))
          {
            while (lo.Lnext != up && (EdgeGoesLeft(lo.Lnext) || EdgeSign(lo.Org, lo.Sym.Org, lo.Lnext.Sym.Org) <= 0)) lo = Connect(lo.Lnext, lo).Sym;
            lo = lo.Onext.Sym;
          }
          else
          {
            while (lo.Lnext != up && (EdgeGoesRight(up.Onext.Sym) || EdgeSign(up.Sym.Org, up.Org, up.Onext.Sym.Org) >= 0)) up = Connect(up, up.Onext.Sym).Sym;
            up = up.Lnext;
          }
        }
        Debug.Assert(lo.Lnext != up);
        while (lo.Lnext.Lnext != up) lo = Connect(lo.Lnext, lo).Sym;
      }
      void TessellateInterior()
      {
        Face f, next;
        for (f = fHead.next; f != fHead; f = next)
        {
          next = f.next;
          if (f.inside) TessellateMonoRegion(f);
        }
      }
      void DiscardExterior()
      {
        Face f, next;
        for (f = fHead.next; f != fHead; f = next)
        {
          next = f.next; if (!f.inside) ZapFace(f);
        }
      }
      void SetWindingNumber(int value, bool keepOnlyBoundary)
      {
        Edge e, eNext;
        for (e = eHead.next; e != eHead; e = eNext)
        {
          eNext = e.next;
          if (e.Sym.Lface.inside != e.Lface.inside)
          {
            e.winding = (e.Lface.inside) ? value : -value;
          }
          else
          {
            if (!keepOnlyBoundary) e.winding = 0; else Delete(e);
          }
        }
      }
      int GetNeighbourFace(Edge edge)
      {
        if (edge.Sym.Lface == null) return ~0;
        if (!edge.Sym.Lface.inside) return ~0;
        return edge.Sym.Lface.n;
      }
      void OutputPolymesh()
      {
        for (var v = vHead.next; v != vHead; v = v.next) v.n = ~0;
        for (var f = fHead.next; f != fHead; f = f.next)
        {
          f.n = ~0; if (!f.inside) continue;
          var edge = f.anEdge; var test = 0;
          do
          {
            if (edge.Org.n == ~0) edge.Org.n = vertices.n++;
            test++; edge = edge.Lnext;
          }
          while (edge != f.anEdge); Debug.Assert(test == 3);
          f.n = indices.n++;
        }
        indices.Capacity(indices.n *= 3);
        vertices.Capacity(vertices.n);
        for (var v = vHead.next; v != vHead; v = v.next)
        {
          if (v.n == ~0) continue;
          vertices.p[v.n] = v.coords; //vertices[v.n].p = v.coords; vertices[v.n].id = v.id;
        }
        int i = 0;
        for (var f = fHead.next; f != fHead; f = f.next)
        {
          if (!f.inside) continue; var edge = f.anEdge;
          do { indices.p[i++] = (ushort)edge.Org.n; edge = edge.Lnext; } while (edge != f.anEdge);
        }
      }
      void OutputContours()
      {
        Edge edge, start; int nv = 0, ni = 0;
        for (var f = fHead.next; f != fHead; f = f.next)
        {
          if (!f.inside) continue;
          start = edge = f.anEdge;
          do { nv++; edge = edge.Lnext; } while (edge != start);
          ni++;
        }
        indices.Capacity(indices.n + ni);
        vertices.Capacity(vertices.n + nv);
        for (var f = fHead.next; f != fHead; f = f.next)
        {
          if (!f.inside) continue;
          var vertCount = 0; start = edge = f.anEdge;
          do
          {
            vertices.p[vertices.n++] = edge.Org.coords; //vertices[vertIndex].p = edge._Org.coords; vertices[vertIndex].id = edge._Org.id;
            ++vertCount; edge = edge.Lnext;
          } while (edge != start);
          indices.p[indices.n++] = (ushort)vertCount;
        }
      }
      Region RegionBelow(Region reg)
      {
        return reg.nodeUp.prev.key;
      }
      Region RegionAbove(Region reg)
      {
        return reg.nodeUp.next.key;
      }
      bool EdgeLeq(Region reg1, Region reg2)
      {
        var e1 = reg1.eUp;
        var e2 = reg2.eUp;
        if (e1.Sym.Org == evt)
        {
          if (e2.Sym.Org == evt)
          {
            if (VertLeq(e1.Org, e2.Org)) return EdgeSign(e2.Sym.Org, e1.Org, e2.Org) <= 0;
            return EdgeSign(e1.Sym.Org, e2.Org, e1.Org) >= 0;
          }
          return EdgeSign(e2.Sym.Org, evt, e2.Org) <= 0;
        }
        if (e2.Sym.Org == evt) return EdgeSign(e1.Sym.Org, evt, e1.Org) >= 0;
        var t1 = EdgeEval(e1.Sym.Org, evt, e1.Org);
        var t2 = EdgeEval(e2.Sym.Org, evt, e2.Org);
        return t1 >= t2;
      }
      void DeleteRegion(Region reg)
      {
        if (reg.fixUpperEdge) { Debug.Assert(reg.eUp.winding == 0); }
        reg.eUp.activeRegion = null;
        Remove(reg.nodeUp);
        reg.next = rCache; rCache = reg; // free(reg);
      }
      void FixUpperEdge(Region reg, Edge newEdge)
      {
        Debug.Assert(reg.fixUpperEdge);
        Delete(reg.eUp);
        reg.fixUpperEdge = false;
        reg.eUp = newEdge;
        newEdge.activeRegion = reg;
      }
      Region TopLeftRegion(Region reg)
      {
        var org = reg.eUp.Org;
        do { reg = RegionAbove(reg); } while (reg.eUp.Org == org);
        if (reg.fixUpperEdge)
        {
          var e = Connect(RegionBelow(reg).eUp.Sym, reg.eUp.Lnext);
          FixUpperEdge(reg, e); reg = RegionAbove(reg);
        }
        return reg;
      }
      Region TopRightRegion(Region reg)
      {
        var dst = reg.eUp.Sym.Org;
        do { reg = RegionAbove(reg); } while (reg.eUp.Sym.Org == dst);
        return reg;
      }
      Region AddRegionBelow(Region regAbove, Edge eNewUp)
      {
        var regNew = newRegion();
        regNew.eUp = eNewUp;
        regNew.nodeUp = InsertBefore(regAbove.nodeUp, regNew);
        //regNew.fixUpperEdge = false;
        //regNew.sentinel = false;
        //regNew.dirty = false;
        eNewUp.activeRegion = regNew;
        return regNew;
      }
      void ComputeWinding(Region reg)
      {
        reg.windingNumber = RegionAbove(reg).windingNumber + reg.eUp.winding;
        reg.inside = IsWindingInside(winding, reg.windingNumber);
      }
      void FinishRegion(Region reg)
      {
        var e = reg.eUp;
        var f = e.Lface;
        f.inside = reg.inside;
        f.anEdge = e;
        DeleteRegion(reg);
      }
      Edge FinishLeftRegions(Region regFirst, Region regLast)
      {
        var regPrev = regFirst;
        var ePrev = regFirst.eUp;
        while (regPrev != regLast)
        {
          regPrev.fixUpperEdge = false;
          var reg = RegionBelow(regPrev);
          var e = reg.eUp;
          if (e.Org != ePrev.Org)
          {
            if (!reg.fixUpperEdge) { FinishRegion(regPrev); break; }
            e = Connect(ePrev.Onext.Sym, e.Sym);
            FixUpperEdge(reg, e);
          }
          if (ePrev.Onext != e) { Splice(e.Sym.Lnext, e); Splice(ePrev, e); }
          FinishRegion(regPrev);
          ePrev = reg.eUp;
          regPrev = reg;
        }
        return ePrev;
      }
      void AddRightEdges(Region regUp, Edge eFirst, Edge eLast, Edge eTopLeft, bool cleanUp)
      {
        bool firstTime = true;
        var e = eFirst; do
        {
          Debug.Assert(VertLeq(e.Org, e.Sym.Org));
          AddRegionBelow(regUp, e.Sym);
          e = e.Onext;
        } while (e != eLast);
        if (eTopLeft == null) eTopLeft = RegionBelow(regUp).eUp.Sym.Onext;
        Region regPrev = regUp, reg;
        var ePrev = eTopLeft;
        while (true)
        {
          reg = RegionBelow(regPrev);
          e = reg.eUp.Sym;
          if (e.Org != ePrev.Org) break;
          if (e.Onext != ePrev)
          {
            Splice(e.Sym.Lnext, e);
            Splice(ePrev.Sym.Lnext, e);
          }
          reg.windingNumber = regPrev.windingNumber - e.winding;
          reg.inside = IsWindingInside(winding, reg.windingNumber);
          regPrev.dirty = true;
          if (!firstTime && CheckForRightSplice(regPrev))
          {
            AddWinding(e, ePrev);
            DeleteRegion(regPrev);
            Delete(ePrev);
          }
          firstTime = false;
          regPrev = reg;
          ePrev = e;
        }
        regPrev.dirty = true;
        Debug.Assert(regPrev.windingNumber - e.winding == reg.windingNumber);
        if (cleanUp) WalkDirtyRegions(regPrev);
      }
      void SpliceMergeVertices(Edge e1, Edge e2)
      {
        Splice(e1, e2);
      }
      void VertexWeights(Vertex isect, Vertex org, Vertex dst)
      {
        var t1 = 0 | Number.Abs(org.x - isect.x) + Number.Abs(org.y - isect.y);
        var t2 = 0 | Number.Abs(dst.x - isect.x) + Number.Abs(dst.y - isect.y);
        var t3 = 0 | (t1 + t2) * 2;
        var w0 = 0 | t2 / t3;
        var w1 = 0 | t1 / t3;
        isect.coords.X = 0 | isect.coords.X + w0 * org.coords.X + w1 * dst.coords.X;
        isect.coords.Y = 0 | isect.coords.Y + w0 * org.coords.Y + w1 * dst.coords.Y;
        isect.coords.Z = 0 | isect.coords.Z + w0 * org.coords.Z + w1 * dst.coords.Z;
      }
      void GetIntersectData(Vertex isect, Vertex orgUp, Vertex dstUp, Vertex orgLo, Vertex dstLo)
      {
        isect.coords = new Vector3();
        VertexWeights(isect, orgUp, dstUp);
        VertexWeights(isect, orgLo, dstLo);
      }
      bool CheckForRightSplice(Region regUp)
      {
        var regLo = RegionBelow(regUp);
        var eUp = regUp.eUp;
        var eLo = regLo.eUp;
        if (VertLeq(eUp.Org, eLo.Org))
        {
          if (EdgeSign(eLo.Sym.Org, eUp.Org, eLo.Org) > 0) return false;
          if (!VertEq(eUp.Org, eLo.Org))
          {
            SplitEdge(eLo.Sym);
            Splice(eUp, eLo.Sym.Lnext);
            regUp.dirty = regLo.dirty = true;
          }
          else if (eUp.Org != eLo.Org)
          {
            vqRemove(eUp.Org.pqHandle);
            SpliceMergeVertices(eLo.Sym.Lnext, eUp);
          }
        }
        else
        {
          if (EdgeSign(eUp.Sym.Org, eLo.Org, eUp.Org) < 0) return false;
          RegionAbove(regUp).dirty = regUp.dirty = true;
          SplitEdge(eUp.Sym);
          Splice(eLo.Sym.Lnext, eUp);
        }
        return true;
      }
      bool CheckForLeftSplice(Region regUp)
      {
        var regLo = RegionBelow(regUp);
        var eUp = regUp.eUp;
        var eLo = regLo.eUp; Debug.Assert(!VertEq(eUp.Sym.Org, eLo.Sym.Org));
        if (VertLeq(eUp.Sym.Org, eLo.Sym.Org))
        {
          if (EdgeSign(eUp.Sym.Org, eLo.Sym.Org, eUp.Org) < 0) return false;
          RegionAbove(regUp).dirty = regUp.dirty = true;
          var e = SplitEdge(eUp);
          Splice(eLo.Sym, e);
          e.Lface.inside = regUp.inside;
        }
        else
        {
          if (EdgeSign(eLo.Sym.Org, eUp.Sym.Org, eLo.Org) > 0) return false;
          regUp.dirty = regLo.dirty = true;
          var e = SplitEdge(eLo);
          Splice(eUp.Lnext, eLo.Sym);
          e.Sym.Lface.inside = regUp.inside;
        }
        return true;
      }
      bool CheckForIntersect(Region regUp)
      {
        var regLo = RegionBelow(regUp);
        var eUp = regUp.eUp;
        var eLo = regLo.eUp;
        var orgUp = eUp.Org;
        var orgLo = eLo.Org;
        var dstUp = eUp.Sym.Org;
        var dstLo = eLo.Sym.Org;
        Debug.Assert(!VertEq(dstLo, dstUp));
        Debug.Assert(EdgeSign(dstUp, evt, orgUp) <= 0);
        Debug.Assert(EdgeSign(dstLo, evt, orgLo) >= 0);
        Debug.Assert(orgUp != evt && orgLo != evt);
        Debug.Assert(!regUp.fixUpperEdge && !regLo.fixUpperEdge);
        if (orgUp == orgLo) return false;
        var tMinUp = Number.Min(orgUp.y, dstUp.y);
        var tMaxLo = Number.Max(orgLo.y, dstLo.y);
        if (tMinUp > tMaxLo) return false;
        if (VertLeq(orgUp, orgLo)) { if (EdgeSign(dstLo, orgUp, orgLo) > 0) return false; }
        else { if (EdgeSign(dstUp, orgLo, orgUp) < 0) return false; }
        var isect = newVertex(); EdgeIntersect(dstUp, orgUp, dstLo, orgLo, isect);
        Debug.Assert(Number.Min(orgUp.y, dstUp.y) <= isect.y);
        Debug.Assert(isect.y <= Number.Max(orgLo.y, dstLo.y));
        Debug.Assert(Number.Min(dstLo.x, dstUp.x) <= isect.x);
        Debug.Assert(isect.x <= Number.Max(orgLo.x, orgUp.x));
        if (VertLeq(isect, evt)) { isect.x = evt.x; isect.y = evt.y; }
        var orgMin = VertLeq(orgUp, orgLo) ? orgUp : orgLo;
        if (VertLeq(orgMin, isect)) { isect.x = orgMin.x; isect.y = orgMin.y; }
        if (VertEq(isect, orgUp) || VertEq(isect, orgLo)) { CheckForRightSplice(regUp); return false; }
        if ((!VertEq(dstUp, evt) && EdgeSign(dstUp, evt, isect) >= 0) || (!VertEq(dstLo, evt) && EdgeSign(dstLo, evt, isect) <= 0))
        {
          if (dstLo == evt)
          {
            SplitEdge(eUp.Sym);
            Splice(eLo.Sym, eUp);
            regUp = TopLeftRegion(regUp);
            eUp = RegionBelow(regUp).eUp;
            FinishLeftRegions(RegionBelow(regUp), regLo);
            AddRightEdges(regUp, eUp.Sym.Lnext, eUp, eUp, true);
            return true;
          }
          if (dstUp == evt)
          {
            SplitEdge(eLo.Sym);
            Splice(eUp.Lnext, eLo.Sym.Lnext);
            regLo = regUp;
            regUp = TopRightRegion(regUp);
            var e = RegionBelow(regUp).eUp.Sym.Onext;
            regLo.eUp = eLo.Sym.Lnext;
            eLo = FinishLeftRegions(regLo, null);
            AddRightEdges(regUp, eLo.Onext, eUp.Sym.Onext, e, true);
            return true;
          }
          if (EdgeSign(dstUp, evt, isect) >= 0)
          {
            RegionAbove(regUp).dirty = regUp.dirty = true;
            SplitEdge(eUp.Sym);
            eUp.Org.x = evt.x;
            eUp.Org.y = evt.y;
          }
          if (EdgeSign(dstLo, evt, isect) <= 0)
          {
            regUp.dirty = regLo.dirty = true;
            SplitEdge(eLo.Sym);
            eLo.Org.x = evt.x;
            eLo.Org.y = evt.y;
          }
          return false;
        }
        SplitEdge(eUp.Sym);
        SplitEdge(eLo.Sym);
        Splice(eLo.Sym.Lnext, eUp);
        eUp.Org.x = isect.x;
        eUp.Org.y = isect.y;
        eUp.Org.pqHandle = vhInsert(eUp.Org);
        Debug.Assert(eUp.Org.pqHandle != 0x0fffffff);
        GetIntersectData(eUp.Org, orgUp, dstUp, orgLo, dstLo);
        RegionAbove(regUp).dirty = regUp.dirty = regLo.dirty = true;
        return false;
      }
      void WalkDirtyRegions(Region regUp)
      {
        var regLo = RegionBelow(regUp);
        Edge eUp, eLo;
        while (true)
        {
          while (regLo.dirty) { regUp = regLo; regLo = RegionBelow(regLo); }
          if (!regUp.dirty)
          {
            regLo = regUp;
            regUp = RegionAbove(regUp);
            if (regUp == null || !regUp.dirty) return;
          }
          regUp.dirty = false;
          eUp = regUp.eUp;
          eLo = regLo.eUp;
          if (eUp.Sym.Org != eLo.Sym.Org)
          {
            if (CheckForLeftSplice(regUp))
            {
              if (regLo.fixUpperEdge)
              {
                DeleteRegion(regLo);
                Delete(eLo);
                regLo = RegionBelow(regUp);
                eLo = regLo.eUp;
              }
              else if (regUp.fixUpperEdge)
              {
                DeleteRegion(regUp);
                Delete(eUp);
                regUp = RegionAbove(regLo);
                eUp = regUp.eUp;
              }
            }
          }
          if (eUp.Org != eLo.Org)
          {
            if (eUp.Sym.Org != eLo.Sym.Org && !regUp.fixUpperEdge && !regLo.fixUpperEdge && (eUp.Sym.Org == evt || eLo.Sym.Org == evt))
            {
              if (CheckForIntersect(regUp)) return;
            }
            else
            {
              CheckForRightSplice(regUp);
            }
          }
          if (eUp.Org == eLo.Org && eUp.Sym.Org == eLo.Sym.Org)
          {
            AddWinding(eLo, eUp);
            DeleteRegion(regUp);
            Delete(eUp);
            regUp = RegionAbove(regLo);
          }
        }
      }
      void ConnectRightVertex(Region regUp, Edge eBottomLeft)
      {
        var eTopLeft = eBottomLeft.Onext;
        var regLo = RegionBelow(regUp);
        var eUp = regUp.eUp;
        var eLo = regLo.eUp;
        bool degenerate = false;
        if (eUp.Sym.Org != eLo.Sym.Org) CheckForIntersect(regUp);
        if (VertEq(eUp.Org, evt))
        {
          Splice(eTopLeft.Sym.Lnext, eUp);
          regUp = TopLeftRegion(regUp);
          eTopLeft = RegionBelow(regUp).eUp;
          FinishLeftRegions(RegionBelow(regUp), regLo);
          degenerate = true;
        }
        if (VertEq(eLo.Org, evt))
        {
          Splice(eBottomLeft, eLo.Sym.Lnext);
          eBottomLeft = FinishLeftRegions(regLo, null);
          degenerate = true;
        }
        if (degenerate)
        {
          AddRightEdges(regUp, eBottomLeft.Onext, eTopLeft, eTopLeft, true);
          return;
        }
        Edge eNew;
        if (VertLeq(eLo.Org, eUp.Org)) eNew = eLo.Sym.Lnext; else eNew = eUp;
        eNew = Connect(eBottomLeft.Onext.Sym, eNew);
        AddRightEdges(regUp, eNew, eNew.Onext, eNew.Onext, false);
        eNew.Sym.activeRegion.fixUpperEdge = true;
        WalkDirtyRegions(regUp);
      }
      void ConnectLeftDegenerate(Region regUp, Vertex vEvent)
      {
        var e = regUp.eUp; Debug.Assert(!VertEq(e.Org, vEvent));
        if (!VertEq(e.Sym.Org, vEvent))
        {
          SplitEdge(e.Sym);
          if (regUp.fixUpperEdge)
          {
            Delete(e.Onext);
            regUp.fixUpperEdge = false;
          }
          Splice(vEvent.anEdge, e);
          SweepEvent(vEvent);
          return;
        }
        Debug.Assert(false);
      }
      void ConnectLeftVertex(Vertex vEvent)
      {
        var tmp = newRegion();
        tmp.eUp = vEvent.anEdge.Sym;
        var regUp = Find(tmp).key;
        var regLo = RegionBelow(regUp); if (regLo == null) return;
        var eUp = regUp.eUp;
        var eLo = regLo.eUp;
        if (EdgeSign(eUp.Sym.Org, vEvent, eUp.Org) == 0) { ConnectLeftDegenerate(regUp, vEvent); return; }
        var reg = VertLeq(eLo.Sym.Org, eUp.Sym.Org) ? regUp : regLo;
        if (regUp.inside || reg.fixUpperEdge)
        {
          Edge eNew;
          if (reg == regUp)
            eNew = Connect(vEvent.anEdge.Sym, eUp.Lnext);
          else
            eNew = Connect(eLo.Sym.Onext.Sym, vEvent.anEdge).Sym;
          if (reg.fixUpperEdge)
            FixUpperEdge(reg, eNew);
          else
            ComputeWinding(AddRegionBelow(regUp, eNew));
          SweepEvent(vEvent);
        }
        else
        {
          AddRightEdges(regUp, vEvent.anEdge, vEvent.anEdge, null, true);
        }
      }
      void SweepEvent(Vertex vEvent)
      {
        evt = vEvent;
        var e = vEvent.anEdge;
        while (e.activeRegion == null)
        {
          e = e.Onext; if (e == vEvent.anEdge) { ConnectLeftVertex(vEvent); return; }
        }
        var regUp = TopLeftRegion(e.activeRegion);
        var reg = RegionBelow(regUp);
        var eTopLeft = reg.eUp;
        var eBottomLeft = FinishLeftRegions(reg, null);
        if (eBottomLeft.Onext == eTopLeft)
        {
          ConnectRightVertex(regUp, eBottomLeft);
        }
        else
        {
          AddRightEdges(regUp, eBottomLeft.Onext, eTopLeft, eTopLeft, true);
        }
      }
      void AddSentinel(Number smin, Number smax, Number t)
      {
        var e = MakeEdge();
        e.Org.x = smax;
        e.Org.y = t;
        e.Sym.Org.x = smin;
        e.Sym.Org.y = t;
        evt = e.Sym.Org;
        var reg = newRegion();
        reg.eUp = e;
        //reg.windingNumber = 0;
        //reg.inside = false;
        //reg.fixUpperEdge = false;
        reg.sentinel = true;
        //reg.dirty = false;
        reg.nodeUp = Insert(reg);
      }
      void InitEdgeDict()
      {
        AddSentinel(-SentinelCoord, SentinelCoord, -SentinelCoord);
        AddSentinel(-SentinelCoord, SentinelCoord, SentinelCoord);
      }
      void DoneEdgeDict()
      {
        Region reg; int fixedEdges = 0;
        while ((reg = Min().key) != null)
        {
          if (!reg.sentinel) { Debug.Assert(reg.fixUpperEdge); Debug.Assert(++fixedEdges == 1); }
          Debug.Assert(reg.windingNumber == 0);
          DeleteRegion(reg);
        }

        //dict = null;
      }
      void RemoveDegenerateEdges()
      {
        Edge eh = eHead, e, eNext, eLnext;
        for (e = eh.next; e != eh; e = eNext)
        {
          eNext = e.next; eLnext = e.Lnext;
          if (VertEq(e.Org, e.Sym.Org) && e.Lnext.Lnext != e)
          {
            SpliceMergeVertices(eLnext, e);
            Delete(e); e = eLnext; eLnext = e.Lnext;
          }
          if (eLnext.Lnext == e)
          {
            if (eLnext != e)
            {
              if (eLnext == eNext || eLnext == eNext.Sym) eNext = eNext.next;
              Delete(eLnext);
            }
            if (e == eNext || e == eNext.Sym) eNext = eNext.next;
            Delete(e);
          }
        }
      }
      void InitPriorityQ()
      {
        Debug.Assert(vqEmpty);
        for (Vertex h = vHead, v = h.next; v != h; v = v.next) v.pqHandle = vhInsert(v);
        vqInit();
      }
      void RemoveDegenerateFaces()
      {
        Face f, fNext; Edge e;
        for (f = fHead.next; f != fHead; f = fNext)
        {
          fNext = f.next; e = f.anEdge; Debug.Assert(e.Lnext != e);
          if (e.Lnext.Lnext == e)
          {
            AddWinding(e.Onext, e);
            Delete(e);
          }
        }
      }
      protected void ComputeInterior()
      {
        RemoveDegenerateEdges();
        InitPriorityQ();
        RemoveDegenerateFaces();
        InitEdgeDict();
        Vertex v, vNext;
        while ((v = vqExtractMin()) != null)
        {
          while (true)
          {
            vNext = vqMinimum(); if (vNext == null || !VertEq(vNext, v)) break;
            vNext = vqExtractMin(); SpliceMergeVertices(v.anEdge, vNext.anEdge);
          }
          SweepEvent(v);
        }
        DoneEdgeDict();
        RemoveDegenerateFaces();
        Check();
      }

      class Region
      {
        internal Region next;
        internal Edge eUp;
        internal Node nodeUp;
        internal int windingNumber;
        internal bool inside, sentinel, dirty, fixUpperEdge;
      }

  #region Mesh
      Vertex vHead; Face fHead; Edge eHead, eHeadSym;
      Edge MakeEdge()
      {
        var e = MakeEdge1(eHead);
        MakeVertex(e, vHead);
        MakeVertex(e.Sym, vHead);
        MakeFace(newFace(), e, fHead);
        return e;
      }
      void Splice(Edge eOrg, Edge eDst)
      {
        if (eOrg == eDst) return;
        bool joiningVertices = false;
        if (eDst.Org != eOrg.Org)
        {
          joiningVertices = true;
          KillVertex(eDst.Org, eOrg.Org);
        }
        bool joiningLoops = false;
        if (eDst.Lface != eOrg.Lface)
        {
          joiningLoops = true;
          KillFace(eDst.Lface, eOrg.Lface);
        }
        Splice1(eDst, eOrg);
        if (!joiningVertices)
        {
          MakeVertex(eDst, eOrg.Org);
          eOrg.Org.anEdge = eOrg;
        }
        if (!joiningLoops)
        {
          MakeFace(newFace(), eDst, eOrg.Lface);
          eOrg.Lface.anEdge = eOrg;
        }
      }
      void Delete(Edge eDel)
      {
        var eDelSym = eDel.Sym;
        bool joiningLoops = false;
        if (eDel.Lface != eDel.Sym.Lface)
        {
          joiningLoops = true;
          KillFace(eDel.Lface, eDel.Sym.Lface);
        }
        if (eDel.Onext == eDel)
        {
          KillVertex(eDel.Org, null);
        }
        else
        {
          eDel.Sym.Lface.anEdge = eDel.Sym.Lnext;
          eDel.Org.anEdge = eDel.Onext;
          Splice1(eDel, eDel.Sym.Lnext);
          if (!joiningLoops) MakeFace(newFace(), eDel, eDel.Lface);
        }
        if (eDelSym.Onext == eDelSym)
        {
          KillVertex(eDelSym.Org, null);
          KillFace(eDelSym.Lface, null);
        }
        else
        {
          eDel.Lface.anEdge = eDelSym.Sym.Lnext;
          eDelSym.Org.anEdge = eDelSym.Onext;
          Splice1(eDelSym, eDelSym.Sym.Lnext);
        }
        KillEdge(eDel);
      }
      Edge AddEdgeVertex(Edge eOrg)
      {
        var eNew = MakeEdge1(eOrg);
        var eNewSym = eNew.Sym;
        Splice1(eNew, eOrg.Lnext);
        eNew.Org = eOrg.Sym.Org;
        MakeVertex(eNewSym, eNew.Org);
        eNew.Lface = eNewSym.Lface = eOrg.Lface;
        return eNew;
      }
      Edge SplitEdge(Edge eOrg)
      {
        var eTmp = AddEdgeVertex(eOrg);
        var eNew = eTmp.Sym;
        Splice1(eOrg.Sym, eOrg.Sym.Sym.Lnext);
        Splice1(eOrg.Sym, eNew);
        eOrg.Sym.Org = eNew.Org;
        eNew.Sym.Org.anEdge = eNew.Sym;
        eNew.Sym.Lface = eOrg.Sym.Lface;
        eNew.winding = eOrg.winding;
        eNew.Sym.winding = eOrg.Sym.winding;
        return eNew;
      }
      Edge Connect(Edge eOrg, Edge eDst)
      {
        var eNew = MakeEdge1(eOrg);
        var eNewSym = eNew.Sym;
        bool joiningLoops = false;
        if (eDst.Lface != eOrg.Lface)
        {
          joiningLoops = true;
          KillFace(eDst.Lface, eOrg.Lface);
        }
        Splice1(eNew, eOrg.Lnext);
        Splice1(eNewSym, eDst);
        eNew.Org = eOrg.Sym.Org;
        eNewSym.Org = eDst.Org;
        eNew.Lface = eNewSym.Lface = eOrg.Lface;
        eOrg.Lface.anEdge = eNewSym;
        if (!joiningLoops) MakeFace(newFace(), eNew, eOrg.Lface);
        return eNew;
      }
      void ZapFace(Face fZap)
      {
        var eStart = fZap.anEdge;
        var eNext = eStart.Lnext;
        Edge e, eSym;
        do
        {
          e = eNext;
          eNext = e.Lnext;
          e.Lface = null;
          if (e.Sym.Lface == null)
          {
            if (e.Onext == e) KillVertex(e.Org, null);
            else
            {
              e.Org.anEdge = e.Onext;
              Splice1(e, e.Sym.Lnext);
            }
            eSym = e.Sym;
            if (eSym.Onext == eSym) KillVertex(eSym.Org, null);
            else
            {
              eSym.Org.anEdge = eSym.Onext;
              Splice1(eSym, eSym.Sym.Lnext);
            }
            KillEdge(e);
          }
        } while (e != eStart);
        var fPrev = fZap.prev;
        var fNext = fZap.next;
        fNext.prev = fPrev;
        fPrev.next = fNext;
        fZap.next = fCache; fCache = fZap; //free(fZap);
      }
      [Conditional("DEBUG")]
      void Check()
      {
        Edge e; Face fPrev = fHead, f;
        for (fPrev = fHead; (f = fPrev.next) != fHead; fPrev = f)
        {
          e = f.anEdge;
          do
          {
            Debug.Assert(e.Sym != e);
            Debug.Assert(e.Sym.Sym == e);
            Debug.Assert(e.Lnext.Onext.Sym == e);
            Debug.Assert(e.Onext.Sym.Lnext == e);
            Debug.Assert(e.Lface == f);
            e = e.Lnext;
          } while (e != f.anEdge);
        }
        Debug.Assert(f.prev == fPrev && f.anEdge == null);
        Vertex vPrev = vHead, v;
        for (vPrev = vHead; (v = vPrev.next) != vHead; vPrev = v)
        {
          Debug.Assert(v.prev == vPrev);
          e = v.anEdge;
          do
          {
            Debug.Assert(e.Sym != e);
            Debug.Assert(e.Sym.Sym == e);
            Debug.Assert(e.Lnext.Onext.Sym == e);
            Debug.Assert(e.Onext.Sym.Lnext == e);
            Debug.Assert(e.Org == v);
            e = e.Onext;
          } while (e != v.anEdge);
        }
        Debug.Assert(v.prev == vPrev && v.anEdge == null);
        Edge ePrev = eHead;
        for (ePrev = eHead; (e = ePrev.next) != eHead; ePrev = e)
        {
          Debug.Assert(e.Sym.next == ePrev.Sym);
          Debug.Assert(e.Sym != e);
          Debug.Assert(e.Sym.Sym == e);
          Debug.Assert(e.Org != null);
          Debug.Assert(e.Sym.Org != null);
          Debug.Assert(e.Lnext.Onext.Sym == e);
          Debug.Assert(e.Onext.Sym.Lnext == e);
        }
        Debug.Assert(e.Sym.next == ePrev.Sym
            && e.Sym == eHeadSym
            && e.Sym.Sym == e
            && e.Org == null && e.Sym.Org == null
            && e.Lface == null && e.Sym.Lface == null);
      }
  #endregion

      class Face
      {
        internal Face prev, next, trail;
        internal Edge anEdge;
        internal int n;
        internal bool marked, inside;
        internal int VertexCount
        {
          get
          {
            int n = 0; var eCur = anEdge;
            do { n++; eCur = eCur.Lnext; } while (eCur != anEdge); return n;
          }
        }
      }
      class Vertex
      {
        internal Vertex prev, next;
        internal Edge anEdge;
        internal Vector3 coords;
        internal Number x, y;
        internal int pqHandle;
        internal int n;//, id;
      }
      class Edge
      {
        internal EdgePair pair;
        internal Edge next, Sym, Onext, Lnext;
        internal Vertex Org;
        internal Face Lface;
        internal Region activeRegion;
        internal int winding;
      }
      struct EdgePair { internal Edge e, eSym; }
      EdgePair CreateEdgePair()
      {
        var pair = new EdgePair();
        pair.e = newEdge(); pair.e.pair = pair;
        pair.eSym = newEdge(); pair.eSym.pair = pair;
        return pair;
      }
      static void EnsureFirst(ref Edge e) { if (e == e.pair.eSym) e = e.Sym; }
      Edge MakeEdge1(Edge eNext)
      {
        Debug.Assert(eNext != null);
        var pair = CreateEdgePair();
        var e = pair.e;
        var eSym = pair.eSym;
        EnsureFirst(ref eNext);
        var ePrev = eNext.Sym.next;
        eSym.next = ePrev;
        ePrev.Sym.next = e;
        e.next = eNext;
        eNext.Sym.next = eSym;
        e.Sym = eSym;
        e.Onext = e;
        e.Lnext = eSym;
        e.Org = null;
        e.Lface = null;
        e.winding = 0;
        e.activeRegion = null;
        eSym.Sym = e;
        eSym.Onext = eSym;
        eSym.Lnext = e;
        eSym.Org = null;
        eSym.Lface = null;
        eSym.winding = 0;
        eSym.activeRegion = null;
        return e;
      }
      static void Splice1(Edge a, Edge b)
      {
        var aOnext = a.Onext;
        var bOnext = b.Onext;
        aOnext.Sym.Lnext = b;
        bOnext.Sym.Lnext = a;
        a.Onext = bOnext;
        b.Onext = aOnext;
      }
      void MakeVertex(Edge eOrig, Vertex next)
      {
        var p = newVertex();
        var vPrev = next.prev; p.prev = vPrev;
        vPrev.next = p; p.next = next;
        next.prev = p; p.anEdge = eOrig;
        var e = eOrig; do { e.Org = p; e = e.Onext; } while (e != eOrig);
      }
      static void MakeFace(Face fNew, Edge eOrig, Face fNext)
      {
        Debug.Assert(fNew != null);
        var fPrev = fNext.prev;
        fNew.prev = fPrev;
        fPrev.next = fNew;
        fNew.next = fNext;
        fNext.prev = fNew;
        fNew.anEdge = eOrig;
        fNew.trail = null;
        fNew.marked = false;
        fNew.inside = fNext.inside;
        var e = eOrig; do { e.Lface = fNew; e = e.Lnext; } while (e != eOrig);
      }
      void KillEdge(Edge eDel)
      {
        EnsureFirst(ref eDel);
        var eNext = eDel.next;
        var ePrev = eDel.Sym.next;
        eNext.Sym.next = ePrev;
        ePrev.Sym.next = eNext;
        eDel.next = eCache; eCache = eDel; //free(eDel);
      }
      void KillVertex(Vertex vDel, Vertex newOrg)
      {
        var eStart = vDel.anEdge;
        var e = eStart;
        do { e.Org = newOrg; e = e.Onext; } while (e != eStart);
        var vPrev = vDel.prev;
        var vNext = vDel.next;
        vNext.prev = vPrev;
        vPrev.next = vNext;
        vDel.next = vCache; vCache = vDel; //free(vDel);
      }
      void KillFace(Face fDel, Face newLFace)
      {
        var eStart = fDel.anEdge;
        var e = eStart;
        do { e.Lface = newLFace; e = e.Lnext; } while (e != eStart);
        var fPrev = fDel.prev;
        var fNext = fDel.next;
        fNext.prev = fPrev;
        fPrev.next = fNext;
        fDel.next = fCache; fCache = fDel; //free(fDel);
      }
      static bool IsWindingInside(Winding rule, int n)
      {
        switch (rule)
        {
          default:
          case Winding.EvenOdd: return (n & 1) == 1;
          case Winding.NonZero: return n != 0;
          case Winding.Positive: return n > 0;
          case Winding.Negative: return n < 0;
          case Winding.AbsGeqTwo: return n >= 2 || n <= -2;
        }
      }

      static bool VertEq(Vertex l, Vertex r)
      {
        if (l.y.Sign != r.y.Sign) return false;
        return l.x.Equals(r.x) && l.y.Equals(r.y);
      }
      static bool VertLeq(Vertex l, Vertex r)
      {
        var t = l.x.CompareTo(r.x); return t < 0 || (t == 0 && l.y.CompareTo(r.y) <= 0);
        //return l.x < r.x || (l.x.Equals(r.x) && l.y <= r.y);
      }
      static bool TransLeq(Vertex l, Vertex r)
      {
        var t = l.y.CompareTo(r.y); return t < 0 || (t == 0 && l.x.CompareTo(r.x) <= 0);
        //return l.y < r.y || (l.y.Equals(r.y) && l.x <= r.x);
      }
      static int EdgeSign(Vertex u, Vertex v, Vertex w)
      {
        //return 0 ^ (v.y - w.y) * (v.x - u.x) + (v.y - u.y) * (w.x - v.x); //todo: check faster?
        var sl = v.x.CompareTo(u.x);
        var sr = w.x.CompareTo(v.x); if (sl + sr <= 0) return 0;
        var sa = v.y.CompareTo(w.y);
        var sb = v.y.CompareTo(u.y);
        var ss = sa * sl + sb * sr; if (ss != 0) return ss;
        return (1 |
          (sa != 0 ? v.y - w.y : 0) *
          (sl != 0 ? v.x - u.x : 0) +
          (sb != 0 ? v.y - u.y : 0) *
          (sr != 0 ? w.x - v.x : 0)).Sign;
      }
      static Number EdgeSign2(Vertex u, Vertex v, Vertex w)
      {
        return 0 | (v.y - w.y) * (v.x - u.x) + (v.y - u.y) * (w.x - v.x);

        //using (Number.Mach mach = 2)
        //{
        //  var l = v.x - u.x;
        //  var r = w.x - v.x; if (l.Sign + r.Sign <= 0) return rnull; //if ((l + r).Sign <= 0) return 0;
        //  var a = v.y - w.y;
        //  var c = v.y - u.y;
        //  //var x = a.Sign * l.Sign + c.Sign * r.Sign;
        //  //if (x == 0) return rnull;
        //  return 0 | a * l + c * r;
        //}

        //return 0 | ((!v.x.Equals(u.x) ? (v.y - w.y) * (v.x - u.x) : 0) +
        //            (!w.x.Equals(v.x) ? (v.y - u.y) * (w.x - v.x) : 0));
        //Debug.Assert(VertLeq(u, v) && VertLeq(v, w));
        //Number gapL = v.s - u.s;
        //Number gapR = w.s - v.s;
        //if ((gapL + gapR).Sign > 0) return (v.t - w.t) * gapL + (v.t - u.t) * gapR;
        //return 0; //vertical line
      }
      static Number EdgeEval(Vertex u, Vertex v, Vertex w)
      {
        using (Number.Mach mach = 2)
        {
          var l = v.x - u.x;
          var r = w.x - v.x; if (l.Sign + r.Sign <= 0) return 0;
          l = 0 | (l < r ?
             (v.y - u.y) + (l.Sign != 0 ? (u.y - w.y) * (l / (l + r)) : 0) :
             (v.y - w.y) + (r.Sign != 0 ? (w.y - u.y) * (r / (l + r)) : 0));
          return l;
        }
        //Debug.Assert(VertLeq(u, v) && VertLeq(v, w));
        //Number gapL = v.s - u.s;
        //Number gapR = w.s - v.s;
        //if ((gapL + gapR).Sign > 0)
        //{
        //  if (gapL < gapR)
        //    return (v.t - u.t) + (u.t - w.t) * (gapL / (gapL + gapR));
        //  else
        //    return (v.t - w.t) + (w.t - u.t) * (gapR / (gapL + gapR));
        //}
        //return 0;// vertical line
      }
      static Number TransEval(Vertex u, Vertex v, Vertex w)
      {
        using (Number.Mach mach = 2)
        {
          var l = v.y - u.y;
          var r = w.y - v.y; if (l.Sign + r.Sign <= 0) return 0;
          return 0 | (l < r ?
            (v.x - u.x) + (l.Sign != 0 ? (u.x - w.x) * (l / (l + r)) : 0) :
            (v.x - w.x) + (r.Sign != 0 ? (w.x - u.x) * (r / (l + r)) : 0));
        }
        //Debug.Assert(TransLeq(u, v) && TransLeq(v, w));
        //Number gapL = v.t - u.t;
        //Number gapR = w.t - v.t;
        //if ((gapL + gapR).Sign > 0)
        //{
        //  if (gapL < gapR)
        //    return (v.s - u.s) + (u.s - w.s) * (gapL / (gapL + gapR));
        //  else
        //    return (v.s - w.s) + (w.s - u.s) * (gapR / (gapL + gapR));
        //}
        //return 0;//vertical line
      }
      static Number TransSign(Vertex u, Vertex v, Vertex w)
      {
        using (Number.Mach mach = 2)
        {
          var l = v.y - u.y;
          var r = w.y - v.y; if (l.Sign + r.Sign <= 0) return 0;
          return 0 | (v.x - w.x) * l + (v.x - u.x) * r;
        }

        //return 0 | ((!v.y.Equals(u.y) ? (v.x - w.x) * (v.y - u.y) : 0) +
        //            (!w.y.Equals(v.y) ? (v.x - u.x) * (w.y - v.y) : 0));

        //Debug.Assert(TransLeq(u, v) && TransLeq(v, w));
        //Number gapL = v.t - u.t;
        //Number gapR = w.t - v.t;
        //if ((gapL + gapR).Sign > 0)
        //  return (v.s - w.s) * gapL + (v.s - u.s) * gapR;
        //return 0;//vertical line
      }
      static Number Interpolate(Number a, Number x, Number b, Number y)
      {
        a = a.Sign < 0 ? 0 : a;
        b = b.Sign < 0 ? 0 : b;
        return 0 | (a > b ? y + (x - y) * b / (a + b) : b.Sign != 0 ? x + (y - x) * a / (a + b) : (x + y) / 2);
      }
      static bool EdgeGoesLeft(Edge e)
      {
        return VertLeq(e.Sym.Org, e.Org);
      }
      static bool EdgeGoesRight(Edge e)
      {
        return VertLeq(e.Org, e.Sym.Org);
      }
      static void AddWinding(Edge eDst, Edge eSrc)
      {
        eDst.winding += eSrc.winding;
        eDst.Sym.winding += eSrc.Sym.winding;
      }
      static void Swap<T>(ref T a, ref T b) { var t = a; a = b; b = t; }
      static void EdgeIntersect(Vertex o1, Vertex d1, Vertex o2, Vertex d2, Vertex v)
      {
        if (!VertLeq(o1, d1)) { Swap(ref o1, ref d1); }
        if (!VertLeq(o2, d2)) { Swap(ref o2, ref d2); }
        if (!VertLeq(o1, o2)) { Swap(ref o1, ref o2); Swap(ref d1, ref d2); }
        if (!VertLeq(o2, d1))
        {
          v.x = 0 | (o2.x + d1.x) / 2;
        }
        else if (VertLeq(d1, d2))
        {
          var z1 = EdgeEval(o1, o2, d1);
          var z2 = EdgeEval(o2, d1, d2); if ((0 ^ z1 + z2) < 0) { z1 = -z1; z2 = -z2; }
          v.x = Interpolate(z1, o2.x, z2, d1.x);
        }
        else
        {
          var z1 = EdgeSign2(o1, o2, d1);
          var z2 = -EdgeSign2(o1, d2, d1); if ((0 ^ z1 + z2) < 0) { z1 = -z1; z2 = -z2; }
          v.x = Interpolate(z1, o2.x, z2, d2.x);
        }
        if (!TransLeq(o1, d1)) { Swap(ref o1, ref d1); }
        if (!TransLeq(o2, d2)) { Swap(ref o2, ref d2); }
        if (!TransLeq(o1, o2)) { Swap(ref o1, ref o2); Swap(ref d1, ref d2); }
        if (!TransLeq(o2, d1))
        {
          v.y = 0 | (o2.y + d1.y) / 2;
        }
        else if (TransLeq(d1, d2))
        {
          var z1 = TransEval(o1, o2, d1);
          var z2 = TransEval(o2, d1, d2); if ((0 ^ z1 + z2) < 0) { z1 = -z1; z2 = -z2; }
          v.y = Interpolate(z1, o2.y, z2, d1.y);
        }
        else
        {
          var z1 = TransSign(o1, o2, d1);
          var z2 = -TransSign(o1, d2, d1); if ((0 ^ z1 + z2) < 0) { z1 = -z1; z2 = -z2; }
          v.y = Interpolate(z1, o2.y, z2, d2.y);
        }
      }

      class Node { internal Region key; internal Node prev, next; }

  #region Regions
      Node nodehead;
      Node Insert(Region key)
      {
        return InsertBefore(nodehead, key);
      }
      Node InsertBefore(Node node, Region key)
      {
        do node = node.prev; while (node.key != null && !EdgeLeq(node.key, key));
        var p = nCache; if (p == null) p = new Node(); else nCache = p.next;
        p.next = node.next; node.next.prev = p;
        p.prev = node; node.next = p; p.key = key; return p;
      }
      Node Find(Region key)
      {
        var p = nodehead; do p = p.next; while (p.key != null && !EdgeLeq(key, p.key)); return p;
      }
      Node Min()
      {
        return nodehead.next;
      }
      void Remove(Node node)
      {
        node.next.prev = node.prev;
        node.prev.next = node.next;
        node.next = nCache; nCache = node; //free(node);
      }
  #endregion
  #region VertexQueue
      Vertex[] vqkeys; int[] vqorder; int vqsize, vqmax;
      bool vqEmpty { get { return vqsize == 0 && vhEmpty; } }
      void vqInit()
      {
        var vqtop = 0; var vqstack = stackalloc int[vqsize + 1];
        int p = 0, r = vqsize - 1, i, j, piv; uint seed = 2016473283;
        if (vqorder == null || vqorder.Length < vqsize) vqorder = new int[((vqsize >> 5) + 1) << 5];
        for (piv = 0, i = p; i <= r; ++piv, ++i) vqorder[i] = piv;
        vqstack[vqtop++] = p | (r << 16);
        while (vqtop > 0)
        {
          var top = vqstack[--vqtop];
          p = top & 0xffff; r = top >> 16;
          while (r > p + 10)
          {
            seed = seed * 1539415821 + 1;
            i = p + (int)(seed % (r - p + 1));
            piv = vqorder[i];
            vqorder[i] = vqorder[p];
            vqorder[p] = piv;
            i = p - 1;
            j = r + 1;
            do
            {
              do { ++i; } while (!VertLeq(vqkeys[vqorder[i]], vqkeys[piv]));
              do { --j; } while (!VertLeq(vqkeys[piv], vqkeys[vqorder[j]]));
              Swap(ref vqorder[i], ref vqorder[j]);
            } while (i < j);
            Swap(ref vqorder[i], ref vqorder[j]); Debug.Assert(vqtop < vqsize);
            if (i - p < r - j) { vqstack[vqtop++] = (j + 1) | (r << 16); r = i - 1; }
            else { vqstack[vqtop++] = p | ((i - 1) << 16); p = j + 1; }
          }
          for (i = p + 1; i <= r; ++i)
          {
            piv = vqorder[i];
            for (j = i; j > p && !VertLeq(vqkeys[piv], vqkeys[vqorder[j - 1]]); --j) vqorder[j] = vqorder[j - 1];
            vqorder[j] = piv;
          }
        }
#if (DEBUG)
        p = 0; r = vqsize - 1;
        for (i = p; i < r; ++i) Debug.Assert(VertLeq(vqkeys[vqorder[i + 1]], vqkeys[vqorder[i]]), "wrong sort");
#endif
        vqmax = vqsize; vhInit();
      }
      Vertex vqExtractMin()
      {
        if (vqsize == 0) return vhExtractMin();
        Vertex sortMin = vqkeys[vqorder[vqsize - 1]];
        if (!vhEmpty)
        {
          Vertex heapMin = vhMinimum();
          if (VertLeq(heapMin, sortMin)) return vhExtractMin();
        }
        do { --vqsize; } while (vqsize > 0 && vqkeys[vqorder[vqsize - 1]] == null);
        return sortMin;
      }
      Vertex vqMinimum()
      {
        if (vqsize == 0) return vhMinimum();
        Vertex sortMin = vqkeys[vqorder[vqsize - 1]];
        if (!vhEmpty)
        {
          Vertex heapMin = vhMinimum();
          if (VertLeq(heapMin, sortMin)) return heapMin;
        }
        return sortMin;
      }
      void vqRemove(int handle)
      {
        int curr = handle;
        if (curr >= 0) { vhRemove(handle); return; }
        curr = -(curr + 1);
        Debug.Assert(curr < vqmax && vqkeys[curr] != null);
        vqkeys[curr] = null; while (vqsize > 0 && vqkeys[vqorder[vqsize - 1]] == null) --vqsize;
      }
  #endregion
  #region VertexHeap
      struct Elem { internal Vertex key; internal int node; }
      int[] vhnodes; Elem[] vhhandles;
      int vhsize, vhmax, vhfreeList;
      bool vhEmpty { get { return vhsize == 0; } }
      void vhFloatDown(int curr)
      {
        int child, hCurr, hChild;
        hCurr = vhnodes[curr];
        while (true)
        {
          child = curr << 1;
          if (child < vhsize && VertLeq(vhhandles[vhnodes[child + 1]].key, vhhandles[vhnodes[child]].key)) ++child;
          Debug.Assert(child <= vhmax);
          hChild = vhnodes[child];
          if (child > vhsize || VertLeq(vhhandles[hCurr].key, vhhandles[hChild].key))
          {
            vhnodes[curr] = hCurr;
            vhhandles[hCurr].node = curr; break;
          }
          vhnodes[curr] = hChild;
          vhhandles[hChild].node = curr;
          curr = child;
        }
      }
      void vhFloatUp(int curr)
      {
        int parent, hCurr, hParent;
        hCurr = vhnodes[curr];
        while (true)
        {
          parent = curr >> 1;
          hParent = vhnodes[parent];
          if (parent == 0 || VertLeq(vhhandles[hParent].key, vhhandles[hCurr].key))
          {
            vhnodes[curr] = hCurr;
            vhhandles[hCurr].node = curr; break;
          }
          vhnodes[curr] = hParent;
          vhhandles[hParent].node = curr;
          curr = parent;
        }
      }
      void vhInit()
      {
        for (int i = vhsize; i >= 1; --i) vhFloatDown(i);
      }
      int vhInsert(Vertex value)
      {
        int curr = ++vhsize;
        if ((curr << 1) > vhmax)
        {
          vhmax <<= 1;
          Array.Resize(ref vhnodes, vhmax + 1);
          Array.Resize(ref vhhandles, vhmax + 1);
        }
        int free;
        if (vhfreeList == 0) free = curr;
        else { free = vhfreeList; vhfreeList = vhhandles[free].node; }
        vhnodes[curr] = free;
        vhhandles[free].node = curr;
        vhhandles[free].key = value;
        vhFloatUp(curr);
        Debug.Assert(free != 0x0fffffff);
        return free;
      }
      Vertex vhExtractMin()
      {
        int hMin = vhnodes[1];
        Vertex min = vhhandles[hMin].key;
        if (vhsize > 0)
        {
          vhnodes[1] = vhnodes[vhsize];
          vhhandles[vhnodes[1]].node = 1;
          vhhandles[hMin].key = null;
          vhhandles[hMin].node = vhfreeList;
          vhfreeList = hMin;
          if (--vhsize > 0) vhFloatDown(1);
        }
        return min;
      }
      Vertex vhMinimum()
      {
        return vhhandles[vhnodes[1]].key;
      }
      void vhRemove(int handle)
      {
        int hCurr = handle;
        Debug.Assert(hCurr >= 1 && hCurr <= vhmax && vhhandles[hCurr].key != null);
        int curr = vhhandles[hCurr].node;
        vhnodes[curr] = vhnodes[vhsize];
        vhhandles[vhnodes[curr]].node = curr;
        if (curr <= --vhsize) { if (curr <= 1 || VertLeq(vhhandles[vhnodes[curr >> 1]].key, vhhandles[vhnodes[curr]].key)) vhFloatDown(curr); else vhFloatUp(curr); }
        vhhandles[hCurr].key = null;
        vhhandles[hCurr].node = vhfreeList;
        vhfreeList = hCurr;
      }
  #endregion
    }
  }
#endif

#if (false)

    //static int find(byte* p, int n, string s)
    //{
    //  int i = 0, l = s.Length, m = n - l;
    //  for (; i < m; i++) { int k = 0; for (; k < l && p[i + k] == s[k]; k++) ; if (k == l) return i; }
    //  return -1;
    //}

    //ushort* iia; var nia = nodes[ia].GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&iia) / sizeof(ushort);
    //float3* ppa; var npa = nodes[ia].GetBufferPtr(BUFFER.POINTBUFFER, (void**)&ppa) / sizeof(float3);
    //ushort* iib; var nib = nodes[ib].GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&iib) / sizeof(ushort);
    //float3* ppb; var npb = nodes[ib].GetBufferPtr(BUFFER.POINTBUFFER, (void**)&ppb) / sizeof(float3);

    static float* zbuffer; static int zbdx, zbdy; static float maxzdz;

    static void randda(int fl, float3* p1, float3* p2, float3* p3)
    {
      if ((*(float2*)p2 - *(float2*)p1 ^ *(float2*)p3 - *(float2*)p1) <= 0) return;
      if (p1->y > p2->y) { var t = p1; p1 = p2; p2 = t; }
      if (p2->y > p3->y) { var t = p2; p2 = p3; p3 = t; }
      if (p1->y > p2->y) { var t = p1; p1 = p2; p2 = t; }
      int y1 = (int)p1->y, y2 = (int)p2->y, y3 = (int)p3->y;
      if (y1 > zbdy) return;
      if (y3 <= 0) return;
      if (y2 <= 0) y1 = y2; else if (y2 > zbdy) y2 = zbdy;
      if (y3 > zbdy) y3 = zbdy;
      int dy31 = y3 - y1; if (dy31 == 0) return;
      var zmin = Math.Min(Math.Min(p1->z, p2->z), p3->z); if (zmin >= 1) return;
      var zmax = Math.Max(Math.Max(p1->z, p2->z), p3->z); if (zmax <= 0) return;
      var e = *p2 - *p1 ^ *p3 - *p1;
      var zz = new float3(e.x, e.y, -(p1->x * e.x + p1->y * e.y + p1->z * e.z)) * (-1 / e.z);
      //var ee = PlaneFromPoints(*p1, *p2, *p3); //if (Math.Abs(e.z) < 1e-3) return; 
      //var zz = new float3(ee.x, ee.y, ee.w) * (-1 / ee.z); //var zz = new float3(-e.x / e.z, -e.y / e.z, -e.w / e.z);
      var zline = zbuffer + y1 * zbdx;
      var ax31 = (p3->x - p1->x) / (p3->y - p1->y);
      var dy21 = y2 - y1;
      if (dy21 != 0)
      {
        float ax21 = (p2->x - p1->x) / (p2->y - p1->y), xx1 = p1->x, xx2 = xx1;
        float dx1 = ax21, dx2 = ax31; if (dx1 > dx2) { var t = dx1; dx1 = dx2; dx2 = t; }
        var y = y1; if (y < 0) { xx1 -= dx1 * y; xx2 -= dx2 * y; zline -= zbdx * y; y = 0; }
        for (; y < y2; y++, xx1 += dx1, xx2 += dx2, zline += zbdx)
        {
          int x1 = Math.Max((int)xx1, 0), x2 = Math.Min((int)xx2, zbdx);
          var yz = zz.y * y + zz.z;
          for (int x = x1; x < x2; x++)
          {
            var z = zz.x * x + yz;// zz.y * y + zz.z;
            if (z < zmin) z = zmin; else if (z > zmax) z = zmax;
            if (z > 1) continue;
            if (fl == 0)
            {
              if (z > zline[x]) zline[x] = z;
            }
            else
            {
              if (z < 0) continue;
              if (z < zline[x]) maxzdz = Math.Max(maxzdz, zline[x] - z);
            }
          }
        }
      }
      int dy32 = y3 - y2;
      if (dy32 != 0)
      {
        float ax32 = (p3->x - p2->x) / (p3->y - p2->y), xx1 = p2->x, xx2 = p1->x + ax31 * (p2->y - p1->y);
        float dx1 = ax32, dx2 = ax31; if (xx1 > xx2) { var t = dx1; dx1 = dx2; dx2 = t; t = xx1; xx1 = xx2; xx2 = t; }
        var y = y2; if (y < 0) { xx1 -= dx1 * y; xx2 -= dx2 * y; zline -= zbdx * y; y = 0; }
        for (; y < y3; y++, xx1 += dx1, xx2 += dx2, zline += zbdx)
        {
          int x1 = Math.Max((int)xx1, 0), x2 = Math.Min((int)xx2, zbdx);
          var yz = zz.y * y + zz.z;
          for (int x = x1; x < x2; x++)
          {
            var z = zz.x * x + yz; // zz.y * y + zz.z;
            if (z < zmin) z = zmin; else if (z > zmax) z = zmax;
            if (z > 1) continue;
            if (fl == 0)
            {
              if (z > zline[x]) zline[x] = z;
            }
            else
            {
              if (z < 0) continue;
              if (z < zline[x]) maxzdz = Math.Max(maxzdz, zline[x] - z);
            }
          }
        }
      }
    }
   static bool intersect_triangle(float3 P, float3 V, float3 A, float3 B, float3 C, out float t)//,out float u out float v, out float3 N)
    {
      var e1 = B - A; t = 0;
      var e2 = C - A; var n = e1 ^ e2;
      var det = V & n; if (det < 1e-6f) return false;
      var t1 = P - A; var t2 = t1 ^ V;
      var u = (e2 & t2) * (det = -1f / det); if (u < 0) return false;
      var v = -(e1 & t2) * det; if (v < 0 || u + v > 1) return false;
      t = (t1 & n) * det; if (t < 0) return false;
      return true;// det >= 1e-6f && t >= 0 && u >= 0 && v >= 0 && u + v <= 1;
    }
    static bool intersect_triangle3(float3 P, float3 V, float3 A, float3 B, float3 C, out float t)//,out float u out float v, out float3 N)
    {
      var e1 = B - A; t = 0;
      var e2 = C - A; var n = e1 ^ e2;
      var det = -(V & n); if (det < 1e-6f) return false;
      var invdet = 1f / det;
      var ao = P - A;
      var dao = ao ^ V;
      var u = (e2 & dao) * invdet; if (u < 0) return false;
      var v = -(e1 & dao) * invdet; if (v < 0 || u + v > 1) return false;
      t = (ao & n) * invdet; if (t < 0) return false;
      return true;// det >= 1e-6f && t >= 0 && u >= 0 && v >= 0 && u + v <= 1;
    }
    static bool intersect_triangle2(float3 P, float3 V, float3 A, float3 B, float3 C, out float3 ps)//,out float u out float v, out float3 N)
    {
      var e1 = B - A; ps = default;
      var e2 = C - A; var n = e1 ^ e2;
      var det = -(V & n); if (Math.Abs(det) < 1e-6f) return false;
      var invdet = 1f / det;
      var ao = P - A;
      var dao = ao ^ V;
      var u = (e2 & dao) * invdet; if (u < 0) return false;
      var v = -(e1 & dao) * invdet; if (v < 0 || u + v > 1) return false;
      var t = (ao & n) * invdet; if (t < 0) return false;
      if (t > 1) return false;
      //ps = A + e1 * u + e2 * v;
      return true;
    }
    static float dist(float3 a, float3 b, float3 p)
    {
      var v1 = p - a; var v2 = b - a;
      var v3 = (v1 & v2) / v2.LengthSq;
      return (v1 - v2 * v3).Length;
    }
    static float dist(float3 a1, float3 a2, float3 b1, float3 b2)
    {
      return ((a2 - a1) ^ (b2 - b1)).Normalize() & (a1 - b2);
    }
    static float3 intersect(float4 e, float3 a, float3 b)
    {
      var u = e.x * a.x + e.y * a.y + e.z * a.z;
      var v = e.x * b.x + e.y * b.y + e.z * b.z; var w = (u + e.w) / (u - v);
      return new float3(a.x + (b.x - a.x) * w, a.y + (b.y - a.y) * w, a.z + (b.z - a.z) * w);
    }
    static float intersectw(float4 e, float3 a, float3 b)
    {
      var u = e.x * a.x + e.y * a.y + e.z * a.z;
      var v = e.x * b.x + e.y * b.y + e.z * b.z;
      return (u + e.w) / (u - v);
    }

      internal void Move2(ref float4x3 m)
      {
        if (boxes == null) return;
        var info = view.debuginfo ?? (view.debuginfo = new System.Collections.Generic.List<string>());
        info.Clear();

        //info.Add($"l: {last:0.##}");

        var v = m.mp;
        var u = v - lastok;
        for (int i = 0; i < nsel; i++) boxes[i] = starts[i] + lastok;
        if (u == default) return;
        var ulength = u.Length; var uno = u * (1 / ulength);

        info.Add($"u: {u:0.##}");

        float4x3 buffa, buffb; var pa = (float3*)&buffa; var pb = (float3*)&buffb;
        var fmax = 0f; int count1 = 0, count2 = 0;
        sw.Restart();
        for (int isel = 0; isel < nsel; isel++)
        {
          var selbox = boxes[isel]; selbox.Extend(u);
          testbox = selbox;

          for (int iunsel = nsel; iunsel < boxes.Length; iunsel++)
          {
            var unselbox = boxes[iunsel];
            var intbox = selbox & unselbox; if (intbox.IsEmpty) continue;
            intbox = intbox.Inflate(0.1f); var catchbox = intbox; catchbox.Extend(-u);
            testbox = intbox;
            //info.Add($"intersect: {nodes[i].Name} {nodes[k].Name}");
            //info.Add($"box: {c.max - c.min:0.#} min: {c.min:0.#} max: {c.max:0.#}");

            ushort* iia; var nia = nodes[isel].GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&iia) / sizeof(ushort);
            float3* ppa; var npa = nodes[isel].GetBufferPtr(BUFFER.POINTBUFFER, (void**)&ppa) / sizeof(float3);
            var wpa = wpps[isel]; if (wpa == null) wpps[isel] = wpa = new float3[npa];
            var ma = nodes[isel].GetTransform(); if (lastok != last) ma = ma * (lastok - last);
            for (int t = 0; t < npa; t++) wpa[t] = ppa[t] * ma;

            ushort* iib; var nib = nodes[iunsel].GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&iib) / sizeof(ushort);
            var wpb = wpps[iunsel];
            if (wpb == null)
            {
              var mb = nodes[iunsel].GetTransform();
              float3* ppb; var npb = nodes[iunsel].GetBufferPtr(BUFFER.POINTBUFFER, (void**)&ppb) / sizeof(float3);
              wpps[iunsel] = wpb = new float3[npb]; for (int t = 0; t < npb; t++) wpb[t] = ppb[t] * mb;
            }

            if (pff == null || pff.Length < nib / 3) pff = new int[nib / 3];
            int nbact = 0;
            for (int i = 0; i < nib; i += 3)
            {
              ref var p1 = ref wpb[iib[i]]; ref var p2 = ref wpb[iib[i + 1]]; ref var p3 = ref wpb[iib[i + 2]];
              if ((u & ((p2 - p1) ^ (p3 - p1))) > -1e-5f) continue;
              var box = new float3box(p1, p2, p3); if ((box & intbox).IsEmpty) continue;
              pff[nbact++] = i;
            }
            if (ppp == null || ppp.Length < npa) ppp = new int[npa]; else Array.Clear(ppp, 0, npa);

            for (int ait = 0; ait < nia; ait += 3)
            {
              pa[0] = wpa[iia[ait + 0]];
              pa[1] = wpa[iia[ait + 1]];
              pa[2] = wpa[iia[ait + 2]];

              var boxa = new float3box(pa[0], pa[1], pa[2]);
              if ((boxa & catchbox).IsEmpty) { count2++; continue; }

              float3 ae1, ae2, anv, ano = (anv = (ae1 = pa[1] - pa[0]) ^ (ae2 = pa[2] - pa[0])).Normalize(); //!!!
              if ((ano & uno) < 1e-5f) continue;
              var plane1 = PlaneFromPointNormal(pa[0], ano);
              var plane2 = PlaneFromPointNormal(pa[0] + u, ano);
              for (int ia = 0; ia < 3; ia++)
              {
                ref var ap1 = ref pa[ia];
                ref var ap2 = ref pa[(ia + 1) % 3];
                ref var ap3 = ref pa[(ia + 2) % 3];

                var testap1 = ppp[iia[ait + ia]] == 0; if (testap1) ppp[iia[ait + ia]] = 1;

                var ap1u = ap1 + u;
                var ap2u = ap2 + u;
                //for (int bit = 0; bit < nib; bit += 3)
                for (var tx = 0; tx < nbact; tx++)
                {
                  var bit = pff[tx];
                  pb[0] = wpb[iib[bit + 0]];
                  pb[1] = wpb[iib[bit + 1]];
                  pb[2] = wpb[iib[bit + 2]];
                  if (DotCoord(plane2, pb[0]) > 0 && DotCoord(plane2, pb[1]) > 0 && DotCoord(plane2, pb[2]) > 0) continue;
                  if (DotCoord(plane1, pb[0]) < 0 && DotCoord(plane1, pb[1]) < 0 && DotCoord(plane1, pb[2]) < 0) continue;
                  count1++;
                  if (testap1)
                  {
                    //if (intersect_triangle(ap1 + u, -u, pb[0], pb[1], pb[2], out var d1))
                    float3 ee1, ee2, bno = (ee1 = pb[1] - pb[0]) ^ (ee2 = pb[2] - pb[0]);
                    var det = u & bno; if (det > -1e-5f) continue;// if((bno & uno) >= 0) continue;
                    var t0 = 1 / det;
                    var t1 = ap1 + u - pb[0]; var t2 = u ^ t1;
                    var uu = (ee2 & t2) * t0;
                    if (uu >= 0)
                    {
                      var vv = -(ee1 & t2) * t0;
                      if (vv >= 0 && uu + vv <= 1)
                      {
                        var tt = (t1 & bno) * t0;
                        if (tt > fmax && tt <= 1)
                        {
                          fmax = tt; plane2 = PlaneFromPointNormal(pa[0] + u * (1 - fmax), ano);
                        }
                      }
                    }
                  }
                  for (int ib = 0; ib < 3; ib++)
                  {
                    ref var bp1 = ref pb[ib];
                    ref var bp2 = ref pb[(ib + 1) % 3];
                    ref var bp3 = ref pb[(ib + 2) % 3];
                    if (ia == 0)
                    {
                      //if (intersect_triangle(bp1 - u, u, ap1, ap2, ap3, out var d2))
                      var adet = u & anv;
                      var tt1 = (bp1 - u) - ap1; var tt2 = tt1 ^ u;
                      var bu = (ae2 & tt2) * (adet = -1f / adet);
                      if (bu >= 0)
                      {
                        var bv = -(ae1 & tt2) * adet;
                        if (bv >= 0 && bu + bv <= 1)
                        {
                          var tt = (tt1 & anv) * adet;
                          if (tt > fmax && tt <= 1)
                          {
                            fmax = tt; plane2 = PlaneFromPointNormal(pa[0] + u * (1 - fmax), ano);
                          }
                        }
                      }
                    }
                    var e = PlaneFromPointNormal(ap1, ap2 - ap1 ^ ap2u - ap1);
                    var f = intersectw(e, bp1, bp2);
                    if (float.IsNaN(f)) continue;
                    if (f < 0 || f > 1) continue;

                    var ps = bp1 + (bp2 - bp1) * f;
                    var e2 = PlaneFromPointNormal(ps, bp2 - bp1 ^ u);
                    var f2 = intersectw(e2, ap1u, ap2u);
                    if (float.IsNaN(f2)) continue;
                    if (f2 < 0 || f2 > 1) continue;

                    var ps2 = ap1u + (ap2u - ap1u) * f2;
                    var dir = (ps2 - ps) & u; if (dir < 0) continue;
                    var d = (ps2 - ps).Length / ulength;
                    if (d > fmax && d <= 1)
                    {
                      fmax = d; plane2 = PlaneFromPointNormal(pa[0] + u * (1 - fmax), ano);
                    }
                  }
                }
              }
            }
          }
        }
        sw.Stop();
        info.Add($"count1: {count1} count2: {count2} fmax: {fmax}  {sw.ElapsedMilliseconds} ms");
        if (fmax != 0) { v -= u * fmax; m.mp = v; } //else lastok = v;
        last = v;
      }
      
#endif
}

// HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad\AAPThreshold 2
//[DllImport("user32.dll")]
//static extern short GetKeyState(Keys key);
//[DllImport("User32.dll")]
//static extern short GetAsyncKeyState(Keys k);
//static bool IsPressed(Keys k) => (GetAsyncKeyState(k) & 1) != 0;
//static void test()
//{
// //var val = Registry.GetValue("HKEY_CURRENT_USER" + '\\' +
// //  "Software\\Microsoft\\Windows\\CurrentVersion\\PrecisionTouchPad",
// //  "AAPThreshold", null);
// //if (val is int i && i != 0)
// //{
// //  Registry.SetValue("HKEY_CURRENT_USER" + '\\' +
// //    "Software\\Microsoft\\Windows\\CurrentVersion\\PrecisionTouchPad",
// //    "AAPThreshold", 0);
// //  System.Windows.Input.Mouse.PrimaryDevice.Synchronize();
// //  System.Windows.Input.Mouse.PrimaryDevice.UpdateCursor();
// //}
// //var t1 = System.Windows.Input.Mouse.PrimaryDevice.ActiveSource;
// //var t2 = t1 as System.Windows.Interop.HwndSource;
// //var t3 = t2.ChildKeyboardInputSinks.ToArray();
// ////var t2 = t1.GetType().GetProperty("ChildKeyboardInputSinks",
// ////  System.Reflection.BindingFlags.Instance| System.Reflection.BindingFlags.NonPublic);
// //t3[2].KeyboardInputSite = null;
//}
//static void DisableBlockPrecisionTrackpad()
//{
//  Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad", true);
//  RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad", true);
//  key.SetValue("AAPThreshold", "0", RegistryValueKind.DWord);
//}

#if (false)
    static void CopyCoords(INode sour, IBuffer destpp, IBuffer destii, ref float2[] btt)
    {
      float3* app; var anp = sour.GetBufferPtr(CDX.Buffer.POINTBUFFER, (void**)&app) / sizeof(float3);
      ushort* aii; var ani = sour.GetBufferPtr(CDX.Buffer.INDEXBUFFER, (void**)&aii) / sizeof(ushort);
      float2* att; var ant = sour.GetBufferPtr(CDX.Buffer.TEXCOORDS, (void**)&att) / sizeof(float2);
      float3* bpp; var bnp = destpp.GetPtr((void**)&bpp) / sizeof(float3);
      ushort* bii; var bni = destii.GetPtr((void**)&bii) / sizeof(ushort);
      if (btt == null) btt = new float2[bni];

      var aee = new float4[ani / 3];
      int nc = 0; var cc = new int[aee.Length];
      for (int j = 0; j < ani; j += 3)
      {
        var e = float4.PlaneFromPoints(app[aii[j + 0]], app[aii[j + 1]], app[aii[j + 2]]);
        if (nc != 0 && e == aee[nc - 1]) { cc[nc - 1] = j + 3; continue; }
        cc[nc] = j + 3; aee[nc++] = e;
      }
      for (int i = 0, lk = 0; i < bni; i += 3)
      {
        var P1 = bpp[bii[i + 0]];
        var P2 = bpp[bii[i + 1]];
        var P3 = bpp[bii[i + 2]];
        var be = float4.PlaneFromPoints(P1, P2, P3);

        var w = new float3(Math.Abs(be.x), Math.Abs(be.y), Math.Abs(be.z));
        var l = w.x > w.y && w.x > w.z ? 0 : w.y > w.z ? 1 : 2;
        var dir = (l == 0 ? -be.x : l == 1 ? +be.y : -be.z) > 0 ? +1f : -1f;
        var p1 = new float2(l == 0 ? P1.y : P1.x, l == 2 ? P1.y : P1.z);
        var p2 = new float2(l == 0 ? P2.y : P2.x, l == 2 ? P2.y : P2.z);
        var p3 = new float2(l == 0 ? P3.y : P3.x, l == 2 ? P3.y : P3.z);
        var mp = (p1 + p2 + p3) * (1f / 3);
        //lk = 0; //xxx
        for (int t = 0; t < nc; t++)
        {
          var k = (lk + t) % nc;
          if ((aee[k].xyz - be.xyz).LengthSq > 1e-8f || Math.Abs(aee[k].w - be.w) > 1e-4f) continue;
          for (int j = k == 0 ? 0 : cc[k - 1]; j < cc[k]; j += 3)
          {
            var T1 = app[aii[j + 0]];
            var T2 = app[aii[j + 1]];
            var T3 = app[aii[j + 2]]; //var ae = float4.PlaneFromPoints(T1, T2, T3);
            var t1 = new float2(l == 0 ? T1.y : T1.x, l == 2 ? T1.y : T1.z);
            var t2 = new float2(l == 0 ? T2.y : T2.x, l == 2 ? T2.y : T2.z);
            var t3 = new float2(l == 0 ? T3.y : T3.x, l == 2 ? T3.y : T3.z);
            var f1 = t2 - t1 ^ mp - t1; if (f1 * dir > 1e-8) continue;
            var f2 = t3 - t2 ^ mp - t2; if (f2 * dir > 1e-8) continue;
            var f3 = t1 - t3 ^ mp - t3; if (f3 * dir > 1e-8) continue;
            var va = t2 - t1;
            var vb = t3 - t1; var d = va ^ vb; if (d == 0) break; d = 1 / d;
            var ua = p1 - t1;
            var ub = p2 - t1;
            var uc = p3 - t1;
            var c1 = att[j + 0];
            var c2 = (att[j + 1] - c1) * d;
            var c3 = (att[j + 2] - c1) * d;
            var r1 = c1 + c2 * (ua ^ vb) + c3 * (va ^ ua);
            var r2 = c1 + c2 * (ub ^ vb) + c3 * (va ^ ub);
            var r3 = c1 + c2 * (uc ^ vb) + c3 * (va ^ uc);
            btt[i + 0] = r1;
            btt[i + 1] = r2;
            btt[i + 2] = r3;
            lk = k; t = nc; break;
          }
        }
      }
    }
#endif
#if (false)
namespace DisableDevice
{
  
  [Flags()]
  internal enum SetupDiGetClassDevsFlags
  {
    Default = 1,
    Present = 2,
    AllClasses = 4,
    Profile = 8,
    DeviceInterface = (int)0x10
  }

  internal enum DiFunction
  {
    SelectDevice = 1,
    InstallDevice = 2,
    AssignResources = 3,
    Properties = 4,
    Remove = 5,
    FirstTimeSetup = 6,
    FoundDevice = 7,
    SelectClassDrivers = 8,
    ValidateClassDrivers = 9,
    InstallClassDrivers = (int)0xa,
    CalcDiskSpace = (int)0xb,
    DestroyPrivateData = (int)0xc,
    ValidateDriver = (int)0xd,
    Detect = (int)0xf,
    InstallWizard = (int)0x10,
    DestroyWizardData = (int)0x11,
    PropertyChange = (int)0x12,
    EnableClass = (int)0x13,
    DetectVerify = (int)0x14,
    InstallDeviceFiles = (int)0x15,
    UnRemove = (int)0x16,
    SelectBestCompatDrv = (int)0x17,
    AllowInstall = (int)0x18,
    RegisterDevice = (int)0x19,
    NewDeviceWizardPreSelect = (int)0x1a,
    NewDeviceWizardSelect = (int)0x1b,
    NewDeviceWizardPreAnalyze = (int)0x1c,
    NewDeviceWizardPostAnalyze = (int)0x1d,
    NewDeviceWizardFinishInstall = (int)0x1e,
    Unused1 = (int)0x1f,
    InstallInterfaces = (int)0x20,
    DetectCancel = (int)0x21,
    RegisterCoInstallers = (int)0x22,
    AddPropertyPageAdvanced = (int)0x23,
    AddPropertyPageBasic = (int)0x24,
    Reserved1 = (int)0x25,
    Troubleshooter = (int)0x26,
    PowerMessageWake = (int)0x27,
    AddRemotePropertyPageAdvanced = (int)0x28,
    UpdateDriverUI = (int)0x29,
    Reserved2 = (int)0x30
  }

  internal enum StateChangeAction
  {
    Enable = 1,
    Disable = 2,
    PropChange = 3,
    Start = 4,
    Stop = 5
  }

  [Flags()]
  internal enum Scopes
  {
    Global = 1,
    ConfigSpecific = 2,
    ConfigGeneral = 4
  }

  internal enum SetupApiError
  {
    NoAssociatedClass = unchecked((int)0xe0000200),
    ClassMismatch = unchecked((int)0xe0000201),
    DuplicateFound = unchecked((int)0xe0000202),
    NoDriverSelected = unchecked((int)0xe0000203),
    KeyDoesNotExist = unchecked((int)0xe0000204),
    InvalidDevinstName = unchecked((int)0xe0000205),
    InvalidClass = unchecked((int)0xe0000206),
    DevinstAlreadyExists = unchecked((int)0xe0000207),
    DevinfoNotRegistered = unchecked((int)0xe0000208),
    InvalidRegProperty = unchecked((int)0xe0000209),
    NoInf = unchecked((int)0xe000020a),
    NoSuchHDevinst = unchecked((int)0xe000020b),
    CantLoadClassIcon = unchecked((int)0xe000020c),
    InvalidClassInstaller = unchecked((int)0xe000020d),
    DiDoDefault = unchecked((int)0xe000020e),
    DiNoFileCopy = unchecked((int)0xe000020f),
    InvalidHwProfile = unchecked((int)0xe0000210),
    NoDeviceSelected = unchecked((int)0xe0000211),
    DevinfolistLocked = unchecked((int)0xe0000212),
    DevinfodataLocked = unchecked((int)0xe0000213),
    DiBadPath = unchecked((int)0xe0000214),
    NoClassInstallParams = unchecked((int)0xe0000215),
    FileQueueLocked = unchecked((int)0xe0000216),
    BadServiceInstallSect = unchecked((int)0xe0000217),
    NoClassDriverList = unchecked((int)0xe0000218),
    NoAssociatedService = unchecked((int)0xe0000219),
    NoDefaultDeviceInterface = unchecked((int)0xe000021a),
    DeviceInterfaceActive = unchecked((int)0xe000021b),
    DeviceInterfaceRemoved = unchecked((int)0xe000021c),
    BadInterfaceInstallSect = unchecked((int)0xe000021d),
    NoSuchInterfaceClass = unchecked((int)0xe000021e),
    InvalidReferenceString = unchecked((int)0xe000021f),
    InvalidMachineName = unchecked((int)0xe0000220),
    RemoteCommFailure = unchecked((int)0xe0000221),
    MachineUnavailable = unchecked((int)0xe0000222),
    NoConfigMgrServices = unchecked((int)0xe0000223),
    InvalidPropPageProvider = unchecked((int)0xe0000224),
    NoSuchDeviceInterface = unchecked((int)0xe0000225),
    DiPostProcessingRequired = unchecked((int)0xe0000226),
    InvalidCOInstaller = unchecked((int)0xe0000227),
    NoCompatDrivers = unchecked((int)0xe0000228),
    NoDeviceIcon = unchecked((int)0xe0000229),
    InvalidInfLogConfig = unchecked((int)0xe000022a),
    DiDontInstall = unchecked((int)0xe000022b),
    InvalidFilterDriver = unchecked((int)0xe000022c),
    NonWindowsNTDriver = unchecked((int)0xe000022d),
    NonWindowsDriver = unchecked((int)0xe000022e),
    NoCatalogForOemInf = unchecked((int)0xe000022f),
    DevInstallQueueNonNative = unchecked((int)0xe0000230),
    NotDisableable = unchecked((int)0xe0000231),
    CantRemoveDevinst = unchecked((int)0xe0000232),
    InvalidTarget = unchecked((int)0xe0000233),
    DriverNonNative = unchecked((int)0xe0000234),
    InWow64 = unchecked((int)0xe0000235),
    SetSystemRestorePoint = unchecked((int)0xe0000236),
    IncorrectlyCopiedInf = unchecked((int)0xe0000237),
    SceDisabled = unchecked((int)0xe0000238),
    UnknownException = unchecked((int)0xe0000239),
    PnpRegistryError = unchecked((int)0xe000023a),
    RemoteRequestUnsupported = unchecked((int)0xe000023b),
    NotAnInstalledOemInf = unchecked((int)0xe000023c),
    InfInUseByDevices = unchecked((int)0xe000023d),
    DiFunctionObsolete = unchecked((int)0xe000023e),
    NoAuthenticodeCatalog = unchecked((int)0xe000023f),
    AuthenticodeDisallowed = unchecked((int)0xe0000240),
    AuthenticodeTrustedPublisher = unchecked((int)0xe0000241),
    AuthenticodeTrustNotEstablished = unchecked((int)0xe0000242),
    AuthenticodePublisherNotTrusted = unchecked((int)0xe0000243),
    SignatureOSAttributeMismatch = unchecked((int)0xe0000244),
    OnlyValidateViaAuthenticode = unchecked((int)0xe0000245)
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct DeviceInfoData
  {
    public int Size;
    public Guid ClassGuid;
    public int DevInst;
    public IntPtr Reserved;
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct PropertyChangeParameters
  {
    public int Size;
    // part of header. It's flattened out into 1 structure.
    public DiFunction DiFunction;
    public StateChangeAction StateChange;
    public Scopes Scope;
    public int HwProfile;
  }

  internal class NativeMethods
  {

    private const string setupapi = "setupapi.dll";

    private NativeMethods()
    {
    }

    [DllImport(setupapi, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiCallClassInstaller(DiFunction installFunction, SafeDeviceInfoSetHandle deviceInfoSet, [In()]
ref DeviceInfoData deviceInfoData);

    [DllImport(setupapi, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiEnumDeviceInfo(SafeDeviceInfoSetHandle deviceInfoSet, int memberIndex, ref DeviceInfoData deviceInfoData);

    [DllImport(setupapi, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern SafeDeviceInfoSetHandle SetupDiGetClassDevs([In()]
ref Guid classGuid, [MarshalAs(UnmanagedType.LPWStr)]
string enumerator, IntPtr hwndParent, SetupDiGetClassDevsFlags flags);

    /*
    [DllImport(setupapi, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInstanceId(SafeDeviceInfoSetHandle deviceInfoSet, [In()]
ref DeviceInfoData did, [MarshalAs(UnmanagedType.LPTStr)]
StringBuilder deviceInstanceId, int deviceInstanceIdSize, [Out()]
ref int requiredSize);
    */
    [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiGetDeviceInstanceId(
       IntPtr DeviceInfoSet,
       ref DeviceInfoData did,
       [MarshalAs(UnmanagedType.LPTStr)] StringBuilder DeviceInstanceId,
       int DeviceInstanceIdSize,
       out int RequiredSize
    );

    [SuppressUnmanagedCodeSecurity()]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [DllImport(setupapi, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport(setupapi, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetupDiSetClassInstallParams(SafeDeviceInfoSetHandle deviceInfoSet, [In()]
ref DeviceInfoData deviceInfoData, [In()]
ref PropertyChangeParameters classInstallParams, int classInstallParamsSize);

  }

  internal class SafeDeviceInfoSetHandle : SafeHandleZeroOrMinusOneIsInvalid
  {

    public SafeDeviceInfoSetHandle()
        : base(true)
    {
    }

    protected override bool ReleaseHandle()
    {
      return NativeMethods.SetupDiDestroyDeviceInfoList(this.handle);
    }

  }

  public sealed class DeviceHelper
  {

    public void EnableMouse(bool enable) //x64 only :-(
    {
      //HID\FTCS1000&Col02\5&47cc086&0&0001
      //745a17a0-74d3-11d0-b6fe-00a0c90f57da
      var mouseGuid = new Guid("{745a17a0-74d3-11d0-b6fe-00a0c90f57da}");
      var instancePath = @"HID\FTCS1000&Col02\5&47cc086&0&0001";

      //var mouseGuid = new Guid("{4d36e96f-e325-11ce-bfc1-08002be10318}");
      //var instancePath = @"ACPI\PNP0F03\4&3688D3F&0";
      try { DisableDevice.DeviceHelper.SetDeviceEnabled(mouseGuid, instancePath, enable); }
      catch { }
    }

    private DeviceHelper()
    {
    }

    /// <summary>
    /// Enable or disable a device.
    /// </summary>
    /// <param name="classGuid">The class guid of the device. Available in the device manager.</param>
    /// <param name="instanceId">The device instance id of the device. Available in the device manager.</param>
    /// <param name="enable">True to enable, False to disable.</param>
    /// <remarks>Will throw an exception if the device is not Disableable.</remarks>
    public static void SetDeviceEnabled(Guid classGuid, string instanceId, bool enable)
    {
      SafeDeviceInfoSetHandle diSetHandle = null;
      try
      {
        // Get the handle to a device information set for all devices matching classGuid that are present on the 
        // system.
        diSetHandle = NativeMethods.SetupDiGetClassDevs(ref classGuid, null, IntPtr.Zero, SetupDiGetClassDevsFlags.Present);
        // Get the device information data for each matching device.
        DeviceInfoData[] diData = GetDeviceInfoData(diSetHandle);
        // Find the index of our instance. i.e. the touchpad mouse - I have 3 mice attached...
        int index = GetIndexOfInstance(diSetHandle, diData, instanceId);
        // Disable...
        EnableDevice(diSetHandle, diData[index], enable);
      }
      finally
      {
        if (diSetHandle != null)
        {
          if (diSetHandle.IsClosed == false)
          {
            diSetHandle.Close();
          }
          diSetHandle.Dispose();
        }
      }
    }

    private static DeviceInfoData[] GetDeviceInfoData(SafeDeviceInfoSetHandle handle)
    {
      List<DeviceInfoData> data = new List<DeviceInfoData>();
      DeviceInfoData did = new DeviceInfoData();
      int didSize = Marshal.SizeOf(did);
      did.Size = didSize;
      int index = 0;
      while (NativeMethods.SetupDiEnumDeviceInfo(handle, index, ref did))
      {
        data.Add(did);
        index += 1;
        did = new DeviceInfoData();
        did.Size = didSize;
      }
      return data.ToArray();
    }

    // Find the index of the particular DeviceInfoData for the instanceId.
    private static int GetIndexOfInstance(SafeDeviceInfoSetHandle handle, DeviceInfoData[] diData, string instanceId)
    {
      const int ERROR_INSUFFICIENT_BUFFER = 122;
      for (int index = 0; index <= diData.Length - 1; index++)
      {
        StringBuilder sb = new StringBuilder(1);
        int requiredSize = 0;
        bool result = NativeMethods.SetupDiGetDeviceInstanceId(handle.DangerousGetHandle(), ref diData[index], sb, sb.Capacity, out requiredSize);
        if (result == false && Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
        {
          sb.Capacity = requiredSize;
          result = NativeMethods.SetupDiGetDeviceInstanceId(handle.DangerousGetHandle(), ref diData[index], sb, sb.Capacity, out requiredSize);
        }
        if (result == false)
          throw new Win32Exception();
        if (string.Compare(instanceId, sb.ToString(), true) == 0)
        {
          return index;
        }
      }
      // not found
      return -1;
    }

    // enable/disable...
    private static void EnableDevice(SafeDeviceInfoSetHandle handle, DeviceInfoData diData, bool enable)
    {
      PropertyChangeParameters @params = new PropertyChangeParameters();
      // The size is just the size of the header, but we've flattened the structure.
      // The header comprises the first two fields, both integer.
      @params.Size = 8;
      @params.DiFunction = DiFunction.PropertyChange;
      @params.Scope = Scopes.Global;
      if (enable)
      {
        @params.StateChange = StateChangeAction.Enable;
      }
      else
      {
        @params.StateChange = StateChangeAction.Disable;
      }

      bool result = NativeMethods.SetupDiSetClassInstallParams(handle, ref diData, ref @params, Marshal.SizeOf(@params));
      if (result == false) throw new Win32Exception();
      result = NativeMethods.SetupDiCallClassInstaller(DiFunction.PropertyChange, handle, ref diData);
      if (result == false)
      {
        int err = Marshal.GetLastWin32Error(); //0xe0000235 WOW64 call from x86 not possible
        if (err == (int)SetupApiError.NotDisableable)
          throw new ArgumentException("Device can't be disabled (programmatically or in Device Manager).");
        else if (err >= (int)SetupApiError.NoAssociatedClass && err <= (int)SetupApiError.OnlyValidateViaAuthenticode)
          throw new Win32Exception("SetupAPI error: " + ((SetupApiError)err).ToString());
        else
          throw new Win32Exception();
      }
    }
  }
}
#endif
