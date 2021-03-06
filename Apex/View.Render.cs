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
    IBuffer checkboard; //List<string> debuginfo;

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
          var o = Node.From(this, p); var f = o.GetMethod<Action<DC>>();
          if (f != null) try { f(dc); } catch (Exception e) { o.disable(f); Debug.WriteLine(e.Message); }

          //if (p.Name == "test")
          //{
          //  float3* pp; var np = p.GetBufferPtr(BUFFER.POINTBUFFER, (void**)&pp) / sizeof(float3);
          //  ushort* ii; var ni = p.GetBufferPtr(BUFFER.INDEXBUFFER, (void**)&ii) / sizeof(ushort);
          //  dc.Transform = p.GetTransform();
          //  dc.Color = 0xffff0000;
          //  dc.DrawLine(pp[ii[ni - 3]], pp[ii[ni - 2]]);
          //  dc.DrawLine(pp[ii[ni - 2]], pp[ii[ni - 1]]);
          //  dc.DrawLine(pp[ii[ni - 1]], pp[ii[ni - 3]]);
          //  dc.DrawLine(pp[ii[ni - 3]], default);
          //  dc.Color = 0xff00ff00;
          //  dc.DrawLine(pp[ii[ni - 6]], pp[ii[ni - 5]]);
          //  dc.DrawLine(pp[ii[ni - 5]], pp[ii[ni - 4]]);
          //  dc.DrawLine(pp[ii[ni - 4]], pp[ii[ni - 6]]);
          //  dc.DrawLine(pp[ii[ni - 6]], default);
          //}
        }
        return;
      }
      if (fl == 1)
      {
        var rf = view.Render;
        if ((rf & RenderFlags.Fps) != 0 || (flags & 4) != 0)// || debuginfo != null)
        {
          dc.SetOrtographic();
          dc.Color = 0xff000000; string s;
          float y = 10 + font.Ascent, dy = font.Height, x = ClientSize.Width - 10f;
          //if (debuginfo != null) for (int i = 0; i < debuginfo.Count; i++) dc.DrawText(10, y + i * dy, debuginfo[i]);
          if ((rf & RenderFlags.Fps) != 0)
          {
            s = $"{view.Fps} fps"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
            y += dy * 0.5f;
          }
          if ((flags & 4) != 0)
          {
            //s = (Factory.Version & 0x100) != 0 ? "Debug" : "Release"; dc.DrawText(x - dc.GetTextExtent(s).x, y, s); y += dy;
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
        }
      }
    }

    void ISink.Animate(INode p, uint t)
    {
      var o = p.Tag as Node ?? Node.From(this, p);
      var f = o.GetMethod<Action<uint>>(); if (f == null) return;
      try { f(t); } catch (Exception e) { o.disable(f); Debug.WriteLine(e.Message); }
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
