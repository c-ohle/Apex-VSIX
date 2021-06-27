using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Xml;

namespace Apex
{
  public static unsafe partial class CDX
  {
    public static readonly IFactory Factory = (IFactory)COM.CreateInstance(
      IntPtr.Size == 8 ? "cdx64.dll" : "cdx32.dll", typeof(CFactory).GUID, typeof(IFactory).GUID);

    [ComImport, Guid("4e957503-5aeb-41f2-975b-3e6ae9f9c75a")]
    public class CFactory { }

    [ComImport, Guid("f0993d73-ea2a-4bf1-b128-826d4a3ba584"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
    public interface IFactory
    {
      int Version { get; }
      string Devices { get; }
      void SetDevice(uint id);
      IView CreateView(IntPtr wnd, ISink sink, uint samples);
      IScene CreateScene();
      INode CreateNode();
      IFont GetFont(string name, float size, System.Drawing.FontStyle style);
      int GetInfo(int id);
      IBuffer GetBuffer(BUFFER id, void* p, int n);
      void CopyCoords(float3* app, ushort* aii, int ani, float2* att, float3* bpp, ushort* bii, int bni, float2* btt, float eps = 0);
      //void Push(void* p, int n);
      //void* Pop(out int n);
    }

    internal struct Range
    {
      internal int Start, Count;
      internal uint Color;
    }

    public enum BUFFER
    {
      POINTBUFFER = 0,
      INDEXBUFFER = 1,
      TEXCOORDS = 2,
      PROPS = 3,
      RANGES = 4,
      //CAMERA = 7,
      SCRIPT = 10,
      TEXTURE = 16,
    }

    [ComImport, Guid("A21FB8D8-33B3-4F8E-8740-8EB4B1FD4153"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
    public interface IBuffer
    {
      BUFFER Id { get; }
      string Name { get; set; }
      object Tag { [return: MarshalAs(UnmanagedType.IUnknown)] get; [param: MarshalAs(UnmanagedType.IUnknown)] set; }
      int GetBufferPtr(void** p);
      void Update(byte* p, int n);
    }

    internal struct BUFFERCAMERA { internal float fov, near, far, minz; }

    [Flags]
    public enum RenderFlags
    {
      BoundingBox = 0x0001,
      Coordinates = 0x0002,
      Normals = 0x0004,
      Wireframe = 0x0008,
      Outlines = 0x0010,
      Shadows = 0x0400,
      ZPlaneShadows = 0x0800,
      SelOnly = 0x1000,
      Fps = 0x2000,
    }

    public enum Cmd
    {
      Center = 1, //in float border, zoom out camdat
      CenterSel = 2, //in float border, zoom out camdat
      SetPlane = 5, //in float4x3
      PickPlane = 6, //in float2, out float2
      SelectRect = 7, //in float2[2] 
      BoxesSet = 8, //in int, out int[2] 
      BoxesGet = 9, //in int, out float3box
      BoxesTra = 10, //in int, out float4x3
      BoxesInd = 11, //in int, out float4x3
    }

    public enum Draw
    {
      Orthographic = 0,
      GetTransform = 1, SetTransform = 2,
      GetColor = 3, SetColor = 4,
      GetFont = 5, SetFont = 6,
      GetTexture = 7, SetTexture = 8,
      GetMapping = 9, SetMapping = 10,
      FillRect = 11,
      FillEllipse = 12,
      GetTextExtent = 13,
      DrawText = 14,
      DrawRect = 15,
      DrawPoints = 16,
      Catch = 17,
      DrawPolyline = 18,
      DrawEllipse = 19,
      DrawBox = 20,
      //Push = 21, Pop = 22,
    }

    [ComImport, Guid("4C0EC273-CA2F-48F4-B871-E487E2774492"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
    public interface IView
    {
      string Samples { get; set; }
      uint BkColor { get; set; }
      RenderFlags Render { get; set; }
      IScene Scene { get; set; }
      INode Camera { get; set; }
      INode MouseOverNode { get; }
      int MouseOverId { get; }
      float3 MouseOverPoint { get; }
      int Dpi { get; }
      int Fps { get; }
      void Draw(Draw draw, void* data);
      void Command(Cmd cmd, void* data);
      void Thumbnail(int dx, int dy, int samples, uint bkcolor, COM.IStream str);
    }

    [ComImport, Guid("982A1DBA-0C12-4342-8F58-A34D83956F0D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
    public interface ISink
    {
      [PreserveSig] void Render(int fl);
      [PreserveSig] void Timer();
      [PreserveSig] void Animate(INode p, uint t);
      [PreserveSig] void Reslove([MarshalAs(UnmanagedType.IUnknown)] object p, COM.IStream s);
    }

    public enum Unit { meter = 1, centimeter = 2, millimeter = 3, micron = 4, foot = 5, inch = 6, }

    [ComImport, Guid("344CC49E-4BBF-4F1E-A17A-55CBF848EED3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
    public interface IRoot
    {
      INode Child { get; }
      int Count { get; }
      INode AddNode(string name);
      void InsertAt(int i, INode p);
      void RemoveAt(int i);
    }

    [ComImport, Guid("98068F4F-7768-484B-A2F8-21D4F7B5D811"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
    public interface IScene //: IRoot
    {
      INode Child { get; }
      int Count { get; }
      INode AddNode(string name);
      void InsertAt(int i, INode p);
      void RemoveAt(int i);
      INode Camera { get; set; }
      object Tag { [return: MarshalAs(UnmanagedType.IUnknown)] get; [param: MarshalAs(UnmanagedType.IUnknown)] set; }
      Unit Unit { get; set; }
      int SelectionCount { get; }
      INode GetSelection(int i);
      void Clear();
      void SaveToStream(COM.IStream str, INode cam); //null selonly 
      void LoadFromStream(COM.IStream str);
    }

    [ComImport, Guid("2BB87169-81D3-405E-9C16-E4C22177BBAA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
    public interface INode
    {
      INode Child { get; }
      int Count { get; }
      INode AddNode(string name);
      void InsertAt(int i, INode p);
      void RemoveAt(int i);
      string Name { get; set; }
      IRoot Parent { get; }
      IScene Scene { get; }
      INode Next { get; }
      int Index { get; set; }
      bool IsSelect { get; set; }
      bool IsStatic { get; set; }
      bool IsActive { get; set; }
      float4x3 Transform { get; set; }
      uint Color { get; set; }
      IBuffer Texture { get; set; }
      object Tag { [return: MarshalAs(UnmanagedType.IUnknown)] get; [param: MarshalAs(UnmanagedType.IUnknown)] set; }
      INode NextSibling(INode p = null);
      float4x3 GetTransform(INode p = null);
      bool HasBuffer(BUFFER id);
      IBuffer GetBuffer(BUFFER id);
      void SetBuffer(BUFFER id, IBuffer p);
      int GetBufferPtr(BUFFER id, void** p);
      void SetBufferPtr(BUFFER id, void* p, int n);
      void GetBox(ref float3box box, float4x3* pm);
      float4x3 GetTypeTransform(int typ);
      void SetTypeTransform(int typ, in float4x3 m);
      void SetProp(char* name, void* p, int n, int typ);
      int GetProp(char* name, void** p, out int typ);
      string GetProps();
    }

    [ComImport, Guid("F063C32D-59D1-4A0D-B209-323268059C12"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
    public interface IFont
    {
      string Name { get; }
      float Size { get; }
      int Style { get; }
      float Ascent { get; }
      float Descent { get; }
      float Height { get; }
    }

    public static double GetVolume(this INode p)
    {
      float3* pp; p.GetBufferPtr(BUFFER.POINTBUFFER, (void**)&pp); if (pp == null) return -1;
      ushort* ii; var ni = p.GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&ii) / sizeof(ushort);
      var v = 0.0; for (int i = 0; i < ni; i += 3) v += pp[ii[i]] & (pp[ii[i + 1]] ^ pp[ii[i + 2]]); return v * (1.0 / 6);
    }
    public static double GetSurfaceArea(this INode p)
    {
      float3* pp; p.GetBufferPtr(BUFFER.POINTBUFFER, (void**)&pp); if (pp == null) return -1;
      ushort* ii; var ni = p.GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&ii) / sizeof(ushort);
      var v = 0.0; for (int i = 0; i < ni; i += 3)
        v += (pp[ii[i + 1]] - pp[ii[i]] ^ pp[ii[i + 2]] - pp[ii[i]]).Length; return v * 0.5;
    }

    internal static T[] GetArray<T>(this IBuffer p) where T : unmanaged
    {
      void* s; var n = p.GetBufferPtr(&s) / sizeof(T); if (s == null) return null;
      var a = new T[n]; fixed (void* d = a) Native.memcpy(d, s, (void*)(n * sizeof(T))); return a;
    }
    internal static T[] GetArray<T>(this INode p, BUFFER id) where T : unmanaged
    {
      void* s; var n = p.GetBufferPtr(id, &s) / sizeof(T); if (s == null) return null;
      var a = new T[n]; fixed (void* d = a) Native.memcpy(d, s, (void*)(n * sizeof(T))); return a;
    }
    internal static void SetArray<T>(this INode p, BUFFER id, T[] a, int c = -1) where T : unmanaged
    {
      fixed (void* t = a) p.SetBufferPtr(id, t, a != null ? (c != -1 ? c : a.Length) * sizeof(T) : 0);
    }
    //internal static float3[] GetPoints(this INode p) => p.GetArray<float3>(BUFFER.POINTBUFFER);
    //internal static ushort[] GetIndices(this INode p) => p.GetArray<ushort>(BUFFER.INDEXBUFFER);
    internal static T GetProp<T>(this INode p, string s) where T : unmanaged
    {
      fixed (char* t = s) { void* unk; var n = p.GetProp(t, &unk, out var vt); if (n == sizeof(T)) return *(T*)unk; }
      return default(T);
    }
    internal static void SetProp<T>(this INode p, string s, T v) where T : unmanaged
    {
      fixed (char* t = s) p.SetProp(t, &v, sizeof(T), (int)Type.GetTypeCode(typeof(T)));
    }
    internal static bool HasProp(this INode p, string s)
    {
      fixed (char* t = s) { void* unk; var n = p.GetProp(t, &unk, out var vt); return n != 0; }
    }

    internal static string GetClassName(this INode node)
    {
      //node.GetProp()
      //if (node.HasBuffer(BUFFER.CAMERA)) return "Camera";
      //if (node.HasBuffer(BUFFER.LIGHT)) return "Light";
      if (node.HasBuffer(BUFFER.POINTBUFFER)) return "Model";
      return "Group";
    }
    public static float3box GetBox(INode node, object root = null)
    {
      return GetBox(Enumerable.Repeat(node, node != null ? 1 : 0), root);
    }
    public static float3box GetBox(IEnumerable<INode> nodes, object root = null)
    {
      var box = float3box.Empty;
      foreach (var node in nodes)
      {
        for (var p = node; p != null; p = p.NextSibling(node))
        {
          if (!p.HasBuffer(BUFFER.POINTBUFFER)) continue;
          if (p == root) { p.GetBox(ref box, null); continue; }
          var m = p.GetTransform(root as INode); p.GetBox(ref box, &m);
        }
      }
      return box;
    }
    public static byte[] GetBytes(this INode p, BUFFER id) => p.GetArray<byte>(id);
    public static void SetBytes(this INode node, BUFFER id, byte[] data) => node.SetArray(id, data);
    internal static void RemoveBuffer(this INode node, BUFFER id) => node.SetBufferPtr(id, null, 0);
    public static void Update(this INode p)
    {
      if (!(p.Tag is Node node) || node.funcs == null) return;
      Node.SaveProps(p, node.GetMethod<Action<IExchange>>());
    }
    public static void CopyTo(this CSG.IMesh a, INode b, float2[] tt = null)
    {
      var nv = a.VertexCount; var ni = a.IndexCount;
      var pp = Marshal.AllocCoTaskMem(Math.Max(nv * sizeof(float3), ni * sizeof(ushort)));
      try
      {
        var vp = (float3*)pp.ToPointer(); a.CopyBuffer(0, 0, new CSG.Variant(&vp->x, 3, nv));
        b.SetBufferPtr(BUFFER.POINTBUFFER, vp, nv * sizeof(float3));
        var ip = (ushort*)pp.ToPointer(); a.CopyBuffer(1, 0, new CSG.Variant(ip, 1, ni));
        b.SetBufferPtr(BUFFER.INDEXBUFFER, ip, ni * sizeof(ushort));
      }
      finally { Marshal.FreeCoTaskMem(pp); }
      b.SetArray(BUFFER.TEXCOORDS, tt, ni);
    }
    //public static void CopyTo(this CSG.IMesh a, out IBuffer pb, out IBuffer ib)
    //{
    //  pb = ib = null; var nv = a.VertexCount; var ni = a.IndexCount;
    //  var pp = Marshal.AllocCoTaskMem(Math.Max(nv * sizeof(float3), ni * sizeof(ushort)));
    //  try
    //  {
    //    var vp = (float3*)pp.ToPointer(); a.CopyBuffer(0, 0, new CSG.Variant(&vp->x, 3, nv));
    //    pb = Factory.GetBuffer(BUFFER.POINTBUFFER, vp, nv * sizeof(float3));
    //    var ip = (ushort*)pp.ToPointer(); a.CopyBuffer(1, 0, new CSG.Variant(ip, 1, ni));
    //    ib = Factory.GetBuffer(BUFFER.INDEXBUFFER, ip, ni * sizeof(ushort));
    //  }
    //  finally { Marshal.FreeCoTaskMem(pp); }
    //}
    public static void CopyTo(this INode a, CSG.IMesh b)
    {
      float3* pp; var np = a.GetBufferPtr(BUFFER.POINTBUFFER, (void**)&pp) / sizeof(float3);
      ushort* ii; var ni = a.GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&ii) / sizeof(ushort);
      b.Update(new CSG.Variant(&pp->x, 3, np), new CSG.Variant(ii, 1, ni));
    }
    public static void Copy(this CSG.IMesh b, float3[] pp, int[] ii)
    {
      for (int i = 0; i < ii.Length; i++) if ((uint)ii[i] >= (uint)pp.Length) throw new Exception("Invalid index");
      fixed (float3* pt = pp) fixed (int* pi = ii)
        b.Update(new CSG.Variant(&pt->x, 3, pp.Length), new CSG.Variant(pi, 1, ii.Length));
    }
    public static void SetMesh(this INode a, float3[] pp, int[] ii, float2[] tt = null)
    {
      if (ii != null) for (int i = 0, m = pp.Length, n = ii.Length; i < n; i++) if ((uint)ii[i] >= m) throw new Exception("Invalid index");
      a.SetArray(BUFFER.POINTBUFFER, pp);
      a.SetArray(BUFFER.INDEXBUFFER | (BUFFER)0x1000, ii);
      a.SetArray(BUFFER.TEXCOORDS, tt);
    }
    public static void SetMesh(this INode a, float3[] pp, int np, ushort[] ii, int ni, float2[] tt = null)
    {
      if (ii != null) for (int i = 0; i < ni; i++) if (ii[i] >= np) throw new Exception("Invalid index");
      a.SetArray(BUFFER.POINTBUFFER, pp, np);
      a.SetArray(BUFFER.INDEXBUFFER, ii, ni);
      a.SetArray(BUFFER.TEXCOORDS, tt, tt != null ? ni : 0);
    }
    public static IEnumerable<INode> Descendants(this IScene p)
    {
      for (var t = p.Child; t != null; t = t.NextSibling(null)) yield return t;
    }
    public static IEnumerable<INode> Selection(this IScene p)
    {
      for (int i = 0, n = p.SelectionCount; i < n; i++) yield return p.GetSelection(i);
    }
    //public static IEnumerable<INode> SelectNodes(this IScene p, BUFFER id)
    //{
    //  for (var t = p.Child; t != null; t = t.NextSibling(null)) if (t.HasBuffer(id)) yield return t;
    //}
    public static IEnumerable<INode> Nodes(this IScene p)
    {
      for (var t = p.Child; t != null; t = t.Next) yield return t;
    }
    public static IEnumerable<INode> Nodes(this INode p)
    {
      for (var t = p.Child; t != null; t = t.Next) yield return t;
    }
    public static IEnumerable<INode> Descendants(this INode p, bool andself = false)
    {
      if (andself) yield return p;
      for (var c = p.Child; c != null; c = c.NextSibling(p)) yield return c;
    }
    public static void Select(this IScene scene, params INode[] a)
    {
      for (int i = scene.SelectionCount - 1; i >= 0; i--)
      {
        var p = scene.GetSelection(i);
        if (Array.IndexOf(a, p) == -1) p.SetSelect(false);
      }
      for (int i = a.Length - 1; i >= 0; i--) a[i].SetSelect(true);
    }
    public static void Select(this IScene scene, INode node = null)
    {
      for (int i = scene.SelectionCount - 1; i >= 0; i--)
      {
        var p = scene.GetSelection(i);
        if (p != node) p.SetSelect(false);
      }
      if (node != null) node.SetSelect();
    }
    public static void Select(this INode node)
    {
      node.Scene.Select(node);
    }
    public static void SetSelect(this INode node, bool select = true)
    {
      if (node.IsSelect == select) return;
      node.IsSelect = select; if (!(node.Tag is Node n)) return;
      n.view.Invalidate(Inval.Select);
      //if (!select) n.RemoveAnnotations(typeof(PropertyDescriptorCollection));
    }
    public static void SetPlane(this IView view, float4x3 p)
    {
      view.Command(Cmd.SetPlane, &p);
    }
    public static float2 PickPlane(this IView view)
    {
      float2 p; p.x = float.NaN; view.Command(Cmd.PickPlane, &p); return p;
    }
    public static void AddUndo(this IView view, Action undo)
    {
      //((MainFrame)UIForm.MainFrame).view.AddUndo(undo);
    }
    public static IFont GetFont(Font p) => Factory.GetFont(p.FontFamily.Name, p.SizeInPoints, p.Style);
    public static IBuffer GetTexture(int dx, int dy, int bp, Action<Graphics> draw)
    {
      using (var bmp = new Bitmap(dx, dy, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
      {
        using (var gr = Graphics.FromImage(bmp)) draw(gr);
        var s = new System.IO.MemoryStream();
        if (bp > 8) bmp.Save(s, System.Drawing.Imaging.ImageFormat.Png);
        else using (var tmp = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.PixelFormat.Format1bppIndexed))
          {
            tmp.SetResolution(1, 1); var t = tmp.Palette; t.Entries[0] = Color.Transparent; tmp.Palette = t;
            tmp.Save(s, System.Drawing.Imaging.ImageFormat.Png);
          }
        fixed (byte* p = s.GetBuffer()) return Factory.GetBuffer(BUFFER.TEXTURE, p, (int)s.Length);//.GetTexture(p, (int)s.Length);
      }
    }
    public static T[] GetBuffer<T>(int minsize, bool clear = true)
    {
      ref var p = ref WeakSingleton<T[]>.p; var v = p.Value;
      if (v == null || v.Length < minsize) p.Value = v = new T[minsize];
      else if (clear) Array.Clear(v, 0, minsize);
      return v;
    }

    public readonly struct DC
    {
      readonly IView p;
      public DC(IView p) => this.p = p;
      public void SetOrtographic()
      {
        p.Draw(Draw.Orthographic, null);
      }
      public float4x3 Transform
      {
        get { float4x3 c; p.Draw(Draw.GetTransform, &c); return c; }
        set { p.Draw(Draw.SetTransform, &value); }
      }
      public uint Color
      {
        get { uint c; p.Draw(Draw.GetColor, &c); return c; }
        set => p.Draw(Draw.SetColor, &value);
      }
      public IFont Font
      {
        get
        {
          IntPtr t; p.Draw(Draw.GetFont, &t); if (t == IntPtr.Zero) return null;
          var f = (IFont)Marshal.GetObjectForIUnknown(t); Marshal.Release(t); return f;
        }
        set
        {
          if (value == null) { p.Draw(Draw.SetFont, IntPtr.Zero.ToPointer()); return; }
          var t = Marshal.GetIUnknownForObject(value); p.Draw(Draw.SetFont, t.ToPointer()); Marshal.Release(t);
        }
      }
      public IBuffer Texture
      {
        get
        {
          IntPtr t; p.Draw(Draw.GetTexture, &t); if (t == IntPtr.Zero) return null;
          var f = (IBuffer)Marshal.GetObjectForIUnknown(t); Marshal.Release(t); return f;
        }
        set
        {
          if (value == null) { p.Draw(Draw.SetTexture, IntPtr.Zero.ToPointer()); return; }
          var t = Marshal.GetIUnknownForObject(value); p.Draw(Draw.SetTexture, t.ToPointer()); Marshal.Release(t);
        }
      }
      public float4x3 Mapping
      {
        get { float4x3 c; p.Draw(Draw.GetMapping, &c); return c; }
        set => p.Draw(Draw.SetMapping, &value);
      }
      public void DrawRect(float x, float y, float dx, float dy)
      {
        var r = new float4(x, y, dx, dy); p.Draw(Draw.DrawRect, &r);
      }
      public void FillRect(float x, float y, float dx, float dy)
      {
        var r = new float4(x, y, dx, dy); p.Draw(Draw.FillRect, &r);
      }
      public void DrawCirc(float2 p, float r, int segs = 32)
      {
        var c = (p.x, p.y, r, r, segs);
        this.p.Draw(Draw.DrawEllipse, &c.Item1);
      }
      public void FillCirc(float2 p, float r, int segs = 32)
      {
        var c = (p.x, p.y, r, r, segs);
        this.p.Draw(Draw.FillEllipse, &c.Item1);
      }
      public void FillEllipse(float x, float y, float dx, float dy, int segs = 32)
      {
        var c = (x + (dx *= 0.5f), y + (dy *= 0.5f), dx, dy, segs);
        p.Draw(Draw.FillEllipse, &c.Item1);
      }
      public void DrawEllipse(float x, float y, float dx, float dy, int segs = 32)
      {
        var c = (x + (dx *= 0.5f), y + (dy *= 0.5f), dx, dy, segs);
        this.p.Draw(Draw.DrawEllipse, &c.Item1);
      }
      public void DrawLine(float2 a, float2 b)
      {
        var t1 = ((float3)a, (float3)b); var t2 = (2, new IntPtr(&t1.Item1));
        p.Draw(Draw.DrawPolyline, &t2.Item1);
        //var c = (2, (float3)a, (float3)b); p.Draw(Draw.DrawPolyline, &c.Item1);
      }
      public void DrawLine(float3 a, float3 b)
      {
        var t1 = (a, b); var t2 = (2, new IntPtr(&t1.Item1));
        p.Draw(Draw.DrawPolyline, &t2.Item1);
        //var c = (2, a, b); p.Draw(Draw.DrawPolyline, &c.Item1);
      }
      public void DrawPolyline(float3[] p, int i, int n)
      {
        fixed (float3* t = p) DrawPolyline(t + i, n);
      }
      public void DrawPolyline(float3* pp, int np)
      {
        var t = (np, new IntPtr(pp)); p.Draw(Draw.DrawPolyline, &t.Item1);
      }
      public void DrawBox(float3box b)
      {
        p.Draw(Draw.DrawBox, &b);
      }
      public float2 GetTextExtent(string s)
      {
        fixed (char* t = s)
        {
          var c = (0f, 0f, (IntPtr)t, s.Length);
          p.Draw(Draw.GetTextExtent, &c.Item1);
          return *(float2*)&c.Item1;
        }
      }
      public void DrawText(float x, float y, string s)
      {
        fixed (char* t = s)
        {
          var c = (x, y, (IntPtr)t, s.Length);
          p.Draw(Draw.DrawText, &c.Item1);
        }
      }
      static IBuffer gettex()
      {
        return GetTexture(32, 32, 32, gr =>
        {
          gr.FillEllipse(System.Drawing.Brushes.Black, 1, 1, 30, 30);
          gr.FillEllipse(System.Drawing.Brushes.White, 4, 4, 30 - 6, 30 - 6);
        });
      }
      static IBuffer texpt;
      public void DrawPoints(params float3[] pp)
      {
        fixed (float3* t = pp) DrawPoints(t, pp.Length, 6);
      }
      public void DrawPoints(float3[] pp, int np, float r = 6)
      {
        fixed (float3* t = pp) DrawPoints(t, np, r);
      }
      public void DrawPoints(float3* vv, int np, float r = 6)
      {
        var t1 = Texture; Texture = texpt ?? (texpt = gettex());
        float4 t; t.x = r; *(int*)&t.y = np;
        *(float3**)&t.z = vv; p.Draw(Draw.DrawPoints, &t);
        Texture = t1;
      }
      public void Catch(INode node = null, int id = 0)
      {
        var u = node != null ? Marshal.GetIUnknownForObject(node) : IntPtr.Zero;
        var t = (u, id); p.Draw(Draw.Catch, &t.u);
        if (node != null) Marshal.Release(t.u);
      }
    }
  }

  public static unsafe partial class CDX
  {
    [TypeConverter(typeof(float2.Converter))]
    public struct float2 : IEquatable<float2>, IFormattable
    {
      public float x, y;
      public override string ToString()
      {
        return $"{x:R}; {y:R}";
      }
      public float2(float x, float y)
      {
        this.x = x; this.y = y;
      }
      public float2(double a)
      {
        x = (float)Math.Cos(a); if (Math.Abs(x) == 1) { y = 0; return; }
        y = (float)Math.Sin(a); if (Math.Abs(y) == 1) { x = 0; return; }
      }
      public string ToString(string fmt, IFormatProvider p)
      {
        return x.ToString(fmt, p) + "; " + y.ToString(fmt, p);
      }
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
      public static implicit operator float2((float x, float y) p)
      {
        float2 v; v.x = p.x; v.y = p.y; return v;
      }
      public static implicit operator float2(System.Drawing.Size p)
      {
        float2 v; v.x = p.Width; v.y = p.Height; return v;
      }
      public static implicit operator float2(System.Drawing.Point p)
      {
        float2 v; v.x = p.X; v.y = p.Y; return v;
      }
      public float LengthSq => x * x + y * y;
      public float Length => (float)Math.Sqrt(x * x + y * y);
      public double Angel => Math.Atan2(y, x);
      public float2 Round(int d) => new float2((float)Math.Round(x, d), (float)Math.Round(y, d));
      public float2 Normalize() { var l = Length; return l != 0 ? this / l : default; }
      public static bool operator ==(float2 a, float2 b) { return a.x == b.x && a.y == b.y; }
      public static bool operator !=(float2 a, float2 b) { return a.x != b.x || a.y != b.y; }
      public static float2 operator -(float2 v) { v.x = -v.x; v.y = -v.y; return v; }
      public static float2 operator *(float2 v, float f)
      {
        v.x *= f; v.y *= f; return v;
      }
      public static float2 operator *(float2 v, double f)
      {
        v.x = (float)(v.x * f); v.y = (float)(v.y * f); return v;
      }
      public static float2 operator /(float2 v, float f)
      {
        v.x /= f; v.y /= f; return v;
      }
      public static float2 operator /(float2 a, float2 b)
      {
        a.x /= b.x; a.y /= b.y; return a;
      }
      public static float2 operator /(float f, float2 v)
      {
        v.x = f / v.x; v.y = f / v.y; return v;
      }
      public static float2 operator +(float2 a, float2 b) { a.x = a.x + b.x; a.y = a.y + b.y; return a; }
      public static float2 operator -(float2 a, float2 b) { a.x = a.x - b.x; a.y = a.y - b.y; return a; }
      public static float2 operator *(float2 a, float2 b) { a.x = a.x * b.x; a.y = a.y * b.y; return a; }
      public static float2 operator ~(float2 v) { float2 b; b.x = -v.y; b.y = v.x; return b; }
      public static float operator ^(float2 a, float2 b) => a.x * b.y - a.y * b.x;
      public static float operator &(float2 a, float2 b) => a.x * b.x + a.y * b.y;
      internal class Converter : TypeConverter
      {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => true;
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
          return value is float2 a ? ((FormattableString)$"{a.x:R}; {a.y:R}").ToString(culture) : null;
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
          var ss = ((string)value).Split(';');
          return new float2(float.Parse(ss[0], culture), float.Parse(ss[1], culture));
        }
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => context.PropertyDescriptor != null; //!Editor
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) => FieldPD.GetProperties(typeof(float2));
      }
    }

    [TypeConverter(typeof(float3.Converter))]
    public struct float3 : IEquatable<float3>, IFormattable
    {
      public float x, y, z;
      public override string ToString()
      {
        return $"{x:R}; {y:R}; {z:R}";
      }
      public string ToString(string fmt, IFormatProvider p)
      {
        //if (fmt == "s") { }
        return x.ToString(fmt, p) + "; " + y.ToString(fmt, p) + "; " + z.ToString(fmt, p);
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
      public float2 xy
      {
        get => new float2(x, y); internal set { x = value.x; y = value.y; }
      }
      public float LengthSq => x * x + y * y + z * z;
      public float Length => (float)Math.Sqrt(x * x + y * y + z * z);
      public float3 Normalize() { var l = Length; return l != 0 ? this / l : default; }
      //public int LongAxis => Math.Abs(x) > Math.Abs(y) && Math.Abs(x) > Math.Abs(z) ? 0 : Math.Abs(y) > Math.Abs(z) ? 1 : 2;
      public float this[int i]
      {
        get => i == 0 ? x : i == 1 ? y : z;
        set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; }
      }
      public static explicit operator string(float3 p)
      {
        return $"{XmlConvert.ToString(p.x)} {XmlConvert.ToString(p.y)} {XmlConvert.ToString(p.z)}";
      }
      public static explicit operator float3(string s)
      {
        float3 p; scan(s, (float*)&p, 3); return p;
      }
      public static implicit operator float3(float p)
      {
        float3 b; b.x = p; b.y = b.z = 0; return b;
      }
      public static implicit operator float3(float2 p)
      {
        float3 b; b.x = p.x; b.y = p.y; b.z = 0; return b;
      }
      public static explicit operator float2(float3 p)
      {
        float2 b; b.x = p.x; b.y = p.y; return b;
      }
      public static bool operator ==(float3 a, float3 b)
      {
        return a.x == b.x && a.y == b.y && a.z == b.z;
      }
      public static bool operator !=(float3 a, float3 b)
      {
        return a.x != b.x || a.y != b.y || a.z != b.z;
      }
      public static float3 operator -(float3 v)
      {
        v.x = -v.x; v.y = -v.y; v.z = -v.z; return v;
      }
      public static float3 operator +(float3 a, float3 b)
      {
        a.x += b.x; a.y += b.y; a.z += b.z; return a;
      }
      public static float3 operator -(float3 a, float3 b)
      {
        a.x -= b.x; a.y -= b.y; a.z -= b.z; return a;
      }
      public static float3 operator *(float3 a, float3 b)
      {
        a.x *= b.x; a.y *= b.y; a.z *= b.z; return a;
      }
      public static float3 operator *(float3 v, float f)
      {
        v.x *= f; v.y *= f; v.z *= f; return v;
      }
      public static float3 operator *(float3 v, double f)
      {
        v.x = (float)(v.x * f); v.y = (float)(v.y * f); v.z = (float)(v.z * f); return v;
      }
      public static float3 operator /(float3 v, float f)
      {
        v.x /= f; v.y /= f; v.z /= f; return v;
      }
      public static float3 operator /(float3 a, float3 b)
      {
        a.x /= b.x; a.y /= b.y; a.z /= b.z; return a;
      }
      public static float3 operator ^(float3 a, float3 b)
      {
        float3 c;
        c.x = a.y * b.z - a.z * b.y;
        c.y = a.z * b.x - a.x * b.z;
        c.z = a.x * b.y - a.y * b.x;
        return c;
      }
      public static float operator &(float3 a, float3 b)
      {
        return a.x * b.x + a.y * b.y + a.z * b.z;
      }
      internal class Converter : TypeConverter
      {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => true;
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
          if (!(value is float3 a)) return null;
          if (context?.PropertyDescriptor.Name == ".s" && a.x == a.y && a.y == a.z)
            return ((FormattableString)$"{a.x:R}").ToString(culture);
          return ((FormattableString)$"{a.x:R}; {a.y:R}; {a.z:R}").ToString(culture);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
          var ss = ((string)value).Split(';');
          if (ss.Length == 1 && context?.PropertyDescriptor.Name == ".s") { var v = float.Parse(ss[0], culture); return new float3(v, v, v); }
          return new float3(float.Parse(ss[0], culture), float.Parse(ss[1], culture), float.Parse(ss[2], culture));
        }
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => context.PropertyDescriptor != null; //!Editor
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) => FieldPD.GetProperties(typeof(float3));
        //public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) => false;
      }
    }

    [TypeConverter(typeof(float4.Converter))]
    public struct float4 : IEquatable<float4>, IFormattable
    {
      public float x, y, z, w;
      public float3 xyz
      {
        get => new float3(x, y, z); internal set { x = value.x; y = value.y; z = value.z; }
      }
      public float4(float x, float y, float z, float w)
      {
        this.x = x; this.y = y; this.z = z; this.w = w;
      }
      public float4(float3 p, float w)
      {
        this.x = p.x; this.y = p.y; this.z = p.z; this.w = w;
      }
      public override string ToString()
      {
        return $"{x:R}; {y:R}; {z:R}; {w:R}";
      }
      public string ToString(string fmt, IFormatProvider p)
      {
        return x.ToString(fmt, p) + "; " + y.ToString(fmt, p) + "; " + z.ToString(fmt, p) + "; " + w.ToString(fmt, p);
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
      public bool Equals(float4 v)
      {
        return this == v;
      }
      public override bool Equals(object obj)
      {
        return obj is float4 && Equals((float4)obj);
      }
      public static explicit operator string(float4 p)
      {
        return $"{XmlConvert.ToString(p.x)} {XmlConvert.ToString(p.y)} {XmlConvert.ToString(p.z)} {XmlConvert.ToString(p.w)}";
      }
      public static explicit operator float4(string s)
      {
        float4 p; scan(s, (float*)&p, 4); return p;
      }
      public static explicit operator int(float4 p)
      {
        return (int)(p.x * 0xff) | ((int)(p.y * 0xff) << 8) | ((int)(p.z * 0xff) << 16) | ((int)(p.w * 0xff) << 24);
      }
      public static explicit operator float4(int c)
      { 
        return new float4((c & 0xff) * (1f / 0xff), ((c >> 8) & 0xff) * (1f / 0xff), ((c >> 16) & 0xff) * (1f / 0xff), ((c >> 24) & 0xff) * (1f / 0xff)); 
      }
      public static bool operator ==(in float4 a, in float4 b)
      {
        return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
      }
      public static bool operator !=(float4 a, float4 b)
      {
        return !(a == b);
      }
      public static float4 operator -(float4 v)
      {
        v.x = -v.x; v.y = -v.y; v.z = -v.z; v.w = -v.w; return v;
      }
      public static float4 operator *(float4 v, float f)
      {
        v.x *= f; v.y *= f; v.z *= f; v.w *= f; return v;
      }
      public static float4 operator +(float4 a, float4 b)
      {
        return new float4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
      }
      public static explicit operator uint(in float4 p)
      {
        uint d;
        ((byte*)&d)[0] = (byte)(p.z * 255);
        ((byte*)&d)[1] = (byte)(p.y * 255);
        ((byte*)&d)[2] = (byte)(p.x * 255);
        ((byte*)&d)[3] = (byte)(p.w * 255); return d;
      }
      public static explicit operator float4(uint p)
      {
        float4 d;
        d.z = ((byte*)&p)[0] * (1.0f / 255);
        d.y = ((byte*)&p)[1] * (1.0f / 255);
        d.x = ((byte*)&p)[2] * (1.0f / 255);
        d.w = ((byte*)&p)[3] * (1.0f / 255); return d;
      }
      internal class Converter : TypeConverter
      {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => true;
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
          return value is float4 a ? ((FormattableString)$"{a.x:R}; {a.y:R}; {a.z:R}; {a.w:R}").ToString(culture) : null;
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
          var ss = ((string)value).Split(';');
          return new float4(float.Parse(ss[0], culture), float.Parse(ss[1], culture), float.Parse(ss[2], culture), float.Parse(ss[3], culture));
        }
        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => context.PropertyDescriptor != null; //!Editor
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) => FieldPD.GetProperties(typeof(float4));
      }
    }

    public static float4 PlaneFromPoints(float3 a, float3 b, float3 c) => PlaneFromPointNormal(a, (b - a ^ c - a).Normalize());
    public static float4 PlaneFromPointNormal(float3 p, float3 n) => new float4(n.x, n.y, n.z, -(p.x * n.x + p.y * n.y + p.z * n.z));
    public static float DotCoord(float4 e, float3 p) => e.x * p.x + e.y * p.y + e.z * p.z + e.w;
    public static float3 PlaneIntersect(float4 e, float3 a, float3 b)
    {
      var u = e.xyz & a;
      var v = e.xyz & b;
      var w = (u + e.w) / (u - v);
      return a + (b - a) * w;
    }

    public struct float3box
    {
      public float3 min, max;
      public static float3box Empty => new float3box
      {
        min = new float3(+float.MaxValue, +float.MaxValue, +float.MaxValue),
        max = new float3(-float.MaxValue, -float.MaxValue, -float.MaxValue)
      };
      public bool IsEmpty => min.x > max.x;
      public float3 size => max - min;
      public float3 mid => (min + max) * 0.5f;
      public float3 Corner(int i) => new float3(
        (i & 1) != 0 ? min.x : max.x,
        (i & 2) != 0 ? min.y : max.y,
        (i & 4) != 0 ? min.z : max.z);
      public static float3box operator +(float3box b, float3 v)
      {
        b.min += v; b.max += v; return b;
      }
      //public static float3box operator -(float3box b, float3 v)
      //{
      //  b.min -= v; b.max -= v; return b;
      //}
      public static float3box operator &(in float3box a, in float3box b)
      {
        var r = new float3box();
        r.min.x = Math.Max(a.min.x, b.min.x); r.max.x = Math.Min(a.max.x, b.max.x); if (r.min.x > r.max.x) return Empty;
        r.min.y = Math.Max(a.min.y, b.min.y); r.max.y = Math.Min(a.max.y, b.max.y); if (r.min.y > r.max.y) return Empty;
        r.min.z = Math.Max(a.min.z, b.min.z); r.max.z = Math.Min(a.max.z, b.max.z); if (r.min.z > r.max.z) return Empty;
        return r;
      }
      public bool Contains(float3 p) =>
        p.x >= min.x && p.x <= max.x &&
        p.y >= min.y && p.y <= max.y &&
        p.z >= min.z && p.z <= max.z;
      public bool Intersect(in float3box b)
      {
        if (Math.Max(min.x, b.min.x) > Math.Min(max.x, b.max.x)) return false;
        if (Math.Max(min.y, b.min.y) > Math.Min(max.y, b.max.y)) return false;
        if (Math.Max(min.z, b.min.z) > Math.Min(max.z, b.max.z)) return false;
        return true;
      }
      public float3box Inflate(float f) => Inflate(new float3(f, f, f));
      public float3box Inflate(float3 v) { var t = this; t.min -= v; t.max += v; return t; }
      public void Extend(float3 v)
      {
        if (v.x > 0) max.x += v.x; else min.x += v.x;
        if (v.y > 0) max.y += v.y; else min.y += v.y;
        if (v.z > 0) max.z += v.z; else min.z += v.z;
      }
      public void Union(in float3box v)
      {
        min.x = Math.Min(min.x, v.min.x); max.x = Math.Max(max.x, v.max.x);
        min.y = Math.Min(min.y, v.min.y); max.y = Math.Max(max.y, v.max.y);
        min.z = Math.Min(min.z, v.min.z); max.z = Math.Max(max.z, v.max.z);
      }
      public void Add(float3 p)
      {
        if (p.x < min.x) min.x = p.x; if (p.x > max.x) max.x = p.x;
        if (p.y < min.y) min.y = p.y; if (p.y > max.y) max.y = p.y;
        if (p.z < min.z) min.z = p.z; if (p.z > max.z) max.z = p.z;
      }
      public float3box(float3 a, float3 b)
      {
        min = new float3(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));
        max = new float3(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));
      }
      public float3box(float3 a, float3 b, float3 c)
      {
        min = new float3(
          Math.Min(Math.Min(a.x, b.x), c.x),
          Math.Min(Math.Min(a.y, b.y), c.y),
          Math.Min(Math.Min(a.z, b.z), c.z));
        max = new float3(
          Math.Max(Math.Max(a.x, b.x), c.x),
          Math.Max(Math.Max(a.y, b.y), c.y),
          Math.Max(Math.Max(a.z, b.z), c.z));
      }

    }

    public struct float4x3
    {
      public float _11, _12, _13;
      public float _21, _22, _23;
      public float _31, _32, _33;
      public float _41, _42, _43;
      public override int GetHashCode()
      {
        return base.GetHashCode();
      }
      public override bool Equals(object p)
      {
        return p is float4x3 && !((float4x3)p != this);
      }
      public float3 mx { get => new float3(_11, _12, _13); set { _11 = value.x; _12 = value.y; _13 = value.z; } }
      public float3 my { get => new float3(_21, _22, _23); set { _21 = value.x; _22 = value.y; _23 = value.z; } }
      public float3 mz { get => new float3(_31, _32, _33); set { _31 = value.x; _32 = value.y; _33 = value.z; } }
      public float3 mp { get => new float3(_41, _42, _43); set { _41 = value.x; _42 = value.y; _43 = value.z; } }
      public float3 this[int i]
      {
        get => i == 0 ? mx : i == 1 ? my : i == 2 ? mz : mp;
        set { if (i == 0) mx = value; else if (i == 1) my = value; else if (i == 2) mz = value; else mp = value; }
      }
      public float3 Scaling
      {
        get => new float3(mx.Length, my.Length, mz.Length);
        set
        {
          mx *= value.x / mx.Length;
          my *= value.y / my.Length;
          mz *= value.z / mz.Length;
        }
      }
      public static bool operator ==(in float4x3 a, in float4x3 b)
      {
        return !(a != b);
      }
      public static bool operator !=(in float4x3 a, in float4x3 b)
      {
        return a._11 != b._11 || a._12 != b._12 || a._13 != b._13 || //a._14 != b._14 ||
               a._21 != b._21 || a._22 != b._22 || a._23 != b._23 || //a._24 != b._24 || 
               a._31 != b._31 || a._32 != b._32 || a._33 != b._33 || //a._34 != b._34 ||  
               a._41 != b._41 || a._42 != b._42 || a._43 != b._43;//|| a._44 != b._44;
                                                                  //for (int i = 0; i < 12; i++) if ((&a._11)[i] != (&b._11)[i]) return true; return false;
      }
      public static float4x3 Identity => 1;
      public static implicit operator float4x3(float s)
      {
        return new float4x3() { _11 = s, _22 = s, _33 = s };
      }
      public static implicit operator float4x3(float2 p)
      {
        float4x3 m; *(float*)&m = m._22 = m._33 = 1; *(float2*)&m._41 = p; return m;
      }
      public static implicit operator float4x3(float3 p)
      {
        float4x3 m; *(float*)&m = m._22 = m._33 = 1; *(float3*)&m._41 = p; return m;
      }
      public static float4x3 operator !(in float4x3 p)
      {
        //inv(&v, &v); return v;
        var b0 = p._31 * p._42 - p._32 * p._41;
        var b1 = p._31 * p._43 - p._33 * p._41;
        var b3 = p._32 * p._43 - p._33 * p._42;
        var d1 = p._22 * p._33 + p._23 * -p._32;
        var d2 = p._21 * p._33 + p._23 * -p._31;
        var d3 = p._21 * p._32 + p._22 * -p._31;
        var d4 = p._21 * b3 + p._22 * -b1 + p._23 * b0;
        var de = p._11 * d1 - p._12 * d2 + p._13 * d3; de = 1f / de; //if (det == 0) throw new Exception();
        var a0 = p._11 * p._22 - p._12 * p._21;
        var a1 = p._11 * p._23 - p._13 * p._21;
        var a3 = p._12 * p._23 - p._13 * p._22;
        var d5 = p._12 * p._33 + p._13 * -p._32;
        var d6 = p._11 * p._33 + p._13 * -p._31;
        var d7 = p._11 * p._32 + p._12 * -p._31;
        var d8 = p._11 * b3 + p._12 * -b1 + p._13 * b0;
        var d9 = p._41 * a3 + p._42 * -a1 + p._43 * a0; float4x3 r;
        r._11 = +d1 * de; r._12 = -d5 * de;
        r._13 = +a3 * de;
        r._21 = -d2 * de; r._22 = +d6 * de;
        r._23 = -a1 * de;
        r._31 = +d3 * de; r._32 = -d7 * de;
        r._33 = +a0 * de;
        r._41 = -d4 * de; r._42 = +d8 * de;
        r._43 = -d9 * de; return r;
      }
      public static float4x3 operator *(in float4x3 a, in float2 b)
      {
        var c = a; c._41 += b.x; c._42 += b.y; return c;
      }
      public static float4x3 operator *(in float4x3 a, in float3 b)
      {
        var c = a; c._41 += b.x; c._42 += b.y; c._43 += b.z; return c;
        //return a * (float4x3)b;
      }
      public static float4x3 operator *(in float4x3 a, in float4x3 b)
      {
        float x = a._11, y = a._12, z = a._13; float4x3 r;
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
      public static float3 operator *(float3 a, in float4x3 b)
      {
        float3 c;
        c.x = b._11 * a.x + b._21 * a.y + b._31 * a.z + b._41;
        c.y = b._12 * a.x + b._22 * a.y + b._32 * a.z + b._42;
        c.z = b._13 * a.x + b._23 * a.y + b._33 * a.z + b._43;
        return c;
      }
      public static explicit operator string(in float4x3 m)
      {
        return string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}",
          XmlConvert.ToString(m._11), XmlConvert.ToString(m._12), XmlConvert.ToString(m._13),
          XmlConvert.ToString(m._21), XmlConvert.ToString(m._22), XmlConvert.ToString(m._23),
          XmlConvert.ToString(m._31), XmlConvert.ToString(m._32), XmlConvert.ToString(m._33),
          XmlConvert.ToString(m._41), XmlConvert.ToString(m._42), XmlConvert.ToString(m._43));
      }
      public static explicit operator float4x3(string ss)
      {
        float4x3 m; scan(ss, (float*)&m, 12); return m;
      }
    }

    public static float4x3 LookAtLH(float3 eye, float3 pos, float3 up)
    {
      var v1 = (pos - eye).Normalize();
      var v2 = (up ^ v1).Normalize();
      var v3 = v1 ^ v2;
      eye = -eye;
      var d1 = v2 & eye;
      var d2 = v3 & eye;
      var d3 = v1 & eye;
      float4x3 m;
      m._11 = v2.x; m._12 = v3.x; m._13 = v1.x;
      m._21 = v2.y; m._22 = v3.y; m._23 = v1.y;
      m._31 = v2.z; m._32 = v3.z; m._33 = v1.z;
      m._41 = d1; m._42 = d2; m._43 = d3;
      return m;
    }
    public static float4x3 Translation(float x, float y, float z)
    {
      float4x3 m; (&m)->_11 = m._22 = m._33 = 1; m._41 = x; m._42 = y; m._43 = z; return m;
    }
    public static float4x3 Scaling(float x, float y, float z)
    {
      float4x3 m; (&m)->_11 = x; m._22 = y; m._33 = z; return m;
    }
    public static float4x3 Scaling(float f)
    {
      float4x3 m; (&m)->_11 = m._22 = m._33 = f; return m;
    }
    public static float4x3 Scaling(float3 s)
    {
      return Scaling(s.x, s.y, s.z);
    }
    public static float4x3 RotationX(double a)
    {
      var sc = new float2(a); var m = new float4x3();
      m._11 = 1; m._22 = m._33 = sc.x; m._32 = -(m._23 = sc.y); return m;
    }
    public static float4x3 RotationY(double a)
    {
      var sc = new float2(a); var m = new float4x3();
      m._22 = 1; m._11 = m._33 = sc.x; m._13 = -(m._31 = sc.y); return m;
    }
    public static float4x3 RotationZ(double a)
    {
      var sc = new float2(a); var m = new float4x3();
      m._33 = 1; m._11 = m._22 = sc.x; m._21 = -(m._12 = sc.y); return m;
    }
    public static float4x3 RotationAxis(float3 v, float a)
    {
      var sc = new float2(a); float s = sc.y, c = sc.x, cc = 1 - c;
      var m = new float4x3();
      m._11 = cc * v.x * v.x + c;
      m._21 = cc * v.x * v.y - s * v.z;
      m._31 = cc * v.x * v.z + s * v.y;
      m._12 = cc * v.y * v.x + s * v.z;
      m._22 = cc * v.y * v.y + c;
      m._32 = cc * v.y * v.z - s * v.x;
      m._13 = cc * v.z * v.x - s * v.y;
      m._23 = cc * v.z * v.y + s * v.x;
      m._33 = cc * v.z * v.z + c;
      return m;
    }

    static void scan(string s, float* pp, int np)
    {
      for (int a = 0, b, n = s.Length, c = 0; c < np; c++, a = b)
      {
        for (; a < n && s[a] <= ' '; a++) ;
        for (b = a; b < n && s[b] > ' '; b++) ;
        pp[c] = XmlConvert.ToSingle(s.Substring(a, b - a));
      }
    }

  }
}
