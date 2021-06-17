using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace cde
{
  static unsafe class fmtfbx
  {
    internal static Node import(string path)
    {
      var root = default(FBXNode);
      using (var st = new FileStream(path, FileMode.Open, FileAccess.Read))
      using (var re = new BinaryReader(st, Encoding.ASCII))
        if (re.ReadInt32() == 0x6479614b) // "Kaydara FBX Binary"
        {
          re.BaseStream.Position = 23; var ver = re.ReadInt32();
          re.BaseStream.Position = 27;
          readfbx(root = new FBXNode(), re, ver, new List<object>());
        }
      if (root == null)
      {
        var ss = File.ReadAllText(path);
        readfbx(root = new FBXNode(), ss, 0, ss.Length, new List<object>());
      }
#if(DEBUG)
      var doc = new XElement("doc");
      fbx2xml(doc, root, new StringBuilder());
      doc.Save(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\fbx.xml");
#endif
      var objects = root["Objects"];
      var conns = root["Connections"];
      var docs = root["Documents"];
      var par = docs != null ? docs["Document"]["RootNode"].props[0] :
        conns != null ? conns[0].props[2] : 0;
      var thenode = new Node();
      build((path, conns, objects, new List<ushort>()), par, thenode);

      var upaxis = 1; //Y
      var p = root.GetProp(0, "GlobalSettings", "Properties70", "UpAxis");
      if (p != null) upaxis = (int)p[4];
      else if ((p = objects.GetProp(0, "GlobalSettings", "Properties60", "UpAxis")) != null) upaxis = (int)p[3];

      if (upaxis == 1) thenode.Transform = double4x3.Rotation(0, Math.PI / 2);

      return thenode;
    }
    [DebuggerDisplay("{name}")]
    class FBXNode : List<FBXNode>
    {
      internal string name;
      internal object[] props;
      internal FBXNode this[string s]
      {
        get { for (int i = 0; i < Count; i++) if (this[i].name == s) return this[i]; return null; }
      }
      internal FBXNode GetNode(string name, int ip, object val)
      {
        for (int i = 0; i < Count; i++)
        {
          var p = this[i];
          if (name == null || p.name == name)
            if (p.props != null && p.props.Length > ip)
              if (p.props[ip].Equals(val))
                return p;
        }
        return null;
      }
      internal object[] GetProp(string name, int ip, object val) => GetNode(name, ip, val)?.props;
      internal object[] GetProp(int ip, params string[] ss)
      {
        var p = this;
        for (int i = 0; i < ss.Length - 1; i++) if ((p = p[ss[i]]) == null) return null;
        return p.GetProp(null, ip, ss[ss.Length - 1]);
      }
    }
    static void readfbx(FBXNode root, BinaryReader reader, int ver, List<object> list)
    {
      for (; ; )
      {
        var endoffs = ver >= 7500 ? (int)reader.ReadInt64() : reader.ReadInt32(); if (endoffs == 0) break;
        var propcnt = ver >= 7500 ? (int)reader.ReadInt64() : reader.ReadInt32();
        var listlen = ver >= 7500 ? (int)reader.ReadInt64() : reader.ReadInt32();
        var namelen = (int)reader.ReadByte(); var ab = reader.BaseStream.Position;
        var name = Encoding.UTF8.GetString(reader.ReadBytes(namelen));
        var node = new FBXNode { name = name }; root.Add(node);
        list.Clear();
        for (int i = 0; i < propcnt; i++)
        {
          var c = (char)reader.ReadByte();
          switch (c)
          {
            case 'I': list.Add(reader.ReadInt32()); continue;
            case 'C': list.Add(reader.ReadByte()); continue;
            case 'Y': list.Add(reader.ReadInt16()); continue;
            case 'L': list.Add(reader.ReadInt64()); continue;
            case 'F': list.Add(reader.ReadSingle()); continue;
            case 'D': list.Add(reader.ReadDouble()); continue;
            case 'R': list.Add(reader.ReadBytes(reader.ReadInt32())); continue;
            case 'S': list.Add(Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())).Replace("\0\u0001", "|")); continue;
            case 'f':
            case 'd':
            case 'i':
            case 'b':
              {
                var len = reader.ReadInt32();
                var enc = reader.ReadInt32();
                var complen = reader.ReadInt32();
                var re = reader; var ep = reader.BaseStream.Position;
                if (enc != 0)
                {
                  if (enc != 1) throw new Exception();
                  reader.BaseStream.Position = ep + 2;
                  re = new BinaryReader(new DeflateStream(reader.BaseStream, CompressionMode.Decompress));
                }
                switch (c)
                {
                  case 'i':
                    {
                      var a = new int[len]; list.Add(a);
                      for (int t = 0; t < len; t++) a[t] = re.ReadInt32(); break;
                    }
                  case 'f':
                    {
                      var a = new float[len]; list.Add(a);
                      for (int t = 0; t < len; t++) a[t] = re.ReadSingle(); break;
                    }
                  case 'd':
                    {
                      var a = new double[len]; list.Add(a);
                      for (int t = 0; t < len; t++) a[t] = re.ReadDouble(); break;
                    }
                  case 'b':
                    {
                      var a = new byte[len]; list.Add(a);
                      for (int t = 0; t < len; t++) a[t] = re.ReadByte(); break;
                    }
                  case 'l':
                    {
                      var a = new long[len]; list.Add(a);
                      for (int t = 0; t < len; t++) a[t] = re.ReadInt64(); break;
                    }
                }
                if (enc != 0) reader.BaseStream.Position = ep + complen;
                continue;
              }
            default: throw new Exception();
          }
        }
        node.props = list.ToArray();
        var x = ab + namelen + listlen;
        if (x < endoffs) { reader.BaseStream.Position = x; readfbx(node, reader, ver, list); }
        reader.BaseStream.Position = endoffs;
      }
    }
    static void readfbx(FBXNode root, string s, int i, int n, List<object> list)
    {
      var nf = NumberFormatInfo.InvariantInfo;
      for (int k, j, c; i < n; i++)
      {
        if (s[i] <= ' ') continue;
        if (s[i] == ';') { for (; s[i] != '\n'; i++) ; continue; }
        for (k = i + 1; s[k] > ' ' && s[k] != ':'; k++) ;
        var name = s.Substring(i, k - i); for (; s[k++] != ':';) ;
        var node = new FBXNode { name = name }; root.Add(node); list.Clear();
        for (; ; )
        {
          for (; s[k] <= ' '; k++) ;
          if (s[k] == '{')
          {
            for (j = ++k, c = 1; ; j++) if (s[j] == '{') c++;
              else if (s[j] == '}' && --c == 0) break;
            //var v = s.Substring(k, j - k); 
            readfbx(node, s, k, j, list);
            i = j; break;
          }
          for (j = k + 1; !(s[j] == ',' || s[j] == '\n' || s[j] == '{'); j++) ;
          var e = j; for (; e > k && s[e - 1] <= ' '; e--) ;
          if (s[k] == '"')
          {
            var o = s.IndexOf("::", k + 1, e - k - 2);
            if (o == -1) list.Add(s.Substring(k + 1, e - k - 2));
            else list.Add(s.Substring(o + 2, e - o - 3) + '|' + s.Substring(k + 1, o - k - 1));
          }
          else if (s[k] == '*')
          {
            var a = k + 1; //for (; s[a] != '{'; a++) ; //var len = int.Parse(s.Substring(k + 1, a - (k + 1)));
            for (; s[a - 1] != ':'; a++) ;
            var b = a; for (; s[b] != '}'; b++) ;
            var su = s.Substring(a, b - a); var uu = su.Split(',');
            if (name == "Vertices" || name == "UV" || su.IndexOf('.') != -1)
            {
              var aa = new double[uu.Length]; list.Add(aa); //NumberFormatInfo.CurrentInfo
              for (int t = 0; t < aa.Length; t++) aa[t] = double.Parse(uu[t], nf);
            }
            else
            {
              var aa = new int[uu.Length]; //list.Add(aa);
              int t = 0; for (; t < aa.Length && int.TryParse(uu[t], out aa[t]); t++) ;
              if (t == aa.Length) list.Add(aa);
              else { var bb = new long[uu.Length]; for (t = 0; t < bb.Length; t++) bb[t] = long.Parse(uu[t], nf); list.Add(bb); }
            }
            node.props = list.ToArray(); i = b; break;
          }
          else
          {
            var v = s.Substring(k, e - k);
            if (char.IsDigit(v[0]) || v[0] == '-' || v[0] == '+')
            {
              if (v.Contains('.')) list.Add(double.Parse(v, nf));
              else
              {
                var l = long.Parse(v, nf);
                if (l == (int)l) list.Add((int)l); else list.Add(l);
              }
            }
            else
            {
              list.Add(v);
            }
          }
          if (s[j] == ',') { k = j + 1; continue; }
          node.props = list.ToArray();
          if (s[j] == '{') { k = j; continue; }
          i = j; break;
        }
      }
    }
    static void build(in (string path, FBXNode conns, FBXNode objs, List<ushort> uus) _, object par, Node root)
    {
      for (int i = 0, k; i < _.conns.Count; i++)
      {
        var ppc = _.conns[i].props; if (!ppc[2].Equals(par)) continue;
        var obj = _.objs.GetNode(null, 0, ppc[1]); if (obj == null) continue;
        var ppo = obj.props;
        var ss = ((string)ppo[par is string ? 0 : 1]).Split('|');
        switch (ss[1])
        {
          case "Model":
            {
              if (ppc[0] as string == "OP") continue;
              var no = new Node { Name = ss[0], Color = 0xffffffff }; root.Add(no);
              var t1 = obj["Properties70"]; var offs = 4; object[] v;
              if (t1 == null) { t1 = obj["Properties60"]; offs = 3; }
              if (t1 != null)
              {
                //GeometricScaling * GeometricRotation * GeometricTranslation *
                //Lcl Scaling * Lcl Rotation * PreRotation * Lcl Translation
                if ((v = t1.GetProp(null, 0, "GeometricScaling")) != null)
                  no.Transform *= double4x3.Scaling(todbl(v[offs + 0]), todbl(v[offs + 1]), todbl(v[offs + 2]));
                if ((v = t1.GetProp(null, 0, "GeometricRotation")) != null)
                  no.Transform *= double4x3.Rotation(0, todbl(v[offs + 0]) * (Math.PI / 180)) * double4x3.Rotation(1, todbl(v[offs + 1]) * (Math.PI / 180)) * double4x3.Rotation(2, todbl(v[offs + 2]) * (Math.PI / 180));
                if ((v = t1.GetProp(null, 0, "GeometricTranslation")) != null)
                  no.Transform *= double4x3.Translation(todbl(v[offs + 0]), todbl(v[offs + 1]), todbl(v[offs + 2]));
                if ((v = t1.GetProp(null, 0, "Lcl Scaling")) != null)
                  no.Transform *= double4x3.Scaling(todbl(v[offs + 0]), todbl(v[offs + 1]), todbl(v[offs + 2]));
                if ((v = t1.GetProp(null, 0, "Lcl Rotation")) != null)
                  no.Transform *= double4x3.Rotation(0, todbl(v[offs + 0]) * (Math.PI / 180)) * double4x3.Rotation(1, todbl(v[offs + 1]) * (Math.PI / 180)) * double4x3.Rotation(2, todbl(v[offs + 2]) * (Math.PI / 180));
                if ((v = t1.GetProp(null, 0, "PreRotation")) != null)
                  no.Transform *= (double4x3.Rotation(0, todbl(v[offs + 0]) * (Math.PI / 180)) * double4x3.Rotation(1, todbl(v[offs + 1]) * (Math.PI / 180)) * double4x3.Rotation(2, todbl(v[offs + 2]) * (Math.PI / 180)));
                if ((v = t1.GetProp(null, 0, "Lcl Translation")) != null)
                  no.Transform *= double4x3.Translation(todbl(v[offs + 0]), todbl(v[offs + 1]), todbl(v[offs + 2]));
              }
              if (offs != 4)
              {
                var t5 = obj["Vertices"];
                if (t5 != null)
                {
                  var t6 = obj["PolygonVertexIndex"];
                  if (t6 != null)
                  {
                    var pv = no.Points = new double3[t5.props.Length / 3];
                    for (int t = 0, s = 0; t < pv.Length; t++, s += 3) pv[t] = new double3(todbl(t5.props[s]), todbl(t5.props[s + 1]), todbl(t5.props[s + 2]));
                    var ii = t6.props; var iii = _.uus; iii.Clear();
                    for (int t = 0, j; t < ii.Length;)
                      for (j = t + 1; ; j++)
                      {
                        iii.Add((ushort)toint(ii[t]));
                        iii.Add((ushort)toint(ii[j])); var l = toint(ii[j + 1]);
                        iii.Add((ushort)(l >= 0 ? l : -l - 1)); if (l < 0) { t = j + 2; break; }
                      }
                    no.Indices = iii.ToArray();
                    var t7 = obj["LayerElementUV"];
                    if (t7 != null)
                    {
                      var aa = t7["UV"].props; var bb = t7["UVIndex"].props;
                      if (bb.Length == ii.Length)
                      {
                        iii.Clear();
                        for (int t = 0, j; t < ii.Length;)
                          for (j = t + 1; ; j++)
                          {
                            iii.Add((ushort)toint(bb[t]));
                            iii.Add((ushort)toint(bb[j]));
                            iii.Add((ushort)toint(bb[j + 1])); if (toint(ii[j + 1]) < 0) { t = j + 2; break; }
                          }
                        var tt = no.Texcoords = new float2[iii.Count];
                        for (int t = 0, x; t < tt.Length; t++)
                          tt[t] = new float2((float)todbl(aa[x = iii[t] << 1]), (float)todbl(aa[x + 1]));
                      }
                    }
                  }
                }
              }
              build(_, ppc[1], no); continue;
            }
          case "Geometry":
            {
              var overt = obj["Vertices"]; //if (overt == null) continue;
              var vv = (double[])overt.props[0];
              var ii = (int[])obj["PolygonVertexIndex"].props[0];
              var pv = root.Points = new double3[vv.Length / 3];
              for (int t = 0, s = 0; t < pv.Length; t++, s += 3) pv[t] = new double3(vv[s], vv[s + 1], vv[s + 2]);
              var iii = _.uus; iii.Clear();
              for (int t = 0, j; t < ii.Length;)
                for (j = t + 1; ; j++)
                {
                  iii.Add((ushort)ii[t]);
                  iii.Add((ushort)ii[j]); var l = ii[j + 1];
                  iii.Add((ushort)(l >= 0 ? l : -l - 1)); if (l < 0) { t = j + 2; break; }
                }
              root.Indices = iii.ToArray();
              var t1 = obj["LayerElementUV"];
              if (t1 != null)
              {
                var aa = (double[])t1["UV"].props[0]; var bb = (int[])t1["UVIndex"].props[0];
                if (bb.Length == ii.Length)
                {
                  iii.Clear();
                  for (int t = 0, j; t < ii.Length;)
                    for (j = t + 1; ; j++)
                    {
                      iii.Add((ushort)bb[t]);
                      iii.Add((ushort)bb[j]);
                      iii.Add((ushort)bb[j + 1]); if (ii[j + 1] < 0) { t = j + 2; break; }
                    }
                  var tt = root.Texcoords = new float2[iii.Count];
                  for (int t = 0, x; t < tt.Length; t++)
                    tt[t] = new float2((float)aa[x = iii[t] << 1], (float)aa[x + 1]);
                }
              }
              //if ((t1 = obj["Properties70"]) != null)
              //{
              //  var t2 = t1.GetProp(null, 0, "Color");
              //  if (t2 != null) root.Color = 0xff000000 |
              //          (((uint)(todbl(t2[6]) * 0xff) & 0xff) << 16) |
              //          (((uint)(todbl(t2[5]) * 0xff) & 0xff) << 8) |
              //          (((uint)(todbl(t2[4]) * 0xff) & 0xff));
              //}
              continue;
            }
          case "Material":
            {
              //if (root.Color == 0xffffffff)
              {
                var t1 = obj["Properties70"];
                if (t1 != null)
                {
                  var t2 = t1.GetProp(null, 0, "DiffuseColor");// "MaterialDiffuse");
                  if (t2 != null)
                    root.Color = 0xff000000 |
                       (((uint)(todbl(t2[6]) * 0xff) & 0xff) << 16) |
                       (((uint)(todbl(t2[5]) * 0xff) & 0xff) << 8) |
                       (((uint)(todbl(t2[4]) * 0xff) & 0xff));
                  else { }
                }
                else if ((t1 = obj["Properties60"]) != null)
                {
                  var t2 = t1.GetProp(null, 0, "DiffuseColor");
                  if (t2 != null)
                    root.Color = 0xff000000 |
                      (((uint)(todbl(t2[5]) * 0xff) & 0xff) << 16) |
                      (((uint)(todbl(t2[4]) * 0xff) & 0xff) << 8) |
                      (((uint)(todbl(t2[3]) * 0xff) & 0xff));
                  else { }
                }
              }
              build(_, ppc[1], root); continue;
            }
          case "LayeredTexture":
            {
              build(_, ppc[1], root); continue;
            }
          case "Texture":
            {
              if (root.Texture != null) continue;
              try
              {
                var t1 = obj["RelativeFilename"].props;
                if (t1[0] is byte[] a) { root.Texture = a; continue; }
                var s1 = (string)t1[0];
                var s2 = !Path.IsPathRooted(s1) ? Path.Combine(Path.GetDirectoryName(_.path), s1) : null;
                if (s2 == null || !File.Exists(s2)) s2 = Directory.EnumerateFiles(Path.GetDirectoryName(_.path), Path.GetFileName(s1), SearchOption.AllDirectories).FirstOrDefault();
                if (s2 == null) continue;
                var tex = File.ReadAllBytes(s2);
                if (s2.EndsWith(".tga", true, null)) tex = fmttga.tga2png(tex);
                t1[0] = root.Texture = tex;
              }
              catch { }
              continue;
            }
          case "Deformer": continue;
          case "Constraint": continue;
          case "NodeAttribute": continue;
          case "AnimCurveNode": continue;
          default: continue;
        }
      }

    }
    static double todbl(object p)
    {
      if (p is double d) return d;
      if (p is int i) return i;
      throw new Exception();
    }
    static double toint(object p)
    {
      if (p is int d) return d;
      throw new Exception();
    }
#if (DEBUG)
    static void fbx2xml(XElement doc, FBXNode node, StringBuilder sb)
    {
      for (int i = 0; i < node.Count; i++)
      {
        var no = node[i]; var e = new XElement(XmlConvert.EncodeName(no.name)); doc.Add(e);
        var a = no.props; sb.Clear();
        for (int t = 0, n = a != null ? a.Length : 0; t < n; t++)
        {
          var p = a[t]; if (t != 0) sb.Append(',');
          { if (p is double v) { sb.Append(XmlConvert.ToString(v)); continue; } }
          { if (p is int v) { sb.Append(XmlConvert.ToString(v)); continue; } }
          { if (p is string v) { sb.Append(v); continue; } }
          { if (p is float v) { sb.Append(XmlConvert.ToString(v)); continue; } }
          { if (p is short v) { sb.Append(XmlConvert.ToString(v)); continue; } }
          { if (p is byte v) { sb.Append(XmlConvert.ToString(v)); continue; } }
          { if (p is long v) { sb.Append(XmlConvert.ToString(v)); continue; } }
          //{ if (p is byte[] v) { for (int x = 0; x < v.Length; x++) sb.AppendFormat("{0:x2}", v[x]); continue; } }
          { if (p is double[] v) { for (int x = 0; x < v.Length; x++) { if (x != 0) sb.Append(','); sb.Append(XmlConvert.ToString(v[x])); } continue; } }
          { if (p is int[] v) { for (int x = 0; x < v.Length; x++) { if (x != 0) sb.Append(','); sb.Append(XmlConvert.ToString(v[x])); } continue; } }
          { if (p is float[] v) { for (int x = 0; x < v.Length; x++) { if (x != 0) sb.Append(','); sb.Append(XmlConvert.ToString(v[x])); } continue; } }
          { if (p is byte[] v) { for (int x = 0; x < v.Length; x++) { if (x != 0) sb.Append(','); sb.Append(XmlConvert.ToString(v[x])); } continue; } }
          { if (p is long[] v) { for (int x = 0; x < v.Length; x++) { if (x != 0) sb.Append(','); sb.Append(XmlConvert.ToString(v[x])); } continue; } }
          { }
        }
        if (a != null && a.Length != 0) e.SetAttributeValue("p", sb.ToString());
        fbx2xml(e, no, sb);
      }
    }
#endif
  }
}
