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
    public Node() { Transform = double3x4.Identity; }
    public string Name;
    public double3x4 Transform;
    public double3[] Points;
    public ushort[] Indices;
    public byte[] Texture;
    public float2[] Texcoords;
    public uint Color;
    public Node Parent { get; private set; }
    public int IndexCount, StartIndex;
    public object Tag;

    //public void Optimize()
    //{
    //  if (Points == null) return;
    //  var dict = new Dictionary<double3, ushort>(Points.Length);
    //  for (int i = 0; i < Indices.Length; i++)
    //  {
    //    var p = Points[Indices[i]];
    //    if (dict.TryGetValue(p, out var t)) Indices[i] = t;
    //    else dict[p] = Indices[i] = (ushort)dict.Count;
    //  }
    //  Points = dict.Keys.ToArray();
    //  //Indices = dict.Values.ToArray();
    //}

    public void GetBox(in double3x4 m, ref double3box box)
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
      GetBox(double3x4.Identity, ref box); return box;
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
    public double3x4 GetTransform(Node root = null)
    {
      if (root == this) return double3x4.Identity;
      if (root == Parent) return Transform;
      return Transform * Parent.GetTransform(root);
    }
    public Node Clone(int fl = 0)
    {
      var p = new Node { Name = Name, Transform = Transform, Points = Points, Indices = Indices, Texture = Texture, Texcoords = Texcoords, Color = Color, IndexCount = IndexCount, StartIndex = StartIndex };
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
    //public static string ImportFmts => "3MF|*.3mf|3DS|*.3ds|Wavefront obj|*.obj|IFC|*.ifc|BTL|*.btl|BTLX|*.btlx";
    //public static string ExportFmts => "3MF|*.3mf|3DS|*.3ds|Wavefront obj|*.obj|IFC|*.ifc";
    public static Node Import(string path)
    {
      //if (path.EndsWith(".3mf", true, null)) return fmt3mf.import(path);
      if (path.EndsWith(".3ds", true, null)) return fmt3ds.import(path);
      if (path.EndsWith(".obj", true, null)) return fmtobj.import(path);
      //if (path.EndsWith(".ifc", true, null)) return fmtifc.import(path);
      //if (path.EndsWith(".ifczip", true, null)) return fmtifc.import(path);
      //if (path.EndsWith(".ifcxml", true, null)) return fmtifc.import(path);
      //if (path.EndsWith(".btl", true, null)) return fmtbtl.import_btl(path);
      //if (path.EndsWith(".btlx", true, null)) return fmtbtl.import_btlx(path);
      return null;
    }
    public void Export(string path, Func<int, int, Bitmap> preview = null)
    {
      //if (path.EndsWith(".3mf", true, null)) { fmt3mf.export(this, path, preview); return; }
      if (path.EndsWith(".3ds", true, null)) { fmt3ds.export(this, path); return; }
      if (path.EndsWith(".obj", true, null)) { fmtobj.export(this, path); return; }
      //if (path.EndsWith(".ifc", true, null)) { fmtifc.export(this, path); return; }
      //if (path.EndsWith(".btl", true, null)) { fmtbtl.export_btl(this, path); return; }
      //if (path.EndsWith(".btlx", true, null)) { fmtbtl.export_btlx(this, path); return; }
      throw new NotImplementedException();
    }
  }

}
