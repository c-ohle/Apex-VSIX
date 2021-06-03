using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Apex.CDX;

namespace Apex
{
  unsafe partial class CDXView : UserControl
  {
    void tipshow()
    {
      if (view.MouseOverId != 0) { tip.ticks = 0; return; }
      var node = mainover();
      var title = "";
      var list = new System.Collections.Generic.List<string>();
      if (tip.mode == 0)
      {
        title = "❶ Tool Assistent ▼";
        list.Add("Select Toolset\t\tArrows");
        list.Add("Tool Assistent On/Off\tEnter");
      }
      else
      {
        if (node != null && !node.IsStatic)
        {
          if (tip.mode == 1)
          {
            title = "❷ Object Mouse Tools ▼▲";
            if (!node.IsSelect) list.Add("Select\t\tClick");
            list.Add("Toggle Selection\tStrg+Click");
            list.Add("Move Horizontal\tClick+Move");
            list.Add("Move Vertical\tShift+Click+Move");
            list.Add("Drag/Drop\tCtrl+Click+Move");
            list.Add("Rotate Horizontal\tAlt+Click+Move");
            list.Add("Rotate X-Axis\tCtrl+Shift+Click+Move");
            list.Add("Rotate Y-Axis\tCtrl+Alt+Click+Move");
            list.Add("Rotate Z-Axis\tCtrl+Shift+Alt+Click+Move");
          }
          else if (tip.mode == 2)
          {
            title = "❸ Object Touchpad Tools ▼▲";
            if (!node.IsSelect)
            {
              list.Add("Select\t\t\tClick");
              list.Add("Toggle Select\t\tCtrl+Click");
              //list.Add("Camera Move Vertical\tV+Move");
              //list.Add("Camera Move X-Axis\tX+Move");
              //list.Add("Camera Move Y-Axis\tY+Move");
              //list.Add("Camera Move Z-Axis\tZ+Move");
            }
            else
            {
              list.Add("Move Vertical\tV+Move");
              list.Add("Move X-Axis\tX+Move");
              list.Add("Move Y-Axis\tY+Move");
              list.Add("Move Z-Axis\tZ+Move");
              list.Add("Rotate X-Axis\tShift+X+Move");
              list.Add("Rotate Y-Axis\tShift+Y+Move");
              list.Add("Rotate Z-Axis\tShift+Z+Move");
            }
            //list.Add("Continue\t\tLeft, Right");
          }
        }
        else
        {
          if (tip.mode == 1)
          {
            title = "❷ Camera Mouse Tools ▼▲";
            list.Add("Select Rect\tClick+Move");
            list.Add("Move Horizontal\tStrg+Click+Move");
            list.Add("Move Vertical\tShift+Click+Move");
            list.Add("Rotate Horizontal\tAlt+Click+Move");
            list.Add("Rotate Vertical\tStrg+Shift+Click+Move");
            //list.Add("Continue\t\tLeft, Right");
          }
          else if (tip.mode == 2)
          {
            title = "❸ Camera Touchpad Tools ▼▲";
            list.Add("Move Vertical\tV+Move");
            list.Add("Move X-Axis\tX+Move");
            list.Add("Move Y-Axis\tY+Move");
            list.Add("Move Z-Axis\tZ+Move");
            //list.Add("Continue\t\tLeft, Right");
          }
        }
        if (tip.mode == 3)
        {
          title = "❹ Touchpad Navigation Tools ▲";
          list.Add("Camera Move Horizontal\tSpace+Move");
          list.Add("Camera Move Vertical\tShift+Space+Move");
          list.Add("Camera Rotate Horizontal\tA+Move");
          list.Add("Camera Rotate Verical\tShift+A+Move");
          list.Add("Camera Rotate Directional\tW+Move");
          if (mainselect() != null)
          {
            list.Add("Selection Rotate Horizontal\tQ+Move");
            list.Add("Selection Rotate Vertical\tShift+Q+Move");
          }
        }
      }
      tip.show(this, title, string.Join("\n", list));
      tip.overid = node != null ? node.GetHashCode() : 0;
    }
    void tiptimer()
    {
      if (tip.ticks == 0) return;
      var t = Environment.TickCount; if (t - tip.ticks < 500) return; if (!Focused) { tip.ticks = 0; return; }
      var p = Cursor.Position; if (Native.WindowFromPoint(p) != Handle) { tip.ticks = 0; return; }
      tipshow();
    }
    void tipmove(MouseEventArgs e)
    {
      if (tip.on)
      {
        var over = mainover(); var overid = over != null ? over.GetHashCode() : 0;
        if (tip.overid != overid) tip.hide();
        else if (tip.point.X >= 0 && ((float2)tip.point - e.Location).LengthSq > 32 * 32)
        {
          tip.hide(); tip.overid = overid; tip.on = true; tip.point.X = -(tip.point.X + 1);
        }
      }
      if ((flags & 4) != 0 && !tip.on) tip.ticks = Environment.TickCount;
    }
    bool tipkey(KeyEventArgs e)
    {
      var vis = tip.on && tip.point.X >= 0;
      switch (e.KeyCode)
      {
        case Keys.Enter:
          if (!vis) { tipshow(); flags |= 4; } else { tip.hide(); flags &= ~4; }
          return true;
        case Keys.Left:
        case Keys.Up:
          if (!vis) break; if (tip.mode <= 0) return true; tip.mode--;
          tipshow(); return true;
        case Keys.Right:
        case Keys.Down:
          if (!vis) break; if (tip.mode >= 3) return true; tip.mode++;
          tipshow(); return true;
        case Keys.ControlKey:
        case Keys.ShiftKey:
        case Keys.Menu:
          return false;
      }
      tip.hide(); return false;
    }
    struct TIP
    {
      ToolTip tooltip; internal bool on;
      internal System.Drawing.Point point;
      internal int ticks, overid, mode;
      internal void show(CDXView view, string title, string v)
      {
        if (tooltip == null) tooltip = new ToolTip { Tag = view };
        var p = point = view.PointToClient(Cursor.Position);
        p.Y += Cursor.Current.Size.Height * 3 / 4;
        tooltip.ToolTipTitle = title; tooltip.Show(string.Join("\n", v), view, p);
        on = true; ticks = 0;
      }
      internal void hide()
      {
        ticks = 0; if (!on) return;
        tooltip.Hide((IWin32Window)tooltip.Tag); on = false; overid = 0;
      }
      internal void dispose()
      {
        if (tooltip != null) { tooltip.Dispose(); tooltip = null; }
      }
    }
    TIP tip;
  }
}
