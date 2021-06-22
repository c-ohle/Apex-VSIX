using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cde
{
  public struct float2 : IEquatable<float2>
  {
    public float x, y;
    public float2(float x, float y) { this.x = x; this.y = y; }
    public override string ToString() { return $"{x:R}; {y:R}"; }
    public override int GetHashCode()
    {
      var h1 = (uint)x.GetHashCode();
      var h2 = (uint)y.GetHashCode();
      h2 = ((h2 << 7) | (h1 >> 25)) ^ h1;
      h1 = ((h1 << 7) | (h2 >> 25)) ^ h2;
      return (int)h1;
    }
    public bool Equals(float2 v)
    {
      return x == v.x && y == v.y;
    }
    public override bool Equals(object obj)
    {
      return obj is float2 && Equals((float2)obj);
    }
  }

  public struct float3 : IEquatable<float3>
  {
    public float x, y, z;
    public override string ToString()
    {
      return $"{x:R}; {y:R}; {z:R}";
    }
    public static float3 Parse(string s)
    {
      var ss = s.Split(';'); return new float3(float.Parse(ss[0]), float.Parse(ss[1]), float.Parse(ss[2]));
    }
    public override int GetHashCode()
    {
      var h1 = (uint)x.GetHashCode();
      var h2 = (uint)y.GetHashCode();
      var h3 = (uint)z.GetHashCode();
      h2 = ((h2 << 7) | (h3 >> 25)) ^ h3;
      h1 = ((h1 << 7) | (h2 >> 25)) ^ h2;
      return (int)h1;
    }
    public bool Equals(float3 v)
    {
      return x == v.x && y == v.y && z == v.z;
    }
    public override bool Equals(object obj)
    {
      return obj is float3 && Equals((float3)obj);
    }
    public float3(float x, float y, float z)
    {
      this.x = x; this.y = y; this.z = z;
    }
    public static float3 operator -(in float3 v)
    {
      return new float3(-v.x, -v.y, -v.z);
    }
  }

  public struct float4
  {
    public float x, y, z, w;
    public override string ToString()
    {
      return string.Format("{0}; {1}; {2}; {3}", x.ToString("R"), y.ToString("R"), z.ToString("R"), w.ToString("R"));
    }
    //public float4(float x, float y, float z, float w)
    //{
    //  this.x = x; this.y = y; this.z = z; this.w = w;
    //}
    //public static float4 operator *(in float4 v, float f)
    //{
    //  return new float4(v.x * f, v.y * f, v.z * f, v.w * f);
    //}
    //public static float4 operator +(in float4 a, in float4 b)
    //{
    //  return new float4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    //}
  }

  public struct double2 : IEquatable<double2>
  {
    public double x, y;
    public override string ToString() { return $"{x:R}; {y:R}"; }
    public override int GetHashCode()
    {
      var h1 = (uint)x.GetHashCode();
      var h2 = (uint)y.GetHashCode();
      h2 = ((h2 << 7) | (h1 >> 25)) ^ h1;
      h1 = ((h1 << 7) | (h2 >> 25)) ^ h2;
      return (int)h1;
    }
    public bool Equals(double2 v)
    {
      return x == v.x && y == v.y;
    }
    public override bool Equals(object obj)
    {
      return obj is double2 && Equals((double2)obj);
    }
    public double2(double x, double y) { this.x = x; this.y = y; }
    public double2(double a)
    {
      x = Math.Cos(a); if (Math.Abs(x) == 1) { y = 0; return; }
      y = Math.Sin(a); if (Math.Abs(y) == 1) { x = 0; return; }
    }
    public double LengthSq => x * x + y * y;
    public double Length => Math.Sqrt(x * x + y * y);
    public static bool operator ==(in double2 a, in double2 b) { return a.x == b.x && a.y == b.y; }
    public static bool operator !=(in double2 a, in double2 b) { return a.x != b.x || a.y != b.y; }
    public static explicit operator float2(in double2 p)
    {
      float2 t; t.x = (float)p.x; t.y = (float)p.y; return t;
    }
    public static double2 operator -(double2 v) { v.x = -v.x; v.y = -v.y; return v; }
    public static double2 operator ~(double2 v) { double2 b; b.x = -v.y; b.y = v.x; return b; }
    public static double2 operator -(double2 a, double2 b) { a.x = a.x - b.x; a.y = a.y - b.y; return a; }
    public static double2 operator *(double2 v, double f) { v.x *= f; v.y *= f; return v; }
    public static double2 operator /(double2 v, double f) { v.x /= f; v.y /= f; return v; }
  }

  public struct double3 : IEquatable<double3>
  {
    public double x, y, z;
    public double3(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
    public override string ToString()
    {
      return string.Format("{0}; {1}; {2}", x.ToString("R"), y.ToString("R"), z.ToString("R"));
    }
    public override int GetHashCode()
    {
      var h1 = (uint)x.GetHashCode();
      var h2 = (uint)y.GetHashCode();
      var h3 = (uint)z.GetHashCode();
      h2 = ((h2 << 7) | (h3 >> 25)) ^ h3;
      h1 = ((h1 << 7) | (h2 >> 25)) ^ h2;
      return (int)h1;
    }
    public bool Equals(double3 v)
    {
      return x == v.x && y == v.y && z == v.z;
    }
    public override bool Equals(object obj)
    {
      return obj is double3 && Equals((double3)obj);
    }
    public double LengthSq => x * x + y * y + z * z;
    public double Length => Math.Sqrt(LengthSq);
    public static double3 operator -(in double3 v)
    {
      return new double3(-v.x, -v.y, -v.z);
    }
    public static double3 operator +(in double3 a, in double3 b)
    {
      return new double3(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    public static double3 operator -(in double3 a, in double3 b)
    {
      return new double3(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    public static double3 operator *(in double3 v, double f)
    {
      return new double3(v.x * f, v.y * f, v.z * f);
    }
    public static double3 operator /(in double3 v, double f)
    {
      return new double3(v.x / f, v.y / f, v.z / f);
    }
    public static double3 operator ^(in double3 a, in double3 b)
    {
      return new double3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
    }
    public static double operator &(in double3 a, in double3 b)
    {
      return a.x * b.x + a.y * b.y + a.z * b.z;
    }
    public static implicit operator double3(float3 p)
    {
      double3 t; t.x = p.x; t.y = p.y; t.z = p.z; return t;
    }
    public static explicit operator float3(double3 p)
    {
      float3 t; t.x = (float)p.x; t.y = (float)p.y; t.z = (float)p.z; return t;
    }
    public static double3 Normalize(in double3 p) => p / p.Length;
  }

  public struct double4 : IEquatable<double4>
  {
    public double x, y, z, w;
    public double4(double x, double y, double z, double w)
    {
      this.x = x; this.y = y; this.z = z; this.w = w;
    }
    public override string ToString()
    {
      return $"{x:R}; {y:R}; {z:R}; {w:R}";
    }
    public override int GetHashCode()
    {
      var h1 = (uint)x.GetHashCode();
      var h2 = (uint)y.GetHashCode();
      var h3 = (uint)z.GetHashCode();
      var h4 = (uint)w.GetHashCode();
      h2 = ((h2 << 7) | (h3 >> 25)) ^ h3;
      h1 = ((h1 << 7) | (h2 >> 25)) ^ h2 ^ h4;
      return (int)h1;
    }
    public bool Equals(double4 v)
    {
      return x == v.x && y == v.y && z == v.z && w == v.w;
    }
    public override bool Equals(object obj)
    {
      return obj is double4 && Equals((double4)obj);
    }
    public static double4 operator -(in double4 v)
    {
      return new double4(-v.x, -v.y, -v.z, -v.w);
    }
    public static double4 QuatAxisAngel(in double3 axis, double angle)
    {
      var l = axis.Length;
      if (l < 1e-12) return new double4 { w = 1 };
      var omega = -0.5 * angle; var s = Math.Sin(omega) / l;
      return new double4 { x = s * axis.x, y = s * axis.y, z = s * axis.z, w = Math.Cos(omega) };
    }
    public static double4 PlaneFromPoints(in double3 a, in double3 b, in double3 c)
    {
      var e = double3.Normalize(b - a ^ c - a); // if (double.IsNaN(e.x)) { }
      return new double4 { x = e.x, y = e.y, z = e.z, w = -(a.x * e.x + a.y * e.y + a.z * e.z) };
    }
    public static double4 PlaneFromPointNormal(in double3 p, in double3 n)
    {
      return new double4 { x = n.x, y = n.y, z = n.z, w = -(p.x * n.x + p.y * n.y + p.z * n.z) };
    }
    public double Dot(in double3 p)
    {
      return x * p.x + y * p.y + z * p.z + w;
    }
    public static double4 Round(in double4 a, int dec)
    {
      return new double4 { x = Math.Round(a.x, dec), y = Math.Round(a.y, dec), z = Math.Round(a.z, dec), w = Math.Round(a.w, dec) };
    }
    public static double4 operator *(in double4 v, double f)
    {
      return new double4(v.x * f, v.y * f, v.z * f, v.w * f);
    }
    public static double4 operator +(in double4 a, in double4 b)
    {
      return new double4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    }
  }

  public struct double3box
  {
    public double3 min, max;
    public double3 size => max - min;
    public double3 center => (max + min) * 0.5;
  }

  public struct double4x3
  {
    public double _11, _12, _13;
    public double _21, _22, _23;
    public double _31, _32, _33;
    public double _41, _42, _43;
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }
    public override bool Equals(object p)
    {
      return p is double4x3 && !((double4x3)p != this);
    }
    public static double4x3 Identity { get { return Scaling(1); } }
    public static bool operator ==(in double4x3 a, in double4x3 b)
    {
      return !(a != b);
    }
    public static bool operator !=(in double4x3 a, in double4x3 b)
    {
      return a._11 != b._11 || a._12 != b._12 || a._13 != b._13 ||
             a._21 != b._21 || a._22 != b._22 || a._23 != b._23 ||
             a._31 != b._31 || a._32 != b._32 || a._33 != b._33 ||
             a._41 != b._41 || a._42 != b._42 || a._43 != b._43;
    }
    public double GetDeterminant()
    {
      return
        _11 * (_22 * _33 - _23 * _32) -
        _12 * (_21 * _33 - _23 * _31) +
        _13 * (_21 * _32 - _22 * _31);
    }
    public static double4x3 operator !(in double4x3 p)
    {
      var b0 = p._31 * p._42 - p._32 * p._41;
      var b1 = p._31 * p._43 - p._33 * p._41;
      var b3 = p._32 * p._43 - p._33 * p._42;
      var d1 = p._22 * p._33 - p._23 * p._32;
      var d2 = p._21 * p._33 - p._23 * p._31;
      var d3 = p._21 * p._32 - p._22 * p._31;
      var de = p._11 * d1 - p._12 * d2 + p._13 * d3; de = 1.0 / de; //if (det == 0) throw new Exception();
      var a0 = p._11 * p._22 - p._12 * p._21;
      var a1 = p._11 * p._23 - p._13 * p._21;
      var a3 = p._12 * p._23 - p._13 * p._22;
      var d5 = p._12 * p._33 - p._13 * p._32;
      var d6 = p._11 * p._33 - p._13 * p._31;
      var d7 = p._11 * p._32 - p._12 * p._31;
      var d8 = p._11 * b3 + p._12 * -b1 + p._13 * b0;
      var d9 = p._41 * a3 + p._42 * -a1 + p._43 * a0;
      var d4 = p._21 * b3 + p._22 * -b1 + p._23 * b0;
      double4x3 r;
      r._11 = +d1 * de; r._12 = -d5 * de; r._13 = +a3 * de;
      r._21 = -d2 * de; r._22 = +d6 * de; r._23 = -a1 * de;
      r._31 = +d3 * de; r._32 = -d7 * de; r._33 = +a0 * de;
      r._41 = -d4 * de; r._42 = +d8 * de; r._43 = -d9 * de;
      return r;
    }
    public static double4x3 operator *(in double4x3 a, double v)
    {
      double4x3 b;
      b._11 = a._11 * v;
      b._12 = a._12 * v;
      b._13 = a._13 * v;
      b._21 = a._21 * v;
      b._22 = a._22 * v;
      b._23 = a._23 * v;
      b._31 = a._31 * v;
      b._32 = a._32 * v;
      b._33 = a._33 * v;
      b._41 = a._41 * v;
      b._42 = a._42 * v;
      b._43 = a._43 * v;
      return b;
    }
    public static double4x3 operator *(in double4x3 a, in double4x3 b)
    {
      double x = a._11, y = a._12, z = a._13; double4x3 r;
      r._11 = b._11 * x + b._21 * y + b._31 * z;
      r._12 = b._12 * x + b._22 * y + b._32 * z;
      r._13 = b._13 * x + b._23 * y + b._33 * z; x = a._21; y = a._22; z = a._23;
      r._21 = b._11 * x + b._21 * y + b._31 * z;
      r._22 = b._12 * x + b._22 * y + b._32 * z;
      r._23 = b._13 * x + b._23 * y + b._33 * z; x = a._31; y = a._32; z = a._33;
      r._31 = b._11 * x + b._21 * y + b._31 * z;
      r._32 = b._12 * x + b._22 * y + b._32 * z;
      r._33 = b._13 * x + b._23 * y + b._33 * z; x = a._41; y = a._42; z = a._43;
      r._41 = b._11 * x + b._21 * y + b._31 * z + b._41;
      r._42 = b._12 * x + b._22 * y + b._32 * z + b._42;
      r._43 = b._13 * x + b._23 * y + b._33 * z + b._43; return r;
    }
    public static double3 operator *(in double3 a, in double4x3 b)
    {
      double3 c;
      c.x = b._11 * a.x + b._21 * a.y + b._31 * a.z + b._41;
      c.y = b._12 * a.x + b._22 * a.y + b._32 * a.z + b._42;
      c.z = b._13 * a.x + b._23 * a.y + b._33 * a.z + b._43;
      return c;
    }
    public static double4x3 Translation(double x, double y, double z)
    {
      return Translation(new double3(x, y, z));
    }
    public static double4x3 Translation(in double3 p)
    {
      return new double4x3 { _11 = 1, _22 = 1, _33 = 1, _41 = p.x, _42 = p.y, _43 = p.z };
    }
    public static double4x3 Scaling(double s)
    {
      return new double4x3 { _11 = s, _22 = s, _33 = s };
    }
    public static double4x3 Scaling(double x, double y, double z)
    {
      return new double4x3 { _11 = x, _22 = y, _33 = z };
    }
    public static double4x3 Rotation(int xyz, double a)
    {
      var m = new double4x3();
      var sc = new double2(a);
      switch (xyz)
      {
        case 0: m._11 = 1; m._22 = m._33 = sc.x; m._32 = -(m._23 = sc.y); break;
        case 1: m._22 = 1; m._11 = m._33 = sc.x; m._13 = -(m._31 = sc.y); break;
        case 2: m._33 = 1; m._11 = m._22 = sc.x; m._21 = -(m._12 = sc.y); break;
      }
      return m;
    }
    public static double4x3 Rotation(in double4 q)
    {
      var lq = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
      var ss = Math.Abs(lq) < 1e-12 ? 1.0 : 2.0 / lq;
      var xs = q.x * ss; var ys = q.y * ss; var zs = q.z * ss;
      var wx = q.w * xs; var wy = q.w * ys; var wz = q.w * zs;
      var xx = q.x * xs; var xy = q.x * ys; var xz = q.x * zs;
      var yy = q.y * ys; var yz = q.y * zs; var zz = q.z * zs;
      double4x3 m;
      m._11 = 1.0f - (yy + zz);
      m._21 = xy - wz;
      m._31 = xz + wy;
      m._12 = xy + wz;
      m._22 = 1.0f - (xx + zz);
      m._32 = yz - wx;
      m._13 = xz - wy;
      m._23 = yz + wx;
      m._33 = 1.0f - (xx + yy);
      m._41 = m._42 = m._43 = 0.0;
      return m;
    }
    public static double4x3 Rotation(in double3 axis, double angle)
    {
      var v = new double2(angle);
      var a = axis.x * axis.x;
      var b = axis.y * axis.y;
      var c = axis.z * axis.z;
      var d = axis.x * axis.y;
      var e = axis.x * axis.z;
      var f = axis.y * axis.z;
      double4x3 m;
      m._11 = a + v.x * (1f - a);
      m._12 = d - v.x * d + v.y * axis.z;
      m._13 = e - v.x * e - v.y * axis.y;
      m._21 = d - v.x * d - v.y * axis.z;
      m._22 = b + v.x * (1f - b);
      m._23 = f - v.x * f + v.y * axis.x;
      m._31 = e - v.x * e + v.y * axis.y;
      m._32 = f - v.x * f - v.y * axis.x;
      m._33 = c + v.x * (1f - c);
      m._41 = m._42 = m._43 = 0;
      return m;
    }
    public double3 this[int i]
    {
      get
      {
        if (i == 0) return new double3(_11, _12, _13);
        if (i == 1) return new double3(_21, _22, _23);
        if (i == 2) return new double3(_31, _32, _33);
        return new double3(_41, _42, _43);
      }
      set
      {
        if (i == 0) { _11 = value.x; _12 = value.y; _13 = value.z; return; }
        if (i == 1) { _21 = value.x; _22 = value.y; _23 = value.z; return; }
        if (i == 2) { _31 = value.x; _32 = value.y; _33 = value.z; return; }
        _41 = value.x; _42 = value.y; _43 = value.z;
      }
    }
  }
  /*
  static class Mem
  {
    struct WeakRef<T> where T : class
    {
      WeakReference p;
      public T Value
      {
        get { if (p != null && p.Target is T v) return v; return null; }
        set { if (value == null) p = null; else if (p == null) p = new WeakReference(value); else p.Target = value; }
      }
    }
    static class wt<T> { internal static WeakRef<T[]> wr; }
    public static T[] GetBuffer<T>(int minsize, bool clear = true)
    {
      var t = wt<T>.wr.Value;
      if (t == null || t.Length < minsize) t = new T[minsize];
      else if (clear) Array.Clear(t, 0, minsize); return t;
    }
    public static void Release<T>(this T[] a)
    {
      var t = wt<T>.wr.Value;
      if (t == null || t.Length < a.Length) wt<T>.wr.Value = a;
    }
  }
  */

  class NURBS
  {
    int dimx, dimy; int deg_u, deg_v;
    internal double[] knots_u, knots_v, a; double4[] points;
    //Dictionary<double3, ushort> dict; ushort[] kk, tt;
    static int getspan(int degree, double[] knots, double u)
    {
      int n = knots.Length - degree - 2;
      if (u > knots[n + 1] - 1e-6f) return n;
      if (u < knots[degree] + 1e-6f) return degree;
      int low = degree;
      int high = n + 1, mid = (int)Math.Floor((low + high) * 0.5);
      for (; (u < knots[mid]) || (u >= knots[mid + 1]);)
      {
        if (u < knots[mid]) high = mid; else low = mid;
        mid = (int)Math.Floor((low + high) * 0.5);
      }
      return mid;
    }
    static void bspline(double[] a, int o, int deg, int span, double[] knots, double u)
    {
      a[o] = 1; var le = o + deg + 1; var ri = le + deg + 1;
      for (int j = 1; j <= deg; j++)
      {
        a[le + j] = u - knots[span + 1 - j];
        a[ri + j] = knots[span + j] - u;
        var s = 0.0;
        for (int r = 0; r < j; r++)
        {
          var t = a[o + r] / (a[ri + r + 1] + a[le + j - r]);
          a[o + r] = s + a[ri + r + 1] * t;
          s = a[le + j - r] * t;
        }
        a[o + j] = s;
      }
    }
    double3 nurbsPoint(double u, double v)
    {
      var p = new double4();
      int span_u = getspan(deg_u, knots_u, u);
      int span_v = getspan(deg_v, knots_v, v);
      bspline(a, 0, deg_u, span_u, knots_u, u); var o = (deg_u + 1) * 3;
      bspline(a, o, deg_v, span_v, knots_v, v);
      for (int l = 0; l <= deg_v; l++)
      {
        var t = new double4(); var vv = ((span_v - deg_v + l) % dimy) * dimx;
        for (int k = 0; k <= deg_u; k++)
          t += points[vv + (span_u - deg_u + k) % dimx] * a[k];
        p += t * a[o + l];
      }
      return new double3(p.x / p.w, p.y / p.w, p.z / p.w);
    }
    void ensure<T>(ref T[] a, int n) { if (a == null || a.Length < n) a = new T[n]; }
    internal void setup(int dimx, int dimy, int deg_u, int deg_v, double[] knots_u, double[] knots_v, double[] v4)
    {
      this.dimx = dimx; this.dimy = dimy; this.deg_u = deg_u; this.deg_v = deg_v;
      this.knots_u = knots_u; this.knots_v = knots_v;
      ensure(ref a, (deg_u + deg_v + 2) * 3);
      var np = dimx * dimy; ensure(ref points, np);
      for (int t = 0, s = 0; t < np; t++, s += 4)
        points[t] = new double4(
          v4[s + 0] * v4[s + 3],
          v4[s + 1] * v4[s + 3],
          v4[s + 2] * v4[s + 3],
          v4[s + 3]);
    }
    internal void calcsurf(int dx, int dy, out double3[] pp, out ushort[] ii)
    {
      var u1 = knots_u[deg_u]; var u2 = knots_u[knots_u.Length - deg_u - 2 + 1];
      var v1 = knots_v[deg_v]; var v2 = knots_v[knots_v.Length - deg_v - 2 + 1];
      var fu = (u2 - u1) / (dx - 1);
      var fv = (v2 - v1) / (dy - 1);
#if(false)
      if (dict == null) dict = new Dictionary<double3, ushort>(dx * dy); else dict.Clear();
      ensure(ref kk, dx * dy);
      for (int y = 0, i = 0; y < dy; y++)
        for (int x = 0; x < dx; x++, i++)
        {
          var p = nurbsPoint(u1 + x * fu, v1 + y * fv);
          if (!dict.TryGetValue(p, out var k)) dict.Add(p, k = (ushort)dict.Count); kk[i] = k;
        }
      pp = new double3[dict.Count]; dict.Keys.CopyTo(pp, 0); 
      var nx = dx - 1;
      var ny = dy - 1;
      var nt = 0; ensure(ref tt, nx * ny * 6); 
      for (int y1 = 0, t = 0; y1 < ny; y1++)
        for (int x1 = 0, y2 = (y1 + 1); x1 < nx; x1++)
        {
          var x2 = (x1 + 1);
          tt[nt + 0] = kk[y1 * dx + x1];
          tt[nt + 1] = kk[y1 * dx + x2];
          tt[nt + 2] = kk[y2 * dx + x2]; if (tt[nt + 0] != tt[nt + 1] && tt[nt + 1] != tt[nt + 2] && tt[nt + 2] != tt[nt + 0]) nt += 3;
          tt[nt + 0] = kk[y2 * dx + x2];
          tt[nt + 1] = kk[y2 * dx + x1];
          tt[nt + 2] = kk[y1 * dx + x1]; if (tt[nt + 0] != tt[nt + 1] && tt[nt + 1] != tt[nt + 2] && tt[nt + 2] != tt[nt + 0]) nt += 3;
        }
      ii = new ushort[nt];Array.Copy(tt, 0, ii, 0, nt);
#else
      pp = new double3[dx * dy];
      for (int y = 0, i = 0; y < dy; y++)
        for (int x = 0; x < dx; x++, i++)
          pp[i] = nurbsPoint(u1 + x * fu, v1 + y * fv);

      var nx = dx - 1;
      var ny = dy - 1;
      ii = new ushort[nx * ny * 6];
      for (int y1 = 0, t = 0; y1 < ny; y1++)
        for (int x1 = 0, y2 = y1 + 1; x1 < nx; x1++, t += 6)
        {
          var x2 = x1 + 1;
          ii[t + 0] = (ushort)(y1 * dx + x1);
          ii[t + 1] = (ushort)(y1 * dx + x2);
          ii[t + 2] = (ushort)(y2 * dx + x2);
          ii[t + 3] = (ushort)(y2 * dx + x2);
          ii[t + 4] = (ushort)(y2 * dx + x1);
          ii[t + 5] = (ushort)(y1 * dx + x1);
        }
#endif
    }
  }
}

