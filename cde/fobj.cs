using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cde
{
  static unsafe class fmtobj
  {
    internal static Node import(string path)
    {
      var sss = ((str)File.ReadAllText(path)).Trim();
      var dict = new Dictionary<string, (uint, string)>(); //Texture
      var points = new List<double3>(); var texpts = new List<float2>(); var gr = string.Empty; var mt = string.Empty;
      var indice = new List<int>(); var ppts = new List<float2>(); var ptl = new Dictionary<double3, int>(); //bool bnorm = false; int inorm = 0; var norm = new double3();
      //var root = new Node { Transform = double3x4.Identity }; 
      var root = new Node { Transform = double4x3.Rotation(0, Math.PI / 2) }; //studio max default orient
      for (var s = sss; s;)
      {
        var l = s.Split('\n'); if (l[0] == '#') continue;
        var n = l.Split(' '); var h = n.GetHashCode();
        switch (h)
        {
          default: continue;
          case 0x02a0032c: //mtllib
            {
              var lib = l.ToString(); if (!Path.IsPathRooted(lib)) lib = Path.Combine(Path.GetDirectoryName(path), lib); if (!File.Exists(lib)) continue;
              var id = string.Empty; uint co = 0; string tex = null; //Texture
              for (var ss = ((str)File.ReadAllText(lib)).Trim(); ss;)
              {
                var ll = ss.Split('\n'); if (ll[0] == '#') continue;
                var nn = ll.Split(' '); var hh = nn.GetHashCode();
                switch (hh)
                {
                  case 0x029f83b3://newmtl
                    id = ll.ToString(); co = 0xff808080; tex = null; continue;
                  case 0x000005d3://Kd
                    for (int x = 2; x >= 0; x--) ((byte*)&co)[x] = (byte)(ll.Split(' ').ToDouble() * 0xff); dict[id] = (co, tex); continue;
                  case 0x00000064://d
                    ((byte*)&co)[3] = (byte)(ll.ToDouble() * 0xff); dict[id] = (co, tex); continue;
                  case 0x0297ea64: //map_Kd
                    var te = ll.ToString();
                    if (!Path.IsPathRooted(te)) te = Path.Combine(Path.GetDirectoryName(lib), te);
                    if (!File.Exists(te))
                    {
                      te = Directory.EnumerateFiles(Path.GetDirectoryName(lib), Path.GetFileName(te), SearchOption.AllDirectories).FirstOrDefault();
                      if (te == null) continue;
                    }
                    dict[id] = (co, tex = te); continue;
                }
              }
            }
            continue;
          case 0x00000076: //v
            points.Add(new double3(l.Split(' ').ToDouble(), l.Split(' ').ToDouble(), l.Split(' ').ToDouble())); continue;
          case 0x00000672: //vt
            texpts.Add(new float2((float)l.Split(' ').ToDouble(), (float)l.Split(' ').ToDouble())); continue;
          case 0x0000066c: //vn
            //if (!bnorm) { bnorm = true; norm = new double3(l.Split(' ').ToDouble(), l.Split(' ').ToDouble(), l.Split(' ').ToDouble()); }
            continue;
          case 0x0000006f: //o
            emit(); gr = l.ToString();
            continue;
          case 0x00000067: //g
            emit(); gr = l.ToString();
            continue;
          case 0x02ccabb2: //usemtl
            emit(); mt = l.ToString();// if (!dict.TryGetValue(mt, out var mat)) { }
            continue;
          case 0x00000073: //s
            continue;
          case 0x00000066: //f
            {
              //if (indice.Count >= 100000) emit();// continue;
              var ab = indice.Count;
              while (l)
              {
                var i = l.Split(' '); var p = i.Split('/'); indice.Add((int)p.ToDouble() - 1);
                if (p = i.Split('/')) ppts.Add(texpts[(int)p.ToDouble() - 1]);
                //if (bnorm && i && (int)i.ToDouble() == 1) { bnorm = false; inorm = ab + 1; }
              }
              for (int t = indice.Count - 1; t > ab + 2; t--)
              {
                indice.Insert(t, indice[t - 1]); indice.Insert(t, indice[ab]);
                if (ppts.Count != 0) { ppts.Insert(t, ppts[t - 1]); ppts.Insert(t, ppts[ab]); }
              }
            }
            continue;
        }
      }
      emit();
      void emit()
      {
        //for (int i = 0; i < indice.Count; i += 3)
        //{
        //  var a = points[indice[i + 0]]; var b = points[indice[i + 1]]; var c = points[indice[i + 2]];
        //  var v = double3.Ccw(b - a, c - a); var l = v.LengthSq; if (l < 1e-6) { indice.RemoveRange(i, 3); i -= 3; continue; }
        //  //if (inorm == 0 || i != inorm - 1) continue; inorm = 0;
        //  //v = double3.Normalize(v); var n = double3.Normalize(norm); var g = double3.Dot(v,n); var d = (v - n).LengthSq; if (d < 1) continue;
        //  //for (int k = 0; k < indice.Count; k += 3) { var t = indice[k + 1]; indice[k + 1] = indice[k + 2]; indice[k + 2] = t; }
        //}
        if (indice.Count == 0) return;
        for (int i = 0; i < indice.Count; i++) { var p = points[indice[i]]; if (!ptl.TryGetValue(p, out var x)) ptl.Add(p, x = ptl.Count); indice[i] = x; }
        if (!dict.TryGetValue(mt, out var mat)) mat.Item1 = 0xff808080;
        var em = new Node { Name = gr, Color = mat.Item1, Points = ptl.Keys.ToArray(), Indices = indice.Select(p => (ushort)p).ToArray() };
        if (mat.Item2 != null) try { var a = File.ReadAllBytes(mat.Item2); em.Textures = new[] { (mat.Item2, a) }; } catch (Exception e) { Debug.WriteLine(e.Message); }
        if (ppts.Count != 0) em.Texcoords = ppts.ToArray();
        indice.Clear(); ppts.Clear(); ptl.Clear(); //bnorm = false; inorm = 0;
        if (ptl.Count < 0xffff) root.Add(em); else { }
      }
      return root;
    }
    internal static void export(Node node, string path)
    {
      var sw = new StringWriter(); var nf = CultureInfo.InvariantCulture.NumberFormat; int ab = 1, tb = 1, mn = 1;
      var mtl = Path.ChangeExtension(path, "mtl"); var list = new List<byte[]>();
      sw.Write("mtllib"); sw.Write(' '); /*sw.Write(".\\");*/ sw.WriteLine(Path.GetFileName(mtl));
      var lib = new StringWriter(); var ppd = new Dictionary<double3, int>(); var ttd = new Dictionary<float2, int>();

      foreach (var g in node.Descendants(false))
      {
        if (g.Points == null) continue;
        sw.Write("g"); sw.Write(' '); sw.WriteLine(g.Name);

        sw.Write("usemtl"); sw.Write(' '); sw.Write('M'); sw.WriteLine(mn);
        lib.Write("newmtl"); lib.Write(' '); lib.Write('M'); lib.WriteLine(mn++);

        var c = g.Color; //var sss = $"Ka {0.2f:0.000000}";
        lib.Write("Ka"); lib.Write(' '); lib.Write((0.2f).ToString("0.000000", nf)); lib.Write(' '); lib.Write((0.2f).ToString("0.000000", nf)); lib.Write(' '); lib.WriteLine((0.2f).ToString("0.000000", nf));
        lib.Write("Kd"); lib.Write(' '); lib.Write((((c >> 16) & 0xff) * (1f / 255)).ToString("0.000000", nf)); lib.Write(' '); lib.Write((((c >> 8) & 0xff) * (1f / 255)).ToString("0.000000", nf)); lib.Write(' '); lib.WriteLine(((c & 0xff) * (1f / 255)).ToString("0.000000", nf));
        lib.Write("Ks"); lib.Write(' '); lib.Write((0.0f).ToString("0.000000", nf)); lib.Write(' '); lib.Write((0.0f).ToString("0.000000", nf)); lib.Write(' '); lib.WriteLine((0.0f).ToString("0.000000", nf));
        lib.Write("Ns"); lib.Write(' '); lib.WriteLine((0.0f).ToString("0.000000", nf));
        var tex = g.Textures != null ? g.Textures[0].bin : null;
        if (tex != null)
        {
          int i = 0; for (; i < list.Count && !Native.Equals(list[i], tex); i++) ;
          var t = $"{mtl}{i + 1}.png";
          if (i == list.Count) { list.Add(tex); using (var bmp = Image.FromStream(new MemoryStream(tex))) bmp.Save(t, System.Drawing.Imaging.ImageFormat.Png); }
          lib.Write("map_Kd"); lib.Write(' '); lib.Write(".\\"); lib.WriteLine(Path.GetFileName(t));
        }
        lib.WriteLine();

        var m = g.GetTransform(node) * double4x3.Rotation(0, -Math.PI / 2); //studio max default orient
        var pp = g.Points.Select(p => p * m).ToArray();
        var ii = g.Indices;
        var tt = g.Texcoords;
        var ik = ii.Select(i => { if (!ppd.TryGetValue(pp[i], out int x)) ppd.Add(pp[i], x = ppd.Count); return x; }).ToArray();
        var tk = tt?.Select(t => { if (!ttd.TryGetValue(t, out int x)) ttd.Add(t, x = ttd.Count); return x; }).ToArray();
        foreach (var p in ppd.Keys)
        {
          sw.Write('v'); sw.Write(' ');
          sw.Write(p.x.ToString("R", nf)); sw.Write(' ');
          sw.Write(p.y.ToString("R", nf)); sw.Write(' ');
          sw.WriteLine(p.z.ToString("R", nf));
        }
        foreach (var p in ttd.Keys)
        {
          sw.Write("vt"); sw.Write(' ');
          sw.Write(p.x.ToString("R", nf)); sw.Write(' ');
          sw.Write(p.y.ToString("R", nf)); sw.WriteLine();
        }
        for (int i = 0; i < ii.Length; i += 3)
        {
          sw.Write('f'); sw.Write(' ');
          sw.Write(ab + ik[i + 0]); if (tt != null) { sw.Write('/'); sw.Write(tb + tk[i + 0]); }
          sw.Write(' ');
          sw.Write(ab + ik[i + 1]); if (tt != null) { sw.Write('/'); sw.Write(tb + tk[i + 1]); }
          sw.Write(' ');
          sw.Write(ab + ik[i + 2]); if (tt != null) { sw.Write('/'); sw.Write(tb + tk[i + 2]); }
          sw.WriteLine(' ');
        }
        ab += ppd.Count; tb += ttd.Count; ppd.Clear(); ttd.Clear();
      }
      var ss = sw.ToString(); File.WriteAllText(path, ss, System.Text.Encoding.ASCII);
      var ms = lib.ToString(); File.WriteAllText(mtl, ms, System.Text.Encoding.ASCII);
    }
  }

  //[DebuggerDisplay("{ToString()}")]
  struct str
  {
    string s; int i, n;
    public override int GetHashCode()
    {
      int c = 0; for (int k = 0; k < n; k++) c = unchecked(c * 13 + (s[i + k] | 0x20)); return c;
    }
    public override bool Equals(object obj)
    {
      return obj is str s ? this == s : false;// for Exchange tests
    }
    public override string ToString() => n != 0 ? s.Substring(i, n) : string.Empty;
    public int Start => i;
    public int End => i + n;
    public int Length => n;
    public char this[int x] => (uint)x < (uint)n ? s[i + x] : '\0';
    public static implicit operator str(string s) => new str { s = s, i = 0, n = s != null ? s.Length : 0 };
    public static implicit operator bool(in str s) => s.n != 0;
    public static bool operator ==(in str a, in str b) { return a.n == b.n && string.Compare(a.s, a.i, b.s, b.i, a.n) == 0; }
    public static bool operator !=(str a, in str b) { return !(a == b); }
    public str Trim()
    {
      var t = this;
      for (; t.n != 0 && s[t.i] <= ' '; t.i++, t.n--) ;
      for (; t.n != 0 && s[t.i + t.n - 1] <= ' '; t.n--) ; return t;
    }
    public int IndexOf(char c)
    {
      var x = s.IndexOf(c, i, n); return x != -1 ? x - i : x;
    }
    public str Substring(int i, int n = -1)
    {
      var t = this; t.i += i; t.n = n != -1 ? n : t.n - i; return t;
    }
    public str Split(char c)
    {
      if (c == ' ') { int t = 0; for (; t < n && s[i + t] > ' '; t++) ; return Split(t, 0); }
      var x = IndexOf(c); return Split(x != -1 ? x : n, x != -1 ? 1 : 0);
    }
    public str Split(int x, int c)
    {
      var l = Substring(0, x).Trim(); this = Substring(x + c, n - x - c).Trim(); return l;
    }
    public double ToDouble()
    {
      //return double.Parse(this.ToString(), System.Globalization.CultureInfo.InvariantCulture);
      double a = 0, b = 0, x = 0, e = 1;
      for (int i = n - 1, c; i >= 0; i--)
      {
        if ((c = s[this.i + i]) >= '0' && c <= '9') { a += (c - '0') * e; e *= 10; continue; }
        if (c == '.') { b = e; continue; }
        if (c == '-') { a = -a; continue; }
        if ((c | 0x20) == 'e') { x = a; a = 0; b = 0; e = 1; continue; }
        continue; //???
      }
      if (b != 0) a /= b;
      if (x != 0) a *= Math.Pow(10, x);
      return a;
    }
    public double ToDouble(double def) => n != 0 ? ToDouble() : def;
    public int ToInt() => (int)ToDouble();
  }

}
