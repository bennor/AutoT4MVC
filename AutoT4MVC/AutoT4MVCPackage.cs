using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AutoT4MVC
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidAutoT4MVCPkgString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideOptionPage(typeof (Options), Options.CategoryName, Options.PageName, 1000, 1001, false)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class AutoT4MVCPackage : Package
    {
        private BuildEvents _buildEvents;
        private Controller _controller;
        private DocumentEvents _documentEvents;
        private DTE _dte;
        private ProjectItemsEvents _projectItemsEvents;
        private SolutionEvents _solutionEvents;

        private Options Options
        {
            get { return (Options)GetDialogPage(typeof (Options)); }
        }

        protected override void Initialize()
        {
            base.Initialize();

            _dte = GetService(typeof (SDTE)) as DTE;
            if (_dte == null)
                return;

            _controller = new Controller();

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += OnBuildBegin;

            _documentEvents = _dte.Events.DocumentEvents;
            _documentEvents.DocumentSaved += DocumentSaved;

            var events2 = _dte.Events as Events2;
            if (events2 == null)
                return;

            _projectItemsEvents = events2.ProjectItemsEvents;
            _projectItemsEvents.ItemAdded += ItemAdded;
            _projectItemsEvents.ItemRemoved += ItemRemoved;
            _projectItemsEvents.ItemRenamed += ItemRenamed;

            _solutionEvents = _dte.Events.SolutionEvents;
            _solutionEvents.ProjectRemoved += ProjectRemoved;

            var menuCommandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != menuCommandService)
            {
                var showOptionsCommandId = new CommandID(GuidList.guidAutoT4MVCCmdSet,
                    (int) PkgCmdIDList.cmdidShowOptions);
                var showOptionsMenuCommand = new OleMenuCommand(ShowOptions, showOptionsCommandId);
                menuCommandService.AddCommand(showOptionsMenuCommand);
                showOptionsMenuCommand.BeforeQueryStatus += ShowOptionsMenuCommandOnBeforeQueryStatus;
            }
        }

        private void ProjectRemoved(Project Project)
        {
            _controller.HandleProjectUnload(Project);
        }

        private void DocumentSaved(Document document)
        {
            if (!Options.RunOnSave)
                return;

            _controller.HandleContentChange(document.ProjectItem);
        }

        private void ItemRenamed(ProjectItem projectItem, string oldName)
        {
            if (!Options.RunOnSave)
                return;

            _controller.HandleNameChange(projectItem);
        }

        private void ItemRemoved(ProjectItem projectItem)
        {
            if (!Options.RunOnSave)
                return;

            _controller.HandleNameChange(projectItem);
        }

        private void ItemAdded(ProjectItem projectItem)
        {
            if (!Options.RunOnSave) 
                return;

            _controller.HandleNameChange(projectItem);
        }

        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            if (!Options.RunOnBuild)
                return;

            IEnumerable<Project> projects;
            switch (scope)
            {
                case vsBuildScope.vsBuildScopeProject:
                case vsBuildScope.vsBuildScopeBatch:
                    projects = ((object[]) _dte.ActiveSolutionProjects).OfType<Project>();
                    break;
                case vsBuildScope.vsBuildScopeSolution:
                default:
                    // Sometimes when you hit F5 to run a project, VS gives the undefined value 0 as vsBuildScope, 
                    // so in this case we default to running templates for all projects
                    projects = _dte.Solution.Projects.OfType<Project>();
                    break;
            }

            _controller.RunTemplates(projects);
        }

        private void ShowOptionsMenuCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var showOptionsMenuCommand = sender as OleMenuCommand;
            if (showOptionsMenuCommand == null)
                return;

            var monitorSelection = (IVsMonitorSelection)GetGlobalService(typeof(SVsShellMonitorSelection));
            IntPtr hierarchyPtr;
            IntPtr selectionContainerPtr;
            uint projectItemId;
            IVsMultiItemSelect mis;
            monitorSelection.GetCurrentSelection(out hierarchyPtr, out projectItemId, out mis, out selectionContainerPtr);

            var hierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
            if (hierarchy == null)
                return;

            object value;
            hierarchy.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_Name, out value);

            showOptionsMenuCommand.Visible = string.Equals(value as string, "T4MVC.tt", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowOptions(object sender, EventArgs e)
        {
            ShowOptionPage(typeof(Options));
        }

        protected override int QueryClose(out bool canClose)
        {
            int result = base.QueryClose(out canClose);
            if (!canClose)
                return result;

            if (_solutionEvents != null)
            {
                _solutionEvents.ProjectRemoved -= ProjectRemoved;
                _solutionEvents = null;
            }
            if (_buildEvents != null)
            {
                _buildEvents.OnBuildBegin -= OnBuildBegin;
                _buildEvents = null;
            }
            if (_documentEvents != null)
            {
                _documentEvents.DocumentSaved -= DocumentSaved;
                _documentEvents = null;
            }
            if (_projectItemsEvents != null)
            {
                _projectItemsEvents.ItemAdded -= ItemAdded;
                _projectItemsEvents.ItemRemoved -= ItemRemoved;
                _projectItemsEvents.ItemRenamed -= ItemRenamed;
            }
            if (_controller != null)
                _controller.Dispose();

            return result;
        }
    }
}