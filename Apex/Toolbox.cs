
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Serialization;
#pragma warning disable VSTHRD010 // Singlethread-Typen im Hauptthread aufrufen

namespace csg3mf
{
  [Serializable()]
  public class ToolboxItem : ISerializable
  {
    internal byte[] data;
    internal ToolboxItem() { }
    internal ToolboxItem(SerializationInfo info, StreamingContext context)
    {
      data = info.GetValue("data", typeof(byte[])) as byte[];
    }
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("data", data);
    }
  }


  class ToolboxDataProvider : IVsToolboxDataProvider
  {
    unsafe static Bitmap icon(Image img, int width, int height)
    {
      var r = new Rectangle(0, 0, width, height);
      var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      using (var g = Graphics.FromImage(bmp))
      {
        g.CompositingMode = CompositingMode.SourceCopy;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        using(var wm = new System.Drawing.Imaging.ImageAttributes())
        {
          wm.SetWrapMode(WrapMode.TileFlipXY);
          g.DrawImage(img, r, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wm);
        }
      }
      var ld = bmp.LockBits(r,
          System.Drawing.Imaging.ImageLockMode.ReadWrite,
          System.Drawing.Imaging.PixelFormat.Format32bppArgb);
      var pp = (uint*)ld.Scan0.ToPointer();
      for (int i = 0, n = width * height; i < n; i++)
      {
        var c = pp[i]; var a = c >> 24;
        if (a == 0) { pp[i] = 0xffff00ff; continue; }
        var b = (0xff - a) * 0xf0;
        pp[i] =
          (((c & 0xff) * a + b) >> 8) |
          (((((c >> 8) & 0xff) * a + b) >> 8) << 8) |
          (((((c >> 16) & 0xff) * a + b) >> 8) << 16) |
          0xff000000;
      }
      bmp.UnlockBits(ld);
      return bmp;
    }

    internal IVsToolbox toolbox;
    int IVsToolboxDataProvider.FileDropped(string pszFilename, IVsHierarchy pHierSource, out int pfFileProcessed)
    {
      var info = new TBXITEMINFO[1];
      info[0].bstrText = Path.GetFileNameWithoutExtension(pszFilename);
      //info[0].dwFlags = (uint)__TBXITEMINFOFLAGS.TBXIF_DONTPERSIST;
      using (var t1 = System.IO.Packaging.Package.Open(pszFilename, FileMode.Open, FileAccess.Read))
      using (var t2 = t1.GetPart(new Uri("/Metadata/thumbnail.png", UriKind.Relative)).GetStream())
      using (var t3 = Image.FromStream(t2))
      using (var t4 = icon(t3, 16, 16))
      {
        info[0].hBmp = t4.GetHbitmap();// System.Drawing.Color.Black);
        info[0].dwFlags = 1; //TBXIF_DELETEBITMAP
        info[0].clrTransparent = 0x00ff00ff;
      }
      var data = new OleDataObject();
      data.SetData(typeof(ToolboxItem), new ToolboxItem { data = File.ReadAllBytes(pszFilename) });
      toolbox.AddItem(data, info, null);
      pfFileProcessed = 1;
      return 0;
    }
    int IVsToolboxDataProvider.IsSupported(Microsoft.VisualStudio.OLE.Interop.IDataObject pDO)
    {
      return 0;
    }
    int IVsToolboxDataProvider.IsDataSupported(FORMATETC[] pfetc, STGMEDIUM[] pstm)
    {
      return 0;
    }
    int IVsToolboxDataProvider.GetItemInfo(Microsoft.VisualStudio.OLE.Interop.IDataObject pDO, TBXITEMINFO[] ptif)
    {
      return -2147221248; //OLECMDERR_E_NOTSUPPORTED;
    }
    internal static void RemoveItems(IVsToolbox toolbox)
    {
      toolbox.EnumItems(null, out var pen);
      var pp = new Microsoft.VisualStudio.OLE.Interop.IDataObject[1];
      for (; ; )
      {
        if (pen.Next(1, pp, out var _) != 0) break;
        var data = new OleDataObject(pp[0]);
        if (!data.GetDataPresent(typeof(ToolboxItem))) continue;
        toolbox.RemoveItem(pp[0]);
      }
    }
    internal static byte[] CopyItems(IVsToolbox toolbox)
    {
      var list = new List<(string, string, byte[])>();
      toolbox.EnumTabs(out var ppt);
      var ss = new string[1];
      var pp = new Microsoft.VisualStudio.OLE.Interop.IDataObject[1];
      for (int i = 0; ; i++)
      {
        if (ppt.Next(1, ss, out var _) != 0) break;
        toolbox.EnumItems(ss[0], out var pEnum);
        for (int k = 0; ; k++)
        {
          if (pEnum.Next(1, pp, out var _) != 0) break;
          var data = new OleDataObject(pp[0]);
          if (!data.GetDataPresent(typeof(ToolboxItem))) continue;
          ((IVsToolbox3)toolbox).GetItemDisplayName(pp[0], out var name);
          var ti = (ToolboxItem)data.GetData(typeof(ToolboxItem));
          list.Add((ss[0], name, ti.data));
        }
      }
      var fmt = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
      var str = new MemoryStream();
      fmt.Serialize(str, list.ToArray());
      return str.ToArray();
    }
    internal static void RestoreItems(IVsToolbox toolbox)
    {
      (string, string, byte[])[] items;
      var ss = Path.Combine(Path.GetDirectoryName(typeof(CDXView).Assembly.Location), "toolbox.bin");
      using (var str = new FileStream(ss, FileMode.Open, FileAccess.Read))
        items = ((string, string, byte[])[])new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter().Deserialize(str);
      var info = new TBXITEMINFO[1];
      foreach (var p in items)
      {
        info[0].bstrText = p.Item2;
        using (var t1 = System.IO.Packaging.Package.Open(new MemoryStream(p.Item3), FileMode.Open, FileAccess.Read))
        using (var t2 = t1.GetPart(new Uri("/Metadata/thumbnail.png", UriKind.Relative)).GetStream())
        using (var t3 = Image.FromStream(t2))
        using (var t4 = icon(t3, 16, 16))
        {
          info[0].hBmp = t4.GetHbitmap();
          info[0].dwFlags = 1; //TBXIF_DELETEBITMAP
          info[0].clrTransparent = 0x00ff00ff;
        }
        var data = new OleDataObject();
        data.SetData(typeof(ToolboxItem), new ToolboxItem { data = p.Item3 });
        toolbox.AddItem(data, info, p.Item1);
      }
    }
  }
}

