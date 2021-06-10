using Apex;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Apex.CDX;

namespace Apex
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
  class FieldPD : PropertyDescriptor
  {
    FieldInfo fi;
    public FieldPD(FieldInfo fi) : base(fi.Name, null) { this.fi = fi; }
    public override Type ComponentType => fi.DeclaringType;
    public override Type PropertyType => fi.FieldType;
    public override bool IsReadOnly => false;
    public override bool CanResetValue(object component) => false;
    public override bool ShouldSerializeValue(object component) => false;
    public override void ResetValue(object component) { }
    public override object GetValue(object component) => fi.GetValue(component);
    public override void SetValue(object component, object value) => fi.SetValue(component, value);
    public static PropertyDescriptorCollection GetProperties(object value) =>
      new PropertyDescriptorCollection(value.GetType().GetFields().Select(fi => new FieldPD(fi)).ToArray());
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
    //int hash; StandardValuesCollection svc; 
    WeakRef<StandardValuesCollection> wsrv;
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
      var name = (string)value; if (string.IsNullOrEmpty(name)) return null;

      foreach (var p in GetStandardValues(context))
        if ((string)ConvertTo(context, culture, p, null) == name)
          return p;

      var tex = context.PropertyDescriptor.GetValue(context.Instance) as IBuffer;
      if (tex != null) tex.Name = name;

      return tex;
    }
    public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => false;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
    {
      var srv = wsrv.Value; if (srv != null) return srv;
      var node = ((Node)context.Instance).node;
      //var hash = (node.GetBuffer(BUFFER.TEXTURE)?.GetHashCode()).GetHashCode();
      //if (this.hash != hash) { this.hash = hash; svc = null; }
      //if (svc != null) return svc;
      var list = new List<object> { null };
      foreach (var t in node.Scene.Descendants().
        SelectMany(p => Enumerable.Range(0, 8).Select(i => p.GetBuffer(BUFFER.TEXTURE + i))).
        OfType<IBuffer>().Distinct().Where(p => !string.IsNullOrEmpty(p.Name))) list.Add(t);
      list.Add((Func<PropertyDescriptor, object, object>)Import);
      var tv = context.PropertyDescriptor.GetValue(context.Instance);
      if (tv != null) list.Add((Func<PropertyDescriptor, object, object>)Export);
      return wsrv.Value = srv = new StandardValuesCollection(list);
    }
    object Import(PropertyDescriptor pd, object inst)
    {
      var dlg = new System.Windows.Forms.OpenFileDialog() { Filter = "Image files|*.png;*.jpg|All files|*.*" }; //;*.gif
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
      if (!(a[0] == 0x89 || a[0] == 0xff)) // png, jpg
      {
        //((Node)context).view.MessageBox("Only PNG and JPEG supported in 3mf");
        var str = new System.IO.MemoryStream();
        using (var bmp = System.Drawing.Image.FromFile(dlg.FileName)) bmp.Save(str, System.Drawing.Imaging.ImageFormat.Png);
        a = str.ToArray();
      }
      IBuffer tex; fixed (byte* p = a) tex = Factory.GetBuffer(BUFFER.TEXTURE, p, a.Length);
      tex.Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
      wsrv.Value = null; return tex;
    }
    object Export(PropertyDescriptor pd, object inst)
    {
      var tex = pd.GetValue(inst) as IBuffer; if (tex == null) return null;
      var dlg = new System.Windows.Forms.SaveFileDialog() { FileName = tex.Name, DefaultExt = "png", Filter = "PNG file|*.png|JPEG file|*.jpg" };
      if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return null;
      byte* pp; var np = tex.GetBufferPtr((void**)&pp);
      using (var bmp = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(new System.IO.UnmanagedMemoryStream(pp, np)))
        bmp.Save(dlg.FileName, dlg.FileName.EndsWith("jpg", true, null) ?
          System.Drawing.Imaging.ImageFormat.Jpeg : System.Drawing.Imaging.ImageFormat.Png);
      //tex.Name = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
      return null;
    }
  }

  struct WeakRef<T> where T : class
  {
    WeakReference p;
    public T Value
    {
      get { if (p != null && p.Target is T v) return v; return null; }
      set { if (value == null) p = null; else if (p == null) p = new WeakReference(value); else p.Target = value; }
    }
  }

}
