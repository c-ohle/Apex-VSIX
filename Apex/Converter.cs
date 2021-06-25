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
    public static PropertyDescriptorCollection GetProperties(Type t) => new PropertyDescriptorCollection(t.GetFields().Select(fi => new FieldPD(fi)).ToArray());
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
      var node = context.Instance is Node n ? n.node : ((Node)((object[])context.Instance)[0]).node;
      //var node = ((Node)context.Instance).node;
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
      //void* px; int nx = tex.GetBufferPtr(&px);
      //var sx = new System.IO.UnmanagedMemoryStream((byte*)px, nx);
      //var pngDecoder = new System.Windows.Media.Imaging.PngBitmapDecoder(sx,
      //  System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
      //  System.Windows.Media.Imaging.BitmapCacheOption.Default);
      //var pngFrame = pngDecoder.Frames[0];
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
  static class WeakSingleton<T> where T : class
  {
    internal static WeakRef<T> p;
  }
  /*
  class MyType : Type
  {
    internal Type t; internal string[] ss;
    public override Guid GUID => t.GUID;
    public override Module Module => t.Module;
    public override Assembly Assembly => t.Assembly;
    public override string FullName => t.FullName;
    public override string Namespace => t.Namespace;
    public override string AssemblyQualifiedName => t.AssemblyQualifiedName;
    public override Type BaseType => t.BaseType;
    public override Type UnderlyingSystemType => t.UnderlyingSystemType;
    public override string Name => "MyType";// t.Name;
    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr) => t.GetConstructors(bindingAttr);
    public override object[] GetCustomAttributes(bool inherit) => t.GetCustomAttributes(inherit);
    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => t.GetCustomAttributes(attributeType, inherit);
    public override Type GetElementType() => t.GetElementType();
    public override EventInfo GetEvent(string name, BindingFlags bindingAttr) => t.GetEvent(name, bindingAttr);
    public override EventInfo[] GetEvents(BindingFlags bindingAttr) => t.GetEvents(bindingAttr);
    public override FieldInfo GetField(string name, BindingFlags bindingAttr) => t.GetField(name, bindingAttr);
    public override FieldInfo[] GetFields(BindingFlags bindingAttr) => t.GetFields(bindingAttr);
    public override Type GetInterface(string name, bool ignoreCase) => t.GetInterface(name, ignoreCase);
    public override Type[] GetInterfaces() => t.GetInterfaces();
    public override MemberInfo[] GetMembers(BindingFlags bindingAttr) => t.GetMembers(bindingAttr);
    public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => t.GetMethods(bindingAttr);
    public override Type GetNestedType(string name, BindingFlags bindingAttr) => t.GetNestedType(name, bindingAttr);
    public override Type[] GetNestedTypes(BindingFlags bindingAttr) => t.GetNestedTypes(bindingAttr);
    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr) => t.GetProperties(bindingAttr);
    public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters) => null;
    public override bool IsDefined(Type attributeType, bool inherit) => t.IsDefined(attributeType, inherit);
    protected override TypeAttributes GetAttributeFlagsImpl() => default;
    protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => null;
    protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => null;
    protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => null;
    protected override bool HasElementTypeImpl() => default;
    protected override bool IsArrayImpl() => default;
    protected override bool IsByRefImpl() => default;
    protected override bool IsCOMObjectImpl() => default;
    protected override bool IsPointerImpl() => default;
    protected override bool IsPrimitiveImpl() => default;

    //public override RuntimeTypeHandle TypeHandle => t.TypeHandle;
    //public override GenericParameterAttributes GenericParameterAttributes => t.GenericParameterAttributes;
    //public override Type DeclaringType => t.DeclaringType;
    //public override IEnumerable<CustomAttributeData> CustomAttributes => t.CustomAttributes;
    //public override bool ContainsGenericParameters => t.ContainsGenericParameters;
    //public override MethodBase DeclaringMethod => t.DeclaringMethod;
    //public override bool Equals(object o)
    //{
    //  return t.Equals(o);
    //}
    //public override bool Equals(Type o)
    //{
    //  return t.Equals(o);
    //}
    //public override Type[] FindInterfaces(TypeFilter filter, object filterCriteria)
    //{
    //  return t.FindInterfaces(filter, filterCriteria);
    //}
    //public override MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria)
    //{
    //  return t.FindMembers(memberType, bindingAttr, filter, filterCriteria);
    //}
    //public override int GenericParameterPosition => t.GenericParameterPosition;
    //public override Type[] GenericTypeArguments => t.GenericTypeArguments;
    //public override IList<CustomAttributeData> GetCustomAttributesData()
    //{
    //  return t.GetCustomAttributesData();
    //}
    //public override int GetArrayRank()
    //{
    //  return t.GetArrayRank();
    //}
    //public override MemberTypes MemberType => t.MemberType;
    //public override int GetHashCode()
    //{
    //  return t.GetHashCode();
    //}
    //public override bool IsConstructedGenericType => t.IsConstructedGenericType;
    //
    //public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
    //{
    //  return t.GetMember(name, bindingAttr);
    //}
    //public override bool IsSubclassOf(Type c)
    //{
    //  return t.IsSubclassOf(c);
    //}
    //public override Type[] GetGenericParameterConstraints()
    //{
    //  return t.GetGenericParameterConstraints();
    //}
    //public override Type[] GetGenericArguments()
    //{
    //  return t.GetGenericArguments();
    //}
    //public override Type GetGenericTypeDefinition()
    //{
    //  return t.GetGenericTypeDefinition();
    //}
    //public override Array GetEnumValues()
    //{
    //  return t.GetEnumValues();
    //}
    //public override Type GetEnumUnderlyingType()
    //{
    //  return t.GetEnumUnderlyingType();
    //}
    //public override bool IsEnum => t.IsEnum;
    //public override InterfaceMapping GetInterfaceMap(Type interfaceType)
    //{
    //  return t.GetInterfaceMap(interfaceType);
    //}
    //protected override TypeCode GetTypeCodeImpl()
    //{
    //  return base.GetTypeCodeImpl();
    //}
    //public override bool IsGenericType => t.IsGenericType;
    //public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
    //{
    //  return t.GetMember(name, type, bindingAttr);
    //}
    //public override StructLayoutAttribute StructLayoutAttribute => t.StructLayoutAttribute;
    //public override bool IsGenericParameter => t.IsGenericParameter;
    //public override MemberInfo[] GetDefaultMembers()
    //{
    //  return t.GetDefaultMembers();
    //}
    //public override string GetEnumName(object value)
    //{
    //  return t.GetEnumName(value);
    //}
    //public override string[] GetEnumNames()
    //{
    //  return t.GetEnumNames();
    //}
    //public override bool IsAssignableFrom(Type c)
    //{
    //  return t.IsAssignableFrom(c);
    //}
    //protected override bool IsContextfulImpl()
    //{
    //  return base.IsContextfulImpl();
    //}
    //public override bool IsEquivalentTo(Type other)
    //{
    //  return t.IsEquivalentTo(other);
    //}
    //public override Type ReflectedType => t.ReflectedType;
    //public override bool IsSecurityTransparent => t.IsSecurityTransparent;
    //public override bool IsInstanceOfType(object o)
    //{
    //  return t.IsInstanceOfType(o);
    //}
    //public override int MetadataToken => t.MetadataToken;
    //protected override bool IsValueTypeImpl()
    //{
    //  return t.IsValueType;// IsValueTypeImpl();
    //}
    //public override bool IsSecuritySafeCritical => t.IsSecuritySafeCritical;
    //public override bool IsSecurityCritical => t.IsSecurityCritical;
    //public override bool IsGenericTypeDefinition => t.IsGenericTypeDefinition;
    //protected override bool IsMarshalByRefImpl()
    //{
    //  return false;
    //}
    //public override EventInfo[] GetEvents()
    //{
    //  return t.GetEvents();
    //}
    //public override bool IsEnumDefined(object value)
    //{
    //  return t.IsEnumDefined(value);
    //}
    //public override bool IsSerializable => t.IsSerializable;
    //public override Type MakeArrayType()
    //{
    //  return t.MakeArrayType();
    //}
    //public override Type MakePointerType()
    //{
    //  return t.MakePointerType();
    //}
    //public override Type MakeGenericType(params Type[] typeArguments)
    //{
    //  return t.MakeGenericType(typeArguments);
    //}
    //public override Type MakeArrayType(int rank)
    //{
    //  return t.MakeArrayType(rank);
    //}
    //public override Type MakeByRefType()
    //{
    //  return t.MakeByRefType();
    //}
    //public override string ToString()
    //{
    //  return t.ToString();
    //}
  }
  */

}
