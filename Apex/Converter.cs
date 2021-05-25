using csg3mf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static csg3mf.CDX;

namespace csg3mf
{
  class ArrayEditor : System.ComponentModel.Design.ArrayEditor
  {
    public ArrayEditor(Type type) : base(type) { }
    protected override object CreateInstance(Type t)
    {
      if (t.IsArray) return Activator.CreateInstance(t, 0);
      return base.CreateInstance(t);
    }
    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
      var p = base.EditValue(context, provider, value);
      ArrayConverter.refresh(context);
      return p;
    }
  }
  class ArrayConverter : System.ComponentModel.ArrayConverter
  {
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      if (context == null) return base.ConvertTo(context, culture, value, destinationType);
      var s = context.PropertyDescriptor.PropertyType.GetElementType().Name;
      return value is Array a ? $"{s}[{a.Length}]" : $"{s}[]";
    }
    public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) => true;// base.GetCreateInstanceSupported(context);
    public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
    {
      var t = context.PropertyDescriptor.PropertyType; var n = propertyValues.Count;
      var a = (Array)Activator.CreateInstance(t, n);
      for (int i = 0; i < n; i++) a.SetValue(propertyValues[$"[{i}]"], i);
      if (t.GetElementType().IsArray) refresh(context);
      return a;// base.CreateInstance(context, propertyValues);
    }
    internal static void refresh(object context)
    {
      var t1 = context.GetType().GetProperty("OwnerGrid", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
      if (t1 == null) return;
      var t2 = t1.GetValue(context) as System.Windows.Forms.PropertyGrid;
      if (t2 == null) return;
      t2.Refresh();
      System.Windows.Forms.PropertyValueChangedEventHandler h = null;
      h = (p, e) => { t2.Refresh(); t2.PropertyValueChanged -= h; };
      t2.PropertyValueChanged += h;
    }
  }

  class VectorConverter : TypeConverter
  {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => true;
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      if (value is float2 a) return ((FormattableString)$"{a.x:R}; {a.y:R}").ToString(culture);
      if (value is float3 b) return ((FormattableString)$"{b.x:R}; {b.y:R}; {b.z:R}").ToString(culture);
      if (value is float4 c) return ((FormattableString)$"{c.x:R}; {c.y:R}; {c.z:R}; {c.w:R}").ToString(culture);
      //if (value is double3 e) return ((FormattableString)$"{e.x:R}; {e.y:R}; {e.z:R}{(context == null ? "d" : "")}").ToString(culture);
      return value != null ? value.ToString() : string.Empty;
    }
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      var s = (string)value;
      var e = '0'; if (context == null && s.Length != 0 && char.IsLetter(e = s[s.Length - 1])) s = s.Substring(0, s.Length - 1);
      var ss = s.Split(';'); int id;
      if (context == null)
      {
        id = ss.Length; if (e == 'd') id = 5;
      }
      else
      {
        var t = context.PropertyDescriptor.PropertyType;
        if (t == typeof(float2)) id = 2;
        else if (t == typeof(float3)) id = 3;
        else if (t == typeof(float4)) id = 4;
        //else if (t == typeof(double3)) id = 5;
        else id = 0;
      }
      switch (id)
      {
        case 2: return new float2(float.Parse(ss[0], culture), float.Parse(ss[1], culture));
        case 3: return new float3(float.Parse(ss[0], culture), float.Parse(ss[1], culture), float.Parse(ss[2], culture));
        case 4: return new float4(float.Parse(ss[0], culture), float.Parse(ss[1], culture), float.Parse(ss[2], culture), float.Parse(ss[3], culture));
          //case 5: return new double3(double.Parse(ss[0], culture), double.Parse(ss[1], culture), double.Parse(ss[2], culture));
      }
      return null;
    }
  }

  class FormatConverter : TypeConverter
  {
    string[] tab;
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => true;
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
    {
      if (tab == null)
      {
        var av = context.PropertyDescriptor.Attributes.OfType<AmbientValueAttribute>().FirstOrDefault();
        if (av != null) tab = ((string)av.Value).Split('|');
      }
      return tab != null && tab.Length > 1;// base.GetStandardValuesSupported(context);
    }
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => tab != null && tab.Length > 1;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
      if (GetStandardValuesSupported(context)) return new StandardValuesCollection(Enumerable.Range(0, tab.Length).ToArray());
      return base.GetStandardValues(context);
    }
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      var sv = GetStandardValuesSupported(context);
      if (sv)
      {
        if (!(value is int i)) return null;
        if (sv && (uint)i < tab.Length) return tab[i];
        return ((int)value).ToString();
      }
      var fmt = tab != null ? tab[0] : "{0}";// if (tab == null) return null;
      if (fmt == ".s")
      {
        var s = (float3)value; if (s.x == s.y && s.y == s.z) return s.x.ToString();
        return s.ToString();
      }
      return string.Format(culture, fmt, value);
    }
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      var s = (string)value;
      var sv = GetStandardValuesSupported(context);
      if (!sv)
      {
        var fmt = tab != null ? tab[0] : "{0}";// if (tab == null) return null;
        if (fmt == ".s")
        {
          var ss = s.Split(new char[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
          if (ss.Length == 1) { var v = float.Parse(ss[0]); return new float3(v, v, v); }
          if (ss.Length == 3) { return new float3(float.Parse(ss[0]), float.Parse(ss[1]), float.Parse(ss[2])); }
        }
        for (int x = s.Length - 1; x >= 0; x--) if (char.IsNumber(s[x])) { s = s.Substring(0, x + 1); break; }
        return TypeDescriptor.GetConverter(context.PropertyDescriptor.PropertyType).ConvertFrom(context, culture, s);
      }
      if (GetStandardValuesSupported(context)) { var i = Array.IndexOf(tab, s); if (i != -1) return i; }
      return int.Parse(s);
    }
  }

  unsafe class TexturConverter : TypeConverter
  {
    int hash; StandardValuesCollection collection;
    public override bool IsValid(ITypeDescriptorContext context, object value)
    {
      return base.IsValid(context, value);
    }
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => true;
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => true;
    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
      if (value == null) return "(none)";
      if (value is IBuffer t) return t.Name;
      if (value is Delegate a) { return a.Method.Name + "..."; }
      return value.ToString();
    }
    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
      var name = (string)value;
      foreach (var p in GetStandardValues(context))
        if ((string)ConvertTo(context, culture, p, null) == name)
          return p;

      var node = (Node)context.Instance;
      var tex = node.node.GetBuffer(BUFFER.TEXTURE);

      //if (Microsoft.VisualStudio.Shell.VsShellUtilities.ShowMessageBox(
      //  node.view.pane, "Rename the texture?", "Question",
      //  Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_QUERY,
      //  Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
      //  Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST)
      //  != (int)Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK)
      //  return tex;

      //if (name.Length == 0) return tex;
      //if (System.Windows.Forms.MessageBox.Show("Rename?", "Texture",
      //  System.Windows.Forms.MessageBoxButtons.OKCancel) != System.Windows.Forms.DialogResult.OK)
      //  return tex;

      tex.Name = name;
      return tex;
    }
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
      var node = ((Node)context.Instance).node;
      var hash = (node.GetBuffer(BUFFER.TEXTURE)?.GetHashCode()).GetHashCode();
      if (this.hash != hash) { this.hash = hash; collection = null; }
      if (collection != null) return collection;
      var list = new List<object> { null };
      foreach (var t in node.Scene.Descendants().Select(p => p.GetBuffer(BUFFER.TEXTURE)).OfType<object>().Distinct()) list.Add(t);
      list.Add((Func<object, object>)Import);
      if (node.GetBuffer(BUFFER.TEXTURE) != null) list.Add((Func<object, object>)Export);
      return collection = new StandardValuesCollection(list);
    }
    object Import(object context)
    {
      var dlg = new System.Windows.Forms.OpenFileDialog() { Filter = "Image files|*.png;*.jpg;*.gif|All files|*.*" };
      if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return null;
      var a = System.IO.File.ReadAllBytes(dlg.FileName);

      //https://media.freestocktextures.com/cache/d0/b6/d0b6a77c19dc713314e3e325d77d55c9.jpg
      var ic = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
      if (dlg.FileName.StartsWith(ic, true, null))
      {
        var uri = System.Windows.Forms.Clipboard.GetText();
        if (uri != null && (uri = uri.Trim()).StartsWith("http", true, null))
        {
          var str = new System.IO.MemoryStream();
          using (var bmp = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(new System.IO.MemoryStream(a)))
          using (var min = bmp.GetThumbnailImage(16, 16, null, IntPtr.Zero))
            min.Save(str, System.Drawing.Imaging.ImageFormat.Png);
          var n = (int)str.Position;
          var t = System.Text.Encoding.UTF8.GetBytes(uri); str.Write(t, 0, t.Length);
          t = BitConverter.GetBytes(n | unchecked((int)0xC0660000)); str.Write(t, 0, t.Length);
          a = str.ToArray();
        }
      }

      IBuffer tex; fixed (byte* p = a) tex = Factory.GetBuffer(BUFFER.TEXTURE, p, a.Length);
      tex.Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
      collection = null; return tex;
    }
    object Export(object context)
    {
      var tex = ((Node)context).node.GetBuffer(BUFFER.TEXTURE);
      var dlg = new System.Windows.Forms.SaveFileDialog() { FileName = tex.Name, DefaultExt = "png", Filter = "PNG file|*.png|JPEG file|*.jpg" };
      if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return false;
      byte* pp; var np = tex.GetPtr((void**)&pp);
      using (var bmp = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(new System.IO.UnmanagedMemoryStream(pp, np)))
        bmp.Save(dlg.FileName, dlg.FileName.EndsWith("jpg", true, null) ?
          System.Drawing.Imaging.ImageFormat.Jpeg : System.Drawing.Imaging.ImageFormat.Png);
      //tex.Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
      return null;
    }

  }

}
