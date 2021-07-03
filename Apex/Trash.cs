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

#if (false)
  unsafe class MiniStream
  {
    byte[] pp; int ip, np;

    internal MiniStream(int cap) { pp = new byte[cap]; }
    internal int Length { get => np; }
    internal int Position { get => ip; set => ip = value; }
    internal void Read(void* p, int n)
    {
      if (ip + n > np) throw new Exception();
      fixed (byte* t = pp) Native.memcpy(p, t + ip, (void*)n); ip += n;
      //for (int i = 0; i < n; i++) ((byte*)p)[i] = pp[ip++];
    }
    internal void Write(void* p, int n)
    {
      while (ip + n > pp.Length) Array.Resize(ref pp, pp.Length << 1);
      fixed (byte* t = pp) Native.memcpy(t + ip, p, (void*)n); ip += n; np = ip;
      //for (int i = 0; i < n; i++) pp[ip++] = ((byte*)p)[i]; np = ip;
    }
    internal int ReadCount()
    {
      int i = 0; for (int s = 0; ; s += 7) { int b = pp[ip++]; i |= (b & 0x7F) << s; if ((b & 0x80) == 0) break; }
      return i;
    }
    internal void WriteCount(int c)
    {
      if (ip + 5 > pp.Length) Array.Resize(ref pp, pp.Length << 1);
      for (; c >= 0x80; pp[ip++] = (byte)(c | 0x80), c >>= 7) ; pp[ip++] = (byte)c; np = ip;
    }
    internal void WriteString(string s)
    {
      WriteCount(s.Length); 
      for (int i = 0; i < s.Length; i++) WriteCount(s[i]);
    }
    internal bool ReadString(string s)
    {
      var n = ReadCount(); var ok = n == s.Length;
      for (int i = 0, k = 0; i < n; i++) { var c = ReadCount(); if (ok && c != s[k++]) ok = false; }
      return ok;
    }
    internal void WriteObject(object value)
    {
      var t = value.GetType();
      if (t.IsArray)
      {
        var a = value as Array; WriteCount(a.Length);
        var e = t.GetElementType();
        if (e.IsValueType)
        {
          var n = Marshal.SizeOf(e); var h = GCHandle.Alloc(a, GCHandleType.Pinned);
          var p = (byte*)h.AddrOfPinnedObject(); Write(p, a.Length * n); h.Free();
        }
        else
        {
          for (int i = 0; i < a.Length; i++) WriteObject(a.GetValue(i));
        }
        return;
      }
      return;
    }
    internal object ReadObject(Type t)
    {
      if (t.IsArray)
      {
        var c = ReadCount(); var e = t.GetElementType();
        var a = Array.CreateInstance(e, c);
        if (e.IsValueType)
        {
          var n = Marshal.SizeOf(e); var h = GCHandle.Alloc(a, GCHandleType.Pinned);
          var p = (byte*)h.AddrOfPinnedObject(); Read(p, a.Length * n); h.Free();
        }
        else
        {
          for (int i = 0; i < c; i++) a.SetValue(ReadObject(e), i);
        }
        return a;
      }
      return null;
    }
  }

#endif
#if (false)
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
