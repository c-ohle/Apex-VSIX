using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Apex.CDX;
#pragma warning disable VSTHRD010

namespace Apex
{
  [Flags]
  enum Inval { Render = 1, Select = 2, Tree = 4, PropertySet = 8, Properties = 16, }

  unsafe partial class CDXView : UserControl, ISink, System.IServiceProvider, ISelectionContainer
  {
    //~CDXView() { Debug.WriteLine("~CDXView"); }

    //protected override void Dispose(bool disposing)
    //{
    //  //base.Dispose(disposing);
    //  if (!disposing) return;
    //  scene = null; undos = null;
    //    //foreach (var p in scene.Nodes()) p.Tag = null;
    //    //Marshal.ReleaseComObject(scene); scene = null;
    //  
    //}

    internal CDXWindowPane pane;
    IScene scene; IView view; static long drvsettings = 0x400000000;
    static int sflags = 2; //1:Init 2:ToolsAssist 
    int flags = 1; //1:Checkboard 2:Collisions 4:Buffers 0x40000000:Tip

    void initview()
    {
      var reg = Application.UserAppDataRegistry;
      if ((sflags & 1) == 0)
      {
        if (reg.GetValue("sfl") is int i) sflags = i; sflags |= 1;
        if (reg.GetValue("drv") is long v) drvsettings = v;
        Factory.SetDevice((uint)drvsettings);
        Trace.Listeners.Add(new Listner());
      }
      view = Factory.CreateView(Handle, this, (uint)(drvsettings >> 32));
      view.BkColor = 0xffcccccc;
      view.Render = (RenderFlags)reg.GetValue("fl",
        (int)(RenderFlags.BoundingBox | RenderFlags.Coordinates | RenderFlags.Wireframe | RenderFlags.Shadows))
        | RenderFlags.ZPlaneShadows;
      view.Scene = scene; var defcam = scene.Camera;
      if (defcam == null)
      {
        defcam = Factory.CreateNode(); defcam.Name = "(default)";
        defcam.Transform = !LookAtLH(new float3(-3, -6, 3), default, new float3(0, 0, 1));
        var c1 = new BUFFERCAMERA { fov = 50, near = 1, far = 10000, minz = -1 };
        defcam.SetProp("@cfov", c1);
        //defcam.SetBufferPtr(BUFFER.CAMERA, &c1, sizeof(BUFFERCAMERA));
        view.Camera = defcam; scene.Camera = defcam;
        var c2 = new BUFFERCAMERA { fov = 100, near = 1 }; view.Command(Cmd.Center, &c2);
        if (c2.near < c1.near || c2.far > c1.far)
        {
          var box = GetBox(scene.Nodes());
          var m = Translation(-box.mid.x, -box.mid.y, -box.min.z) * Scaling(500 / box.size.Length);
          foreach (var p in scene.Nodes()) p.Transform *= m;
          box = GetBox(scene.Nodes());
          c2 = new BUFFERCAMERA { fov = 100, near = 1 }; view.Command(Cmd.Center, &c2);
        }
      }
      else
      {
        view.Camera = scene.Tag as INode ?? defcam; scene.Tag = null;
      }
      inval |= Inval.Tree | Inval.Select; //-> PropertyGrid
    }

    class Listner : TraceListener
    {
      public override void WriteLine(string s) => Write(s + '\n');
      public override void Write(string s)
      {
        //var dte = (EnvDTE.DTE)this.pane.GetVsService(typeof(EnvDTE.DTE));
        //var window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
        //window.Activate();
        //var pane = ((EnvDTE.OutputWindow)window.Object).OutputWindowPanes.Cast<EnvDTE.OutputWindowPane>().FirstOrDefault(p =>
        //  new Guid(p.Guid) == Microsoft.VisualStudio.VSConstants.GUID_OutWindowDebugPane);
        //if (pane != null) { pane.Activate(); pane.OutputString(s); }
        var wnd = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
        var guid = Microsoft.VisualStudio.VSConstants.GUID_OutWindowDebugPane;
        wnd.GetPane(ref guid, out var pane);
        pane.Activate(); pane.OutputString(s);
      }
    }

    internal void LoadDocData(string path)
    {
      if (path.EndsWith(".b3mf", true, null))
      {
        var str = COM.Stream(File.ReadAllBytes(path));
        (scene = Factory.CreateScene()).LoadFromStream(str);
        Marshal.ReleaseComObject(str);
      }
      else
      {
        scene = Import3MF(path, out _);
      }
    }
    internal void Save(string path)
    {
      var str = COM.SHCreateMemStream();
      if (path.EndsWith(".b3mf", true, null))
      {
        //foreach (var p in scene.SelectNodes(BUFFER.SCRIPT)) p.FetchBuffer();
        scene.SaveToStream(str, view.Camera);
        File.WriteAllBytes(path, str.ToArray());
        return;
      }
      view.Thumbnail(256, 256, 4, 0x00ffffff, str);
      scene.Export3MF(path, str, null, view.Camera);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
      base.OnHandleCreated(e);
    }
    protected override void OnHandleDestroyed(EventArgs e)
    {
      if (view != null) { Marshal.ReleaseComObject(view); view = null; }
      scene = null; undos = null; tip.dispose();
      base.OnHandleDestroyed(e);
    }
    protected override void OnSizeChanged(EventArgs e)
    {
      //if (view != null) System.Diagnostics.Debug.WriteLine("OnSizeChanged with view " + Size);
      if (view == null) initview();
    }
    protected override void WndProc(ref Message m)
    {
      switch (m.Msg)
      {
        case 0x020A: //WM_MOUSEWHEEL
          { var w = m.WParam.ToInt32(); if ((w & 0x8) != 0) OnMouseWheel(w >> 15); else OnScroll(0, w >> 16); return; }
        case 0x020E: //WM_MOUSEWHEEL2
          { OnScroll(m.WParam.ToInt32() >> 16, 0); return; }
        case 0x007B: // WM_CONTEXTMENU
          ShowContextMenu(this, 0x2100, m.LParam); return;
      }
      base.WndProc(ref m);
    }

    int ISelectionContainer.CountObjects(uint dwFlags, out uint pc)
    {
      var sc = scene.SelectionCount; if (sc == 0) { pc = 1; return 0; }
      pc = (uint)sc; return 0;
    }
    int ISelectionContainer.GetObjects(uint dwFlags, uint cObjects, object[] apUnkObjects)
    {
      var sc = scene.SelectionCount;
      //if (sc == 0) { apUnkObjects[0] = new CExchange(); return 0; }
      //if (sc == 0) { apUnkObjects[0] = Node.From(this, view.Camera); return 0; }
      if (sc == 0) { apUnkObjects[0] = scene.Tag ?? (scene.Tag = new Scene { view = this }); return 0; }
      for (int i = 0, n = Math.Min(apUnkObjects.Length, sc); i < n; i++)
        apUnkObjects[i] = Node.From(this, scene.GetSelection(i));
      return 0;
    }
    int ISelectionContainer.SelectObjects(uint cSelect, object[] apUnkSelect, uint dwFlags)
    {
      if (apUnkSelect.Length == 1 && apUnkSelect[0] is Node node)
      {
        node.node.Select(); Invalidate(Inval.Select);
      }
      return 0;
    }
    internal Node unisel() => scene.SelectionCount == 1 ? Node.From(this, scene.GetSelection(0)) : null;

    Inval inval;
    internal void Invalidate(Inval f)
    {
      if (inval == 0) Invalidate(); inval |= f;
    }

    void ISink.Timer()
    {
      tiptimer();
      if (inval == 0) return;
      var f = inval; inval = 0;
      if ((f & (Inval.Tree | Inval.Select | Inval.PropertySet)) != 0)
      {
        if (pane.treeview != null) pane.treeview.inval();
        pane.GetService<STrackSelection, ITrackSelection>()?.OnSelectChange(this);
      }
      else if ((f & (Inval.Properties)) != 0)
      {
        pane.GetService<SVsUIShell, IVsUIShell>()?.RefreshPropertyBrowser(-1);
      }
    }

    public bool IsModified { get => undoi != 0; set { undos = null; undoi = 0; } }

    List<Action> undos; int undoi;
    internal void AddUndo(Action p)
    {
      if (p == null) return;
      if (undos == null) undos = new List<Action>();
      //for (int i = undoi; i < undos.Count; i++) undos[i] = null;
      undos.RemoveRange(undoi, undos.Count - undoi);
      undos.Add(p); undoi = undos.Count;
    }
    internal void Execute(Action p)
    {
      if (p == null) return;
      p(); AddUndo(p); Invalidate(Inval.Properties);
    }
    internal int undopos() => undoi;
    internal static Action undo(INode p, float4x3 m)
    {
      if (m == p.GetTypeTransform(0)) return null;
      return () => { var t = p.GetTypeTransform(0); p.SetTypeTransform(0, m); m = t; };
    }
    internal static Action undo(IEnumerable<Action> a)
    {
      var b = a.OfType<Action>().ToArray(); if (b.Length == 0) return null;
      if (b.Length == 1) return b[0];
      return () => { for (int i = 0; i < b.Length; i++) b[i](); Array.Reverse(b); };
    }
    internal static Action undo(INode n, BUFFER id, IBuffer p)
    {
      if (n.GetBuffer(id) == p) return null;
      return () => { var t = n.GetBuffer(id); n.SetBuffer(id, p); p = t; };
    }
    internal static Action undo<T>(INode node, BUFFER id, T[] b) where T : unmanaged
    {
      void* pd; var nd = node.GetBufferPtr(id, &pd) / sizeof(T);
      if (nd == (b != null ? b.Length : 0))
        fixed (void* t = b) if (Native.memcmp(t, pd, (void*)(nd * sizeof(T))) == 0) return null;
      return () => { var t = node.GetArray<T>(id); node.SetArray(id, b); b = t; };
    }
    internal static Action undo(INode node, BUFFER id, void* p, int n)
    {
      var a = new byte[n]; fixed (byte* t = a) Native.memcpy(t, p, (void*)n);
      return undo(node, id, a);
    }
    internal Action undo(params Action[] a)
    {
      if (Array.IndexOf(a, null) != -1) a = a.OfType<Action>().ToArray();
      if (a.Length == 1) return a[0];
      return () => { for (int i = 0; i < a.Length; i++) a[i](); Array.Reverse(a); };
    }
    Action undosel(bool sel, params INode[] a)
    {
      if (a.Length == 0) a = scene.Selection().ToArray();
      return () => { scene.Select(sel ? a : Array.Empty<INode>()); sel = !sel; Invalidate(Inval.Tree); };
    }
    Action undodel(INode p, object root = null, int i = 0)
    {
      var r = root as IRoot;
      return () =>
      {
        if (r == null)
        {
          //if (p.Tag is Node n) n.onremove();
          (r = p.Parent).RemoveAt(i = p.Index);
        }
        else
        {
          r.InsertAt(i, p); r = null;
          //if (p.Tag is Node n) n.oninsert();
        }
      };
    }

    static int ctxon;
    void ShowContextMenu(Control wnd, int id, IntPtr lParam)
    {
      var l = lParam.ToInt32();
      if (l == -1)
      {
        if (Environment.TickCount - ctxon < 100) return;
        var p = wnd.PointToScreen(new System.Drawing.Point());
        ((short*)&l)[0] = (short)p.X;
        ((short*)&l)[1] = (short)p.Y;
      }
      else if (wnd == this)
      {
        var p = mainover();
        if (p != null && !p.IsSelect) { p.Select(); Invalidate(Inval.Select); }
      }
      var shell = pane.GetService<SVsUIShell, IVsUIShell>();
      var guid = Guids.CmdSet;
      var pnts = new[] { new POINTS { x = ((short*)&l)[0], y = ((short*)&l)[1] } };
      shell.ShowContextMenu(0, ref guid, id, pnts, pane);
      ctxon = Environment.TickCount;
    }

    internal void MessageBox(string message) =>
      VsShellUtilities.ShowMessageBox(pane, message, "Error", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

    object System.IServiceProvider.GetService(Type serviceType)
    {
      return pane.GetVsService(serviceType);
    }

    string[] samples; static string[] driver;

    class Scene : NodeBase, ICustomTypeDescriptor
    {
      protected override void Exchange(IExchange e)
      {
        if (e.Category("General"))
        {
          var t1 = view.scene.Unit; if (e.Exchange("Unit", ref t1)) view.scene.Unit = t1;
        }
        if (e.Category("View"))
        {
          var t2 = System.Drawing.Color.FromArgb((int)view.view.BkColor);
          if (e.Exchange("BkColor", ref t2)) view.view.BkColor = (uint)t2.ToArgb();
          var p = view.view.Camera;
          e.TypeConverter(typeof(CamConv));
          if (e.Exchange("Camera", ref p)) { view.view.Camera = p; }

          var cd = p.GetProp<BUFFERCAMERA>("@cfov"); e.GetModified();
          e.DisplayName("Fov"); e.Description("Field Of View in Degree"); e.Exchange(".fo", ref cd.fov);
          e.DisplayName("Range"); e.Description("Depth Near- and Farplane"); e.Exchange(".ra", ref *(float2*)&cd.near);
          if (e.GetModified()) p.SetProp("@cfov", cd);
        }
      }
      string ICustomTypeDescriptor.GetClassName() => GetType().Name;
      string ICustomTypeDescriptor.GetComponentName() => "3MF";
      PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
      {
        var pdc = wpdc.Value; if (pdc == null) GetProps(wpdc.Value = pdc = new PropertyDescriptorCollection(null));
        return pdc;
      }
      WeakRef<PropertyDescriptorCollection> wpdc;

      class CamConv : TypeConverter
      {
        INode[] pp; string[] ss;
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => true;
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
          if (value is INode p)
          {
            if (pp != null) { var i = Array.IndexOf(pp, p); if (i != -1 && p.Name == ss[i]) return ss[i]; }
            return p.Name;
          }
          return "(default)";
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
          if (value is string s)
          {
            for (int i = 0; i < ss.Length; i++) if (ReferenceEquals(ss[i], s)) return pp[i];
            for (int i = 0; i < ss.Length; i++) if (ss[i] == s) return pp[i];
          }
          return null;
        }
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
          var view = ((Scene)context.Instance).view;
          pp = Enumerable.Repeat(view.scene.Camera, 1). //tp != null && tp.Scene == null ? tp : null, 1).
            Concat(view.scene.Descendants().Where(p => p.HasProp("@cfov"))).ToArray();
          ss = pp.Select(p => p.Name).ToArray();
          return new StandardValuesCollection(pp);
        }
      }

    }

  }

}

