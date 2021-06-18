using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace cde
{
  [DebuggerDisplay("{Name}")]
  public class Node : List<Node>
  {
    public Node() { Transform = double4x3.Identity; }
    public string Name;
    public double4x3 Transform;
    public double3[] Points;
    public ushort[] Indices;
    public (string path, byte[] bin)[] Textures;
    public float2[] Texcoords;
    public uint Color;
    public struct Range { internal int i, n; internal uint c; }
    public Range[] Ranges;
    public Node Parent { get; private set; }
    public object Tag;
#if (DEBUG)
    internal void CheckMesh()
    {
      var ff = new bool[Points.Length];
      for (int i = 0; i < Indices.Length; i++) ff[Indices[i]] = true;
      var unused = ff.Count(p => !p); if (unused != 0) { }
    }
#endif
    public void MeshCompact()
    {
      var dict = new Dictionary<double3, ushort>(4096);
      ushort[] ii = null, ff = null; double3[] vv = null;
      recurs(this);
      void recurs(Node node)
      {
        for (int i = 0; i < node.Count; i++)
        {
          recurs(node[i]);
          if (node[i].Points == null && node[i].Count == 0) node.RemoveAt(i--);
        }
        if (node.Points == null) return;
        dict.Clear(); var ni = node.Indices.Length;
        if (ii == null || ii.Length < ni) ii = new ushort[((ni >> 12) + 1) << 12];
        for (int i = 0; i < ni; i++)
        {
          var pt = node.Points[node.Indices[i]]; pt = (float3)pt;
          if (dict.TryGetValue(pt, out var x)) ii[i] = x;
          else dict[pt] = ii[i] = (ushort)dict.Count;
        }
        var nii = 0;
        for (int i = 0; i < ni; i += 3)
        {
          if (ii[i] == ii[i + 1] || ii[i + 1] == ii[i + 2] || ii[i + 2] == ii[i]) continue;
          if (nii != i) { ii[nii] = ii[i]; ii[nii + 1] = ii[i + 1]; ii[nii + 2] = ii[i + 2]; }
          nii += 3;
        }
        if (nii == 0) { node.Points = null; node.Indices = null; return; }
        var nvv = dict.Keys.Count;
        if (node.Indices.Length == nii && node.Points.Length == nvv) return;
        if (nii != ni)
        {
          if (ff == null || ff.Length < nii) ff = new ushort[((nii >> 12) + 1) << 12]; else Array.Clear(ff, 0, nvv);
          if (vv == null || vv.Length < nvv) vv = new double3[((nvv >> 10) + 1) << 10];
          dict.Keys.CopyTo(vv, 0);
          for (int i = 0; i < nii; i++) ff[ii[i]] = 1; var t = 0;
          for (int i = 0; i < nvv; i++) if (ff[i] != 0) { if (i != t) vv[t] = vv[i]; ff[i] = (ushort)t++; }
          if (nvv != t) { for (int i = 0; i < nii; i++) ii[i] = ff[ii[i]]; nvv = t; }
          Array.Copy(vv, 0, node.Points = new double3[nvv], 0, nvv);
        }
        else
        {
          dict.Keys.CopyTo(node.Points = new double3[nvv], 0);
        }
        Array.Copy(ii, 0, node.Indices = new ushort[nii], 0, nii);
        if (node.Texcoords != null) { }
      }
    }
    public void GetBox(in double4x3 m, ref double3box box)
    {
      var wm = Transform * m;
      if (Points != null)
        for (int i = 0; i < Points.Length; i++)
        {
          var p = Points[i] * wm;
          box.min.x = Math.Min(box.min.x, p.x); box.min.y = Math.Min(box.min.y, p.y); box.min.z = Math.Min(box.min.z, p.z);
          box.max.x = Math.Max(box.max.x, p.x); box.max.y = Math.Max(box.max.y, p.y); box.max.z = Math.Max(box.max.z, p.z);
        }
      for (int i = 0; i < Count; i++) this[i].GetBox(wm, ref box);
    }
    public double3box GetBox()
    {
      var box = new double3box { min = new double3(+double.MaxValue, +double.MaxValue, +double.MaxValue), max = new double3(-double.MaxValue, -double.MaxValue, -double.MaxValue) };
      GetBox(double4x3.Identity, ref box); return box;
    }
    public new void Add(Node p)
    {
      base.Add(p); p.Parent = this;
    }
    public new void AddRange(IEnumerable<Node> p)
    {
      foreach (var t in p) Add(t);
    }
    public IEnumerable<Node> Descendants(bool andself)
    {
      if (andself) yield return this;
      for (int i = 0; i < Count; i++)
        foreach (var p in this[i].Descendants(true))
          yield return p;
    }
    public double4x3 GetTransform(Node root = null)
    {
      if (root == this) return double4x3.Identity;
      if (root == Parent) return Transform;
      return Transform * Parent.GetTransform(root);
    }
    public Node Clone(int fl = 0)
    {
      var p = new Node { Name = Name, Transform = Transform, Points = Points, Indices = Indices, Textures = Textures, Texcoords = Texcoords, Color = Color };
      if ((fl & 1) != 0 && p.Points != null) p.Points = (double3[])p.Points.Clone();
      if ((fl & 2) != 0 && p.Indices != null) p.Indices = (ushort[])p.Indices.Clone();
      foreach (var t in this) p.Add(t.Clone());
      return p;
    }
    public void Unscale()
    {
      var om = Transform;
      var scale = om[0].Length;
      if (scale != 1)
      {
        om[0] /= scale;
        om[1] /= scale;
        om[2] /= scale; Transform = om;
        if (Points != null) Points = Points.Select(t => t * scale).ToArray();
        foreach (var c in this)
        {
          var cm = c.Transform;
          cm[0] *= scale;
          cm[1] *= scale;
          cm[2] *= scale;
          cm[3] *= scale;
          c.Transform = cm;
        }
      }
      foreach (var c in this) c.Unscale();
    }
    public static Node Import(string path)
    {
      if (path.EndsWith(".3ds", true, null)) return fmt3ds.import(path);
      if (path.EndsWith(".obj", true, null)) return fmtobj.import(path);
      if (path.EndsWith(".fbx", true, null)) return fmtfbx.import(path);
      //if (path.EndsWith(".3mf", true, null)) return fmt3mf.import(path);
      //if (path.EndsWith(".ifc", true, null)) return fmtifc.import(path);
      //if (path.EndsWith(".ifczip", true, null)) return fmtifc.import(path);
      //if (path.EndsWith(".ifcxml", true, null)) return fmtifc.import(path);
      //if (path.EndsWith(".btl", true, null)) return fmtbtl.import_btl(path);
      //if (path.EndsWith(".btlx", true, null)) return fmtbtl.import_btlx(path);
      return null;
    }
    public void Export(string path, Func<int, int, Bitmap> preview = null)
    {
      if (path.EndsWith(".3ds", true, null)) { fmt3ds.export(this, path); return; }
      if (path.EndsWith(".obj", true, null)) { fmtobj.export(this, path); return; }
      //if (path.EndsWith(".3mf", true, null)) { fmt3mf.export(this, path, preview); return; }
      //if (path.EndsWith(".ifc", true, null)) { fmtifc.export(this, path); return; }
      //if (path.EndsWith(".btl", true, null)) { fmtbtl.export_btl(this, path); return; }
      //if (path.EndsWith(".btlx", true, null)) { fmtbtl.export_btlx(this, path); return; }
      throw new NotImplementedException();
    }
  }

}
