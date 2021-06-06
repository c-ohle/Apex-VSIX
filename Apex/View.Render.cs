using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Apex.CDX;

namespace Apex
{
  unsafe partial class CDXView
  {
    //IFont font = GetFont(new System.Drawing.Font("Tahoma", 13));
    IFont font = GetFont(System.Drawing.SystemFonts.MenuFont);
    IBuffer checkboard;
    List<string> debuginfo;
    
    void ISink.Render(int fl)
    {
      var dc = new DC(view); dc.Font = font;
      if (fl == 0)
      {
        if ((flags & 1) != 0) //Checkboard
        {
          if (checkboard == null) checkboard = GetTexture(512, 512, 1, gr =>
          {
            //for (int i = 0; i < 10; i++)
            //{
            //  var l = i * 100; var d = i == 0 ? 20 : i == 5 ? 10 : 5;
            //  gr.FillRectangle(System.Drawing.Brushes.White, 0, l, 1000, d);
            //  gr.FillRectangle(System.Drawing.Brushes.White, l, 0, d, 1000);
            //}
            gr.FillRectangle(System.Drawing.Brushes.White, 0, 0, 512, 3);
            gr.FillRectangle(System.Drawing.Brushes.White, 0, 0, 3, 512);
          });
          dc.Transform = 1; dc.Color = 0xfe000000;// 0xfe000000;
          var t1 = dc.Texture; dc.Texture = checkboard;
          dc.Mapping = 0.1f; dc.FillRect(-10000, -10000, 20000, 20000); dc.Texture = t1;
        }
        tool?.Invoke(4);
        for (int i = 0, n = scene.SelectionCount; i < n; i++)
        {
          var p = scene.GetSelection(i); if (p == view.Camera) continue;
          var f = Node.From(this, p).GetMethod<Action<DC>>();
          if (f != null) try { f(dc); } catch (Exception e) { System.Diagnostics.Debug.WriteLine(e.Message); }
        }
        return;
      }
      if (fl == 1)
      {
        if (true)
        {
          dc.SetOrtographic();

          //dc.Color = 0x80ffffff; dc.FillRect(8, 8, Width - 16, 24);
          //dc.Color = 0x80000000; dc.DrawText(16, 24, "File"); dc.DrawText(100, 24, "Edit");

          dc.Color = 0xff000000;
          float y = 10 + font.Ascent, dy = font.Height, x = ClientSize.Width - 10f;
          if (debuginfo != null) for (int i = 0; i < debuginfo.Count; i++) dc.DrawText(10, y + i * dy, debuginfo[i]);
          var s = (Factory.Version & 0x100) != 0 ? "Debug" : "Release"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          s = $"Dpi: {view.Dpi}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          s = $"Buffer {Factory.GetInfo(2)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          s = $"Vertexbuffer {Factory.GetInfo(0)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          s = $"Indexbuffer {Factory.GetInfo(1)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          s = $"Textures {Factory.GetInfo(3)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          s = $"Fonts {Factory.GetInfo(4)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          s = $"Views {Factory.GetInfo(5)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          //s = $"Over {view.MouseOverNode?.Name} id 0x{view.MouseOverId:x4} {view.MouseOverPoint}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
          //s = $"Capture {Capture}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
        }
        //animate();
      }
#if (false)
      var dc = new DC(view);
      var scene = view.Scene;

      if (true)
      {
        for (int a = -1, b; (b = scene.Select(a, 1)) != -1; a = b)
        {
          var p = scene[b]; if (!(p.Tag is XNode xp)) continue;
          var draw = xp.GetMethod<Action<DC>>(); if (draw == null) continue;
          dc.Transform = p.GetTransform(null);
          try { DC.icatch = b + 1; draw(dc); } catch (Exception e) { Debug.WriteLine(e.Message); }
        }
      }

      if (true)
      {
        if (checkboard == null) checkboard = GetTexture(256, 256, 1, gr =>
        {
          gr.FillRectangle(Brushes.White, 0, 0, 256, 1);
          gr.FillRectangle(Brushes.White, 0, 0, 1, 256);
        });
        dc.Transform = 1;
        dc.Color = 0xfe000000;
        var t1 = dc.Texture; dc.Texture = checkboard;
        dc.Mapping = 1; dc.FillRect(-100, -100, 200, 200); dc.Texture = t1;
      }

      var infos = XScene.From(scene).Infos;
      if (infos.Count != 0)
      {
        dc.SetOrtographic(); dc.Font = font; dc.Color = 0xff000000;
        float y = 10 + font.Ascent, dy = font.Height;
        for (int i = 0; i < infos.Count; i++, y += dy) dc.DrawText(10, y, infos[i]);
      }
      if (true)
      {
        dc.SetOrtographic();
        dc.Font = font; dc.Color = 0xff000000;
        float y = 10 + font.Ascent, dy = font.Height, x = ClientSize.Width - 10f;
        var s = $"Vertexbuffer {Factory.GetInfo(0)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
        s = $"Indexbuffer {Factory.GetInfo(1)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
        s = $"Mappings {Factory.GetInfo(2)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
        s = $"Textures {Factory.GetInfo(3)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
        s = $"Fonts {Factory.GetInfo(4)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
        s = $"Views {Factory.GetInfo(5)}"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
      }
#endif
    }
    
    void ISink.Animate(INode p, uint t)
    {
      var node = p.Tag as Node ?? Node.From(this, p);
      try { node.GetMethod<Action<uint>>()?.Invoke(t); }
      catch (Exception e) { Debug.WriteLine(e.Message); }
    }
    void ISink.Reslove(object p, COM.IStream s)
    {
      long t1; s.Seek(-4, 2, &t1); int t2; s.Read(&t2, 4);
      if (((t2 >> 16) & 0xffff) != 0xC066) return;
      if ((t2 &= 0xffff) > t1) return;
      var a = new byte[(int)t1 - t2];
      s.Seek(t2, 0); fixed (byte* t = a) s.Read(t, a.Length);
      var uri = System.Text.Encoding.UTF8.GetString(a);

      var wcl = new System.Net.WebClient();
      wcl.DownloadDataCompleted += (x, e) =>
      {
        if (e.Error != null) { System.Diagnostics.Debug.WriteLine(e.Error.Message); return; }
        var tex = (IBuffer)e.UserState; var data = e.Result;
        fixed (byte* t = data) tex.Update(t, data.Length); Invalidate();
      };
      wcl.DownloadDataAsync(new Uri(uri), p);
      //try 
      //{
      //  var wcl = new System.Net.WebClient();
      //  var data = wcl.DownloadData(uri);
      //  s.Seek(0, 0); fixed (byte* pp = data) s.Write(pp, data.Length); s.Seek(0, 0);
      //  //var data2 = wcl.DownloadData(uri+".png");
      //} catch(Exception e) { System.Diagnostics.Debug.WriteLine(e.Message); }
    }
  }
}
