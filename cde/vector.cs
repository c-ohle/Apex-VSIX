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
  }

  public struct double3box
  {
    public double3 min, max;
    public double3 size => max - min;
    public double3 center => (max + min) * 0.5;
  }

  public struct double3x4
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
      return p is double3x4 && !((double3x4)p != this);
    }
    public static double3x4 Identity { get { return Scaling(1); } }
    public static bool operator ==(in double3x4 a, in double3x4 b)
    {
      return !(a != b);
    }
    public static bool operator !=(in double3x4 a, in double3x4 b)
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
    public static double3x4 operator !(in double3x4 p)
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
      double3x4 r;
      r._11 = +d1 * de; r._12 = -d5 * de; r._13 = +a3 * de;
      r._21 = -d2 * de; r._22 = +d6 * de; r._23 = -a1 * de;
      r._31 = +d3 * de; r._32 = -d7 * de; r._33 = +a0 * de;
      r._41 = -d4 * de; r._42 = +d8 * de; r._43 = -d9 * de;
      return r;
    }
    public static double3x4 operator *(in double3x4 a, double v)
    {
      double3x4 b;
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
    public static double3x4 operator *(in double3x4 a, in double3x4 b)
    {
      double x = a._11, y = a._12, z = a._13; double3x4 r;
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
    public static double3 operator *(in double3 a, in double3x4 b)
    {
      double3 c;
      c.x = b._11 * a.x + b._21 * a.y + b._31 * a.z + b._41;
      c.y = b._12 * a.x + b._22 * a.y + b._32 * a.z + b._42;
      c.z = b._13 * a.x + b._23 * a.y + b._33 * a.z + b._43;
      return c;
    }
    public static double3x4 Translation(double x, double y, double z)
    {
      return Translation(new double3(x, y, z));
    }
    public static double3x4 Translation(in double3 p)
    {
      return new double3x4 { _11 = 1, _22 = 1, _33 = 1, _41 = p.x, _42 = p.y, _43 = p.z };
    }
    public static double3x4 Scaling(double s)
    {
      return new double3x4 { _11 = s, _22 = s, _33 = s };
    }
    public static double3x4 Scaling(double x, double y, double z)
    {
      return new double3x4 { _11 = x, _22 = y, _33 = z };
    }
    public static double3x4 Rotation(int xyz, double a)
    {
      var m = new double3x4();
      var sc = new double2(a);
      switch (xyz)
      {
        case 0: m._11 = 1; m._22 = m._33 = sc.x; m._32 = -(m._23 = sc.y); break;
        case 1: m._22 = 1; m._11 = m._33 = sc.x; m._13 = -(m._31 = sc.y); break;
        case 2: m._33 = 1; m._11 = m._22 = sc.x; m._21 = -(m._12 = sc.y); break;
      }
      return m;
    }
    public static double3x4 Rotation(in double4 q)
    {
      var lq = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
      var ss = Math.Abs(lq) < 1e-12 ? 1.0 : 2.0 / lq;
      var xs = q.x * ss; var ys = q.y * ss; var zs = q.z * ss;
      var wx = q.w * xs; var wy = q.w * ys; var wz = q.w * zs;
      var xx = q.x * xs; var xy = q.x * ys; var xz = q.x * zs;
      var yy = q.y * ys; var yz = q.y * zs; var zz = q.z * zs;
      double3x4 m;
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
    public static double3x4 Rotation(in double3 axis, double angle)
    {
      var v = new double2(angle);
      var a = axis.x * axis.x;
      var b = axis.y * axis.y;
      var c = axis.z * axis.z;
      var d = axis.x * axis.y;
      var e = axis.x * axis.z;
      var f = axis.y * axis.z;
      double3x4 m;
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

}

