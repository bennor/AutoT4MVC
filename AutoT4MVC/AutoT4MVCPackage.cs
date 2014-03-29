using System;
using System.Collections.Generic;
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
    public sealed class AutoT4MVCPackage : Package
    {
        private BuildEvents _buildEvents;
        private Controller _controller;
        private DocumentEvents _documentEvents;
        private DTE _dte;
        private ProjectItemsEvents _projectItemsEvents;

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

        protected override int QueryClose(out bool canClose)
        {
            int result = base.QueryClose(out canClose);
            if (!canClose)
                return result;

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