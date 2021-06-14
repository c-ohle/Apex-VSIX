using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
#pragma warning disable VSTHRD010

// kreatinin wert 16. 10:50 röntgen anmeldung
// 0261 281 28888

//https://3mf.io/specification/
//file:///C:/Users/cohle/Downloads/3MF_Core_Specification_v1_2_3.pdf
//todo: tool rot vert um obj
//todo: mesh flat angel
//todo: ranges as/in buffer 
//todo: refresh after load
//todo: camera dpi
//todo: F1
//todo: multisel undo 
//todo: VS for 2D
//todo: texture names, io bin...
//todo: collision
//todo: import paint3d export
//todo: cmd check mesh
//todo: search box in script
//todo: script foreach
//todo: script typexplorer namespaces
//todo: script stop debug on window hide
//todo: AAPThreshold Computer\HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad
//todo: wheel slow device (no ani)
//todo: csg ops in groups
//todo: csg ops progress with break?
//todo: csg checks befor ops 
//todo: shared textures for toolbox
//[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
//[ProvideEditorLogicalView(typeof(EditorFactory), "{7651a703-06e5-11d1-8ebd-00a0c90f26ea}")]

namespace Apex
{
  [Guid(Guids.guidPackage)]
  [PackageRegistration(UseManagedResourcesOnly = true)]//, AllowsBackgroundLoading = true)]
  [ProvideMenuResource("Menus.ctmenu", 1)]
  [ProvideEditorExtension(typeof(EditorFactory), ".3mf", 32)]
  [ProvideEditorExtension(typeof(EditorFactory), ".b3mf", 32)]
  [ProvideToolboxItems(1, NeedsCallBackAfterReset = true)]
  [ProvideToolWindow(typeof(ScriptToolWindowPane), Orientation = ToolWindowOrientation.Right, Window = EnvDTE.Constants.vsWindowKindOutput, Style = VsDockStyle.Tabbed)]
  //[ProvideToolWindow(typeof(ToolsToolWindowPane), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindSolutionExplorer)]
  public sealed class CDXPackage : Package //AsyncPackage
  {
    internal static CDXPackage Package;
    public CDXPackage()
    {
      Package = this;
      ToolboxInitialized += ToolboxInit;
      ToolboxUpgraded += ToolboxInit;
    }
    protected override void Initialize()
    {
      RegisterEditorFactory(new EditorFactory());
      //var cmds = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
      //if (cmds == null) return;
      //cmds.AddCommand(new MenuCommand((p, a) =>
      //{
      //  VsShellUtilities.ShowMessageBox(this, "hello", "title", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
      //}, new CommandID(Guids.CmdSet, 4070)));
      //cmds.AddCommand(new DynamicItemMenuCommand(0x11002));
      //cmds.AddCommand(new DynamicItemMenuCommand(0x11081));
    }
    //protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    //{
    //  RegisterEditorFactory(new EditorFactory());
    //  await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
    //}
    void ToolboxInit(object sender, EventArgs e)
    {
      var toolbox = GetService(typeof(SVsToolbox)) as IVsToolbox;
      if (toolbox == null) return;
      Apex.ToolboxDataProvider.RemoveItems(toolbox);
      Apex.ToolboxDataProvider.RestoreItems(toolbox);
    }
  }

  internal static class Guids
  {
    internal const string guidPackage = "785bd27f-9b97-45c5-b877-b701378798ac";
    internal static readonly Guid CmdSet = new Guid("d761bf5e-28df-41a8-9168-07703f46cac1");
    internal const string GuidEditorFactory = "93fa4dc3-61ec-47af-b0ba-50cad3caf049";
    internal static readonly Guid guidEditorFactory = new Guid(GuidEditorFactory);
    //internal static int ToolbarScript = 0x1000;
    //internal static int Toolbar3D = 0x1002;
  }
}
