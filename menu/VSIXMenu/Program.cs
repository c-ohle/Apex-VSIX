using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace VSIXMenu
{
  static class Program
  {
    static void Convert()
    {
      var xml = XElement.Load(typeof(Program).Assembly.Location + "\\..\\..\\..\\..\\menu.xml");
      var ns = (XNamespace)"http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable";
      var doc = new XElement(ns + "CommandTable");
      doc.Add(new XElement(ns + "Extern", new XAttribute("href", "stdidcmd.h")));
      doc.Add(new XElement(ns + "Extern", new XAttribute("href", "vsshlids.h")));
      doc.Add(new XElement(ns + "Extern", new XAttribute("href", "virtkeys.h")));
      var commands = new XElement(ns + "Commands", new XAttribute("package", "g1")); doc.Add(commands);
      var menus = new XElement(ns + "Menus"); commands.Add(menus);
      var groups = new XElement(ns + "Groups"); commands.Add(groups);
      var buttons = new XElement(ns + "Buttons"); commands.Add(buttons);
      var cmdplacements = new XElement(ns + "CommandPlacements"); doc.Add(cmdplacements);
      var keybinds = new XElement(ns + "KeyBindings"); doc.Add(keybinds);
      var symbols = new XElement(ns + "Symbols"); doc.Add(symbols);
      var pack = new XElement(ns + "GuidSymbol", new XAttribute("name", "g1"), new XAttribute("value", "{785bd27f-9b97-45c5-b877-b701378798ac}")); symbols.Add(pack);
      var cmds = new XElement(ns + "GuidSymbol", new XAttribute("name", "g2"), new XAttribute("value", "{d761bf5e-28df-41a8-9168-07703f46cac1}")); symbols.Add(cmds);
      int freeid = 1000000, imgc = 0;
      foreach (var ctm in xml.Elements())
      {
        var id = (string)ctm.Attribute("id"); var sid = '_' + id;
        cmds.Add(new XElement(ns + "IDSymbol", new XAttribute("name", sid), new XAttribute("value", id)));
        var menu =
        new XElement(ns + "Menu", new XAttribute("guid", "g2"), new XAttribute("id", sid),
          new XAttribute("type", ctm.Name == "ContextMenu" ? "Context" : ctm.Name),
          new XElement(ns + "Strings", new XElement(ns + "ButtonText",
            (string)ctm.Attribute("text"))));
        menus.Add(menu);
        //var t = (string)ctm.Attribute("text");
        //if (t != null) menu.Add(new XElement(ns+ "Strings",
        //  new XElement(ns+ "ButtonText", t)));
        recurs(ctm, sid);
        void recurs(XElement ee, string _sid)
        {
          var group = (XElement)null; int pri = 1;
          foreach (var e in ee.Elements())
          {
            if (e.Name == "Seperator") { group = null; continue; }
            if (group == null)
            {
              var gid = freeid++; var sgid = "_" + gid;
              cmds.Add(new XElement(ns + "IDSymbol", new XAttribute("name", sgid), new XAttribute("value", gid)));
              groups.Add(group = new XElement(ns + "Group",
                new XAttribute("guid", "g2"), new XAttribute("id", sgid), new XAttribute("priority", pri++),
                new XElement(ns + "Parent", new XAttribute("guid", "g2"), new XAttribute("id", _sid))));
            }
            if (e.Name == "MenuItem")
            {
              var text = (string)e.Attribute("text");
              if (text == null)
              {
                cmdplacements.Add(new XElement(ns + "CommandPlacement",
                  new XAttribute("guid", "guidVSStd97"),
                  new XAttribute("id", (string)e.Attribute("id")),
                  new XAttribute("priority", pri++),
                  new XElement(ns + "Parent", new XAttribute("guid", "g2"),
                    new XAttribute("id", (string)group.Attribute("id")))
                  ));
                continue;
              }
              var iid = (string)e.Attribute("id");
              if (iid == null)
              {
                var gid = freeid++; var sgid = "_" + gid; iid = gid.ToString();
                cmds.Add(new XElement(ns + "IDSymbol", new XAttribute("name", sgid), new XAttribute("value", gid)));
              }
              var siid = '_' + iid;
              if (!cmds.Elements().Any(p => (string)p.Attribute("value") == iid))
                cmds.Add(new XElement(ns + "IDSymbol", new XAttribute("name", siid), new XAttribute("value", iid)));
              var btn = new XElement(ns + "Button", new XAttribute("guid", "g2"),
                new XAttribute("id", siid), new XAttribute("type", (string)e.Attribute("type") ?? "Button"), new XAttribute("priority", pri++),
                new XElement(ns + "Parent", new XAttribute("guid", "g2"),
                  new XAttribute("id", (string)group.Attribute("id"))));
              buttons.Add(btn);
              var img = (string)e.Attribute("img");
              if (img != null)
              {
                imgc = Math.Max(imgc, int.Parse(img) + 1);
                btn.Add(new XElement(ns + "Icon", new XAttribute("guid", "g3"),
                  new XAttribute("id", "i" + img)));
              }
              var fl = (string)e.Attribute("fl");
              if (fl == null) fl = "DefaultDisabled";
              foreach (var s in fl.Split('|')) btn.Add(new XElement(ns + "CommandFlag", s));
              var str = new XElement(ns + "Strings"); btn.Add(str);
              str.Add(new XElement(ns + "ButtonText", text));
              var tip = (string)e.Attribute("tip");
              if (tip != null) str.Add(new XElement(ns + "ToolTipText", tip));
              var keys = (string)e.Attribute("keys");
              if (keys != null)
              {
                var ss = keys.Split('|');
                if(ss.Length==1)
                  keybinds.Add(new XElement(ns + "KeyBinding", new XAttribute("guid", "g2"),
                    new XAttribute("id", siid),
                    new XAttribute("editor", "guidVSStd97"),
                    new XAttribute("key1", ss[0])));
                else if(ss.Length == 3)
                  keybinds.Add(new XElement(ns + "KeyBinding", new XAttribute("guid", "g2"),
                  new XAttribute("id", siid),
                  new XAttribute("editor", "guidVSStd97"),
                  new XAttribute("key1", ss[1]),
                  new XAttribute("key2", ss[2]),
                  new XAttribute("mod1", ss[0]),
                  new XAttribute("mod2", ss[0])));
                //<KeyBinding guid="guidCmdSet" id="cmdidGroup" editor="guidVSStd97"
                //key1="W" key2="G" mod1="Control" mod2="Control" />

              }
              continue;
            }
            if (e.Name == "SubMenu")
            {
              var gid = freeid++; var sgid = "_" + gid;
              cmds.Add(new XElement(ns + "IDSymbol", new XAttribute("name", sgid), new XAttribute("value", gid)));
              menus.Add(new XElement(ns + "Menu",
                new XAttribute("guid", "g2"),
                new XAttribute("id", sgid),
                new XAttribute("type", (string)e.Attribute("type") ?? "Menu"), new XAttribute("priority", pri++),
                new XElement(ns + "Parent", new XAttribute("guid", "g2"),
                  new XAttribute("id", (string)group.Attribute("id"))),
                new XElement(ns + "Strings",
                  new XElement(ns + "ButtonText", (string)e.Attribute("text")))));
              recurs(e, sgid);
            }
            if (e.Name == "Combo")
            {
              //var cid = freeid++; var scid = "_" + cid;
              //cmds.Add(new XElement(ns + "IDSymbol", new XAttribute("name", scid), new XAttribute("value", cid)));

              var iid = (string)e.Attribute("id"); var siid = '_' + iid;
              if (!cmds.Elements().Any(p => (string)p.Attribute("value") == iid))
                cmds.Add(new XElement(ns + "IDSymbol", new XAttribute("name", siid), new XAttribute("value", iid)));

              var iid2 = (string)e.Attribute("idlist"); var siid2 = '_' + iid2;
              if (!cmds.Elements().Any(p => (string)p.Attribute("value") == iid2))
                cmds.Add(new XElement(ns + "IDSymbol", new XAttribute("name", siid2), new XAttribute("value", iid2)));

              var combos = commands.Element(ns + "Combos");
              if (combos == null) commands.Add(combos = new XElement(ns + "Combos"));
              var combo = new XElement(ns + "Combo", new XAttribute("guid", "g2"), new XAttribute("priority", pri++),
                new XAttribute("defaultWidth", (string)e.Attribute("width")),
                new XAttribute("id", siid),
                new XAttribute("idCommandList", siid2),//scid),
                new XAttribute("type", (string)e.Attribute("type")),
                new XElement(ns + "Parent", new XAttribute("guid", "g2"),
                  new XAttribute("id", (string)group.Attribute("id"))));
              var fl = (string)e.Attribute("fl");
              if (fl == null) fl = "DefaultDisabled";
              foreach (var s in fl.Split('|')) combo.Add(new XElement(ns + "CommandFlag", s));
              combo.Add(new XElement(ns + "Strings", new XElement(ns + "ButtonText", (string)e.Attribute("text"))));
              combos.Add(combo);
              continue;
            }
          }
        }
      }
      if (imgc != 0)
      {
        //<GuidSymbol name="guidImages" value="{8410C7B1-EA41-4466-B68E-FDB2E9A41237}" >
        var gs = new XElement(ns + "GuidSymbol", new XAttribute("name", "g3"),
          new XAttribute("value", "{8410C7B1-EA41-4466-B68E-FDB2E9A41237}"));
        for (int i = 0; i < imgc; i++) gs.Add(new XElement(ns + "IDSymbol",
         new XAttribute("name", "i" + i), new XAttribute("value", i + 1)));
        symbols.Add(gs);
        var bmp = new XElement(ns + "Bitmap",
          new XAttribute("guid", "g3"), new XAttribute("href", "Resources\\Images_32bit.bmp"),
          new XAttribute("usedList", string.Join(',', Enumerable.Range(0, imgc).Select(p => "i" + p))));
        commands.Add(new XElement(ns + "Bitmaps", bmp));

      }
      doc.Save(typeof(Program).Assembly.Location + "\\..\\..\\..\\..\\..\\..\\Apex\\Package.vsct");
    }

    [STAThread]
    static void Main()
    {
      Convert();
      //Application.SetHighDpiMode(HighDpiMode.SystemAware);
      //Application.EnableVisualStyles();
      //Application.SetCompatibleTextRenderingDefault(false);
      //Application.Run(new MyForm { AllowDrop = true });
    }

    //class MyForm : Form
    //{
    //  protected override void OnDragEnter(DragEventArgs drgevent)
    //  {
    //    var dat = drgevent.Data;
    //    var a = dat.GetData("csg3mf");
    //  }
    //}
  }
}
