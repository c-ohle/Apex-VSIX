using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Apex
{
#if(false)
  [Guid("61C47B35-B845-44E3-A4DE-7C8751B64A44")]
  public class ToolsToolWindowPane : Microsoft.VisualStudio.Shell.ToolWindowPane
  {
    PropertyGrid grid;
    public override IWin32Window Window => grid;
    public ToolsToolWindowPane() : base(null)
    {
      this.Caption = "Apex Tools";
      using (DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware)) grid = new PropertyGrid();
      grid.ToolbarVisible = false;
      grid.Font = System.Drawing.SystemFonts.MenuFont;
      grid.PropertySort = PropertySort.Categorized;
      grid.HelpVisible = false;
      grid.DisabledItemForeColor = System.Drawing.Color.Black;
      grid.SelectedObject = new ToolProvider();
      ((Control)grid).Controls[2].MouseDoubleClick += (p, e) =>
      {
        var t = grid.SelectedGridItem;
        if (t?.PropertyDescriptor is ToolProvider.PD pd && !pd.edit)
        {
          pd.edit = true; grid.Refresh();
        }
      };
      grid.SelectedGridItemChanged += (p, e) =>
      {
        if (e.OldSelection?.PropertyDescriptor is ToolProvider.PD pd)
          pd.edit = false;
      };
      //var items = (grid.ContextMenuStrip = new ContextMenuStrip()).Items;
      //items.Add(new ToolStripMenuItem("Edit Values", null, (p,e) => 
      //{ 
      //  ToolProvider.edit ^= true; ((ToolStripMenuItem)p).Checked = ToolProvider.edit;
      //  grid.Refresh(); }));
      //items.Add(new ToolStripMenuItem("Reset Values", null, (p, e) => { }));
    }
  }
#endif

  enum ToolEnum
  {
    None,
    SelectRect,
    CameraMoveHorizontal,
    CameraMoveVertical,
    CameraRotateHorizontal,
    CameraRotateVerical,
    CameraRotateDirectional,
    CameraMoveXAxis,
    CameraMoveYAxis,
    CameraMoveZAxis,
    CameraSelectionRotateHorizontal,
    CameraSelectionRotateVertical,
    ObjectMoveHorizontal,
    ObjectMoveVertical,
    ObjectDragDrop,
    ObjectRotateHorizontal,
    ObjectMoveXAxis,
    ObjectMoveYAxis,
    ObjectMoveZAxis,
    ObjectRotateXAxis,
    ObjectRotateYAxis,
    ObjectRotateZAxis,
  }
  //[Flags]
  enum ToolFlags
  {
    GroundClick = 1, ObjectClick = 2,
    TouchpadGround = 4, TouchpadSelection = 8, TouchpadAlways = 16
  }

  class SimpleTypeDescriptor : ICustomTypeDescriptor
  {
    AttributeCollection ICustomTypeDescriptor.GetAttributes()
    {
      return TypeDescriptor.GetAttributes(GetType());
    }
    string ICustomTypeDescriptor.GetClassName()
    {
      return TypeDescriptor.GetClassName(GetType());
    }
    string ICustomTypeDescriptor.GetComponentName()
    {
      return TypeDescriptor.GetComponentName(GetType());
    }
    TypeConverter ICustomTypeDescriptor.GetConverter()
    {
      return TypeDescriptor.GetConverter(GetType());
    }
    EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
    {
      return TypeDescriptor.GetDefaultEvent(GetType());
    }
    PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
    {
      return TypeDescriptor.GetDefaultProperty(GetType());
    }
    object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
    {
      return TypeDescriptor.GetEditor(GetType(), editorBaseType);
    }
    EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
    {
      return TypeDescriptor.GetEvents(GetType());
    }
    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
    {
      return TypeDescriptor.GetEvents(GetType(), attributes);
    }
    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
    {
      return this;
    }
    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
    {
      return TypeDescriptor.GetProperties(GetType());
    }
    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
    {
      return TypeDescriptor.GetProperties(GetType(), attributes);
    }
  }

  class ToolProvider //: SimpleTypeDescriptor, ICustomTypeDescriptor
  {
    //internal static bool edit;
    internal static (ToolEnum i, Keys k, ToolFlags f)[] kvs = new (ToolEnum, Keys, ToolFlags)[] {
      //ground click
      (ToolEnum.SelectRect, Keys.LButton, ToolFlags.GroundClick),
      (ToolEnum.CameraMoveHorizontal, Keys.Control | Keys.LButton, ToolFlags.GroundClick),
      (ToolEnum.CameraMoveVertical, Keys.Shift | Keys.LButton, ToolFlags.GroundClick),
      (ToolEnum.CameraRotateHorizontal, Keys.Alt | Keys.LButton, ToolFlags.GroundClick),
      (ToolEnum.CameraRotateVerical, Keys.Control | Keys.Shift | Keys.LButton, ToolFlags.GroundClick),
      //object click
      (ToolEnum.ObjectMoveHorizontal, Keys.LButton, ToolFlags.ObjectClick),
      (ToolEnum.ObjectMoveVertical, Keys.Shift | Keys.LButton, ToolFlags.ObjectClick),
      (ToolEnum.ObjectDragDrop, Keys.Control | Keys.LButton, ToolFlags.ObjectClick),
      (ToolEnum.ObjectRotateHorizontal, Keys.Alt | Keys.LButton, ToolFlags.ObjectClick),
      (ToolEnum.ObjectRotateXAxis, Keys.Control | Keys.Shift| Keys.LButton, ToolFlags.ObjectClick),
      (ToolEnum.ObjectRotateYAxis, Keys.Control | Keys.Alt | Keys.LButton, ToolFlags.ObjectClick),
      (ToolEnum.ObjectRotateZAxis, Keys.Control | Keys.Shift | Keys.Alt | Keys.LButton, ToolFlags.ObjectClick),
      //touchpad always
      (ToolEnum.CameraMoveHorizontal, Keys.Space, ToolFlags.TouchpadAlways),
      (ToolEnum.CameraMoveVertical, Keys.Shift | Keys.Space, ToolFlags.TouchpadAlways),
      (ToolEnum.CameraRotateHorizontal, Keys.A, ToolFlags.TouchpadAlways),
      (ToolEnum.CameraRotateVerical, Keys.Shift | Keys.A, ToolFlags.TouchpadAlways),
      (ToolEnum.CameraRotateDirectional, Keys.W, ToolFlags.TouchpadAlways),
      (ToolEnum.CameraSelectionRotateHorizontal, Keys.Q, ToolFlags.TouchpadAlways),
      (ToolEnum.CameraSelectionRotateVertical, Keys.Shift | Keys.Q, ToolFlags.TouchpadAlways),
      //touchpad over selection
      (ToolEnum.ObjectMoveVertical, Keys.V, ToolFlags.TouchpadSelection),
      (ToolEnum.ObjectMoveXAxis, Keys.X, ToolFlags.TouchpadSelection),
      (ToolEnum.ObjectMoveYAxis, Keys.Y, ToolFlags.TouchpadSelection),
      (ToolEnum.ObjectMoveZAxis, Keys.Z, ToolFlags.TouchpadSelection),
      (ToolEnum.ObjectRotateXAxis, Keys.Shift | Keys.X, ToolFlags.TouchpadSelection),
      (ToolEnum.ObjectRotateYAxis, Keys.Shift | Keys.Y, ToolFlags.TouchpadSelection),
      (ToolEnum.ObjectRotateZAxis, Keys.Shift | Keys.Z, ToolFlags.TouchpadSelection),
      //touchpad over ground
      (ToolEnum.CameraMoveVertical, Keys.V, ToolFlags.TouchpadGround),
      (ToolEnum.CameraMoveXAxis, Keys.X, ToolFlags.TouchpadGround),
      (ToolEnum.CameraMoveYAxis, Keys.Y, ToolFlags.TouchpadGround),
      (ToolEnum.CameraMoveZAxis, Keys.Z, ToolFlags.TouchpadGround),
    };

    //internal class PD : PropertyDescriptor
    //{
    //  int i; Keys old; internal bool edit;
    //  internal PD(int i) : base(i.ToString(), null) { old = kvs[this.i = i].k; }
    //  public override Type ComponentType => typeof(ToolProvider);
    //  public override bool IsReadOnly => !edit;
    //  public override Type PropertyType => typeof(Keys);
    //  public override bool CanResetValue(object component) => true;
    //  public override void ResetValue(object component) => kvs[i].k = old;
    //  public override bool ShouldSerializeValue(object component) => kvs[i].k != old;
    //  static unsafe string nice(string s)
    //  {
    //    int ns = 0; var ss = stackalloc char[s.Length << 1];
    //    for (int i = 0; i < s.Length; i++)
    //    {
    //      if (i != 0 && char.IsUpper(s[i])) ss[ns++] = char.IsUpper(s[i - 1]) ? '-' : ' ';
    //      ss[ns++] = s[i];
    //    }
    //    return new string(ss);
    //  }
    //  public override string DisplayName => nice(kvs[i].i.ToString()) + "\0\n" + Name;
    //  public override string Category => nice(kvs[i].f.ToString());
    //  public override object GetValue(object component) => kvs[i].k;
    //  public override void SetValue(object component, object value)
    //  {
    //    if (!(value is Keys k)) k = 0; kvs[i].k = k != 0 ? k : old;
    //  }
    //}
    //PropertyDescriptorCollection pdc;
    //PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
    //{
    //  return pdc ?? (pdc = new PropertyDescriptorCollection(kvs.Select((p, i) => new PD(i)).ToArray()));
    //}
  }
}
