using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static csg3mf.CDX;


namespace csg3mf
{
  unsafe partial class CDXView : UserControl
  {
    protected override void OnMouseDown(MouseEventArgs e)
    {
      if (tool != null || view == null || view.Camera == null) return;
      Focus();
      var main = mainover();
      if (main != null && main.Tag is Node n)
      {
        var ft = n.GetMethod<Func<IView, Action<int>>>();
        if (ft != null) try { tool = ft(view); } catch { }
      }
      if (tool == null)
      {
        var k = ModifierKeys | (
          e.Button == MouseButtons.Left ? Keys.LButton :
          e.Button == MouseButtons.Right ? Keys.RButton :
          e.Button == MouseButtons.Middle ? Keys.MButton : 0);
        var f = main == null || main.IsStatic ? ToolFlags.GroundClick : ToolFlags.ObjectClick;
        var a = ToolProvider.kvs; int i = 0; for (; i < a.Length && !(a[i].k == k && a[i].f == f); i++) ;
        if (i < a.Length) tool = id2tool(a[i].i, main);
        if (tool == null && main != null && main.IsStatic && k == (Keys.LButton | Keys.Control | Keys.Alt | Keys.Shift)) { main.Select(); Invalidate(Inval.Select); }
      }
      if (tool == null) return; Capture = true; Invalidate();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
      //Debug.WriteLine(Capture);
      if (tool != null) { tool(0); Invalidate(); return; }
      var id = view != null ? view.MouseOverId : 0;
      Cursor =
        (id & 0x1000) != 0 ? Cursors.Cross :
        (id & 0x8000) != 0 ? Cursors.UpArrow :
        Cursors.Default;
      //Debug.WriteLine(view.MouseOverNode + " " + view.MouseOverPoint);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
      if (tool == null) return;
      var t = tool; tool = null; Capture = false; t(1); Invalidate(Inval.Properties);
    }
    protected override void OnLostFocus(EventArgs e)
    {
      if (tool == null) return;
      tool(1); tool = null; Invalidate();
    }

    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e) { }
    protected override void OnKeyPress(KeyPressEventArgs e) { }
    protected override void OnKeyDown(KeyEventArgs e)
    {
      if (tool != null || view == null || view.Camera == null) return;
      var main = mainover();
      var k = e.KeyCode | e.Modifiers;
      var f = main != null && main.IsSelect ? ToolFlags.TouchpadSelection | ToolFlags.TouchpadAlways : ToolFlags.TouchpadAlways | ToolFlags.TouchpadGround;
      var a = ToolProvider.kvs; int i = 0; for (; i < a.Length && !(a[i].k == k && (a[i].f & f) != 0); i++) ;
      if (i < a.Length) tool = id2tool(a[i].i, main);
      if (tool != null) { Capture = true; Invalidate(); }
      //var k = e.KeyData;
      ////var num=IsKeyLocked(Keys.NumLock); if(num) { }
      //var caps = IsKeyLocked(Keys.CapsLock); //if (caps) { }
      //if (caps) { k |= Keys.Shift; }
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
      if (tool == null) return;
      if ((MouseButtons & (MouseButtons.Left | MouseButtons.Right | MouseButtons.Middle)) != 0) return;
      OnMouseUp(null);
    }

    void OnScroll(int xdelta, int ydelta)
    {
      if (tool != null) return;
      var t = Environment.TickCount;
      if (t - lastzoom < 250) return;
      var m = view.Camera.Transform;
      for (int i = 0; i < 5; i++)
      {
        view.Camera.Transform = xdelta != 0 ?
          m * -m.mp * RotationZ(i * xdelta * -0.00004f) * m.mp :
          RotationX(i * ydelta * -0.00004f) * m;
        Invalidate(); Update();
      }
      if (t - lastwheel > 500) AddUndo(undo(view.Camera, m)); lastwheel = t;
    }
    void OnMouseWheel(int delta)
    {
      if (tool != null) return;
      var t = Environment.TickCount;
      var wp = overwp();
      var m = view.Camera.Transform;
      var v = wp - m.mp;
      var l = v.Length;
      var d = (v.Normalize() * (l * 0.1f * delta * (1f / 120)));
      for (int i = 0; i < 5; i++)
      {
        view.Camera.Transform = m * (d * (i * 0.2f));
        Invalidate(); Update();
      }
      if (t - lastwheel > 500) AddUndo(undo(view.Camera, m)); lastwheel = lastzoom = t;
    }
    static int lastwheel, lastzoom;

    class CollCtrl
    {
      CDXView view; IView iview; (int nsel, int count) inf; bool ansch; int cnt;
      float3 dir, midp, lastok, overwp;

      static bool test = true;
      float3 last; float3box testbox; float4x3 testmat;

      (int nsel, int count) setbox(bool on)
      {
        (int nsel, int count) info; info.nsel = on ? 1 : 0; info.count = 0;
        iview.Command(Cmd.BoxesSet, &info.nsel); return info;
      }
      float3box getbox(int i)
      {
        float3box box; ((int*)&box)[0] = i;
        iview.Command(Cmd.BoxesGet, &box); return box;
      }
      float4x3 gettrans(int i)
      {
        float4x3 m; ((int*)&m)[0] = i;
        iview.Command(Cmd.BoxesTra, &m); return m;
      }
      float collision(float4x3* m, int i)
      {
        var info = (i, new IntPtr(m));
        iview.Command(Cmd.BoxesInd, &info.i);
        return *(float*)&info.i;
      }

      internal CollCtrl(CDXView view, IScene scene)
      {
        this.view = view; iview = view.view; overwp = view.overwp();
        inf = setbox(true);
      }
      internal void Move(ref float4x3 m)
      {
        if (inf.count == 0) return;
        var infos = view.debuginfo ?? (view.debuginfo = new System.Collections.Generic.List<string>());
        infos.Clear();

        var mp = m.mp; if (test) last = mp;
        var u = ansch ? (dir != default ? dir : (dir = mp - lastok)) : mp - lastok;
        if (u == default) return;
        //infos.Add($"u: {u}");

        var shift = 0f; if (test) { testmat = 0; testbox = float3box.Empty; }
        for (int ia = 0; ia < inf.nsel; ia++)
        {
          var selbox = getbox(ia); //var orgselbox = selbox;
          var xbox = selbox; xbox += mp;//
          selbox += lastok; selbox.Extend(mp - lastok); if (test) testbox = selbox;
          for (int ib = inf.nsel; ib < inf.count; ib++)
          {
            var unselbox = getbox(ib);
            if ((xbox & unselbox).IsEmpty) continue;
            var intbox = selbox & unselbox; //if (intbox.IsEmpty) continue;
            intbox = intbox.Inflate(0.1f);
            //testbox = intbox;
            var um = LookAtLH(intbox.mid + u, intbox.mid, Math.Abs(u.z) < 0.7f ? new float3(0, 0, 1) : new float3(1, 0, 0));
            var xb = float3box.Empty; for (int i = 0; i < 8; i++) xb.Add(intbox.Corner(i) * um);
            xb.min.z -= xb.size.z * 2;// xb.size.z;// orgselbox.size.x;//.Length;// xb.size.z;
            var xbsize = xb.size; xbsize.z = 1 / xbsize.z;
            int dx = 256, dy = 256;
            //var xm = Scaling(dx / xbsize.x, dy / xbsize.y, 1) * new float3(dx * 0.5f, dy * 0.5f, -xb.min.z) * Scaling(1, 1, xbsize.z);
            var xm = new float4x3 { _11 = dx / xbsize.x, _22 = dy / xbsize.y, _33 = xbsize.z, _41 = dx * 0.5f, _42 = dy * 0.5f, _43 = -xb.min.z * xbsize.z };
            um *= xm;

            var ma = gettrans(ia) * mp * um;
            var mb = gettrans(ib) * um;
            collision(null, dx | (dy << 16));
            collision(&mb, ib);
            var dz = collision(&ma, ia | (1 << 31));
            if (dz != 0)
            {
              var v = (mp - lastok) - u * (dz / xbsize.z / u.Length);
              //infos.Add($"u {u}");
              if ((u & v) > 0)// || (u - v).LengthSq < u.LengthSq)//u.LengthSq * 0.5f)
                shift = Math.Max(shift, dz / xbsize.z);
            }
            if (test) testmat = !um;
          }
        }
        if (u.z < 0)
        {
          //info.Add("ztest");
          for (int i = 0; i < inf.nsel; i++)
          {
            var box = getbox(i); //box += lastok. + mp;// lastok;
            var z1 = box.min.z; if (z1 < 0) continue;
            var z2 = z1 + mp.z; if (z2 > 0) continue;
            shift = Math.Max(shift, -z2 * mp.Length / (z1 - z2));
          }
        }
        if (ansch = shift != 0) { m.mp -= u * (shift / u.Length); }
        else
        {
          if (u.LengthSq > dir.LengthSq)
          {
            dir = u;
            if (cnt == 10) midp = mp;
            if (cnt++ == 20) { lastok = midp; cnt = 0; dir = mp - lastok; }
          }
          else { dir = default; lastok = mp; cnt = 0; }
        }
      }
      internal void Draw()
      {
        if (!test) return;
        if (inf.count == 0) return;
        var dc = new DC(view.view);
        dc.Transform = 1;
        dc.Color = 0xff000000;
        //dc.DrawLine(overwp, overwp + last);

        dc.DrawLine(overwp + last, overwp + lastok);//.Normalize() * 30);
        if (!testbox.IsEmpty)
        {
          dc.Color = 0xffff0000;
          dc.DrawBox(testbox);
        }
        if (testmat.mx != default)
        {
          int dx = 256, dy = 256;
          dc.Transform = testmat;
          dc.Color = 0x80080000;
          dc.DrawBox(new float3box { min = default, max = new float3(dx, dy, 1) });
          dc.Color = 0x40080000;
          dc.FillRect(0, 0, dx, dy);
          dc.Color = 0xffff0000; dc.DrawLine(default, new float3(dx, 0, 0));
          dc.Color = 0xff00ff00; dc.DrawLine(default, new float3(0, dy, 0));
          //dc.Color = 0xff0000ff; dc.DrawLine(default, new float3(0, 0, 2));
        }
      }
      internal void End() => setbox(false);
    }

    Action<int> tool;
    INode mainselect()
    {
      var sc = scene.SelectionCount;
      return sc != 0 ? scene.GetSelection(sc - 1) : null;
    }
    INode mainover()
    {
      var p = view.MouseOverNode; if (p == null) return null;
      for (INode t; !p.IsSelect && (t = p.Parent as INode) != null; p = t) ; return p;
    }
    float3 overwp()
    {
      var p = view.MouseOverPoint; var t = view.MouseOverNode;
      if (t != null) p *= t.GetTransform(null); return p;
    }

    Action<int> id2tool(ToolEnum id, INode main)
    {
      switch (id)
      {
        case ToolEnum.SelectRect: return tool_select();
        case ToolEnum.CameraMoveHorizontal: return camera_movxy();
        case ToolEnum.CameraMoveVertical: return camera_movz();
        case ToolEnum.CameraRotateHorizontal: return camera_rotz(null);
        case ToolEnum.CameraRotateVerical: return camera_rotx();
        case ToolEnum.CameraRotateDirectional: return camera_free2();
        case ToolEnum.CameraMoveXAxis: return camera_movxy2(1);
        case ToolEnum.CameraMoveYAxis: return camera_movxy2(2);
        case ToolEnum.CameraMoveZAxis: return camera_movz();  //CameraMoveVertical
        case ToolEnum.CameraSelectionRotateHorizontal: return camera_rotz(mainselect());
        case ToolEnum.CameraSelectionRotateVertical: break;
        case ToolEnum.ObjectMoveHorizontal: return obj_movxy(main);
        case ToolEnum.ObjectMoveVertical: return obj_movz(main);
        case ToolEnum.ObjectDragDrop: return obj_drag(main);
        case ToolEnum.ObjectRotateHorizontal: return obj_rotz(main);
        case ToolEnum.ObjectMoveXAxis: return obj_movxy2(main, 1);
        case ToolEnum.ObjectMoveYAxis: return obj_movxy2(main, 2);
        case ToolEnum.ObjectMoveZAxis: return obj_movxy2(main, 3);
        case ToolEnum.ObjectRotateXAxis: return obj_rot(main, 0);
        case ToolEnum.ObjectRotateYAxis: return obj_rot(main, 1);
        case ToolEnum.ObjectRotateZAxis: return obj_rot(main, 2);
      }
      return null;
    }

    Action<int, float4x3> getmover(bool coll)
    {
      var pp = view.Scene.Selection().ToArray();
      var um = pp.Select(p => p.GetTypeTransform(0)).ToArray();
      var mm = um.Any(p => *(int*)&p._33 == 0x7f000001) ? pp.Select(p => p.Transform).ToArray() : um;
      var cc = coll && (flags & 2) != 0 ? new CollCtrl(this, view.Scene) : null;
      return (id, m) =>
      {
        if (id == 0)
        {
          cc?.Move(ref m);
          for (int i = 0; i < pp.Length; i++) pp[i].Transform = mm[i] * m;
        }
        if (id == 2) { AddUndo(undo(pp.Select((p, i) => undo(p, um[i])))); cc?.End(); }
        if (id == 4) cc?.Draw();
      };
    }

    Action<int> camera_free2()
    {
      var camera = view.Camera; var m = camera.GetTransform(); Cursor = Cursors.SizeAll;
      view.SetPlane(m * m.mz); var p1 = view.PickPlane(); var p2 = p1; //var mover = move(camera);
      return id =>
      {
        if (id == 0)
        {
          p2 = view.PickPlane();
          camera.Transform = RotationX(Math.Atan(p2.y) - Math.Atan(p1.y)) * m *
          -m.mp * RotationZ(Math.Atan(p1.x) - Math.Atan(p2.x)) * m.mp;
        }
        if (id == 1) AddUndo(undo(camera, m));
      };
    }
    //Action<int> camera_free()
    //{
    //  var mb = float4x3.Identity; view.Command(Cmd.GetBox, &mb);
    //  var boxmin = *(float3*)&mb._11; var boxmax = *(float3*)&mb._22;
    //  var pm = (boxmin + boxmax) * 0.5f; var tm = (float4x3)pm;
    //  var cm = view.Camera.Transform; var p1 = (float2)Cursor.Position; bool moves = false;
    //  return id =>
    //  {
    //    if (id == 0)
    //    {
    //      var v = (Cursor.Position - p1) * -0.01f;
    //      if (!moves && v.LengthSq < 0.03) return; moves = true;
    //      view.Camera.Transform = cm * !tm * RotationAxis(cm.mx, -v.y) * RotationZ(v.x) * tm;
    //    }
    //    if (id == 1) AddUndo(undo(view.Camera, cm));
    //  };
    //}
    Action<int> camera_movxy2(int fl)
    {
      var wp = overwp(); view.SetPlane(new float3(0, 0, wp.z));
      var p1 = view.PickPlane(); var p2 = p1;
      var camera = view.Camera; var m = camera.Transform;
      return id =>
      {
        if (id == 0)
        {
          p2 = view.PickPlane(); var d = p1 - p2;
          if ((fl & 1) == 0) d.x = 0;
          if ((fl & 2) == 0) d.y = 0;
          camera.Transform = m * d;
        }
        if (id == 1) AddUndo(undo(camera, m));
        if (id == 4)
        {
          if ((fl & 3) == 3) return;
          var dc = new DC(view); dc.Transform = wp;
          if ((fl & 1) != 0) { dc.Color = 0x80ff0000; dc.DrawLine(new float2(-1000, 0), new float2(+1000, 0)); }
          if ((fl & 2) != 0) { dc.Color = 0x8000aa00; dc.DrawLine(new float2(0, -1000), new float2(0, +1000)); }
          float3 tm = default; dc.DrawPoints(&tm, 1);
        }
      };
    }
    Action<int> camera_movxy()
    {
      var wp = overwp(); view.SetPlane(new float3(0, 0, wp.z));
      var p1 = view.PickPlane(); var p2 = p1;
      var camera = view.Camera; var m = camera.Transform;
      return id =>
      {
        if (id == 0) { p2 = view.PickPlane(); camera.Transform = m * (p1 - p2); }
        if (id == 1) AddUndo(undo(camera, m));
      };
    }
    Action<int> camera_movz()
    {
      var camera = view.Camera; var m = camera.Transform; var wp = overwp();
      var wm = (float4x3)wp; wm.mx = (wm.my = new float3(0, 0, 1)) ^ (wm.mz = (m.mp - wp).Normalize());
      view.SetPlane(wm);
      var p1 = view.PickPlane(); var p2 = p1; //var mover = move(camera);
      return id =>
      {
        if (id == 0) { p2 = view.PickPlane(); camera.Transform = m * new float3(0, 0, p1.y - p2.y); }
        if (id == 1) AddUndo(undo(camera, m));
        if (id == 4)
        {
          var dc = new DC(view); dc.Transform = wm;
          //dc.Color = 0x80808080; dc.FillRect(0, 0, 40, 20);
          dc.Color = 0x800000ff; dc.DrawLine(new float2(0, -100), new float2(0, +100));
          float3 tm = default; dc.DrawPoints(&tm, 1);
        }
      };
    }
    Action<int> camera_rotz(INode prot)
    {
      var cam = view.Camera; var m = cam.Transform; Cursor = Cursors.SizeWE;
      var rot = (prot ?? cam).GetTransform(null).mp;
      var wp = overwp(); var mp = new float3(rot.x, rot.y, wp.z);
      view.SetPlane(mp);
      var p1 = view.PickPlane(); var p2 = p1; var a1 = p1.Angel; //var mover = move(camera);
      return id =>
      {
        if (id == 0) { p2 = view.PickPlane(); cam.Transform = m * -rot * RotationZ(a1 - view.PickPlane().Angel) * rot; }
        if (id == 1) AddUndo(undo(cam, m));
        if (id == 4)
        {
          if (prot == null) return;
          var dc = new DC(view); dc.Transform = mp;// dc.Plane;          
          dc.Color = 0x800000ff; var r = p1.Length;
          dc.DrawLine(new float3(0, 0, -100), new float3(0, 0, +100));
          var pp = (new float3(), (float3)p1);
          dc.DrawPoints(&pp.Item1, 2);
          dc.DrawCirc(default, r); dc.Color = 0x080000ff;
          dc.FillCirc(default, r);

        }
      };
    }
    Action<int> camera_rotx()
    {
      var camera = view.Camera; var m = camera.GetTransform(); Cursor = Cursors.SizeNS;
      view.SetPlane(m * m.mz); var p1 = view.PickPlane(); var p2 = p1; //var mover = move(camera);
      return id =>
      {
        if (id == 0) { p2 = view.PickPlane(); camera.Transform = RotationX(Math.Atan(p2.y) - Math.Atan(p1.y)) * m; }
        if (id == 1) AddUndo(undo(camera, m));
      };
    }
    Action<int> tool_select()
    {
      var wp = overwp(); view.SetPlane(wp = new float3(0, 0, wp.z + 0.1f));
      var p1 = view.PickPlane(); var p2 = p1;
      return id =>
      {
        if (id == 0) { p2 = view.PickPlane(); }
        if (id == 4)
        {
          var dc = new DC(view); dc.Transform = wp; var dp = p2 - p1;
          dc.Color = 0x808080ff; dc.FillRect(p1.x, p1.y, dp.x, dp.y);
          dc.Color = 0xff8080ff; dc.DrawRect(p1.x, p1.y, dp.x, dp.y);
        }
        if (id == 1)
        {
          if (p1 == p2) { view.Scene.Select(); return; }
          //if (p1.x > p2.x) { var o = p1.x; p1.x = p2.x; p2.x = o; }
          //if (p1.y > p2.y) { var o = p1.y; p1.y = p2.y; p2.y = o; }
          float4 t; ((float2*)&t)[0] = p1; ((float2*)&t)[1] = p2;
          view.Command(Cmd.SelectRect, &t);
          Invalidate(Inval.Select);
        }
      };
    }

    Action<int> obj_movxy2(INode main, int fl)
    {
      var ws = main.IsSelect; if (!ws) { main.Select(); Invalidate(Inval.Select); }
      var wm = main.GetTransform(null);
      var wp = overwp();
      var cm = view.Camera.GetTransform(null); fl &= 3;
      var v = fl == 1 ? wm.mx : fl == 2 ? wm.my : wm.mz;
      wm = wp; wm.mx = v; wm.mz = (cm.mp - wp).Normalize(); wm.my = wm.mx ^ wm.mz;
      view.SetPlane(wm); var lm = wm; if (main.Parent is INode pn) lm *= !pn.GetTransform(null); //var im = !lm;
      var p1 = view.PickPlane(); var p2 = p1; var dp = new float2();
      Action<int, float4x3> mover = null;
      return id =>
      {
        if (id == 0)
        {
          dp = (p2 = view.PickPlane()) - p1;
          if (mover == null && dp == default) return;
          if (fl != 0) dp.y = 0; var d = (!lm * dp * lm).mp;
          //d = new float3(fl == 1 ? d.x : 0, fl == 2 ? d.y : 0, fl == 3 ? d.z : 0);
          (mover ?? (mover = getmover(true)))(0, d);
        }
        if (id == 1)
        {
          if (mover != null) mover(2, default);
          else if (ws) { main.Select(); Invalidate(Inval.Select); }
        }
        if (id == 4)
        {
          mover?.Invoke(4, default);
          var dc = new DC(view); dc.Transform = wm;
          //dc.Color = 0x80808080; dc.FillRect(0, 0, 40, 20);
          dc.Color = (fl & 3) == 1 ? 0x80ff0000 : (fl & 3) == 2 ? 0x8000aa00 : 0x800000ff;
          dc.DrawLine(new float2(-100, 0), new float2(+100, 0));
          var pp = (new float3(), (float3)dp);
          dc.DrawPoints(&pp.Item1, 2);
#if(DEBUG)
          var vm = view.Camera.GetTransform();
          var pw = (p1 + dp) * wm;
          pw -= (pw - vm.mp).Normalize() * 50;
          var d = (vm.mp - pw).Length; var f = -2 * view.Projection;
          vm.mp = default;
          dc.Transform = Scaling(d * f) * vm * pw;
          var s = $"dx: {dp.x} mm";
          var ds = dc.GetTextExtent(s); var fo = dc.Font; var r = fo.Descent * 0.5f;
          dc.Color = 0x80ffffff; dc.FillRect(0, 20, ds.x + 4 * r, ds.y + 2 * r);
          dc.Color = 0xff000000; dc.DrawText(r * 2, 20 + fo.Ascent + r, s);
#endif
        }
      };
    }
    Action<int> obj_movxy(INode main)
    {
      var ws = main.IsSelect; if (!ws) { main.Select(); Invalidate(Inval.Select); }
      var me = (float4x3)(view.MouseOverPoint * view.MouseOverNode.GetTransform(main.Parent as INode));
      if (main.Parent is INode pn) me = me * pn.GetTransform(null);
      view.SetPlane(me); var p1 = view.PickPlane();
      Action<int, float4x3> mover = null;
      return id =>
      {
        if (id == 0)
        {
          var p2 = view.PickPlane(); var dp = p2 - p1;//).Round(4);
          if (mover == null && dp == default) return;
          (mover ?? (mover = getmover(true)))(0, dp);
        }
        if (id == 1)
        {
          if (mover != null) mover(2, default);
          else if (ws) main.Select();
        }
        if (id == 4)
        {
          //var dc = new DC(view); dc.Transform = me;
          //dc.Color = 0x80ffffff; dc.FillRect(0, 0, 40, 20);
          mover?.Invoke(4, default);
        }
      };
    }
    Action<int> obj_movz(INode main)
    {
      var ws = main.IsSelect; if (!ws) { main.Select(); Invalidate(Inval.Select); }

      var box = GetBox(scene.Selection());
      var miz = box.min.z; var lov = miz != float.MaxValue ? miz : 0;
      var boxz = lov; var ansch = Math.Abs(boxz) < 0.1f;
      var min = view.ToolScale * 0.5f;
      var cm = view.Camera.GetTransform();

      var op = view.MouseOverPoint * view.MouseOverNode.GetTransform(main.Parent as INode);
      var me = main.Parent is INode pn ? pn.GetTransform(null) : 1;

      cm = cm * !me;
      var wp = op;// overwp();
      var wm = (float4x3)wp;
      wm.mx = (wm.my = new float3(0, 0, 1)) ^ (wm.mz = (cm.mp - wp).Normalize());

      wm = wm * me;

      view.SetPlane(wm); var p1 = view.PickPlane(); var p2 = p1; var mover = getmover(true);
      return id =>
      {
        if (id == 0)
        {
          p2 = view.PickPlane(); if (p2 == p1) return; var dz = p2.y - p1.y;// (float)Math.Round(p1.y - p2.y, 4);
          if (miz != float.MaxValue) { var ov = boxz + dz; if (ov < 0 && ov > -min && lov >= 0) { if (!ansch) { ansch = true; } dz = -boxz; ov = 0; } else ansch = false; lov = ov; }
          mover(0, Translation(0, 0, dz));
        }
        if (id == 1) mover(2, default);
        if (id == 4)
        {
          mover?.Invoke(4, default);
          var dc = new DC(view); dc.Transform = wm;
          //dc.Color = 0x80ffffff; dc.FillRect(0, 0, 40, 20);
          dc.Color = 0x800000ff;
          dc.DrawLine(new float2(0, -100), new float2(0, +100)); //dc.DrawLine(p2.Normalize() * l, p2);
          var pp = (new float3(), new float3(0, p2.y - p1.y, 0));
          dc.DrawPoints(&pp.Item1, 2);
        }
      };
    }
    Action<int> obj_rotz(INode main)
    {
      var ws = main.IsSelect; if (!ws) { main.Select(); Invalidate(Inval.Select); }
      var wp = view.MouseOverPoint * view.MouseOverNode.GetTransform(main.Parent as INode);
      var lm = main.Parent is INode pn ? pn.GetTransform() : 1;
      var mw = main.Transform; var mp = mw.mp; mp.z = wp.z; var me = (float4x3)mp * lm;
      var v = Math.Abs(mw._13) > 0.8f ? new float2(mw._21, mw._22) : new float2(mw._11, mw._12);
      var w = v.Angel;
      view.SetPlane(me); lm = mp;
      var p1 = view.PickPlane(); var p2 = p1; var a1 = p1.Angel;
      var mover = getmover(false); var raster = angelstep();
      return id =>
      {
        if (id == 0)
        {
          var a2 = (p2 = view.PickPlane()).Angel; var rw = raster(w + a2 - a1);
          mover(0, !lm * RotationZ(rw - w) * lm);
        }
        if (id == 1) mover(2, default);
        if (id == 4)
        {
          var dc = new DC(view); dc.Transform = me;
          //dc.Color = 0x80ffffff; dc.FillRect(0, 0, 40, 20);
          dc.Color = 0x800000ff; var r = p1.Length;
          dc.DrawLine(new float3(0, 0, -100), new float3(0, 0, +100));
          var pp = (new float3(), (float3)p1, (float3)(p2.Normalize() * r));
          dc.DrawPoints(&pp.Item1, 3);
          dc.DrawCirc(default, r); dc.Color = 0x080000ff;
          dc.FillCirc(default, r);
        }
      };

    }
    Action<int> obj_rot(INode main, int xyz)
    {
      if (!main.IsSelect) { main.Select(); Invalidate(Inval.Select); }
      var wp = overwp();
      var wm = main.GetTransform(); //main.Parent as INode); 
      for (int i = 0; i < 3; i++) wm[i] /= wm[i].Length; //wm.mx /= wm.mx.Length; wm.my /= wm.my.Length; wm.mz /= wm.mz.Length;
      var op = wp * !wm; //main.GetTransform();
      var me = (float4x3)new float3(xyz == 0 ? op.x : 0, xyz == 1 ? op.y : 0, xyz == 2 ? op.z : 0) * wm;
      initme(wm, ref me, xyz, xyz == 0 ? 1 : xyz == 1 ? 0 : 0, xyz == 0 ? 2 : xyz == 1 ? 2 : 1);
      if (float.IsNaN(me._11)) return null; var im = !me;
      var a0 = ((wm.mp + wm[xyz == 0 ? 1 : xyz == 1 ? 0 : 0]) * im).xy.Angel; var rw = a0;
      view.SetPlane(me); im = me; if (main.Parent is INode pn) { var t = pn.GetTransform(); im *= !t; }
      var p1 = view.PickPlane(); var p2 = p1;
      var a1 = p1.Angel; var a2 = a1; var angelgrid = angelstep(); var mover = getmover(false);
      return (id) =>
      {
        if (id == 0)
        {
          a2 = (p2 = view.PickPlane()).Angel;
          rw = a0 + a2 - a1; //Debug.WriteLine(rw);
          rw = Math.Round(rw * (180 / Math.PI), 1) * (Math.PI / 180);
          rw = angelgrid(rw);
          mover(0, !im * RotationZ(rw - a0) * im);
          var tm = main.Transform;
          if (tm.mp != wm.mp) { } //tm.mp = wm.mp; main.Transform = tm; }
        }
        if (id == 1) mover(2, default);
        if (id == 4)
        {
          mover?.Invoke(4, default);
          var dc = new DC(view); dc.Transform = me;
          //dc.Color = 0x80808080; dc.FillRect(0, 0, 40, 20);
          var l = (wp - me.mp).Length; l *= 1.01f;
          dc.Color = xyz == 0 ? 0xffff0000 : xyz == 1 ? 0xff00aa00 : 0xff0000ff;
          //var v = new float2(rw - a0 + a1);
          //dc.DrawLine(v * l, v * p2.Length); //dc.DrawLine(p2.Normalize() * l, p2);
          var pp = (new float3(), (float3)p1, (float3)(p2.Normalize() * l));
          dc.DrawPoints(&pp.Item1, 3);
          dc.DrawCirc(default, l); dc.Color &= 0x0fffffff;
          dc.FillCirc(default, l);
          //if (false)
          //{
          //  var tm = main.Transform;
          //  var aw = ((tm.mp + tm[xyz == 0 ? 1 : xyz == 1 ? 0 : 0]) * !me).xy.Angel;
          //  dc.SetOrtographic();
          //  dc.Transform = new float2(32, 32);
          //  dc.Color = 0xff000000;
          //  dc.DrawText(0, 0, $"{rw * (180 / Math.PI):0.##} °");
          //
          //  dc.DrawText(0, 30, $"a0: {a0 * (180 / Math.PI)} °");
          //  dc.DrawText(0, 50, $"rw: {rw * (180 / Math.PI)} °");
          //  dc.DrawText(0, 70, $"aw: {aw * (180 / Math.PI)} °");
          //  dc.DrawText(0, 90, $"euler: {double3.Euler(tm) * (180 / Math.PI)} °");
          //}
        }
      };
    }

    static void initme(in float4x3 wm, ref float4x3 me, int a, int b, int c)
    {
      me[0] = wm[(a + 1) % 3]; me[1] = wm[(a + 2) % 3]; me[2] = wm[a];

      if (Math.Abs(wm[a].x) < 1e-5f && Math.Abs(wm[a].y) < 1e-5f) //if (Math.Abs(wm[a].z) > 0.9999f)
      {
        me[1] = wm[a] ^ (me[0] = new float3(1, 0, 0));
        return;
      }
      if (Math.Abs(wm[c].z) > Math.Abs(wm[b].z))
      {
        var f = -wm[b].z / wm[c].z;
        me[1] = me[2] ^ (me[0] = (wm[b] + wm[c] * f).Normalize());
      }
      else
      {
        var f = -wm[c].z / wm[b].z;
        me[1] = me[2] ^ (me[0] = -(wm[c] + wm[b] * f).Normalize()); c = b;
      }
      if (wm[c].z < 0) { me[0] = -me[0]; me[1] = -me[1]; me[2] = -me[2]; }
    }

    Action<int> obj_drag(INode main)
    {
      var ws = main.IsSelect; if (!ws) { main.IsSelect = true; Invalidate(Inval.Select); }
      var wp = overwp(); var p1 = (float2)Cursor.Position;
      return id =>
      {
        if (id == 0)
        {
          var p2 = (float2)Cursor.Position; if ((p2 - p1).LengthSq < 10 /* * DpiScale*/) return;
          //if (!ws) { main.Select(); selchange(); } ws = false; 
          if (!AllowDrop) return;
          var path = Path.Combine(Path.GetTempPath(), string.Join("_", (main.Name ?? main.GetClassName()).Trim().Split(Path.GetInvalidFileNameChars())) + ".3mf");
          try
          {
            var str = COM.SHCreateMemStream();
            view.Thumbnail(256, 256, 4, 0x00fffffe, str);
            view.Scene.Export3MF(path, str, wp, null);
            var data = new DataObject(); data.SetFileDropList(new System.Collections.Specialized.StringCollection { path });
            DoDragDrop(data, DragDropEffects.Copy);
          }
          catch (Exception e) { Debug.WriteLine(e.Message); }
          finally { try { File.Delete(path); } catch (Exception t) { Debug.WriteLine(t.Message); } }
        }
        if (id == 1 && ws) main.SetSelect(false);
      };
    }
    protected override void OnDragEnter(DragEventArgs e)
    {
      var files = e.Data.GetData(DataFormats.FileDrop) as string[]; object data;
      if (files != null && files.Length == 1) { var s = files[0]; if (!s.EndsWith(".3mf", true, null)) return; data = s; }
      else { var t = e.Data.GetData(typeof(ToolboxItem)) as ToolboxItem; if (t == null) return; data = new MemoryStream(t.data); }
      IScene drop; float3 wp;
      try { drop = Import3MF(data, out wp); } catch { return; }
      var scene = view.Scene;
      //if (scene.Unit != drop.Unit)
      //{
      //  var f = (float)(scaling(drop.Unit) / scaling(scene.Unit));
      //  Scale(drop, f); if (!float.IsNaN(wp.x)) wp *= f;
      //}
      var pp = drop.Nodes().ToArray(); while (drop.Child != null) drop.RemoveAt(0);
      if (float.IsNaN(wp.x))
      {
        var box = GetBox(pp); if (box.IsEmpty) return;
        wp = (box.min + box.max) * 0.5f; wp.z = box.min.z;
      }
      var del = undo(pp.Select((p, x) => undodel(p, scene, scene.Count + x)).ToArray());
      del();
      var mm = pp.Select(p => p.Transform).ToArray();
      view.Command(Cmd.SetPlane, null); view.SetPlane(wp);
      tool = id =>
      {
        if (id == 0)
        {
          var p2 = view.PickPlane(); var dm = (float4x3)p2;
          for (int i = 0; i < pp.Length; i++) pp[i].Transform = mm[i] * dm; Invalidate();
        }
        if (id == 1)
        {
          scene.Select(); for (int i = 0; i < pp.Length; i++) pp[i].IsSelect = true;
          AddUndo(del); Invalidate(Inval.Tree | Inval.Select);
        }
        if (id == 2) { del(); Invalidate(Inval.Render); }
      };
    }
    protected override void OnDragOver(DragEventArgs e)
    {
      if (tool == null) { e.Effect = DragDropEffects.None; return; }
      tool(0); e.Effect = DragDropEffects.Copy;
    }
    protected override void OnDragDrop(DragEventArgs e)
    {
      if (tool == null) { e.Effect = DragDropEffects.None; return; }
      tool(1); tool = null; e.Effect = DragDropEffects.Copy;
    }
    protected override void OnDragLeave(EventArgs e)
    {
      if (tool == null) return;
      tool(2); tool = null;
    }

    static Func<double, double> angelstep()
    {
      var seg1 = 0.0; var hang = 0; var count = 0; var len = Math.PI / 4; var modula = 2 * Math.PI;
      return val =>
      {
        var seg2 = Math.Floor(val / len); var len2 = len * 0.33f;
        if (0 == count++) seg1 = seg2;
        if (seg2 == seg1) { hang = 0; return val; }
        if (Math.Abs(seg2 * len - val) < len2) { if (hang != 1) { hang = 1; /*Program.play(30);*/ } return seg2 * len; }
        var d = seg1 * len - val; if (modula != 0) d = Math.IEEERemainder(d, modula); //d % modula;// 
        if (Math.Abs(d) < len2) { val = seg1 * len; if (hang != 2) { hang = 2; /*Program.play(30);*/ } } else { seg1 = seg2; hang = 0; }
        return val;
      };
    }

  }
}