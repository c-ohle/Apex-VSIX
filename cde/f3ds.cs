using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cde
{
  static unsafe class fmt3ds
  {
    struct chunk
    {
      internal byte* p; internal int n;
      internal int id => *(ushort*)&p[0];
      internal int len => *(int*)&p[2] - 6;
      internal byte* ptr => p + 6;
      internal void next() { var l = *(int*)&p[2]; p += l; n -= l; }
      internal chunk inner(int e = 0) => new chunk { p = p + 6 + e, n = len - e };
    }
    class NodeTag { public int id; public double3x4 meshmat, locmat; }

    internal static Node import(string path)
    {
      var root = new Node { };
      var materials = new List<(string name, uint diffuse, byte[] tex)>(); //Dictionary<Node, double3x4> meshmat = null;
      var a = File.ReadAllBytes(path);
      fixed (byte* p = a)
      {
        chunk chunk; chunk.p = p; chunk.n = a.Length;
        if (chunk.id != 0x4d4d) return null; //M3DMAGIC
        chunk = chunk.inner();
        for (; chunk.n != 0; chunk.next())
          switch (chunk.id)
          {
            default: continue; //???
            case 0x0002: { var ver = *(short*)chunk.ptr; } continue; //M3D_VERSION
            case 0x3d3d: //MDATA
              {
                int meshver = 3; float masterscale = 1;
                for (var mdata = chunk.inner(); mdata.n != 0; mdata.next())
                  switch (mdata.id)
                  {
                    default: continue; //???
                    case 0x3d3e: meshver = *(short*)mdata.ptr; continue; //MESH_VERSION
                    case 0x2100: continue; //AMBIENT_LIGHT
                    case 0xafff: //MAT_ENTRY
                      {
                        string name = null; byte[] tex = null; uint diffuse = 0xff808080;//, difflin = 0xff808080;
                        for (var mate = mdata.inner(); mate.n != 0; mate.next())
                          switch (mate.id)
                          {
                            default: continue; //???
                            case 0xa000: name = new string((sbyte*)mate.ptr); continue; //MAT_NAME
                            case 0xa010: continue; //MAT_AMBIENT
                            case 0xa030: continue; //MAT_SPECULAR
                            case 0xa020: //MAT_DIFFUSE
                              for (var col = mate.inner(); col.n != 0; col.next())
                                switch (col.id)
                                {
                                  default: continue; //???
                                  case 0x0010: //COLOR_F
                                    { var c = (float*)col.ptr; diffuse = (diffuse & 0xff000000) | ((uint)((uint)(c[0] * 255)) << 16) | ((uint)((uint)(c[1] * 255)) << 8) | ((uint)(c[2] * 255)); }
                                    continue;
                                  case 0x0011: //COLOR_24
                                    { var c = col.ptr; diffuse = (diffuse & 0xff000000) | ((uint)c[0] << 16) | ((uint)c[1] << 8) | c[2]; }
                                    continue;
                                  case 0x0012: continue; //LIN_COLOR_24
                                }
                              continue;
                            case 0xa040: continue; //MAT_SHININESS
                            case 0xa041: continue; //MAT_SHIN2PCT
                            case 0xa080: continue; //MAT_SELF_ILLUM
                            case 0xa084: continue; //MAT_SELF_ILPCT
                            case 0xa087: continue; //MAT_WIRESIZE
                            case 0xa08a: continue; //MAT_XPFALLIN
                            case 0xa050: //MAT_TRANSPARENCY
                              for (var col = mate.inner(); col.n != 0; col.next())
                                switch (col.id)
                                {
                                  default: continue; //???
                                  case 0x0030: //INT_PERCENTAGE
                                    ((byte*)&diffuse)[3] = (byte)(0xff - *(short*)col.ptr * 0xff / 100); continue;
                                }
                              continue;
                            case 0xa052: continue; //MAT_XPFALL
                            case 0xa053: continue; //MAT_REFBLUR
                            case 0xa081: continue; //MAT_TWO_SIDE			 
                            case 0xa100: continue; //MAT_SHADING
                            case 0xa200: //MAT_TEXMAP
                              for (var col = mate.inner(); col.n != 0; col.next())
                                switch (col.id)
                                {
                                  default: continue; //??? 
                                  case 0xa300: //MAT_MAPNAME
                                    try
                                    {
                                      var s1 = new string((sbyte*)col.ptr);
                                      var s2 = Path.IsPathRooted(s1) ? s1 : Path.Combine(Path.GetDirectoryName(path), s1);
                                      if (!File.Exists(s2)) s2 = Directory.EnumerateFiles(Path.GetDirectoryName(path), s1, SearchOption.AllDirectories).FirstOrDefault();
                                      if (s2 == null) continue;
                                      tex = File.ReadAllBytes(s2);
                                      if (s2.EndsWith(".tga", true, null)) tex = fmttga.tga2png(tex);
                                    }
                                    catch (Exception e) { Debug.WriteLine(e.Message); }
                                    continue;
                                  case 0x0030: continue; //INT_PERCENTAGE
                                  case 0xa351: continue; //MAT_MAP_TILING short flags;
                                  case 0xa353: continue; //MAT_MAP_TEXBLUR float blurring;
                                  case 0xa354: continue; //MAT_MAP_USCALE
                                  case 0xa356: continue; //MAT_MAP_VSCALE
                                  case 0xa358: continue; //MAT_MAP_UOFFSET
                                  case 0xa35a: continue; //MAT_MAP_VOFFSET
                                }
                              continue;
                            case 0xa204: continue; //MAT_SPECMAP
                            case 0xa230: continue; //MAT_BUMPMAP
                          }
                        materials.Add((name, diffuse, tex));
                      }
                      continue;
                    case 0x0100: masterscale = *(float*)mdata.ptr; continue; //MASTER_SCALE 
                    case 0x4000: //NAMED_OBJECT
                      {
                        var node = new Node { Name = new string((sbyte*)mdata.ptr), Color = 0xff808080, Tag = new NodeTag { id = -1 } }; root.Add(node);
                        double3[] points = null; float2* ppt = null; var invmesh = false;
                        for (var nobj = mdata.inner(node.Name.Length + 1); nobj.n != 0; nobj.next())
                          switch (nobj.id)
                          {
                            default: continue; //???
                            case 0x4100: //N_TRI_OBJECT
                              for (var tobj = nobj.inner(); tobj.n != 0; tobj.next())
                                switch (tobj.id)
                                {
                                  default: continue; //???
                                  case 0x4160: //MESH_MATRIX
                                    {
                                      double3x4 m; var v = (double*)&m;
                                      for (int t = 0; t < 12; t++) v[t] = ((float*)tobj.ptr)[t];
                                      var d = m.GetDeterminant();
                                      if (d < 0) { invmesh = true; var t = !m * double3x4.Scaling(-1, 1, 1) * m; for (int i = 0; i < points.Length; i++) points[i] *= t; } //m[0] *= -1;                                                                                                                                    //if (meshmat == null) meshmat = new Dictionary<Node, double3x4>(); meshmat[node] = m;
                                      var nodetag = (NodeTag)node.Tag; nodetag.meshmat = m;
                                    }
                                    continue;
                                  case 0x4110: //POINT_ARRAY
                                    {
                                      int np = *(ushort*)tobj.ptr; var pp = (float3*)(tobj.ptr + 2);
                                      points = new double3[np]; for (int t = 0; t < np; t++) points[t] = pp[t];
                                    }
                                    continue;
                                  case 0x4140: //TEX_VERTS
                                    if (points != null && points.Length == *(ushort*)tobj.ptr) ppt = (float2*)(tobj.ptr + 2); else continue; //???
                                    continue;
                                  case 0x4120: //FACE_ARRAY { uint16 v1, v2, v3, flag; } 
                                    {
                                      int np = *(ushort*)tobj.ptr; var pp = (long*)(tobj.ptr + 2);
                                      for (var mats = tobj.inner(2 + (np << 3)); mats.n != 0; mats.next())
                                        switch (mats.id)
                                        {
                                          default: continue; //???
                                          case 0x4130: //MSH_MAT_GROUP
                                            {
                                              var name = new string((sbyte*)mats.ptr);
                                              var nfaces = *(ushort*)(mats.ptr + name.Length + 1);
                                              var pfaces = (ushort*)(mats.ptr + name.Length + 3);
                                              var mat = materials.FirstOrDefault(x => x.name == name); if (mat.name == null) continue; //???
                                              var ii = new ushort[nfaces * 3];
                                              for (int t = 0, s = 0; t < nfaces; t++) { var up = (ushort*)&pp[pfaces[t]]; ii[s++] = up[0]; ii[s++] = up[invmesh ? 2 : 1]; ii[s++] = up[invmesh ? 1 : 2]; }
                                              var pn = node.Points == null ? node : new Node { };
                                              if (node.Points != null) node.Add(pn);
                                              pn.Points = points; pn.Indices = ii; pn.Color = mat.diffuse; pn.Texture = mat.tex;
                                              if (pn.Texture == null) continue;
                                              if (ppt == null) continue; //??? 
                                              pn.Texcoords = ii.Select(i => ppt[i]).ToArray();
                                            }
                                            continue;
                                          case 0x4150: continue; //SMOOTH_GROUP
                                        }
                                      invmesh = false;
                                    }
                                    continue;
                                }
                              continue;
                          }
                        Debug.Assert(!invmesh);
                      }
                      continue;
                  }
              }
              continue;
            case 0xb000: //KFDATA
              for (var data = chunk.inner(); data.n != 0; data.next())
                switch (data.id)
                {
                  default: continue; //???
                  case 0xb00a: //KFHDR typedef struct { uint16 rev; char str[9]; uint16 AnimLen; }  
                    { var rev = *(ushort*)data.ptr; var str = new string((sbyte*)(data.ptr + 2)); var AnimLen = *(ushort*)(data.ptr + 11); }
                    continue;
                  case 0xb008: //KFSEG SHORT[2] start, end
                    { var start = *(short*)data.ptr; var end = *(short*)(data.ptr + 2); }
                    continue;
                  case 0xb009: //KFCURTIME SHORT curframe
                    { var curframe = *(short*)data.ptr; }
                    continue;
                  case 0xb002: //OBJECT_NODE_TAG
                    {
                      string name = null, instance = null; ushort nodeid = 0xffff, parentid = 0xffff; var pivot = new float3();
                      float3[] pos = null, sca = null; float4[] rot = null;
                      for (var tag = data.inner(); tag.n != 0; tag.next())
                        switch (tag.id)
                        {
                          default: continue; //???
                          case 0xb030: nodeid = *(ushort*)tag.ptr; continue; //NODE_ID 
                          case 0xb010: //NODE_HDR
                            name = new string((sbyte*)tag.ptr); // var t1 = *(ushort*)(tag.ptr + name.Length + 1); var t2 = *(ushort*)(tag.ptr + name.Length + 3);
                            parentid = *(ushort*)(tag.ptr + name.Length + 5);
                            continue;
                          case 0xb013: pivot = *(float3*)tag.ptr; break; //PIVOT
                          case 0xb014: continue; //BOUNDBOX
                          case 0xb011: instance = new string((sbyte*)tag.ptr); break; //INSTANCE_NAME           
                          case 0xb020: //POS_TRACK_TAG trackheader3ds { uint16 flags; uint32 nu1, nu2; uint32 keycount; }
                            {
                              var flags = *(ushort*)tag.ptr; uint nu1 = *(uint*)(tag.ptr + 2), nu2 = *(uint*)(tag.ptr + 6), keycount = *(uint*)(tag.ptr + 10);
                              pos = new float3[keycount]; var pp = tag.ptr + 14;
                              for (int i = 0; i < keycount; i++)
                              {
                                var time = *(uint*)pp; pp += 4; var rflags = *(ushort*)pp; pp += 2;
                                if ((rflags & 0x01) != 0) { var tension = *(float*)pp; pp += 4; }
                                if ((rflags & 0x02) != 0) { var continuity = *(float*)pp; pp += 4; }
                                if ((rflags & 0x04) != 0) { var bias = *(float*)pp; pp += 4; }
                                if ((rflags & 0x08) != 0) { var easeto = *(float*)pp; pp += 4; }
                                if ((rflags & 0x10) != 0) { var easefrom = *(float*)pp; pp += 4; }
                                pos[i] = *(float3*)pp; pp += 12;
                              }
                            }
                            continue;
                          case 0xb021: //ROT_TRACK_TAG trackheader3ds { uint16 flags; uint32 nu1, nu2; uint32 keycount; }
                            {
                              var flags = *(ushort*)tag.ptr; uint nu1 = *(uint*)(tag.ptr + 2), nu2 = *(uint*)(tag.ptr + 6), keycount = *(uint*)(tag.ptr + 10);
                              rot = new float4[keycount]; var pp = tag.ptr + 14; //(angel,x,y,z) default 0,0,0,1
                              for (int i = 0; i < keycount; i++)
                              {
                                var time = *(uint*)pp; pp += 4; var rflags = *(ushort*)pp; pp += 2;
                                if ((rflags & 0x01) != 0) { var tension = *(float*)pp; pp += 4; }
                                if ((rflags & 0x02) != 0) { var continuity = *(float*)pp; pp += 4; }
                                if ((rflags & 0x04) != 0) { var bias = *(float*)pp; pp += 4; }
                                if ((rflags & 0x08) != 0) { var easeto = *(float*)pp; pp += 4; }
                                if ((rflags & 0x10) != 0) { var easefrom = *(float*)pp; pp += 4; }
                                rot[i] = *(float4*)pp; pp += 16; //if (v.y == 0 && v.z == 0 && v.w == 0) v.w = 1;
                              }
                            }
                            continue;
                          case 0xb022: //SCL_TRACK_TAG trackheader3ds { uint16 flags; uint32 nu1, nu2; uint32 keycount; }
                            {
                              var flags = *(ushort*)tag.ptr; uint nu1 = *(uint*)(tag.ptr + 2), nu2 = *(uint*)(tag.ptr + 6), keycount = *(uint*)(tag.ptr + 10);
                              sca = new float3[keycount]; var pp = tag.ptr + 14;
                              for (int i = 0; i < keycount; i++)
                              {
                                var time = *(uint*)pp; pp += 4; var rflags = *(ushort*)pp; pp += 2;
                                if ((rflags & 0x01) != 0) { var tension = *(float*)pp; pp += 4; }
                                if ((rflags & 0x02) != 0) { var continuity = *(float*)pp; pp += 4; }
                                if ((rflags & 0x04) != 0) { var bias = *(float*)pp; pp += 4; }
                                if ((rflags & 0x08) != 0) { var easeto = *(float*)pp; pp += 4; }
                                if ((rflags & 0x10) != 0) { var easefrom = *(float*)pp; pp += 4; }
                                sca[i] = *(float3*)pp; pp += 12;
                              }
                            }
                            continue;
                        }

                      var locmat =
                        double3x4.Scaling(sca[0].x, sca[0].y, sca[0].z) * //double3x4.Rotation(double4.QuatAxisAngel(new double3(rot[0].y, rot[0].z, rot[0].w), rot[0].x)) *
                        double3x4.Rotation(new double3(rot[0].y, rot[0].z, rot[0].w), -rot[0].x) *
                        double3x4.Translation(pos[0].x, pos[0].y, pos[0].z);
                      if (parentid != 0xffff)
                      {
                        var parent = root.FirstOrDefault(t => t.Tag != null && ((NodeTag)t.Tag).id == parentid); if (parent == null) continue; //???
                        locmat = locmat * ((NodeTag)parent.Tag).locmat;
                      }

                      if (name == "$$$DUMMY") { root.Add(new Node { Name = name, Tag = new NodeTag { id = nodeid, locmat = locmat } }); continue; }

                      var node = root.FirstOrDefault(t => t.Name == name); if (node == null) continue; //???
                      var nodetag = (NodeTag)node.Tag;
                      nodetag.locmat = locmat;
                      var m = !nodetag.meshmat * double3x4.Translation(-pivot.x, -pivot.y, -pivot.z) * locmat;
                      if (instance == null) node.Transform = m;
                      else { node = node.Clone(); node.Name = instance; node.Transform = m; root.Add(node); }
                      nodetag.id = nodeid;
                    }
                    continue;
                }
              continue;
          }
      }
      for (int i = 0; i < root.Count; i++) { root[i].Tag = null; if (root[i].Name == "$$$DUMMY") root.RemoveAt(i--); }
      return root;
    }

    internal static void export(Node root, string path)
    {
      var tt = new List<byte[]>(); var mm = new List<(uint color, int tex)>(); var nodes = new List<(Node node, int mat)>();
      foreach (var node in root.Descendants(true).Where(t => t.Points != null))
      {
        int i = -1; var t = node.Texture; if (t != null) { for (i = 0; i < tt.Count && !Native.Equals(tt[i], t); i++) ; if (i == tt.Count) tt.Add(t); }
        var m = (node.Color, i); var im = mm.IndexOf(m); if (im == -1) { im = mm.Count; mm.Add(m); }
        nodes.Add((node, im));
      }

      var ms = new MemoryStream(); var bw = new BinaryWriter(ms);
      void emit(ushort id, Action act) { var x = (int)ms.Position; bw.Write(id); bw.Write(0); act(); var y = (int)ms.Position; ms.Position = x + 2; bw.Write(y - x); ms.Position = y; }
      emit(0x4d4d, () => //M3DMAGIC
      {
        emit(0x0002, () => bw.Write(3)); //M3D_VERSION
        emit(0x3d3d, () => //MDATA
        {
          emit(0x3d3e, () => bw.Write(3)); //MESH_VERSION
          for (int i = 0; i < mm.Count; i++)
            emit(0xafff, () => //MAT_ENTRY
            {
              var color = mm[i].color;
              emit(0xa000, () => { bw.Write(('M' + (i + 1)).ToString().ToCharArray()); bw.Write((byte)0); }); //MAT_NAME
              emit(0xa020, () => //MAT_DIFFUSE
              {
                emit(0x0011, () => { var c = color; bw.Write(((byte*)&c)[2]); bw.Write(((byte*)&c)[1]); bw.Write(((byte*)&c)[0]); }); //COLOR_24
              });
              if ((color >> 24) != 0xff) emit(0xa050, () => //MAT_TRANSPARENCY
              {
                emit(0x0030, () => bw.Write((short)((0x100 - (color >> 24)) * 100 / 0xff))); //INT_PERCENTAGE
              });
              if (mm[i].tex != -1) emit(0xa200, () => //MAT_TEXMAP
              {
                emit(0xa300, () => { bw.Write($"{path}_{mm[i].tex + 1}.png".ToString().ToCharArray()); bw.Write((byte)0); }); //MAT_MAPNAME
              });
            });
          emit(0x0100, () => bw.Write(1.0f)); //MASTER_SCALE
          foreach (var node in nodes)
            emit(0x4000, () => //NAMED_OBJECT
            {
              bw.Write((node.node.Name ?? "Node").ToCharArray()); bw.Write((byte)0); var m = node.node.GetTransform();
              var pp = node.node.Points; var ii = node.node.Indices; var tc = node.node.Texcoords;
              emit(0x4100, () => //N_TRI_OBJECT
              {
                //emit(0x4160, () => //MESH_MATRIX
                //{ var m = !node.GetTransform(); for (int i = 0; i < 12; i++) bw.Write((float)(&m._11)[i]); });
                emit(0x4110, () => //POINT_ARRAY
                { bw.Write((ushort)pp.Length); foreach (var p in pp) { var t = p * m; bw.Write((float)t.x); bw.Write((float)t.y); bw.Write((float)t.z); } });
                if (tc != null) emit(0x4140, () => //TEX_VERTS
                {
                  var uv = new float2[pp.Length]; for (int i = 0; i < ii.Length; i++) uv[ii[i]] = tc[i];
                  bw.Write((ushort)pp.Length); for (int i = 0; i < pp.Length; i++) { bw.Write(uv[i].x); bw.Write(uv[i].y); }
                });
                emit(0x4120, () => //FACE_ARRAY
                {
                  var si = node.node.StartIndex; var ic = node.node.IndexCount != 0 ? node.node.IndexCount : ii.Length; //if (node.node.IndexCount != 0) { }
                  bw.Write((ushort)(ic / 3)); for (int i = 0; i < ic; i += 3) { bw.Write(ii[si + i + 0]); bw.Write(ii[si + i + 1]); bw.Write(ii[si + i + 2]); bw.Write((ushort)0); }
                  emit(0x4130, () => //MSH_MAT_GROUP
                  {
                    bw.Write(('M' + (node.mat + 1)).ToString().ToCharArray()); bw.Write((byte)0);
                    var ni = ic / 3; bw.Write((ushort)ni); for (int i = 0; i < ni; i++) bw.Write((ushort)i);
                  });
                });
              });
            });
        });
      });
      File.WriteAllBytes(path, ms.ToArray());
      for (int i = 0; i < tt.Count; i++) using (var bmp = Image.FromStream(new MemoryStream(tt[i]))) bmp.Save($"{path}_{i + 1}.png", System.Drawing.Imaging.ImageFormat.Png);
      //var tt = import(path);
    }
  }
}
