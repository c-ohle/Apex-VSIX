using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Apex.CDX;
#pragma warning disable VSTHRD010

namespace Apex
{
  unsafe partial class CDXView
  {
    public int OnCommand(int id, object test)
    {
      if (view == null) return -1;
      switch (id)
      {
        case 2000: if (test == null) return 1; return 1; //F1 
        case 2010: //Undo
          if (undos == null || undoi == 0) return 8;
          if (test == null) { undos[undoi - 1](); undoi--; Invalidate(Inval.Properties); }
          return 1;
        case 2011: //Redo
          if (undos == null || undoi >= undos.Count) return 8;
          if (test == null) { undos[undoi](); undoi++; Invalidate(Inval.Properties); }
          return 1;
        case 2060: //SelectAll
          if (test != null) return 1;
          foreach (var p in view.Scene.Nodes()) p.IsSelect = !p.IsStatic; Invalidate(Inval.Select);
          return 1;
        //case 4020: return OnStatic(test);
        case 2020: return OnCut(test);
        case 2030: return OnCopy(test);
        case 2040: return OnPaste(test);
        case 2015: return OnDelete(test);
        case 2035: return OnGroup(test);
        case 2036: return OnUngroup(test);
        case 2037: return OnScript(test);
        case 2038: return OnNormalize(test);
        case 2039: return OnRetess(test);
        case 2300: return OnCenter(test);
        case 2305: return OnCheck(test);
        case 2210: //Select Box
        case 2211: //Select Pivot
        case 2212: //Select Normals
        case 2213: //Select Wireframe
        case 2214: //Select Outline
        case 2220: //Shadows
          if (test != null) return (view.Render & (CDX.RenderFlags)(1 << (id - 2210))) != 0 ? 3 : 1;
          view.Render ^= (CDX.RenderFlags)(1 << (id - 2210)); Application.UserAppDataRegistry.SetValue("fl", (int)view.Render);
          Invalidate(); return 1;
        case 2301: return OnJoin(test, id);//Union   
        case 2302: return OnJoin(test, id);//Intersection
        case 2303: return OnJoin(test, id);//Difference
        case 2304: return OnPlaneCut(test);//PlaneCut
        case 2320://Grid
          if (test != null) return (flags & 1) != 0 ? 3 : 1;
          flags ^= 1; return 1;
        case 2330://Collision
          if (test != null) return (flags & 2) != 0 ? 3 : 1;
          flags ^= 2; return 1;
        case 2221: //Tooltips
          if (test != null) return (flags & 4) != 0 ? 3 : 1;
          flags ^= 4; return 1;
        //case 2340: return OnTools(test);
        case 5100: //BringForward:
        case 5101: //SendBackward:
        case 5103: //SendToBack:  
        case 5102: //BringToFront
          return OnOrder(id, test);
        //case 5104: return 1; //InsertObject
        case 2755: //combo driver
        case 2756: return comboDriver(id, test);
        case 2757: //combo samples
        case 2758: return comboSamples(id, test);
      }
      //if (id >= 6200 && id <= 6250)
      //{
      //  return 1;
      //}
      return -1;
    }

    int comboDriver(int id, object test)
    {
      if (!(test is object[] a)) return 1;
      if (driver == null) driver = Factory.Devices.Split('\n');
      if (id == 2755) { a[1] = driver.Where((p, i) => i != 0 && (i & 1) == 0).ToArray(); return 1; }
      if (a[0] == null) { a[1] = driver[Array.IndexOf(driver, driver[0], 1) + 1]; return 1; }
      var x = (int)a[0];
      var di = uint.Parse(driver[1 + (x << 1)]); Factory.SetDevice(di); driver = null; samples = null;
      drvsettings = (drvsettings >> 32 << 32) | di;
      Application.UserAppDataRegistry.SetValue("drv", drvsettings, Microsoft.Win32.RegistryValueKind.QWord);
      return 1;
    }
    int comboSamples(int id, object test)
    {
      if (!(test is object[] a)) return 1;
      if (samples == null) samples = view.Samples.Split('\n');
      if (id == 2757) { a[1] = samples.Skip(1).ToArray(); return 1; }
      if (a[0] == null) { a[1] = samples[0]; return 1; }
      var s = (string)a[0]; view.Samples = s; samples = null;
      drvsettings = (drvsettings & 0xffffffff) | ((long)int.Parse(s) << 32);
      Application.UserAppDataRegistry.SetValue("drv", drvsettings, Microsoft.Win32.RegistryValueKind.QWord);
      return 1;
    }

    int OnDelete(object test)
    {
      if (scene.SelectionCount == 0) return 0;
      if (test != null) return 1;
      var a = scene.Selection();
      Execute(undo(
        undosel(false, a.ToArray()),
        undo(a.OrderBy(p => -p.Index).Select(p => undodel(p)).ToArray())));
      return 1;
    }
#if(false)    
    int OnJoinCS(object test, int id)
    {
      if (scene.SelectionCount != 2) return 0;
      var n1 = scene.GetSelection(0); if (!n1.HasBuffer(BUFFER.POINTBUFFER)) return 0;
      var n2 = scene.GetSelection(1); if (!n2.HasBuffer(BUFFER.POINTBUFFER)) return 0;
      if (test != null) return 1; //UseWaitCursor();
      Cursor = Cursors.WaitCursor;
      var r1 = Rational.Polyhedron.Create(n1);
      var r2 = Rational.Polyhedron.Create(n2);
      var rm = n2.GetTransform() * !n1.GetTransform();
      var vm = (Rational.Matrix)rm; r2.Transform(vm);
      if (id == 2301) r1 |= r2;
      else if (id == 2302) r1 &= r2;
      else r1 -= r2;
      var pp = r1.Points.Select(p => (float3)p).ToArray();
      var ii = r1.Indices.ToArray();
      IBuffer pb; fixed (void* p = pp) pb = Factory.GetBuffer(BUFFER.POINTBUFFER, p, pp.Length * sizeof(float3));
      IBuffer ib; fixed (void* p = ii) ib = Factory.GetBuffer(BUFFER.INDEXBUFFER, p, ii.Length * sizeof(ushort));
      var tb = n1.CopyCoords(pb, ib);
      Execute(undo(
        undosel(false, n1, n2),
        undo(n1, BUFFER.POINTBUFFER, pb.ToBytes()),
        undo(n1, BUFFER.INDEXBUFFER, ib.ToBytes()),
        undo(n1, BUFFER.TEXCOORDS, tb?.ToBytes()),
        undodel(n2), undosel(true, n1)));
      return 1;
    }
    int OnJoinFl(object test, int id)
    {
      if (scene.SelectionCount != 2) return 0;
      var n1 = scene.GetSelection(0); if (!n1.HasBuffer(BUFFER.POINTBUFFER)) return 0;
      var n2 = scene.GetSelection(1); if (!n2.HasBuffer(BUFFER.POINTBUFFER)) return 0;
      if (test != null) return 1;
      Cursor = Cursors.WaitCursor;
      var r1 = CSG.Factory.CreateMesh(); n1.CopyTo(r1);
      var r2 = CSG.Factory.CreateMesh(); n2.CopyTo(r2);
      var rm = n2.GetTransform() * !n1.GetTransform();
      var vm = (CSG.Rational.Matrix)rm; r2.Transform(vm);
      CSG.Tesselator.Join(r1, r2, id == 2301 ? CSG.JoinOp.Union : id == 2302 ? CSG.JoinOp.Intersection : CSG.JoinOp.Difference);
      CSG.Tesselator.Round(r1, CSG.VarType.Float);
      r1.CopyTo(out var pb, out var ib);
      Marshal.ReleaseComObject(r1);
      Marshal.ReleaseComObject(r2);
      var tb = n1.CopyCoords(pb, ib);
      Execute(undo(
        undosel(false, n1, n2),
        undo(n1, BUFFER.POINTBUFFER, pb.ToBytes()),
        undo(n1, BUFFER.INDEXBUFFER, ib.ToBytes()),
        undo(n1, BUFFER.TEXCOORDS, tb?.ToBytes()),
        undodel(n2), undosel(true, n1)));
      return 1;
    }
#endif
    //static CSG.IMesh BytesToMesh(byte[] a) { var m = CSG.Factory.CreateMesh(); m.ReadFromStream(COM.Stream(a)); return m; }
    static CSG.IMesh MeshFromNode(INode node)
    {
      var mesh = CSG.Factory.CreateMesh();
      void* s; var n = node.GetBufferPtr(BUFFER.CSGMESH, &s);
      if (n == 0) node.CopyTo(mesh);
      else { var str = COM.SHCreateMemStream(s, n); mesh.ReadFromStream(str); Marshal.ReleaseComObject(str); }
      return mesh;
    }
    static byte[] MeshToBytes(CSG.IMesh mesh)
    {
      var str = COM.SHCreateMemStream(); mesh.WriteToStream(str); return COM.Stream(str);
    }

    static int[] caladj(int[] a)
    {
      var b = new int[a.Length];
      var dict = new System.Collections.Generic.Dictionary<(int, int), int>(a.Length);
      for (int i = 0; i < a.Length; i++) dict[(a[i], a[i + (i % 3 == 2 ? -2 : 1)])] = i;
      for (int i = 0; i < a.Length; i++) b[i] = dict.TryGetValue((a[i + (i % 3 == 2 ? -2 : 1)], a[i]), out var k) ? k : -1;
      return b;
    }
    static void testmesh(CSG.IMesh mesh)
    {
      var vv = mesh.Vertices().ToArray();
      var ii = mesh.Indices().ToArray();
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

    static void MeshRound(ref float3[] pp, ref ushort[] ii)
    {
      for (int i = 0, j, k; i < ii.Length; i++)
      {
        var d = (pp[j = ii[i]] - pp[k = ii[i + (i % 3 == 2 ? -2 : 1)]]).LengthSq;
        if (d != 0 && d < 1e-10f) pp[j] = pp[k];// = (pp[j] + pp[k]) / 2;
      }
      var dict = new Dictionary<float3, ushort>(pp.Length); var ni = 0;
      for (int i = 0; i < ii.Length; i++)
      {
        if (!dict.TryGetValue(pp[ii[i]], out var k)) dict.Add(pp[ii[i]], k = (ushort)dict.Count);
        ii[ni++] = k; if (i % 3 != 2) continue;
        if (ii[ni - 3] != ii[ni - 2] && ii[ni - 2] != ii[ni - 1] && ii[ni - 1] != ii[ni - 3]) continue;
        ni -= 3;
      }
      Array.Resize(ref pp, dict.Count); dict.Keys.CopyTo(pp, 0);
      Array.Resize(ref ii, ni);
    }

    //static void MeshRound2(ref float3[] pp, ref int[] ii)
    //{
    //  var ad = new int[ii.Length];
    //  var dict = new System.Collections.Generic.Dictionary<(int, int), int>(ii.Length);
    //  for (int i = 0; i < ii.Length; i++) dict[(ii[i], ii[i + (i % 3 == 2 ? -2 : 1)])] = i;
    //  for (int i = 0; i < ii.Length; i++) ad[i] = dict.TryGetValue((ii[i + (i % 3 == 2 ? -2 : 1)], ii[i]), out var k) ? k : -1;
    //  var ee = new float4[ii.Length / 3];
    //  for (int i = 0, k = 0; i < ee.Length; i++, k += 3) ee[i] = PlaneFromPoints(pp[ii[k]], pp[ii[k + 1]], pp[ii[k + 2]]);
    //
    //  int wrong = 0; //var join = new System.Collections.Generic.List<int>(); 
    //  for (int i = 0; i < ii.Length; i++)
    //  {
    //    if (i > ad[i]) continue;
    //    var e1 = ee[i / 3]; var e2 = ee[ad[i] / 3];
    //    if (e1 != -e2) continue;
    //    wrong++;
    //    //var i1 = i / 3 * 3;
    //    //var i2 = ad[i] / 3 * 3;
    //    //var a1 = (pp[ii[i1 + 1]] - pp[ii[i1]] ^ pp[ii[i1 + 2]] - pp[ii[i1]]).Length;
    //    //var a2 = (pp[ii[i2 + 1]] - pp[ii[i2]] ^ pp[ii[i2 + 2]] - pp[ii[i2]]).Length;
    //    ref var p1 = ref pp[ii[i]]; ref var p2 = ref pp[ii[i + (i % 3 == 2 ? -2 : 1)]];
    //    //var dp = (p2 - p1).Length;
    //    p1 = p2;
    //  }
    //  if (wrong == 0) return;
    //
    //  var newii = new List<int>();
    //  var ptdict = new Dictionary<float3, int>(pp.Length);
    //  for (int i = 0; i < ii.Length; i++)
    //  {
    //    if (!ptdict.TryGetValue(pp[ii[i]], out var k)) ptdict.Add(pp[ii[i]], k = ptdict.Count);
    //    newii.Add(k); if (i % 3 != 2) continue; var x = newii.Count - 3;
    //    if (newii[x] != newii[x + 1] && newii[x + 1] != newii[x + 2] && newii[x + 2] != newii[x]) continue;
    //    newii.RemoveRange(x, 3);
    //  }
    //  pp = ptdict.Keys.ToArray();
    //  ii = newii.ToArray();
    //
    //}

    int OnJoin(object test, int id)
    {
      if (scene.SelectionCount != 2) return 0;
      var n1 = scene.GetSelection(0); if (!n1.HasBuffer(BUFFER.POINTBUFFER)) return 0;
      var n2 = scene.GetSelection(1); if (!n2.HasBuffer(BUFFER.POINTBUFFER)) return 0;
      if (test != null) return 1;
      Cursor = Cursors.WaitCursor;

      var r1 = MeshFromNode(n1);
      var r2 = MeshFromNode(n2);

      var rm = n2.GetTransform() * !n1.GetTransform();
      var vm = (CSG.Rational.Matrix)rm; r2.Transform(vm);
      CSG.Tesselator.Join(r1, r2, id == 2301 ? CSG.JoinOp.Union : id == 2302 ? CSG.JoinOp.Intersection : CSG.JoinOp.Difference);

      //var ro = MeshToBytes(r1);
      //CSG.Tesselator.Round(r1, CSG.VarType.Float);

      var vv = r1.GetVertices();
      var ii = r1.GetIndices();
      MeshRound(ref vv, ref ii);

      r1.Copy(vv, ii.Select(p => (int)p).ToArray()); testmesh(r1);
      r1.CopyTo(out var pb, out var ib);
      var tb = n1.CopyCoords(pb, ib);

      Execute(undo(
        undosel(false, n1, n2),
          //undo(n1, BUFFER.CSGMESH, ro),
          undo(n1, BUFFER.POINTBUFFER, vv),//pb.ToBytes()), //
          undo(n1, BUFFER.INDEXBUFFER, ii),//ib.ToBytes()), //
          undo(n1, BUFFER.TEXCOORDS, tb?.ToBytes()),
          undodel(n2), undosel(true, n1)));

      return 1;
    }
    int OnPlaneCut(object test)
    {
      if (scene.SelectionCount != 2) return 0;
      var n1 = scene.GetSelection(0); if (!n1.HasBuffer(CDX.BUFFER.POINTBUFFER)) return 0;
      if (test != null) return 1;
      Cursor = Cursors.WaitCursor;
      var n2 = scene.GetSelection(1);
      var rm = n2.GetTransform() * !n1.GetTransform();
      var e = CSG.Rational.Plane.FromPointNormal(rm.mp, rm.mz);
      var r1 = MeshFromNode(n1);// CSG.Factory.CreateMesh(); n1.CopyTo(r1);
      CSG.Tesselator.Cut(r1, e);
      var ro = MeshToBytes(r1);
      CSG.Tesselator.Round(r1, CSG.VarType.Float);
      r1.CopyTo(out var pb, out var ib); Marshal.ReleaseComObject(r1);
      var tb = n1.CopyCoords(pb, ib);
      Execute(undo(
        undo(n1, BUFFER.CSGMESH, ro),
        undo(n1, BUFFER.POINTBUFFER, pb.ToBytes()),
        undo(n1, BUFFER.INDEXBUFFER, ib.ToBytes()),
        undo(n1, BUFFER.TEXCOORDS, tb?.ToBytes())));
      return 1;
    }
    int OnRetess(object test)
    {
      if (scene.SelectionCount != 1) return 0;
      var n1 = scene.GetSelection(0);
      if (!n1.HasBuffer(BUFFER.POINTBUFFER)) return 0;
      if (n1.HasBuffer(BUFFER.CSGMESH)) return 0;
      if (test != null) return 1;
      Cursor = Cursors.WaitCursor;
      var r1 = MeshFromNode(n1);
      CSG.Tesselator.Join(r1, r1, 0);
       
      var ro = MeshToBytes(r1);
      CSG.Tesselator.Round(r1, CSG.VarType.Float);
      r1.CopyTo(out var pb, out var ib);
      var tb = n1.CopyCoords(pb, ib);
      Execute(undo(
        undo(n1, BUFFER.CSGMESH, ro),
        undo(n1, BUFFER.POINTBUFFER, pb.ToBytes()),
        undo(n1, BUFFER.INDEXBUFFER, ib.ToBytes()),
        undo(n1, BUFFER.TEXCOORDS, tb?.ToBytes())));
      return 1;
    }
    int OnCenter(object test)
    {
      //if (!view.Scene.Descendants().Any(p => p.Mesh != null && p.Mesh.VertexCount != 0)) return 0;
      if (test != null) return 1;
      var m = view.Camera.GetTypeTransform(0); // Transform;
      var data = new float4(100, 1, 1, 0);
      view.Command(view.Scene.Selection().Any() ? Cmd.CenterSel : Cmd.Center, &data);
      //todo: check nearfar
      AddUndo(undo(view.Camera, m)); Invalidate(); return 1;
    }
    int OnCut(object test)
    {
      if (OnCopy(test) == 0) return 0;
      return test == null ? OnDelete(test) : 1;
    }
    int OnCopy(object test)
    {
      if (scene.SelectionCount == 0) return 0;
      if (test != null) return 1;
      foreach (var p in scene.Selection().SelectMany(p => p.Descendants(true))) p.FetchBuffer();
      var str = COM.SHCreateMemStream();
      scene.SaveToStream(str, null); //long l1; str.Seek(0, 1, &l1); 
      var data = new DataObject();
      data.SetData("csg3mf", false, COM.Stream(str));
      Clipboard.SetDataObject(data, true);
      return 1;
    }
    int OnPaste(object test)
    {
      var data = Clipboard.GetDataObject();// as DataObject;
      if (data == null) return 0; int i = 0; string[] ss = null;
      if (data.GetDataPresent("csg3mf")) i = 1;
      if (i == 0 && data.GetDataPresent("System.Drawing.Bitmap"))//DeviceIndependentBitmap"))
        if (scene.SelectionCount == 1 && scene.GetSelection(0).HasBuffer(BUFFER.POINTBUFFER))
          i = 2;
      if (i == 0)
      {
        ss = data.GetData("FileName") as string[];
        if (ss != null && ss.Length == 1 && ss[0].EndsWith(".3mf", true, null)) i = 3;
      }
      if (i == 0)
      {
        //var tt = data.GetFormats();
      }
      if (i == 0) return 0;
      if (test != null) return 1;
      if (i == 1)
      {
        var a = data.GetData("csg3mf") as byte[]; if (a == null) return 0;
        var str = COM.Stream(a);
        var drop = Factory.CreateScene();
        drop.LoadFromStream(str);
        if (pane.treeview != null && pane.treeview.Focused && scene.SelectionCount == 1)
        { paste(drop, (IRoot)scene.GetSelection(0)); return 1; }
        paste(drop, (IRoot)scene);
      }
      else if (i == 2)
      {
        var bmp = data.GetData("System.Drawing.Bitmap") as System.Drawing.Bitmap;
        if (bmp == null) return 0;
        var str = new MemoryStream(); bmp.Save(str, System.Drawing.Imaging.ImageFormat.Png);
        var a = str.ToArray();
        Execute(undo(scene.GetSelection(0), BUFFER.TEXTURE, a));
      }
      else if (i == 3)
      {
        paste(Import3MF(ss[0], out _), (IRoot)scene);
      }
      return 1;
    }
    void paste(IScene drop, IRoot root)
    {
      var pp = drop.Nodes().ToArray(); drop.Clear();
      Execute(undo(undo(
        pp.Select((p, x) => undodel(p, root, root.Count + x)).ToArray()),
        undosel(true, pp)));
    }

    int OnScript(object test)
    {
      if (scene.SelectionCount != 1) return 0;
      if (test != null) return 1;
      var p = CDXPackage.Package.FindToolWindow(typeof(ScriptToolWindowPane), 0, true);
      if (p.Frame is Microsoft.VisualStudio.Shell.Interop.IVsWindowFrame f) f.Show();
      return 0;
    }
    int OnOrder(int id, object test)
    {
      if (pane.treeview == null || !pane.treeview.Focused) return -1;
      if (scene.SelectionCount != 1) return 0;
      var node = scene.GetSelection(0); var pa = node.Parent;
      var i = node.Index; var k = i; var c = pa.Count;
      switch (id)
      {
        case 5100: k = Math.Min(i + 1, c - 1); break; //BringForward:
        case 5101: k = Math.Max(i - 1, 0); break; //SendBackward:
        case 5102: k = c - 1; break; //BringToFront:
        case 5103: k = 0; break; //SendToBack:  
      }
      if (k == i) return 0;
      if (test != null) return 1;
      Execute(() => { var t = node.Index; node.Index = k; k = t; Invalidate(Inval.Tree); });
      return 1;
    }
    int OnGroup(object test)
    {
      var scene = view.Scene;
      if (scene.SelectionCount == 0) return 0;
      if (test != null) return 1;
      var a = scene.Selection();
      var box = GetBox(a); if (box.IsEmpty) return 1;
      var mp = (box.min + box.max) / 2;
      var gr = Factory.CreateNode();
      gr.Transform = Translation(mp.x, mp.y, box.min.z);
      Execute(undo(undosel(false, a.ToArray()),
        undo(a.OrderBy(p => -p.Index).Select(p => undodel(p)).
        Concat(a.Select(p => undo(p, p.Transform * !gr.Transform))).
        Concat(a.Select((p, i) => undodel(p, gr, i))).OfType<Action>().ToArray()),
        undodel(gr, scene, a.Min(p => p.Index)), undosel(true, gr)));
      return 1;
    }
    int OnUngroup(object test)
    {
      var scene = view.Scene;
      if (scene.SelectionCount == 0) return 0;
      var a = scene.Selection().Where(p => p.Child != null);
      if (!a.Any()) return 0; if (test != null) return 1;
      Execute(undo(
        undosel(false, a.ToArray()),
        undo(a.OrderBy(p => -p.Index).Select(p => undo(undodel(p),
        undo(
          p.Nodes().Reverse().Select(c => undodel(c)).
          Concat(p.Nodes().Select(c => undo(c, c.Transform * p.Transform))).
          Concat(p.Nodes().Select((c, ci) => undodel(c, (IRoot)scene, p.Index + ci))).
          OfType<Action>().ToArray()))).ToArray()),
        undosel(true, a.SelectMany(c => c.Nodes()).ToArray())));
      return 1;
    }
    int OnNormalize(object test)
    {
      if (scene.SelectionCount == 0) return 0;
      var subsel = scene.Selection().SelectMany(p => p.Descendants(true));
      if (!subsel.Any(p => p.Transform.Scaling != new float3(1, 1, 1))) return 0;
      if (test != null) return 1;
      Action act = null;
      foreach (var node in scene.Selection()) trans(node, 1);
      Execute(act);
      void trans(INode node, float4x3 sm)
      {
        var m = node.Transform * sm; var sc = m.Scaling;
        if (sc != new float3(1, 1, 1))
        {
          m.mx /= sc.x; m.my /= sc.y; m.mz /= sc.z;
          var pp = node.GetBytes(BUFFER.POINTBUFFER);
          if (pp != null)
          {
            fixed (byte* p = pp)
              for (int t = 0, n = pp.Length / sizeof(float3); t < n; t++)
                ((float3*)p)[t] *= sc;
            act += undo(node, BUFFER.POINTBUFFER, pp);
          }
        }
        act += undo(node, m);
        foreach (var p in node.Nodes()) trans(p, Scaling(sc));
      }
      return 1;
    }
    int OnCheck(object test)
    {
      if (scene.SelectionCount != 1) return 0;
      if (test != null) return 1; Cursor = Cursors.WaitCursor;
      var node = scene.GetSelection(0);
      var box = GetBox(scene.Selection(), node.Parent);
      var ss = $"Size: {box.max - box.min} {(ShortUnit)node.Scene.Unit}";
      int nc = 0, np = 0, ni = 0, pl = 0, eg = 0; CSG.MeshCheck checks = 0; double vol = 0, surf = 0;
      foreach (var p in node.Descendants(true))
      {
        nc++; void* t;
        if (!p.HasBuffer(BUFFER.POINTBUFFER)) { if (p.Child == null) eg++; continue; }
        np += p.GetBufferPtr(BUFFER.POINTBUFFER, &t) / sizeof(float3);
        ni += p.GetBufferPtr(BUFFER.INDEXBUFFER, &t) / sizeof(ushort);
        var mesh = CSG.Factory.CreateMesh(); p.CopyTo(mesh); mesh.InitPlanes();
        var check = mesh.Check(); if (check == 0) { /*mesh.InitPlanes();*/ pl += mesh.PlaneCount; }
        checks |= check; Marshal.ReleaseComObject(mesh); if (check == 0) { vol += p.GetVolume(); surf += p.GetSurface(); }
      }
      ss += '\n'; ss += $"{np} Vertices {ni / 3} Polygones Planes {pl} in {nc} Models.";
      if (checks != 0) { ss += '\n'; ss += $"Errors: {checks}"; }
      if (vol != 0) { ss += '\n'; ss += $"Volume: {vol} {(ShortUnit)scene.Unit}³ Surface: {surf} {(ShortUnit)scene.Unit}²"; }
      if (eg != 0) { { ss += '\n'; ss += $"Errors: {eg} Empty Groups"; } }
      VsShellUtilities.ShowMessageBox(pane, ss, "Check",
        OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
      return 1;
    }
    public enum ShortUnit { m = 1, cm = 2, mm = 3, μm = 4, ft = 5, @in = 6, }


    //static double scaling(Unit u)
    //{
    //  switch (u)
    //  {
    //    default:
    //    case Unit.meter: return 1;
    //    case Unit.centimeter: return 0.01;
    //    case Unit.millimeter: return 0.001;
    //    case Unit.micron: return 1e-6;
    //    case Unit.foot: return 0.3048;
    //    case Unit.inch: return 0.0254;
    //  }
    //}

  }
}
