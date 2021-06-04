using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Apex.CDX;

namespace Apex
{
  public interface IExchange
  {
    bool Category(string name);
    bool DisplayName(string name);
    void Description(string text);
    void ReadOnly();
    void Format(string text);
    void TypeConverter(Type t);
    bool Exchange<T>(string name, ref T value);
    void Annotation<T>(string name, T value);
    bool Modified { get; }
  }

  unsafe abstract class NodeBase : SimpleTypeDescriptor
  {
    internal CDXView view; // -> undo inval
    protected abstract void Exchange(IExchange ex);
    internal void GetProps(PropertyDescriptorCollection pdc)
    {
      if (ex.attris == null) ex.attris = new List<Attribute>();
      ex.todo = 0; ex.value = pdc; Exchange(ex); ex.attris.Clear();
    }
    internal object GetProp(string name)
    {
      ex.todo = 1; ex.name = name; Exchange(ex);
      return ex.todo != 1 ? ex.value : null;
    }
    internal bool SetProp(string name, object value)
    {
      ex.todo = 2; ex.name = name; ex.value = value; Exchange(ex);
      if (ex.todo == 2) return false;
      view.Invalidate(Inval.Properties);
      return true;
    }
    internal static string GetData(Action<IExchange> func)
    {
      if (func == null) return null;
      var e = new XElement("x"); ex.todo = 3; ex.value = e;
      try { func(ex); } catch { }
      return e.HasAttributes ? e.ToString() : null;
    }
    internal static void SetData(Action<IExchange> func, string s)
    {
      if (func == null) return;
      var t1 = ex.todo; var t2 = ex.value;
      try { ex.todo = 4; ex.value = XElement.Parse(s); func(ex); } catch { }
      ex.todo = t1; ex.value = t2;
    }
    static readonly EX ex = new EX();
    class EX : IExchange
    {
      internal int todo; internal string name; internal object value; internal List<Attribute> attris;
      bool IExchange.Category(string name)
      {
        if (todo < 0) return false;
        if (todo == 0) { attris.RemoveAll(p => p is CategoryAttribute); attris.Add(new CategoryAttribute(name)); }
        return true;
      }
      bool IExchange.DisplayName(string name)
      {
        if (todo != 0) return false;
        if (name != null) attris.Add(new DisplayNameAttribute(name)); return true;
      }
      void IExchange.Description(string text)
      {
        if (todo == 0) attris.Add(new DescriptionAttribute(text));
      }
      void IExchange.TypeConverter(Type t)
      {
        if (todo == 0) attris.Add(new TypeConverterAttribute(t));
      }
      void IExchange.ReadOnly()
      {
        if (todo == 0) attris.Add(new ReadOnlyAttribute(true));
        else if (todo == 3) todo = 5;
      }
      void IExchange.Format(string text)
      {
        if (todo != 0) return;
        attris.Add(new TypeConverterAttribute(typeof(FormatConverter)));
        attris.Add(new AmbientValueAttribute(text));
      }
      bool IExchange.Exchange<T>(string name, ref T value)
      {
        switch (todo)
        {
          case 0: create(name, typeof(T)); break;
          case 1: if (name == this.name) { this.value = value; todo = -1; } break;
          case 2: if (name == this.name) { value = (T)this.value; todo = -2; return true; } break;
          case 3: save(name, value, typeof(T)); break;
          case 4: if (load(name, typeof(T)) is T p) value = p; break;
          case 5: todo = 3; break;
        }
        return false;
      }
      void IExchange.Annotation<T>(string name, T value)
      {
        if (todo == 3) save(name, value, typeof(T));
      }
      bool IExchange.Modified { get => todo == -2; }
      void create(string name, Type t)
      {
        for (var a = t; a.IsArray; a = a.GetElementType())
        {
          if (TypeDescriptor.GetConverter(a) is ArrayConverter) break;
          TypeDescriptor.AddAttributes(a, new TypeConverterAttribute(typeof(ArrayConverter)));
          TypeDescriptor.AddAttributes(a, new EditorAttribute(typeof(ArrayEditor), typeof(System.Drawing.Design.UITypeEditor)));
        }
        ((PropertyDescriptorCollection)value).Add(new PD(name, attris.ToArray()) { type = t });
        attris.RemoveAll(p => !(p is CategoryAttribute));
      }
      static string savea(object value, Type t)
      {
        if (!t.IsArray) return TypeDescriptor.GetConverter(t).ConvertToInvariantString(value);
        t = t.GetElementType(); return $"{{{string.Join("}{", ((Array)value).Cast<object>().Select(p => savea(p, t)))}}}";
      }
      static ReadOnlySpan<char> take(ref ReadOnlySpan<char> a, string sep)
      {
        var x = a.IndexOf(sep.AsSpan()); var r = a.Slice(0, x != -1 ? x : a.Length);
        a = a.Slice(x != -1 ? x + sep.Length : a.Length); return r;
      }
      static object loada(ReadOnlySpan<char> s, Type t)
      {
        if (!t.IsArray) return TypeDescriptor.GetConverter(t).ConvertFromInvariantString(s.ToString());
        int c = 0; for (int i = 0, k = 0; i < s.Length; i++) if (s[i] == '{') k++; else if (s[i] == '}' && --k == 0) c++;
        var a = (Array)Activator.CreateInstance(t, c); t = t.GetElementType();
        for (int i = 0, k = 0, j = 0, l = 0; i < s.Length; i++)
          if (s[i] == '{') { if (k++ == 0) l = i + 1; }
          else if (s[i] == '}' && --k == 0) a.SetValue(loada(s.Slice(l, i - l), t), j++);
        return a;
      }
      void save(string name, object value, Type t)
      {
        if (value == null) return;
        if (t.IsArray) value = savea(value, t);
        else
        {
          var c = TypeDescriptor.GetConverter(t); if (!c.CanConvertFrom(typeof(string))) return;
          value = c.ConvertToInvariantString(value);
        }
        name = name.Replace(' ', '_'); //name = System.Xml.XmlConvert.EncodeName(name);
        ((XElement)this.value).SetAttributeValue(name, value);
      }
      object load(string name, Type t)
      {
        name = name.Replace(' ', '_'); //name = System.Xml.XmlConvert.EncodeName(name);
        var a = ((XElement)value).Attribute(name); if (a == null) return null;
        if (t.IsArray)
        {
          var p = loada(a.Value.AsSpan(), t); return p;
        }
        else
        {
          var c = TypeDescriptor.GetConverter(t);
          var p = c.ConvertFromInvariantString(a.Value); return p;
        }
      }
      class PD : PropertyDescriptor
      {
        internal Type type;
        public PD(string s, Attribute[] a) : base(s, a) { }
        public override Type ComponentType => typeof(NodeBase);
        public override Type PropertyType => type;
        public override bool IsReadOnly => Attributes.OfType<ReadOnlyAttribute>().Any();
        public override bool ShouldSerializeValue(object component) => true;
        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component) { }
        public override object GetValue(object component)
        {
          return ((NodeBase)component).GetProp(Name);
        }
        public override void SetValue(object component, object value)
        {
          if (value is Func<object, object> x) if ((value = x(component)) == null) return;
          var node = (NodeBase)component; var name = Name; var up = node.view.undopos();
          var old = node.GetProp(name); if (!node.SetProp(name, value)) return;
          if (node.view.undopos() > up) return;
          if (old != null && (value = node.GetProp(name)) != null && old.Equals(value)) return;
          node.view.AddUndo(() => { var t = node.GetProp(name); node.SetProp(name, old); old = t; });
        }
      }
    }
  }

  unsafe class Node : NodeBase, ICustomTypeDescriptor,
    ISite, IServiceProvider, IComponent, IMenuCommandService
  {
    //~Node() { Debug.WriteLine("~Node()"); }
    private Node() { }
    internal static Node From(CDXView v, INode p)
    {
      if (p.Tag is Node node) return node;
      node = new Node { view = v, ptr = Marshal.GetIUnknownForObject(p) };
      Marshal.Release(node.ptr); p.Tag = node; return node;
    }
    IntPtr ptr;
    internal INode node => (INode)Marshal.GetObjectForIUnknown(ptr);
    internal object[] funcs;

    internal protected INode this[string name]
    {
      get { for (var p = node.Child; p != null; p = p.Next) if (p.Name == name) return p; return null; }
    }
    internal protected void SetMesh(float3[] pp, int[] ii, float2[] tt = null) => node.SetMesh(pp, ii, tt);
    internal protected void Invalidate(Inval fl = Inval.Render)
    {
      if ((fl & Inval.PropertySet) != 0) RemoveAnnotations(typeof(PropertyDescriptorCollection));
      //if ((fl & Inval.Properties) != 0) node.RemoveBuffer(BUFFER.SCRIPTDATA);
      view.Invalidate(fl);
    }
    internal protected void Animate(Action<int> act)
    {
      if (animation != null) view.animations -= animation;
      animation = act;
      if (animation != null) view.animations += animation;
    }
    internal protected void AddUndo(Action p)
    {
      view.AddUndo(p);
    }

    internal protected void WriteLine(string s)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
      var wnd = Microsoft.VisualStudio.Shell.Package.GetGlobalService(
        typeof(Microsoft.VisualStudio.Shell.Interop.SVsOutputWindow)) as
        Microsoft.VisualStudio.Shell.Interop.IVsOutputWindow;
      // GUID_OutWindowGeneralPane
      var guid = Microsoft.VisualStudio.VSConstants.GUID_OutWindowDebugPane;
      wnd.GetPane(ref guid, out var pane);
      pane.OutputString(s);
      pane.OutputString("\n");
      pane.Activate();
    }

    Action<int> animation
    {
      get => Annotation<Action<int>>();
      set => SetAnnotation(value);
    }
    internal void oninsert()
    {
      if (animation != null) view.animations += animation;
    }
    internal void onremove()
    {
      if (animation != null) view.animations -= animation;
      RemoveAnnotations(typeof(PropertyDescriptorCollection));
    }

    object annotations;
    internal void AddAnnotation(object p)
    {
      if (annotations == null)
      {
        annotations = p is object[]? new object[] { p } : p;
      }
      else
      {
        object[] a = annotations as object[];
        if (a == null)
        {
          annotations = new object[] { annotations, p };
        }
        else
        {
          int i = 0;
          while (i < a.Length && a[i] != null) i++;
          if (i == a.Length)
          {
            Array.Resize(ref a, i * 2);
            annotations = a;
          }
          a[i] = p;
        }
      }
    }
    internal object Annotation(Type t)
    {
      if (annotations == null) return null;
      var a = annotations as object[];
      if (a == null)
      {
        if (t.IsInstanceOfType(annotations)) return annotations;
        return null;
      }
      for (int i = 0; i < a.Length; i++)
      {
        var obj = a[i];
        if (obj == null) break;
        if (t.IsInstanceOfType(obj)) return obj;
      }
      return null;
    }
    internal T Annotation<T>() where T : class => Annotation(typeof(T)) as T;
    internal void SetAnnotation<T>(T p)
    {
      RemoveAnnotations(typeof(T)); if (p != null) AddAnnotation(p);
    }
    internal void RemoveAnnotations(Type type)
    {
      if (annotations != null)
      {
        var a = annotations as object[];
        if (a == null)
        {
          if (type.IsInstanceOfType(annotations)) annotations = null;
        }
        else
        {
          int i = 0, j = 0;
          while (i < a.Length)
          {
            object obj = a[i];
            if (obj == null) break;
            if (!type.IsInstanceOfType(obj)) a[j++] = obj;
            i++;
          }
          if (j == 0)
          {
            annotations = null;
          }
          else
          {
            while (j < i) a[j++] = null;
          }
        }
      }
    }

    internal string getcode()
    {
      var bs = node.GetBuffer(CDX.BUFFER.SCRIPT);
      return bs != null ? Encoding.UTF8.GetString(bs.ToBytes()) : string.Empty;
    }
    object[] getfuncs()
    {
      if (funcs != null) return funcs;
      var node = this.node;
      var bs = node.GetBuffer(BUFFER.SCRIPT);
      if (bs != null)
      {
        try
        {
          var debug = Script.dbg != null;
          var ctor = !debug ? bs.Tag as Func<Node, object[]> : null;
          if (ctor == null)
          {
            var code = Encoding.UTF8.GetString(bs.ToBytes());
            var expr = Script.Compile(GetType(), code);
            ctor = expr.Compile(); if (!debug) bs.Tag = ctor;
          }
          funcs = ctor(this); if (debug) funcs[0] = null;
          var bd = node.GetBytes(BUFFER.SCRIPTDATA);
          if (bd != null)
          {
            var code = Encoding.UTF8.GetString(bd);
            SetData(GetMethod<Action<IExchange>>(), code);
          }
          return funcs;
        }
        catch (Exception e) { Debug.WriteLine(e.Message); }
      }
      funcs = Array.Empty<object>();
      return funcs;
    }

    internal T GetMethod<T>() where T : Delegate
    {
      var a = getfuncs(); for (int i = 1; i < a.Length; i++) if (a[i] is T t) return t; return null;
    }

    override protected void Exchange(IExchange e)
    {
      GetMethod<Action<IExchange>>()?.Invoke(e);
      var node = this.node;
      if (e.Category("General"))
      {
        var s = node.Name; e.DisplayName("Name"); if (e.Exchange(".N", ref s)) { node.Name = s != string.Empty ? s : null; Invalidate(Inval.Tree); }
        var b = node.IsStatic; e.DisplayName("Static"); if (e.Exchange(".S", ref b)) node.IsStatic = b;
      }
      if (node.HasBuffer(BUFFER.POINTBUFFER) && e.Category("Material"))
      {
        var c = System.Drawing.Color.FromArgb(unchecked((int)node.Color));
        e.DisplayName("Color"); if (e.Exchange(".c", ref c)) node.Color = (uint)c.ToArgb();
        var t = node.GetBuffer(BUFFER.TEXTURE);
        e.TypeConverter(typeof(TexturConverter));
        e.DisplayName("Texture"); if (e.Exchange(".t", ref t))
        {
          if (t != null) node.SetBuffer(t);
          else node.RemoveBuffer(BUFFER.TEXTURE); Invalidate(Inval.PropertySet);
        }
      }
      if (node.HasBuffer(BUFFER.CAMERA) && e.Category("Camera"))
      {
        BUFFERCAMERA* t; node.GetBufferPtr(BUFFER.CAMERA, (void**)&t); var cd = *t;
        if (e.Exchange("Fov", ref cd.fov) ||
            e.Exchange("NearPlane", ref cd.near) ||
            e.Exchange("FarPlane", ref cd.far))
          node.SetBufferPtr(BUFFER.CAMERA, &cd, sizeof(BUFFERCAMERA));
      }
      if (node.HasBuffer(BUFFER.LIGHT) && e.Category("Light"))
      {
        BUFFERLIGHT* t; node.GetBufferPtr(BUFFER.LIGHT, (void**)&t); var ld = *t;
        if (e.Exchange("Light Color", ref ld.a))
          node.SetBufferPtr(BUFFER.LIGHT, &ld, sizeof(BUFFERLIGHT));
      }
      if (e.Category("Transform"))
      {
        var m = node.GetTypeTransform(1);
        var a = m.my * (180 / Math.PI); var d = false;
        e.DisplayName("Position"); d |= e.Exchange(".p", ref *(float3*)&m._41);
        e.DisplayName("Rotation"); if (e.Exchange(".a", ref a)) { *(float3*)&m._21 = a * (Math.PI / 180); d = true; }
        e.DisplayName("Scaling"); //e.Format(".s"); 
        d |= e.Exchange(".s", ref *(float3*)&m._11);

        //e.DisplayName("x"); d |= e.Exchange(".x", ref m._41);
        //e.DisplayName("y"); d |= e.Exchange(".y", ref m._42);
        //e.DisplayName("z"); d |= e.Exchange(".z", ref m._43);
        //e.DisplayName("α"); e.Format("{0:0.##} °"); if (e.Exchange(".α", ref u.x)) { m._21 = u.x * (float)(Math.PI / 180); d = true; }
        //e.DisplayName("β"); e.Format("{0:0.##} °"); if (e.Exchange(".β", ref u.y)) { m._22 = u.y * (float)(Math.PI / 180); d = true; }
        //e.DisplayName("γ"); e.Format("{0:0.##} °"); if (e.Exchange(".γ", ref u.z)) { m._23 = u.z * (float)(Math.PI / 180); d = true; }
        //e.DisplayName("δ"); e.Format(".s"); d |= e.Exchange(".s", ref *(float3*)&m._11);
        if (d)
        {
          var t1 = node.GetTypeTransform(0); node.SetTypeTransform(0, m);
          var t2 = node.Transform; node.SetTypeTransform(0, t2);
          var t3 = node.GetTypeTransform(1); node.SetTypeTransform(0, t1);
          if (t3 == m) m = t2;
          view.Execute(CDXView.undo(node, m));
        }
      }
    }

    string ICustomTypeDescriptor.GetClassName()
    {
      return node.GetClassName();// GetType().Name;
    }
    string ICustomTypeDescriptor.GetComponentName()
    {
      return node.Name ?? "(noname)";
    }
    object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
    {
      if (editorBaseType == typeof(System.ComponentModel.ComponentEditor) && view.OnInfo(this) != 0)
        return new ComponentEditor();
      return TypeDescriptor.GetEditor(GetType(), editorBaseType);
    }
    class ComponentEditor : System.ComponentModel.ComponentEditor
    {
      public override bool EditComponent(ITypeDescriptorContext context, object component) { ((Node)component).view.OnInfo(null); return false; }
    }
    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
    {
      var pdc = Annotation<PropertyDescriptorCollection>();
      if (pdc != null) return pdc;
      pdc = new PropertyDescriptorCollection(null);
      GetProps(pdc); AddAnnotation(pdc); return pdc;
    }

    ISite IComponent.Site { get => this; set { } }
    object IServiceProvider.GetService(Type t)
    {
      if (t == typeof(IMenuCommandService))
        if (funcs != null && funcs.OfType<Action>().Any())
          return this;
      return null;
    }
    DesignerVerbCollection IMenuCommandService.Verbs
    {
      get
      {
        var verbs = new DesignerVerbCollection();
        if (funcs != null) foreach (var p in funcs.OfType<Action>())
            verbs.Add(new DesignerVerb(p.Method.Name, (o, e) => invoke(p)));
        return verbs;
      }
    }
    void invoke(Action p)
    {
      try { p(); view.Invalidate(0); }
      catch (Exception e) { view.MessageBox(e.Message); }
    }
    event EventHandler IComponent.Disposed { add { } remove { } }
    IComponent ISite.Component => null;
    IContainer ISite.Container => null;
    bool ISite.DesignMode => false;
    string ISite.Name { get => node.Name; set { } }
    void IMenuCommandService.AddCommand(MenuCommand command) { }
    void IMenuCommandService.AddVerb(DesignerVerb verb) { }
    MenuCommand IMenuCommandService.FindCommand(CommandID commandID) => null;
    bool IMenuCommandService.GlobalInvoke(CommandID commandID) => false;
    void IMenuCommandService.RemoveCommand(MenuCommand command) { }
    void IMenuCommandService.RemoveVerb(DesignerVerb verb) { }
    void IMenuCommandService.ShowContextMenu(CommandID menuID, int x, int y) { }
    void IDisposable.Dispose() { }
  }
}
