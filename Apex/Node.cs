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
    bool GetModified();
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
      if (!(name[0]=='.' || name[0] == '_') && this is Node n)
      {
        ex.todo = 3; ex.value = n.node; Exchange(ex); //save prop
      }
      view.Invalidate(Inval.Properties);
      return true;
    }
    internal static void SaveProps(INode node, Action<IExchange> func)
    {
      if (func == null) return;
      var t1 = ex.todo; var t2 = ex.name; var t3 = ex.value; //Update in exchange
      try { ex.todo = 3; ex.name = null; ex.value = node; func(ex); }
      catch { }
      ex.todo = t1; ex.name = t2; ex.value = t3;
    }
    internal static void LoadProps(INode node, Action<IExchange> func)
    {
      if (func == null) return;
      var t1 = ex.todo; var t2 = ex.name; var t3 = ex.value;
      try { ex.todo = 4; ex.name = null; ex.value = node; func(ex); }
      catch { }
      ex.todo = t1; ex.name = t2; ex.value = t3;
    }
    internal static void CompactProps(INode node, Action<IExchange> func)
    {
      var l1 = new List<string>();
      if (func != null) { ex.todo = 6; ex.value = l1; func(ex); }
      var l2 = (node.GetProps() ?? string.Empty).Split('\n');
      var l3 = l2.Except(l1).ToList();
      foreach (var p in l3) fixed (char* s = p) node.SetProp(s, null, 0, 0);
    }
    static readonly Ex ex = new Ex();
    class Ex : IExchange
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
        attris.Add(name != null ? (Attribute)new DisplayNameAttribute(name) : new BrowsableAttribute(false));
        return true;
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
          case 3: save(name, value); break;
          case 4: load(name, ref value); break;
          case 5: todo = 3; break; //skip readonly
          case 6: ((List<string>)this.value).Add(name); break;
        }
        return false;
      }
      bool IExchange.Modified { get => todo == -2; }
      bool IExchange.GetModified() { if (todo != -2) return false; todo = -1; return true; }
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
      void save<T>(string name, T v)
      {
        if (name[0] == '_') return;
        if (this.name != null) { if (name != this.name) return; todo = -1; }
        var node = (INode)this.value; var t = typeof(T);
        if (blittable(t))
        {
          var n = Marshal.SizeOf(t); var r = __makeref(v);
          fixed (char* ss = name) node.SetProp(ss, *(void**)&r, n, (int)Type.GetTypeCode(t));
        }
        else writeobj(node, name, v, t);
      }
      void load<T>(string name, ref T v)
      {
        if (name[0] == '_') return;
        var node = (INode)this.value;
        void* p; int n, typ; fixed (char* ss = name) n = node.GetProp(ss, &p, out typ);
        if (p == null) return;
        var t = typeof(T);
        if (blittable(t))
        {
          if (n == Marshal.SizeOf(t) && typ == (int)Type.GetTypeCode(t))
          {
            var h = default(T); var r = __makeref(h);
            Native.memcpy(*(void**)&r, p, (void*)n); v = h;
          }
        }
        else
        {
          var str = (p: (IntPtr)p, i: 0, n: n);
          if (readobj(ref str, t) is T x) v = x;
        }
      }
      static bool blittable(Type t) => t.IsValueType && t.IsLayoutSequential; //t.IsLayoutSequential || t.IsExplicitLayout
      static object readobj(ref (IntPtr p, int i, int n) str, Type t)
      {
        if (t.IsArray)
        {
          var c = readcount(ref str); var e = t.GetElementType();
          var a = Array.CreateInstance(e, c);
          if (blittable(e))
          {
            var n = Marshal.SizeOf(e); var h = GCHandle.Alloc(a, GCHandleType.Pinned);
            var p = (byte*)h.AddrOfPinnedObject(); read(ref str, p, a.Length * n); h.Free();
          }
          else
          {
            for (int i = 0; i < c; i++) a.SetValue(readobj(ref str, e), i);
          }
          return a;
        }
        var ns = readcount(ref str);
        var ss = new string(' ', ns); fixed (char* p = ss) for (int i = 0; i < ns; i++) p[i] = (char)readcount(ref str);
        var co = TypeDescriptor.GetConverter(t);
        var po = co.ConvertFromInvariantString(ss);
        return po;
      }
      static void read(ref (IntPtr p, int i, int n) str, byte* p, int n)
      {
        if (str.i + n > str.n) throw new Exception();
        Native.memcpy(p, (byte*)str.p.ToPointer() + str.i, (void*)n); str.i += n;
      }
      static int readcount(ref (IntPtr p, int i, int n) str)
      {
        int i = 0; var pp = (byte*)str.p.ToPointer();
        for (int s = 0; ; s += 7) { int b = pp[str.i++]; i |= (b & 0x7F) << s; if ((b & 0x80) == 0) break; }
        return i;
      }

      //static WeakRef<byte[]> wbytes;
      static void writeobj(INode node, string name, object o, Type t)
      {
        var a = GetBuffer<byte>(1024); //var a = wbytes.Value; if (a == null) wbytes.Value = a = new byte[256];
        var str = (p: a, i: 0); writeobj(ref str, o);
        fixed (void* aa = str.p) fixed (char* ss = name)
          node.SetProp(ss, aa, str.i, (int)Type.GetTypeCode(t));
        str.p.Release();
      }
      static void writeobj(ref (byte[] a, int i) str, object o)
      {
        if (o is Array a)
        {
          writecount(ref str, a.Length);
          var e = a.GetType().GetElementType();
          if (blittable(e))
          {
            var n = Marshal.SizeOf(e); var h = GCHandle.Alloc(a, GCHandleType.Pinned);
            var p = (byte*)h.AddrOfPinnedObject(); write(ref str, p, a.Length * n); h.Free();
          }
          else
          {
            for (int i = 0; i < a.Length; i++) writeobj(ref str, a.GetValue(i));
          }
          return;
        }
        var co = TypeDescriptor.GetConverter(o.GetType());
        if (co.CanConvertFrom(typeof(string)))
        {
          var ss = co.ConvertToInvariantString(o);
          writecount(ref str, ss.Length); for (int i = 0; i < ss.Length; i++) writecount(ref str, ss[i]);
          return;
        }
        return;
      }
      static void write(ref (byte[] a, int i) str, void* p, int n)
      {
        while (str.a.Length < str.i + n) Array.Resize(ref str.a, str.a.Length << 1);
        fixed (byte* t = str.a) Native.memcpy(t + str.i, p, (void*)n); str.i += n;
      }
      static void writecount(ref (byte[] a, int i) str, int c)
      {
        if (str.i + 5 > str.a.Length) Array.Resize(ref str.a, str.a.Length << 1);
        for (; c >= 0x80; str.a[str.i++] = (byte)(c | 0x80), c >>= 7) ; str.a[str.i++] = (byte)c;
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
          if (value is Func<PropertyDescriptor, object, object> f) if ((value = f(this, component)) == null) return;
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
    internal ScriptEditor editor;

    internal protected INode this[string name]
    {
      get { for (var p = node.Child; p != null; p = p.Next) if (p.Name == name) return p; return null; }
    }
    internal protected void SetMesh(float3[] pp, int[] ii, float2[] tt = null) => node.SetMesh(pp, ii, tt);
    internal protected void Invalidate(Inval fl = Inval.Render)
    {
      if ((fl & Inval.PropertySet) != 0) wpdc.Value = null;//RemoveAnnotations(typeof(PropertyDescriptorCollection));
      view.Invalidate(fl);
    }
    internal protected void AddUndo(Action p)
    {
      view.AddUndo(p);
    }
    internal string getcode()
    {
      var bs = node.GetBuffer(CDX.BUFFER.SCRIPT);
      return bs != null ? Encoding.UTF8.GetString(bs.GetArray<byte>()) : string.Empty;
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
            var code = Encoding.UTF8.GetString(bs.GetArray<byte>());
            var expr = Script.Compile(GetType(), code);
            ctor = expr.Compile(); if (!debug) bs.Tag = ctor;
          }
          funcs = ctor(this); if (debug) funcs[0] = null;
          LoadProps(node, GetMethod<Action<IExchange>>());
          return funcs;
        }
        catch (Exception e) { Debug.WriteLine(e.Message); }
      }
      funcs = Array.Empty<object>();
      return funcs;
    }
    internal void disable(Delegate t) => funcs[Array.IndexOf(funcs, t)] = null;

    internal T GetMethod<T>() where T : Delegate
    {
      var a = getfuncs(); for (int i = 1; i < a.Length; i++) if (a[i] is T t) return t; return null;
    }

    override protected void Exchange(IExchange e)
    {
      GetMethod<Action<IExchange>>()?.Invoke(e);
      var node = this.node;
      //if(e.Modified) { var t = node.GetBuffer(BUFFER.SCRIPTDATA);  }
      if (e.Category("General"))
      {
        var s = node.Name; e.DisplayName("Name"); if (e.Exchange(".n", ref s)) { node.Name = s != string.Empty ? s : null; Invalidate(Inval.Tree); }
        var b = node.IsStatic; e.DisplayName("Static"); if (e.Exchange(".f", ref b)) node.IsStatic = b;
      }
      if (node.HasBuffer(BUFFER.POINTBUFFER))
      {
        if (e.Category("Material"))
        {
          Range* rr; var nr = node.GetBufferPtr(BUFFER.RANGES, (void**)&rr) / sizeof(Range);
          if (nr > 1)
          {
            for (int i = 0; i < nr; i++)
            {
              var cc = System.Drawing.Color.FromArgb(unchecked((int)rr[i].Color));
              e.DisplayName($"Color{(i + 1):00}"); if (e.Exchange(".c" + i, ref cc))
              {
                var a = node.GetArray<Range>(BUFFER.RANGES); a[i].Color = (uint)cc.ToArgb();
                node.SetArray(BUFFER.RANGES, a);
              }
              if (i >= 15) continue;
              var t = node.GetBuffer(BUFFER.TEXTURE + i);
              e.TypeConverter(typeof(TexturConverter));
              e.DisplayName($"Texture{(i + 1):00}"); if (e.Exchange(".t" + i, ref t))
              {
                if (node.HasBuffer(BUFFER.TEXTURE + i) != (t != null)) Invalidate(Inval.PropertySet);
                node.SetBuffer(BUFFER.TEXTURE + i, t);
              }
            }
          }
          else
          {
            var c = System.Drawing.Color.FromArgb(unchecked((int)node.Color));
            e.DisplayName("Color"); if (e.Exchange(".c", ref c)) node.Color = (uint)c.ToArgb();
            var t = node.GetBuffer(BUFFER.TEXTURE);
            e.TypeConverter(typeof(TexturConverter));
            e.DisplayName("Texture"); if (e.Exchange(".t", ref t))
            {
              if (node.HasBuffer(BUFFER.TEXTURE) != (t != null)) Invalidate(Inval.PropertySet);
              node.SetBuffer(BUFFER.TEXTURE, t);
            }
          }
        }
      }
      //if (node.HasProp("@cfov") && e.Category("Camera")) excam(e, node); 
      if (e.Category("Transform"))
      {
        var m = node.GetTypeTransform(1);
        var a = m.my * (180 / Math.PI); var d = false;
        e.DisplayName("Position"); d |= e.Exchange(".p", ref *(float3*)&m._41);
        e.DisplayName("Rotation"); if (e.Exchange(".a", ref a)) { *(float3*)&m._21 = a * (Math.PI / 180); d = true; }
        e.DisplayName("Scaling"); //e.Format(".s"); 
        d |= e.Exchange(".s", ref *(float3*)&m._11);
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
      if (editorBaseType == typeof(System.ComponentModel.ComponentEditor) && view.OnCommand(2305, this) != 0)
        return new ComponentEditor();
      return TypeDescriptor.GetEditor(GetType(), editorBaseType);
    }
    class ComponentEditor : System.ComponentModel.ComponentEditor
    {
      public override bool EditComponent(ITypeDescriptorContext context, object component)
      {
        ((Node)component).view.OnCommand(2305, null); return false;
      }
    }
    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
    {
      var pdc = wpdc.Value; if (pdc == null) GetProps(wpdc.Value = pdc = new PropertyDescriptorCollection(null));
      return pdc;
    }
    WeakRef<PropertyDescriptorCollection> wpdc;

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
