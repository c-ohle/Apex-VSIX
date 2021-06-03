using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Apex.CDX;

namespace Apex
{
  unsafe partial class CDXView : UserControl
  {
    internal class TreeView : System.Windows.Forms.TreeView
    {
      internal CDXView view; TreeNode newsel; bool invok;
      protected override void WndProc(ref Message m)
      {
        switch (m.Msg)
        {
          case 0x000F: update(); break;//WM_PAINT
          case 0x007B: view.ShowContextMenu(this, 0x2104, m.LParam); return;
        }
        base.WndProc(ref m);
      }
      internal void inval() { invok = false; Invalidate(); }
      void update()
      {
        if (invok) return; invok = true;
        BeginUpdate(); newsel = null;
        recurs(Nodes, view.scene.Child);
        var p = SelectedNode;
        if (newsel != null)
        {
          if (p != newsel)
          {
            SelectedNode = newsel;
            if (!Focused) newsel.EnsureVisible();
          }
          newsel = null;
        }
        else if (p != null && !((INode)p.Tag).IsSelect) SelectedNode = null;
        EndUpdate();
      }
      void recurs(TreeNodeCollection tvn, INode node)
      {
        var i = 0;
        for (; node != null; node = node.Next, i++)
        {
          var sel = node.IsSelect;
          var name = node.Name;
          if (string.IsNullOrEmpty(name)) name = node.GetClassName(); m1:
          if (tvn.Count == i) tvn.Add(name).Tag = node;
          var p = tvn[i];
          if (p.Tag != node)
          {
            if (p.Tag == null) p.Tag = node;
            else
            {
              int k = i + 1; for (; k < tvn.Count && tvn[k].Tag != node; k++) ;
              if (k != tvn.Count) { tvn.RemoveAt(i); goto m1; }
              else { p = tvn.Insert(i, name); p.Tag = node; }
            }
          }
          if (p.Text != name) p.Text = name;
          var bk = sel ? System.Drawing.SystemColors.GradientInactiveCaption : new System.Drawing.Color();
          if (p.BackColor != bk) { p.BackColor = bk; if (sel) newsel = p; }
          var ic = node.Child;
          if (ic != null)
          {
            if (p.IsExpanded) recurs(p.Nodes, ic);
            else if (p.FirstNode == null) p.Nodes.Add(string.Empty);
          }
          else if (p.FirstNode != null) p.Nodes.Clear();
        }
        while (i < tvn.Count) tvn.RemoveAt(i);
      }
      protected override void OnNodeMouseClick(TreeNodeMouseClickEventArgs e)
      {
        if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Control)
        {
          var node = (INode)e.Node.Tag;
          node.IsSelect ^= true; view.Invalidate(Inval.Select); return;
        }
        //base.OnNodeMouseClick(e);
      }
      protected override void OnBeforeSelect(TreeViewCancelEventArgs e)
      {
        var node = (INode)e.Node.Tag;
        if (e.Action == TreeViewAction.Unknown)
        {
          if (!node.IsSelect) e.Cancel = true; return;
        }
        if (e.Action == TreeViewAction.ByMouse && ModifierKeys == Keys.Control)
        {
          e.Cancel = true; return;
        }
      }
      protected override void OnAfterSelect(TreeViewEventArgs e)
      {
        if (e.Action == TreeViewAction.Unknown) return;
        var node = (INode)e.Node.Tag;
        node.Select(); view.Invalidate(Inval.Select);
      }
      protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
      {
        var node = (INode)e.Node.Tag;
        recurs(e.Node.Nodes, node.Child);
      }
      protected override void OnAfterCollapse(TreeViewEventArgs e)
      {
      }
      protected override void OnBeforeLabelEdit(NodeLabelEditEventArgs e) { }
      protected override void OnAfterLabelEdit(NodeLabelEditEventArgs e)
      {
        e.CancelEdit = true; if (e.Label == null) return;
        var node = e.Node.Tag as INode;
        var a = node.Name; if (string.IsNullOrEmpty(a)) a = null;
        var b = e.Label; if (string.IsNullOrEmpty(b) || b == node.GetClassName()) b = null;
        if (a == b) return;
        if (b == "Group") { settype(node, 0); return; }
        if (b == "Camera") { settype(node, 1); return; }
        if (b == "Light") { settype(node, 2); return; }
        view.Execute(setname(node, b));
        return;
      }
      Action setname(INode node, string s)
      {
        var v = view;
        return () => { var t = node.Name; node.Name = s; s = t; v.Invalidate(Inval.Tree); };
      }
      void settype(INode node, int type)
      {
        var act = setname(node, null);
        if (node.HasBuffer(BUFFER.CAMERA) && type != 1) act += undo(node, BUFFER.CAMERA, (byte[])null);
        if (node.HasBuffer(BUFFER.LIGHT) && type != 2) act += undo(node, BUFFER.LIGHT, (byte[])null);
        if (type == 0) view.Execute(act);
        else if (type == 1)
        {
          var data = new Node.cameradata() { near = 0.1f, far = 1000, fov = 50 * (float)(Math.PI / 180) };
          view.Execute(undo(node, BUFFER.CAMERA, &data, sizeof(Node.cameradata)) + act);
        }
        else if (type == 2)
        {
          var data = new Node.lightdata() { };
          view.Execute(undo(node, BUFFER.LIGHT, &data, sizeof(Node.lightdata)) + act);
        }
      }
      protected override void OnMouseUp(MouseEventArgs e)
      {
        base.OnMouseUp(e);
        //if (e.Button == MouseButtons.Right)
        //  view.ShowContextMenu(0x2104);
      }
      protected override void OnKeyDown(KeyEventArgs e)
      {
        switch (e.KeyCode)
        {
          case Keys.Up:
          case Keys.Down:
            if (e.Modifiers == Keys.Control)
              view.OnCommand(e.KeyCode == Keys.Up ? 5101 : 5100, null); //SendBackward : BringForward
            break;
        }
        base.OnKeyDown(e);
      }
    }
  }
}
