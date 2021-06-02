using System;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.TextManager.Interop;
#pragma warning disable VSTHRD010

namespace csg3mf
{
  [Guid(Guids.GuidEditorFactory)]
  public class EditorFactory : IVsEditorFactory, IDisposable
  {
    ServiceProvider serviceprov;
    void IDisposable.Dispose()
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      if (serviceprov != null)
      {
        serviceprov.Dispose();
        serviceprov = null;
      }
    }
    int IVsEditorFactory.SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
    {
      serviceprov = new ServiceProvider(psp);
      return 0;
    }
    int IVsEditorFactory.MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
    {
      pbstrPhysicalView = null;
      if (VSConstants.LOGVIEWID_Primary == rguidLogicalView) return VSConstants.S_OK;
      return VSConstants.E_NOTIMPL;
    }
    int IVsEditorFactory.Close()
    {
      return 0;
    }
    [EnvironmentPermission(SecurityAction.Demand, Unrestricted = true)]
    int IVsEditorFactory.CreateEditorInstance(uint grfCreateDoc, string pszMkDocument,
      string pszPhysicalView, IVsHierarchy pvHier, uint itemid, IntPtr punkDocDataExisting,
      out IntPtr ppunkDocView, out IntPtr ppunkDocData, out string pbstrEditorCaption,
      out Guid pguidCmdUI, out int pgrfCDW)
    {
      pguidCmdUI = default; ppunkDocView = default; ppunkDocData = default; pgrfCDW = default; pbstrEditorCaption = default;
      if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0) return VSConstants.E_INVALIDARG;
      if (punkDocDataExisting != IntPtr.Zero) return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
      var pane = new CDXWindowPane();
      ppunkDocView = Marshal.GetIUnknownForObject(pane);
      ppunkDocData = Marshal.GetIUnknownForObject(pane);
      pguidCmdUI = Guids.guidEditorFactory;
      return 0;
    }
  }

  class CDXWindowPane : WindowPane,
    IVsPersistDocData, IPersistFileFormat,
    IOleCommandTarget, IVsDocOutlineProvider, IVsToolboxUser //, IVsStatusbarUser
  {
    internal static int transcmd(in Guid guid, int id)
    {
      if (guid == Guids.CmdSet) return id;
      if (guid == VSConstants.GUID_VSStandardCommandSet97)
      {
        switch (id)
        {
          case (int)VSConstants.VSStd97CmdID.MultiLevelUndo:
          case (int)VSConstants.VSStd97CmdID.Undo: return 2010;
          case (int)VSConstants.VSStd97CmdID.MultiLevelRedo:
          case (int)VSConstants.VSStd97CmdID.Redo: return 2011;
          case (int)VSConstants.VSStd97CmdID.Group: return 2035;
          case (int)VSConstants.VSStd97CmdID.Ungroup: return 2036;
          case (int)VSConstants.VSStd97CmdID.Cut: return 2020;
          case (int)VSConstants.VSStd97CmdID.Copy: return 2030;
          case (int)VSConstants.VSStd97CmdID.Paste: return 2040;
          case (int)VSConstants.VSStd97CmdID.Delete: return 2015;
          case (int)VSConstants.VSStd97CmdID.SelectAll: return 2060;
          case (int)VSConstants.VSStd97CmdID.FindSelectedNext: return 2066;
          case (int)VSConstants.VSStd97CmdID.FindNext: return 2067;
          case (int)VSConstants.VSStd97CmdID.FindPrev: return 2068;
          case (int)VSConstants.VSStd97CmdID.StartNoDebug: return 5011;
          case (int)VSConstants.VSStd97CmdID.Stop: return 5013;
          case (int)VSConstants.VSStd97CmdID.ToggleBreakpoint: return 5020;
          case (int)VSConstants.VSStd97CmdID.Start: return 5010;
          case (int)VSConstants.VSStd97CmdID.StepInto: return 5015;
          case (int)VSConstants.VSStd97CmdID.StepOver: return 5016;
          case (int)VSConstants.VSStd97CmdID.StepOut: return 5017;
          //case (int)VSConstants.VSStd97CmdID.BringForward: return 5100; //not in vs
          //case (int)VSConstants.VSStd97CmdID.SendBackward: return 5101; //not in vs
          case (int)VSConstants.VSStd97CmdID.BringToFront: return 5102;
          case (int)VSConstants.VSStd97CmdID.SendToBack: return 5103;
          //case (int)VSConstants.VSStd97CmdID.InsertObject: return 5104;
          case (int)VSConstants.VSStd97CmdID.F1Help: return 2000;
        }
        return 0;
      }
      //if (guid == typeof(VSConstants.VSStd2010CmdID).GUID)
      //{
      //  switch (id)
      //  {
      //    case (int)VSConstants.VSStd2010CmdID.OUTLN_EXPAND_CURRENT:
      //    case (int)VSConstants.VSStd2010CmdID.OUTLN_COLLAPSE_CURRENT:
      //    case (int)VSConstants.VSStd2010CmdID.OUTLN_EXPAND_ALL:
      //    case (int)VSConstants.VSStd2010CmdID.OUTLN_COLLAPSE_ALL: return 2064;
      //  }
      //}
      if (guid == typeof(VSConstants.VSStd2KCmdID).GUID)
      {
        //case 2062: return OnToggle(test);
        //case 2063: return OnToggleAll(test);
        switch (id)
        {
          case (int)VSConstants.VSStd2KCmdID.OUTLN_TOGGLE_CURRENT: return 2062;
          case (int)VSConstants.VSStd2KCmdID.OUTLN_TOGGLE_ALL: return 2063;
          case (int)VSConstants.VSStd2KCmdID.OUTLN_COLLAPSE_TO_DEF: return 2064;
            //case (int)VSConstants.VSStd2KCmdID.OUTLN_STOP_HIDING_ALL: id = 7; break;
            //case (int)VSConstants.VSStd2KCmdID.OUTLN_STOP_HIDING_CURRENT: id = 7; break;
        }
      }
      return 0;
    }
    //~CDXPane() { Debug.WriteLine("~CDXPane"); }

    //protected override void Dispose(bool disposing)
    //{
    //  base.Dispose(disposing);
    //  if (!disposing) return;
    //  //if (view != null) { view.Dispose(); view = null; }
    //  if (treeview != null) { treeview.Dispose(); treeview = null; }
    //}
    public CDXWindowPane() : base(null)
    {
      view = new CDXView { AllowDrop = true, pane = this };
    }
    protected override void Initialize()
    {
      //base.Initialize();
      //var host = gettbh(); host.AddToolbar(VSTWT_LOCATION.VSTWT_TOP, Guids.CmdSet, 0x1002); tbon = true;
      if (toolbox != null) return;
      toolbox = (IVsToolbox)GetService(typeof(SVsToolbox));
      toolbox.RegisterDataProvider(new ToolboxDataProvider { toolbox = toolbox }, out var id);
    }
    //bool tbon;
    //internal void tbonoff()
    //{
    //  var host = gettbh(); var guid = Guids.CmdSet;
    //  tbon = !tbon; host.ShowHideToolbar(ref guid, 0x1002, tbon ? 1 : 0);
    //}
    //IVsToolWindowToolbarHost gettbh()
    //{
    //  var frame = (IVsWindowFrame)base.GetService(typeof(SVsWindowFrame));
    //  frame.GetProperty((int)__VSFPROPID.VSFPROPID_ToolbarHost, out var t);
    //  return t as IVsToolWindowToolbarHost;
    //}

    CDXView view; string fileName;
    static IVsToolbox toolbox;
    public override IWin32Window Window => view;
    public object GetVsService(Type t) => GetService(t);

    int IVsPersistDocData.GetGuidEditorType(out Guid pClassID)
    {
      pClassID = Guids.guidEditorFactory; return 0;
    }
    int IVsPersistDocData.IsDocDataDirty(out int pfDirty)
    {
      pfDirty = view.IsModified ? 1 : 0; return 0;
    }
    int IVsPersistDocData.SetUntitledDocPath(string pszDocDataPath)
    {
      throw new NotImplementedException();
    }
    int IVsPersistDocData.LoadDocData(string pszMkDocument)
    {
      view.LoadDocData(fileName = pszMkDocument);
      return 0;
    }
    int IVsPersistDocData.SaveDocData(VSSAVEFLAGS dwSave, out string pbstrMkDocumentNew, out int pfSaveCanceled)
    {
      pbstrMkDocumentNew = null;
      pfSaveCanceled = 0;
      switch (dwSave)
      {
        case VSSAVEFLAGS.VSSAVE_Save:
        case VSSAVEFLAGS.VSSAVE_SilentSave:
          {
            var queryEditQuerySave = (IVsQueryEditQuerySave2)GetService(typeof(SVsQueryEditQuerySave));
            var hr = queryEditQuerySave.QuerySaveFile(fileName, 0, null, out var result);
            if (hr < 0) return hr;
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            switch ((tagVSQuerySaveResult)result)
            {
              case tagVSQuerySaveResult.QSR_NoSave_Cancel: pfSaveCanceled = ~0; break;
              case tagVSQuerySaveResult.QSR_SaveOK:
                {
                  hr = uiShell.SaveDocDataToFile(dwSave, this, fileName, out pbstrMkDocumentNew, out pfSaveCanceled);
                  if (hr < 0) return hr;
                }
                break;
              case tagVSQuerySaveResult.QSR_ForceSaveAs:
                {
                  hr = uiShell.SaveDocDataToFile(VSSAVEFLAGS.VSSAVE_SaveAs, this, fileName, out pbstrMkDocumentNew, out pfSaveCanceled);
                  if (hr < 0) return hr;
                }
                break;
              case tagVSQuerySaveResult.QSR_NoSave_Continue: break;
              default: throw new COMException();
            }
            break;
          }
        case VSSAVEFLAGS.VSSAVE_SaveAs:
        case VSSAVEFLAGS.VSSAVE_SaveCopyAs:
          {
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            var hr = uiShell.SaveDocDataToFile(dwSave, this, fileName, out pbstrMkDocumentNew, out pfSaveCanceled);
            if (hr < 0) return hr;
            break;
          }
        default: throw new ArgumentException();
      }

      return 0;
    }
    int IVsPersistDocData.Close()
    {
      //if (view != null) { view.Dispose(); view = null; }
      return 0;
    }
    int IVsPersistDocData.OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew)
    {
      return 0;
    }
    int IVsPersistDocData.RenameDocData(uint grfAttribs, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
    {
      return 0;
    }
    int IVsPersistDocData.IsDocDataReloadable(out int pfReloadable)
    {
      pfReloadable = 0; return 0;
    }
    int IVsPersistDocData.ReloadDocData(uint grfFlags)
    {
      return 0;
    }

    int IPersistFileFormat.GetClassID(out Guid pClassID)
    {
      throw new NotImplementedException();
    }
    int IPersistFileFormat.IsDirty(out int pfIsDirty)
    {
      throw new NotImplementedException();
    }
    int IPersistFileFormat.InitNew(uint nFormatIndex)
    {
      throw new NotImplementedException();
    }
    int IPersistFileFormat.Load(string pszFilename, uint grfMode, int fReadOnly)
    {
      throw new NotImplementedException();
    }
    int IPersistFileFormat.Save(string pszFilename, int fRemember, uint nFormatIndex)
    {
      view.Save(pszFilename);
      if (fRemember != 0) { fileName = pszFilename; view.IsModified = false; }
      return 0;
    }
    int IPersistFileFormat.SaveCompleted(string pszFilename)
    {
      return 0;
    }
    int IPersistFileFormat.GetCurFile(out string ppszFilename, out uint pnFormatIndex)
    {
      ppszFilename = fileName; pnFormatIndex = 0; return 0;
    }
    int IPersistFileFormat.GetFormatList(out string ppszFormatList)
    {
      ppszFormatList = $"3MF Format (*{".3mf"})\n*{".3mf"}\nBinary Format (*{".b3mf"})\n*{".b3mf"}\n\n";
      return 0;
    }
    int IPersist.GetClassID(out Guid pClassID)
    {
      throw new NotImplementedException();
    }

    unsafe int IOleCommandTarget.QueryStatus(ref Guid guid, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
    {
      if (prgCmds == null || cCmds != 1) return VSConstants.E_INVALIDARG;
      var id = transcmd(guid, (int)prgCmds[0].cmdID);
      if (id == 0) return -2147221248; //OLECMDERR_E_NOTSUPPORTED 
      var fl = view.OnCommand(id, this); if (fl == -1) return -2147221248;
      if ((fl & 0x80) != 0) view.OnCommand(id, pCmdText);
      //OLECMDF_SUPPORTED = 0x1, OLECMDF_ENABLED = 0x2, OLECMDF_LATCHED = 0x4, OLECMDF_NINCHED = 0x8, OLECMDF_INVISIBLE = 0x10, OLECMDF_DEFHIDEONCTXTMENU = 0x20
      prgCmds[0].cmdf = (uint)(((fl & 1) != 0 ? 3 : 0x11) | ((fl & 2) != 0 ? 4 : 0) | (fl & 0x10));
      return 0;
    }
    int IOleCommandTarget.Exec(ref Guid guid, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
    {
      var id = transcmd(guid, (int)nCmdID);
      if (id == 0) return -2147221248; //OLECMDERR_E_NOTSUPPORTED
      try
      {
        var a = pvaIn != IntPtr.Zero || pvaOut != IntPtr.Zero ? (exa2 ?? (exa2 = new object[2])) : null;
        if (a != null) { a[0] = pvaIn != IntPtr.Zero ? Marshal.GetObjectForNativeVariant(pvaIn) : null; a[1] = null; }
        view.OnCommand(id, a);
        if (pvaOut != IntPtr.Zero && a[1] != null) Marshal.GetNativeVariantForObject(a[1], pvaOut);
      }
      catch (Exception e) { view.MessageBox(e.Message); }
      return 0;
    }
    internal CDXView.TreeView treeview; static object[] exa2;
    int IVsDocOutlineProvider.GetOutlineCaption(VSOUTLINECAPTION nCaptionType, out string pbstrCaption)
    {
      pbstrCaption = null; return 0; //caption TreeView
    }
    int IVsDocOutlineProvider.GetOutline(out IntPtr phwnd, out IOleCommandTarget ppCmdTarget)
    {
      treeview = new CDXView.TreeView { view = view, BorderStyle = BorderStyle.None, Font = System.Drawing.SystemFonts.SmallCaptionFont, LabelEdit = true };
      treeview.CreateControl(); phwnd = treeview.Handle; ppCmdTarget = this;
      treeview.inval(); /*view.Invalidate(4);*/ return 0;
    }
    int IVsDocOutlineProvider.ReleaseOutline(IntPtr hwnd, IOleCommandTarget pCmdTarget)
    {
      treeview.Dispose(); treeview = null; return 0;
    }
    int IVsDocOutlineProvider.OnOutlineStateChange(uint dwMask, uint dwState)
    {
      return 0;
    }

    int IVsToolboxUser.IsSupported(Microsoft.VisualStudio.OLE.Interop.IDataObject pDO)
    {
      //FORMATETC fetc = { m_CF_CUSTOM_FORMAT, NULL, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
      var data = new OleDataObject(pDO);
      if (data.GetDataPresent(typeof(ToolboxItem)))
        return VSConstants.S_OK;
      return VSConstants.S_FALSE;
    }
    int IVsToolboxUser.ItemPicked(Microsoft.VisualStudio.OLE.Interop.IDataObject pDO)
    {
#if (DEBUG)
      if (System.IO.Directory.Exists("C:\\Users\\cohle\\Desktop\\Apex VSIX\\Apex"))
      {
        if (MessageBox.Show("save toolbox?", "Debug only", MessageBoxButtons.YesNo) != DialogResult.Yes) return 0;
        System.IO.File.WriteAllBytes("C:\\Users\\cohle\\Desktop\\Apex VSIX\\Apex\\toolbox.bin", ToolboxDataProvider.CopyItems(toolbox));
      }
#endif
      ////ToolboxDataProvider.RestoreItems(toolbox);
      return 0;
    }
  }

 
  [Guid("381f778f-1111-4f04-88dd-241de0ad3e71")]
  public class ScriptToolWindowPane : ToolWindowPane, IVsSelectionEvents, IOleCommandTarget//IVsFindTarget
  {
    //public override bool SearchEnabled => true;//base.SearchEnabled;
    //public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
    //{
    //  var s = pSearchQuery.SearchString;
    //  return base.CreateSearch(dwCookie, pSearchQuery, pSearchCallback);
    //}
    //public override Guid SearchCategory => base.SearchCategory;
    //public override IVsEnumWindowSearchFilters SearchFiltersEnum => base.SearchFiltersEnum;
    //public override IVsEnumWindowSearchOptions SearchOptionsEnum => base.SearchOptionsEnum;
    //public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
    //{
    //  base.ProvideSearchSettings(pSearchSettings);
    //}
    //public override void ClearSearch()
    //{
    //  base.ClearSearch();
    //}

    UserControl host; ScriptEditor edit;
    public override IWin32Window Window => host;
    public ScriptToolWindowPane() : base(null)
    {
      this.Caption = "Script";
      using (DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware))
        host = new UserControl();
      //this.ToolBar = new CommandID(Guids.CmdSet, 0x1000);
    }
    void Edit_MouseUp(object sender, MouseEventArgs e)
    {
      if (e.Button != MouseButtons.Right) return;
      var uishell = GetService(typeof(SVsUIShell)) as IVsUIShell; if (uishell == null) return;
      var p = Cursor.Position; var set = Guids.CmdSet;
      uishell.ShowContextMenu(0, ref set, 0x2101, new[] { new POINTS { x = (short)p.X, y = (short)p.Y } }, this);
    }
    uint cookie;
    protected override void OnCreate()
    {
      base.OnCreate();
      if (GetService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection mon)
      {
        mon.AdviseSelectionEvents(this, out cookie);
        mon.GetCurrentSelection(out IntPtr ppHier, out uint pitemid, out IVsMultiItemSelect ppMIS, out IntPtr ppSC);
        if (ppHier != IntPtr.Zero) Marshal.Release(ppHier);
        if (ppSC != IntPtr.Zero)
        {
          var sc = Marshal.GetTypedObjectForIUnknown(ppSC, typeof(ISelectionContainer));
          Marshal.Release(ppSC);
          Show(sc is CDXView view ? view.unisel() : null);
        }
      }
    }
    protected override void OnClose()
    {
      if (GetService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection mon)
        mon.UnadviseSelectionEvents(cookie);
      base.OnClose();
    }
    void Show(Node node)
    {
      if (edit != null && edit.node == node) return;
      if (edit != null)
      {
        if (edit.SelectionStart != 0 || edit.SelectionLength != 0 ||
          edit.AutoScrollPosition != default ||
          (edit.getlineflags() & 2) != 0 ||
          edit.EditText != edit.node.getcode())
        {
          if (edit.node.Annotation<ScriptEditor>() == null) edit.node.AddAnnotation(edit);
        }
        else
        {
          edit.node.RemoveAnnotations(edit.GetType());
          edit.Dispose();
        }
        host.Controls.Remove(edit);
      }
      if (node == null) { edit = null; return; }
      edit = node.Annotation<ScriptEditor>();
      if (edit != null) host.Controls.Add(edit);
      else
      {
        edit = new ScriptEditor { Dock = DockStyle.Fill };
        edit.pane = this;
        edit.MouseUp += Edit_MouseUp;
        edit.EditText = (edit.node = node).getcode();
        host.Controls.Add(edit);
      }
    }
    int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld,
      IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew,
      uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
    {
      Show(pSCNew is CDXView view ? view.unisel() : null);
      //if (pSCNew is CDXView view)
      //{ 
      //  //view.getscript()
      //  pSCNew.CountObjects(2, out var n);
      //  if (n == 1)
      //  {
      //    var pp = new object[1];
      //    pSCNew.GetObjects(2, 1, pp);
      //    var node = pp[0] as Node;
      //    if (node != null)
      //    {
      //      Show(node);
      //      return 0;
      //    }
      //  }
      //}
      //Show(null);
      return 0;
    }
    int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
    {
      return 0;
    }
    int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
    {
      return 0;
    }
    int IOleCommandTarget.QueryStatus(ref Guid guid, uint nCmdId, OLECMD[] oleCmd, IntPtr oleText)
    {
      if (edit == null) return -2147221248;
      var id = CDXWindowPane.transcmd(guid, (int)oleCmd[0].cmdID);
      if (id != 0)
      {
        var fl = edit.OnCommand(id, this);
        if (fl != -1) { oleCmd[0].cmdf = (uint)(((fl & 1) != 0 ? 3 : 1) | ((fl & 2) != 0 ? 4 : 0)); return 0; }
      }
      //Debug.WriteLine(guid + " " +oleCmd[0].cmdID);
      //var t = GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;
      //if (t != null) return t.QueryStatus(ref guid, nCmdId, oleCmd, oleText);
      return -2147221248;
    }
    int IOleCommandTarget.Exec(ref Guid guid, uint nCmdId, uint nCmdExcept, IntPtr pIn, IntPtr vOut)
    {
      if (edit == null) return -2147221248;
      var id = CDXWindowPane.transcmd(guid, (int)nCmdId);
      if (id != 0) { edit.OnCommand(id, null); return 0; }
      return -2147221248;
    }

  }

}
