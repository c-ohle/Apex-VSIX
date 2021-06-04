using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace cde
{
  static unsafe class fmttga
  {
    internal static byte[] tga2png(byte[] tex)
    {
      var test = new fmttga.TargaImage(tex);
      var str = new MemoryStream();
      test.Image.Save(str, ImageFormat.Png);
      tex = str.ToArray();
      return tex;
    }
    class TargaImage
    {
      internal Bitmap Image = null;
      TGAFormat Format = TGAFormat.UNKNOWN;
      int intStride, intPadding, ExtensionAreaOffset, AttributesType;
      byte ImageIDLength = 0;
      ColorMapType ColorMapType = ColorMapType.NO_COLOR_MAP;
      ImageType ImageType = ImageType.NO_IMAGE_DATA;
      short ColorMapFirstEntryIndex = 0;
      short ColorMapLength = 0;
      byte ColorMapEntrySize = 0;
      short XOrigin = 0;
      short YOrigin = 0;
      short Width = 0;
      short Height = 0;
      byte PixelDepth = 0;
      VerticalTransferOrder VerticalTransferOrder = VerticalTransferOrder.UNKNOWN;
      HorizontalTransferOrder HorizontalTransferOrder = HorizontalTransferOrder.UNKNOWN;
      byte AttributeBits = 0;
      string ImageIDValue = string.Empty;
      List<Color> ColorMap = new List<System.Drawing.Color>();
      FirstPixelDestination FirstPixelDestination
      {
        get
        {
          if (VerticalTransferOrder == VerticalTransferOrder.UNKNOWN || HorizontalTransferOrder == HorizontalTransferOrder.UNKNOWN) return FirstPixelDestination.UNKNOWN;
          else if (VerticalTransferOrder == VerticalTransferOrder.BOTTOM && HorizontalTransferOrder == HorizontalTransferOrder.LEFT) return FirstPixelDestination.BOTTOM_LEFT;
          else if (VerticalTransferOrder == VerticalTransferOrder.BOTTOM && HorizontalTransferOrder == HorizontalTransferOrder.RIGHT) return FirstPixelDestination.BOTTOM_RIGHT;
          else if (VerticalTransferOrder == VerticalTransferOrder.TOP && HorizontalTransferOrder == HorizontalTransferOrder.LEFT) return FirstPixelDestination.TOP_LEFT;
          else return FirstPixelDestination.TOP_RIGHT;
        }
      }
      int ImageDataOffset
      {
        get
        {
          int intImageDataOffset = 18; intImageDataOffset += ImageIDLength;
          int Bytes = 0;
          switch (ColorMapEntrySize)
          {
            case 15:
              Bytes = 2;
              break;
            case 16:
              Bytes = 2;
              break;
            case 24:
              Bytes = 3;
              break;
            case 32:
              Bytes = 4;
              break;
          }
          intImageDataOffset += (ColorMapLength * Bytes);
          return intImageDataOffset;
        }
      }
      int BytesPerPixel
      {
        get { return PixelDepth / 8; }
      }

      public TargaImage(byte[] filebytes)
      {
        using (var filestream = new MemoryStream(filebytes))
        using (var binReader = new BinaryReader(filestream))
        {
          LoadTGAFooterInfo(binReader);
          LoadTGAHeaderInfo(binReader);
          LoadTGAExtensionArea(binReader);
          LoadTGAImage(binReader);
        }
      }
      void LoadTGAFooterInfo(BinaryReader binReader)
      {
        binReader.BaseStream.Seek(18 * -1, SeekOrigin.End);
        var Signature = Encoding.ASCII.GetString(binReader.ReadBytes(16)).TrimEnd('\0');
        if (string.Compare(Signature, "TRUEVISION-XFILE") == 0)
        {
          Format = TGAFormat.NEW_TGA;
          binReader.BaseStream.Seek((26 * -1), SeekOrigin.End);
          var ExtOffset = binReader.ReadInt32();
          var DevDirOff = binReader.ReadInt32();
          binReader.ReadBytes(16);
          var ResChar = Encoding.ASCII.GetString(binReader.ReadBytes(1)).TrimEnd('\0');
          ExtensionAreaOffset = ExtOffset;
        }
        else
        {
          Format = TGAFormat.ORIGINAL_TGA;
        }
      }
      void LoadTGAHeaderInfo(BinaryReader br)
      {
        br.BaseStream.Seek(0, SeekOrigin.Begin);
        ImageIDLength = br.ReadByte();
        ColorMapType = (ColorMapType)br.ReadByte();
        ImageType = (ImageType)br.ReadByte();
        ColorMapFirstEntryIndex = br.ReadInt16();
        ColorMapLength = br.ReadInt16();
        ColorMapEntrySize = br.ReadByte();
        XOrigin = br.ReadInt16();
        YOrigin = br.ReadInt16();
        Width = br.ReadInt16();
        Height = br.ReadInt16();
        byte pixeldepth = br.ReadByte();
        switch (pixeldepth)
        {
          case 8: case 16: case 24: case 32: PixelDepth = pixeldepth; break;
          default: throw new Exception();
        }
        byte ImageDescriptor = br.ReadByte();
        AttributeBits = (byte)GetBits(ImageDescriptor, 0, 4);
        VerticalTransferOrder = (VerticalTransferOrder)GetBits(ImageDescriptor, 5, 1);
        HorizontalTransferOrder = (HorizontalTransferOrder)GetBits(ImageDescriptor, 4, 1);
        if (ImageIDLength > 0)
        {
          byte[] ImageIDValueBytes = br.ReadBytes(ImageIDLength);
          ImageIDValue = System.Text.Encoding.ASCII.GetString(ImageIDValueBytes).TrimEnd('\0');
        }
        if (ColorMapType == ColorMapType.COLOR_MAP_INCLUDED)
        {
          if (ImageType == ImageType.UNCOMPRESSED_COLOR_MAPPED || ImageType == ImageType.RUN_LENGTH_ENCODED_COLOR_MAPPED)
          {
            if (ColorMapLength <= 0) throw new Exception();
            for (int i = 0; i < ColorMapLength; i++)
            {
              int a = 0, r = 0, g = 0, b = 0;

              switch (ColorMapEntrySize)
              {
                case 15:
                  byte[] color15 = br.ReadBytes(2);
                  ColorMap.Add(GetColorFrom2Bytes(color15[1], color15[0]));
                  break;
                case 16:
                  byte[] color16 = br.ReadBytes(2);
                  ColorMap.Add(GetColorFrom2Bytes(color16[1], color16[0]));
                  break;
                case 24:
                  b = Convert.ToInt32(br.ReadByte());
                  g = Convert.ToInt32(br.ReadByte());
                  r = Convert.ToInt32(br.ReadByte());
                  ColorMap.Add(Color.FromArgb(r, g, b));
                  break;
                case 32:
                  a = Convert.ToInt32(br.ReadByte());
                  b = Convert.ToInt32(br.ReadByte());
                  g = Convert.ToInt32(br.ReadByte());
                  r = Convert.ToInt32(br.ReadByte());
                  ColorMap.Add(Color.FromArgb(a, r, g, b));
                  break;
                default: throw new Exception();
              }
            }
          }
        }
        else
        {
          if (ImageType == ImageType.UNCOMPRESSED_COLOR_MAPPED || ImageType == ImageType.RUN_LENGTH_ENCODED_COLOR_MAPPED) throw new Exception();
        }
      }
      void LoadTGAExtensionArea(BinaryReader br)
      {
        if (ExtensionAreaOffset <= 0) return;
        br.BaseStream.Seek(ExtensionAreaOffset, SeekOrigin.Begin);
        br.ReadInt16();// ExtensionSize
        br.ReadBytes(41);// AuthorName 
        br.ReadBytes(324); // AuthorComments
        var iMonth = br.ReadInt16();
        var iDay = br.ReadInt16();
        var iYear = br.ReadInt16();
        var iHour = br.ReadInt16();
        var iMinute = br.ReadInt16();
        var iSecond = br.ReadInt16();
        br.ReadBytes(41);//JobName
        iHour = br.ReadInt16();
        iMinute = br.ReadInt16();
        iSecond = br.ReadInt16();
        br.ReadBytes(41);//SoftwareID
        float iVersionNumber = br.ReadInt16() / 100.0F;
        br.ReadBytes(1);// SoftwareID
        int a = br.ReadByte();
        int r = br.ReadByte();
        int b = br.ReadByte();
        int g = br.ReadByte(); //KeyColor
        br.ReadInt16();// PixelAspectRatioNumerator
        br.ReadInt16(); // PixelAspectRatioDenominator
        br.ReadInt16(); // GammaNumerator
        br.ReadInt16(); // GammaDenominator
        br.ReadInt32(); // ColorCorrectionOffset;
        br.ReadInt32();// PostageStampOffset
        br.ReadInt32(); // ScanLineOffset
        AttributesType = br.ReadByte();
      }
      byte[] LoadImageBytes(BinaryReader binReader)
      {
        byte[] data = null;
        var padding = new byte[intPadding]; MemoryStream msData = null;
        var rows = new List<List<byte>>(); var row = new List<byte>();
        binReader.BaseStream.Seek(ImageDataOffset, SeekOrigin.Begin);
        int intImageRowByteSize = Width * (BytesPerPixel);
        int intImageByteSize = intImageRowByteSize * Height;
        if (ImageType == ImageType.RUN_LENGTH_ENCODED_BLACK_AND_WHITE ||
            ImageType == ImageType.RUN_LENGTH_ENCODED_COLOR_MAPPED ||
            ImageType == ImageType.RUN_LENGTH_ENCODED_TRUE_COLOR)
        {
          byte bRLEPacket = 0;
          int intRLEPacketType = -1;
          int intRLEPixelCount = 0;
          byte[] bRunLengthPixel = null;
          int intImageBytesRead = 0;
          int intImageRowBytesRead = 0;
          while (intImageBytesRead < intImageByteSize)
          {
            bRLEPacket = binReader.ReadByte();
            intRLEPacketType = GetBits(bRLEPacket, 7, 1);
            intRLEPixelCount = GetBits(bRLEPacket, 0, 7) + 1;
            if ((RLEPacketType)intRLEPacketType == RLEPacketType.RUN_LENGTH)
            {
              bRunLengthPixel = binReader.ReadBytes(BytesPerPixel);
              for (int i = 0; i < intRLEPixelCount; i++)
              {
                foreach (byte b in bRunLengthPixel) row.Add(b);
                intImageRowBytesRead += bRunLengthPixel.Length;
                intImageBytesRead += bRunLengthPixel.Length;
                if (intImageRowBytesRead == intImageRowByteSize)
                {
                  rows.Add(row);
                  row = new List<byte>();
                  intImageRowBytesRead = 0;
                }
              }
            }
            else if ((RLEPacketType)intRLEPacketType == RLEPacketType.RAW)
            {
              int intBytesToRead = intRLEPixelCount * BytesPerPixel;
              for (int i = 0; i < intBytesToRead; i++)
              {
                row.Add(binReader.ReadByte());
                intImageBytesRead++;
                intImageRowBytesRead++;
                if (intImageRowBytesRead == intImageRowByteSize)
                {
                  rows.Add(row);
                  row = new List<byte>();
                  intImageRowBytesRead = 0;
                }

              }
            }
          }
        }
        else
        {
          for (int i = 0; i < Height; i++)
          {
            for (int j = 0; j < intImageRowByteSize; j++) row.Add(binReader.ReadByte());
            rows.Add(row); row = new List<byte>();
          }
        }
        bool blnRowsReverse = false, blnEachRowReverse = false;
        switch (FirstPixelDestination)
        {
          case FirstPixelDestination.TOP_LEFT:
            blnRowsReverse = false;
            blnEachRowReverse = true;
            break;
          case FirstPixelDestination.TOP_RIGHT:
            blnRowsReverse = false;
            blnEachRowReverse = false;
            break;
          case FirstPixelDestination.BOTTOM_LEFT:
            blnRowsReverse = true;
            blnEachRowReverse = true;
            break;
          case FirstPixelDestination.BOTTOM_RIGHT:
          case FirstPixelDestination.UNKNOWN:
            blnRowsReverse = true;
            blnEachRowReverse = false;
            break;
        }
        using (msData = new MemoryStream())
        {
          if (blnRowsReverse == true) rows.Reverse();
          for (int i = 0; i < rows.Count; i++)
          {
            if (blnEachRowReverse == true) rows[i].Reverse();
            byte[] brow = rows[i].ToArray();
            msData.Write(brow, 0, brow.Length);
            msData.Write(padding, 0, padding.Length);
          }
          data = msData.ToArray();
        }
        if (rows != null)
        {
          for (int i = 0; i < rows.Count; i++) { rows[i].Clear(); rows[i] = null; }
          rows.Clear(); rows = null;
        }
        if (row != null) { row.Clear(); row = null; }
        return data;
      }
      void LoadTGAImage(BinaryReader binReader)
      {
        intStride = ((Width * PixelDepth + 31) & ~31) >> 3;
        intPadding = intStride - (((Width * PixelDepth) + 7) / 8);
        var pf = GetPixelFormat();
        var bimagedata = LoadImageBytes(binReader);
        fixed (byte* p = bimagedata) Image = new Bitmap(Width, Height, intStride, pf, new IntPtr(p));
        if (ColorMap.Count > 0)
        {
          var pal = Image.Palette;
          for (int i = 0; i < ColorMap.Count; i++)
          {
            var forceopaque = false;
            if (Format == TGAFormat.NEW_TGA && ExtensionAreaOffset > 0) { if (AttributesType == 0 || AttributesType == 1) forceopaque = true; }
            else if (AttributeBits == 0 || AttributeBits == 1) forceopaque = true;
            if (forceopaque) pal.Entries[i] = Color.FromArgb(255, ColorMap[i].R, ColorMap[i].G, ColorMap[i].B);
            else pal.Entries[i] = ColorMap[i];
          }
          Image.Palette = pal; pal = null;
        }
        else
        {
          if (PixelDepth == 8 && (ImageType == ImageType.UNCOMPRESSED_BLACK_AND_WHITE || ImageType == ImageType.RUN_LENGTH_ENCODED_BLACK_AND_WHITE))
          {
            ColorPalette pal = Image.Palette;
            for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
            Image.Palette = pal; pal = null;
          }
        }
      }
      PixelFormat GetPixelFormat()
      {
        PixelFormat pfTargaPixelFormat = PixelFormat.Undefined;
        switch (PixelDepth)
        {
          case 8:
            pfTargaPixelFormat = PixelFormat.Format8bppIndexed;
            break;
          case 16:
            if (Format == TGAFormat.NEW_TGA && ExtensionAreaOffset > 0)
            {
              switch (AttributesType)
              {
                case 0:
                case 1:
                case 2:
                  pfTargaPixelFormat = PixelFormat.Format16bppRgb555;
                  break;
                case 3:
                  pfTargaPixelFormat = PixelFormat.Format16bppArgb1555;
                  break;
              }
            }
            else
            {
              if (AttributeBits == 0)
                pfTargaPixelFormat = PixelFormat.Format16bppRgb555;
              if (AttributeBits == 1)
                pfTargaPixelFormat = PixelFormat.Format16bppArgb1555;
            }
            break;
          case 24:
            pfTargaPixelFormat = PixelFormat.Format24bppRgb;
            break;
          case 32:
            if (Format == TGAFormat.NEW_TGA && ExtensionAreaOffset > 0)
            {
              switch (AttributesType)
              {
                case 0:
                case 1:
                case 2:
                  pfTargaPixelFormat = PixelFormat.Format32bppRgb;
                  break;
                case 3:
                  pfTargaPixelFormat = PixelFormat.Format32bppArgb;
                  break;
                case 4:
                  pfTargaPixelFormat = PixelFormat.Format32bppPArgb;
                  break;
              }
            }
            else
            {
              if (AttributeBits == 0) pfTargaPixelFormat = PixelFormat.Format32bppRgb;
              if (AttributeBits == 8) pfTargaPixelFormat = PixelFormat.Format32bppArgb;
              break;
            }
            break;
        }
        return pfTargaPixelFormat;
      }
      static int GetBits(byte b, int offset, int count)
      {
        return (b >> offset) & ((1 << count) - 1);
      }
      static Color GetColorFrom2Bytes(byte one, byte two)
      {
        int r1 = GetBits(one, 2, 5);
        int r = r1 << 3;
        int bit = GetBits(one, 0, 2);
        int g1 = bit << 6;
        bit = GetBits(two, 5, 3);
        int g2 = bit << 3;
        int g = g1 + g2;
        int b1 = GetBits(two, 0, 5);
        int b = b1 << 3;
        int a1 = GetBits(one, 7, 1);
        int a = a1 * 255;
        return Color.FromArgb(a, r, g, b);
      }
    }

    enum TGAFormat { UNKNOWN = 0, ORIGINAL_TGA = 100, NEW_TGA = 200 }
    enum ColorMapType : byte { NO_COLOR_MAP = 0, COLOR_MAP_INCLUDED = 1 }
    enum ImageType : byte { NO_IMAGE_DATA = 0, UNCOMPRESSED_COLOR_MAPPED = 1, UNCOMPRESSED_TRUE_COLOR = 2, UNCOMPRESSED_BLACK_AND_WHITE = 3, RUN_LENGTH_ENCODED_COLOR_MAPPED = 9, RUN_LENGTH_ENCODED_TRUE_COLOR = 10, RUN_LENGTH_ENCODED_BLACK_AND_WHITE = 11 }
    enum VerticalTransferOrder { UNKNOWN = -1, BOTTOM = 0, TOP = 1 }
    enum HorizontalTransferOrder { UNKNOWN = -1, RIGHT = 0, LEFT = 1 }
    enum FirstPixelDestination { UNKNOWN = 0, TOP_LEFT = 1, TOP_RIGHT = 2, BOTTOM_LEFT = 3, BOTTOM_RIGHT = 4 }
    enum RLEPacketType { RAW = 0, RUN_LENGTH = 1 }
  }

}
