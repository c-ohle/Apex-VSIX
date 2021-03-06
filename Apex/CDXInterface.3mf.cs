using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using static Apex.CDX;

namespace Apex
{
  public static unsafe partial class CDX
  {
    public static XElement Export3MF(this IScene scene, string path, COM.IStream prev, float3? dragpt, INode actcam)
    {
      var ns = (XNamespace)"http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
      var ms = (XNamespace)"http://schemas.microsoft.com/3dmanufacturing/material/2015/02";
      var ax = (XNamespace)"http://schemas.covyset.com/3mf/2021";
      var doc = new XElement(ns + "model");
      doc.Add(new XAttribute(XNamespace.Xml + "lang", "en-US"));
      doc.Add(new XAttribute(XNamespace.Xmlns + "m", ms.NamespaceName));
      doc.Add(new XAttribute(XNamespace.Xmlns + "x", ax.NamespaceName));
      var unit = scene.Unit; doc.SetAttributeValue("unit", (unit != 0 ? unit : Unit.meter).ToString());
      //doc.Add(new XElement(ns + "metadata", new XAttribute("name", "Title"), "Hello World"));
      if (dragpt.HasValue) doc.SetAttributeValue(ax + "pt", (string)dragpt.Value);
      if (actcam != null)
      {
        var defcam = scene.Camera;
        doc.SetAttributeValue(ax + "ct", (string)defcam.Transform);
        doc.SetAttributeValue(ax + "cf", (string)defcam.GetProp<float4>("@cfov"));
      }
      var resources = new XElement(ns + "resources"); doc.Add(resources);
      var build = new XElement(ns + "build"); doc.Add(build);
      var uid = 1; var textures = new List<(IBuffer str, int id, XElement e)>();
      var basematerials = new XElement(ns + "basematerials"); resources.Add(basematerials);
      var bmid = uid++; basematerials.SetAttributeValue("id", bmid);
      foreach (var group in scene.Nodes()) { if (actcam != null || group.IsSelect) add(group, build); }
      void add(INode group, XElement dest)
      {
        var obj = new XElement(ns + "object"); obj.SetAttributeValue("id", 0);
        if (!string.IsNullOrEmpty(group.Name)) obj.SetAttributeValue("name", group.Name);
        //var desc = group.Nodes().ToArray();
        //var sub = desc.Length != 0 && issubset(desc); //if (sub) { }
        //var mgroup = sub ? desc[0] : group;
        var components = /*!sub && desc.Length != 0*/ group.Child != null ? new XElement(ns + "components") : null;
        if (group.HasBuffer(BUFFER.POINTBUFFER))
        {
          var tag = obj;
          if (components != null)
          {
            tag = new XElement(ns + "object"); var id = uid++; tag.SetAttributeValue("id", id); resources.Add(tag);
            var it = new XElement(ns + "component"); it.SetAttributeValue("objectid", id); components.Add(it);
          }
          tag.SetAttributeValue("type", "model");
          tag.SetAttributeValue("pid", bmid);
          var color = group.Color; var dcolor = $"#{(color << 8) | (color >> 24):X8}";
          var mat = basematerials.Elements().Select((p, i) => (p, i)).FirstOrDefault(p => (string)p.p.Attribute("displaycolor") == dcolor);
          if (mat.p == null)
          {
            mat.i = basematerials.Elements().Count();
            basematerials.Add(mat.p = new XElement(ns + "base"));
            mat.p.SetAttributeValue("name", "Material" + (mat.i + 1));
            mat.p.SetAttributeValue("displaycolor", dcolor);
          }
          tag.SetAttributeValue("pindex", mat.i);

          var mesh = new XElement(ns + "mesh"); tag.Add(mesh);
          var vertices = new XElement(ns + "vertices"); mesh.Add(vertices);
          var triangles = new XElement(ns + "triangles"); mesh.Add(triangles);
          float3* vp; var np = group.GetBufferPtr(BUFFER.POINTBUFFER, (void**)&vp) / sizeof(float3);
          for (int i = 0; i < np; i++)
          {
            var vertex = new XElement(ns + "vertex"); vertices.Add(vertex);
            vertex.SetAttributeValue("x", vp[i].x);
            vertex.SetAttributeValue("y", vp[i].y);
            vertex.SetAttributeValue("z", vp[i].z);
          }
          Range* pr; var nr = group.GetBufferPtr(BUFFER.RANGES, (void**)&pr) / sizeof(Range);
          for (int k = 0, nk = nr != 0 ? nr : 1; k < nk; k++)
          {
            //var tg = sub ? desc[k] : group;
            var texgid = 0; var ucolor = nr != 0 ? pr[k].Color : group.Color; var itex = group.GetBuffer(BUFFER.TEXTURE + k);
            if (itex != null)
            {
              var tex = itex;
              int texid; int i = 0; for (; i < textures.Count && textures[i].str != tex; i++) ;
              if (i != textures.Count) texid = textures[i].id;
              else
              {
                texid = uid++;
                var texture2d = new XElement(ms + "texture2d"); resources.Add(texture2d);
                texture2d.SetAttributeValue("id", texid);
                texture2d.SetAttributeValue("name", itex.Name);
                byte* kbp; itex.GetBufferPtr((void**)&kbp);
                string typ = *kbp == 0x89 ? "png" : "jpeg"; //switch (*kbp) { case 0x89: typ = "png"; break; case 0xff: typ = "jpeg"; break; case 0x47: typ = "gif"; break; default: typ = "bmp"; break; }
                texture2d.SetAttributeValue("path", $"/3D/Textures/{texid}.{typ}");
                texture2d.SetAttributeValue("contenttype", $"image/{typ}");
                textures.Add((tex, texid, texture2d));
              }
              var texture2dgroup = new XElement(ms + "texture2dgroup"); resources.Add(texture2dgroup);
              texture2dgroup.SetAttributeValue("id", texgid = uid++);
              texture2dgroup.SetAttributeValue("texid", texid);
              float2* tt; var nt = group.GetBufferPtr(BUFFER.TEXCOORDS, (void**)&tt) / sizeof(float2);
              for (int j = 0; j < nt; j++)
              {
                var tex2coord = new XElement(ms + "tex2coord"); texture2dgroup.Add(tex2coord);
                tex2coord.SetAttributeValue("u", +tt[j].x);
                tex2coord.SetAttributeValue("v", -tt[j].y);
              }
            }
            ushort* ii; var ni = group.GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&ii) / sizeof(ushort);
            (int First, int Length) range = nr != 0 ? (pr[k].Start, pr[k].Count) : (0, ni);
            for (int i = 0; i < range.Length; i += 3)
            {
              var triangle = new XElement(ns + "triangle"); triangles.Add(triangle);
              for (int t = 0; t < 3; t++)
                triangle.SetAttributeValue(t == 0 ? "v1" : t == 1 ? "v2" : "v3", ii[range.First + i + t]);
              if (texgid == 0) continue;
              triangle.SetAttributeValue("pid", texgid);
              for (int t = 0; t < 3; t++) triangle.SetAttributeValue(t == 0 ? "p1" : t == 1 ? "p2" : "p3", range.First + i + t);
            }
          }
        }
        if (components != null) { obj.Add(components); for (var p = group.Child; p != null; p = p.Next) add(p, components); }
        resources.Add(obj);
        var item = new XElement(ns + (dest.Name.LocalName == "build" ? "item" : "component"));
        var objectid = uid++; obj.SetAttributeValue("id", objectid);
        item.SetAttributeValue("objectid", objectid);
        item.SetAttributeValue("transform", (string)group.Transform);
        dest.Add(item); byte[] bb;
        var fl = (group.IsStatic ? 1 : 0) | (group.IsActive ? 2 : 0); if (fl != 0) obj.SetAttributeValue(ax + "fl", fl);
        //var bb = group.GetBytes(BUFFER.CAMERA);
        //if (bb != null)
        //{
        //  obj.SetAttributeValue(ax + "ca", Convert.ToBase64String(bb));
        //  if (group == actcam) obj.SetAttributeValue(ax + "ct", string.Empty);
        //}
        //if ((bb = group.GetBytes(BUFFER.LIGHT)) != null) obj.SetAttributeValue(ax + "li", Convert.ToBase64String(bb));
        if (group == actcam) obj.SetAttributeValue(ax + "ct", string.Empty);
        if ((bb = group.GetBytes(BUFFER.SCRIPT)) != null)
        {
          obj.SetAttributeValue(ax + "cs", Convert.ToBase64String(bb)); //group.FetchBuffer();
          //if ((bb = group.GetBytes(BUFFER.SCRIPTDATA)) != null) obj.SetAttributeValue(ax + "cd", Convert.ToBase64String(bb));
        }
        if ((bb = group.GetBytes(BUFFER.PROPS)) != null)
          obj.SetAttributeValue(ax + "pr", Convert.ToBase64String(bb));
      };
      if (path == null) return doc;//doc.Save("C:\\Users\\cohle\\Desktop\\test2.xml");
      var memstr = new MemoryStream();
      using (var package = System.IO.Packaging.Package.Open(memstr, FileMode.Create))
      {
        var packdoc = package.CreatePart(new Uri("/3D/3dmodel.model", UriKind.Relative), "application/vnd.ms-package.3dmanufacturing-3dmodel+xml", System.IO.Packaging.CompressionOption.Normal);
        using (var str = packdoc.GetStream()) doc.Save(str);
        package.CreateRelationship(packdoc.Uri, System.IO.Packaging.TargetMode.Internal, "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel", "rel0");
        foreach (var (tex, id, e) in textures)
        {
          var pack = package.CreatePart(new Uri((string)e.Attribute("path"), UriKind.Relative), (string)e.Attribute("contenttype"));
          using (var str = pack.GetStream())
          {
            byte* pp; var nb = tex.GetBufferPtr((void**)&pp);
            new UnmanagedMemoryStream(pp, nb).CopyTo(str);
            //bin.Seek(0); for (int nr; ;) { fixed (byte* p = buff) bin.Read(p, 4096, &nr); str.Write(buff, 0, nr); if (nr < 4096) break; }
          }
          packdoc.CreateRelationship(pack.Uri, System.IO.Packaging.TargetMode.Internal, "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dtexture", "rel" + id);
        }
        if (prev != null)
        {
          var packpng = package.CreatePart(new Uri("/Metadata/thumbnail.png", UriKind.Relative), "image/png");
          using (var str = packpng.GetStream())
          {
            prev.Seek(0); var buff = new byte[4096];
            for (; ; ) { int nr; fixed (byte* p = buff) prev.Read(p, 4096, &nr); str.Write(buff, 0, nr); if (nr < 4096) break; }
          }
          package.CreateRelationship(packpng.Uri, System.IO.Packaging.TargetMode.Internal, "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail", "rel1");
        }
      }
      File.WriteAllBytes(path, memstr.ToArray()); return null;
    }

    public static IScene Import3MF(object data, out float3 dragpt)
    {
      var ms = (XNamespace)"http://schemas.microsoft.com/3dmanufacturing/material/2015/02";
      var ax = (XNamespace)"http://schemas.covyset.com/3mf/2021";
      using (var package = data is string path ?
        System.IO.Packaging.Package.Open(path, FileMode.Open, FileAccess.Read) :
        System.IO.Packaging.Package.Open((Stream)data, FileMode.Open, FileAccess.Read))
      {
        var xml = package.GetPart(new Uri("/3D/3dmodel.model", UriKind.Relative));
        XDocument doc; long len; using (var str = xml.GetStream()) { doc = XDocument.Load(str); len = str.Length; } //560280 doc.Save("C:\\Users\\cohle\\Desktop\\test1.xml");
        var model = doc.Root; var ns = model.Name.Namespace;
        //var apex = model.GetPrefixOfNamespace(ax); if (apex == null) ax = string.Empty;
        var scene = Factory.CreateScene();
        var pt = model.Attribute(ax + "pt");
        dragpt = pt != null ? (float3)pt.Value : float.NaN;
        if ((pt = model.Attribute(ax + "ct")) != null)
        {
          var defcam = Factory.CreateNode(); defcam.Name = "(default)"; defcam.Transform = (float4x3)pt.Value;
          if ((pt = model.Attribute(ax + "cf")) != null) defcam.SetProp("@cfov", (float4)pt.Value);
          else defcam.SetProp("@cfov", new float4(50, 1, 1000, -1));//todo: remove
          scene.Camera = defcam;
        }
        switch ((string)model.Attribute("unit"))
        {
          default: scene.Unit = CDX.Unit.meter; break; //1
          case "centimeter": scene.Unit = CDX.Unit.centimeter; break; //0.01
          case "millimeter": scene.Unit = CDX.Unit.millimeter; break; //0.001
          case "micron": scene.Unit = CDX.Unit.micron; break; //0.000001
          case "foot": scene.Unit = CDX.Unit.foot; break; //0.3048
          case "inch": scene.Unit = CDX.Unit.inch; break; //0.0254
        }
        var res = model.Element(ns + "resources");
        var build = model.Element(ns + "build");
        foreach (var p in build.Elements(ns + "item")) convert(scene.AddNode(null), p);
        void convert(INode node, XElement e)
        {
          var oid = (string)e.Attribute("objectid");
          var obj = res.Elements(ns + "object").First(p => (string)p.Attribute("id") == oid);
          var mesh = obj.Element(ns + "mesh"); obj.AddAnnotation(node);
          node.Name = (string)obj.Attribute("name"); var st = obj.Attribute(ax + "fl");
          if (st != null) { var fl = (int)st; if ((fl & 1) != 0) node.IsStatic = true; if ((fl & 2) != 0) node.IsActive = true; }
          var tra = (string)e.Attribute("transform");
          if (tra != null) node.Transform = (float4x3)tra;
          if (mesh != null)
          {
            var mid = (string)obj.Attribute("materialid"); var color = 0xffffffff;
            if (mid != null) //2013/01
            {
              var mat = res.Elements(ns + "material").First(p => (string)p.Attribute("id") == mid);
              var cid = (string)mat.Attribute("colorid");
              var col = res.Elements(ns + "color").First(p => (string)p.Attribute("id") == cid);
              var sco = (string)col.Attribute("value");
              if (sco[0] == '#') color = 0xff000000 | uint.Parse(sco.Substring(1), System.Globalization.NumberStyles.HexNumber);
            }
            else if ((mid = (string)obj.Attribute("pid")) != null) //core/2015/02
            {
              var mat = res.Elements(ns + "basematerials").First(p => (string)p.Attribute("id") == mid).Elements(ns + "base").ElementAt((int)obj.Attribute("pindex"));
              var sco = (string)mat.Attribute("displaycolor");
              if (sco[0] == '#') color = uint.Parse(sco.Substring(1), System.Globalization.NumberStyles.HexNumber);
              if (sco.Length == 9) color = (color >> 8) | (color << 24); else color |= 0xff000000;
            }
            var vertices = mesh.Element(ns + "vertices").Elements(ns + "vertex");
            var triangles = mesh.Element(ns + "triangles").Elements(ns + "triangle");
            var kk = triangles.OrderBy(p => p.Attribute("pid")?.Value).ToArray();
            var mm = kk.Select(p => p.Attribute("pid")?.Value).Distinct().ToArray();
            var rr = mm.Length > 1 ? new Range[mm.Length] : null;
            {
              var np = vertices.Count();
              var ni = kk.Count() * 3;
              var pp = Marshal.AllocCoTaskMem(Math.Max(np * sizeof(float3), ni * sizeof(ushort)));
              try
              {
                var ip = 0; var vp = (float3*)pp.ToPointer();
                foreach (var p in vertices.Select(v => new float3((float)v.Attribute("x"), (float)v.Attribute("y"), (float)v.Attribute("z")))) vp[ip++] = p;
                node.SetBufferPtr(BUFFER.POINTBUFFER, (byte*)vp, np * sizeof(float3));
                var vi = (ushort*)vp; ip = 0;
                foreach (var p in kk)
                {
                  vi[ip++] = (ushort)(int)p.Attribute("v1");
                  vi[ip++] = (ushort)(int)p.Attribute("v2");
                  vi[ip++] = (ushort)(int)p.Attribute("v3");
                }
                node.SetBufferPtr(BUFFER.INDEXBUFFER, (byte*)vi, ni * sizeof(ushort));
              }
              finally { Marshal.FreeCoTaskMem(pp); }
            }
            float2[] tt = null;
            for (int i = 0, ab = 0, bis = 1; i < mm.Length; i++, ab = bis)
            {
              var pid = mm[i]; for (; bis < kk.Length && kk[bis - 1].Attribute("pid")?.Value == pid; bis++) ;
              IBuffer tex = null;//ref var ma = ref node.Materials[i];ma.Color = color; ma.IndexCount = bis * 3 - (ma.StartIndex = ab * 3);
              if (pid != null)
              {
                var basematerials = res.Elements(ns + "basematerials").FirstOrDefault(p => (string)p.Attribute("id") == pid);
                if (basematerials != null)
                {
                  var mat = basematerials.Elements(ns + "base").ElementAt((int)kk[ab].Attribute("p1"));
                  var sco = (string)mat.Attribute("displaycolor"); if (sco[0] != '#') continue;
                  var col = uint.Parse(sco.Substring(1), System.Globalization.NumberStyles.HexNumber);
                  if (sco.Length == 9) col = (col >> 8) | (col << 24); else col |= 0xff000000; color = col; goto addmat;
                }
                var texture2dgroup = res.Elements(ms + "texture2dgroup").FirstOrDefault(p => (string)p.Attribute("id") == pid);
                if (texture2dgroup == null) goto addmat;
                var pp = texture2dgroup.Elements(ms + "tex2coord").Select(p => new float2((float)p.Attribute("u"), -(float)p.Attribute("v"))).ToArray();
                if (tt == null) tt = new float2[kk.Length * 3];
                for (int t = ab, x; t < bis; t++)
                {
                  var p1 = kk[t].Attribute("p1"); x = (int)p1; /*   */ if (x < pp.Length) tt[t * 3 + 0] = pp[x];
                  var p2 = kk[t].Attribute("p2"); x = (int)(p2 ?? p1); if (x < pp.Length) tt[t * 3 + 1] = pp[x];
                  var p3 = kk[t].Attribute("p3"); x = (int)(p3 ?? p1); if (x < pp.Length) tt[t * 3 + 2] = pp[x];
                }
                var texid = (string)texture2dgroup.Attribute("texid");
                var texture2d = res.Elements(ms + "texture2d").Where(t => (string)t.Attribute("id") == texid).First();
                tex = texture2d.Annotation<IBuffer>();
                if (tex == null)
                {
                  var texpath = (string)texture2d.Attribute("path");
                  var texpart = package.GetPart(new Uri(texpath, UriKind.Relative));
                  using (var str = texpart.GetStream())
                  {
                    var nss = (int)str.Length;
                    var pss = Marshal.AllocCoTaskMem(nss);
                    try
                    {
                      str.CopyTo(new UnmanagedMemoryStream((byte*)pss.ToPointer(), nss, nss, FileAccess.Write));
                      tex = Factory.GetBuffer(BUFFER.TEXTURE, pss.ToPointer(), nss);
                    }
                    finally { Marshal.FreeCoTaskMem(pss); }
                    texture2d.AddAnnotation(tex);
                    if (string.IsNullOrEmpty(tex.Name))
                    {
                      var s = (string)texture2d.Attribute("name");
                      tex.Name = !string.IsNullOrEmpty(s) ? s : Path.GetFileNameWithoutExtension(texpath);
                    }
                  }
                }
              }
            addmat:
              if (i == 0) node.Color = color;
              if (rr != null)
              {
                ref var r = ref rr[i]; r.Start = ab * 3; r.Count = (bis - ab) * 3;
                r.Color = color;
              }
              if (tex != null && i < 16) node.SetBuffer(BUFFER.TEXTURE + i, tex);
            }
            if (tt != null) node.SetArray(BUFFER.TEXCOORDS, tt);
            if (rr != null) node.SetArray(BUFFER.RANGES, rr);
          }
          var bb = (string)obj.Attribute(ax + "cs");
          if (bb != null) node.SetBytes(BUFFER.SCRIPT, Convert.FromBase64String(bb)); //apex = "x"; //todo: remove, detect old apex docs without x namespace
          if ((bb = (string)obj.Attribute(ax + "pr")) != null) node.SetBytes(BUFFER.PROPS, Convert.FromBase64String(bb));
          if (obj.Attribute(ax + "ct") != null) scene.Tag = node;
          var cmp = obj.Element(ns + "components");
          if (cmp != null) foreach (var p in cmp.Elements(ns + "component")) convert(node.AddNode(null), p);
        };
        /////////////
        if (model.GetPrefixOfNamespace(ax) == null)
        {
          var box = GetBox(scene.Nodes());
          if (!box.IsEmpty)
          {
            //var unit = scene.Unit;
            var size = box.size; var max = Math.Max(size.x, Math.Max(size.y, size.z));
            var m = (float4x3)1;
            if (Math.Abs(box.min.z) != 0) m = new float3(0, 0, -box.min.z);
            if (max < 1) m *= Scaling(100 / max); //paint 3d crap to 100mm
            if (m != 1) foreach (var p in scene.Nodes()) p.Transform *= m;
          }
        }
        return scene;
      }
    }
  }
}
