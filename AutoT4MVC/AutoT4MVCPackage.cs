using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;

namespace AutoT4MVC
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidAutoT4MVCPkgString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    public sealed class AutoT4MVCPackage : Package
    {
        private DTE dte;
        private BuildEvents buildEvents;
        private DocumentEvents documentEvents;
        private ProjectItemsEvents projectItemsEvents;
        private Controller controller;

        protected override void Initialize()
        {
            base.Initialize();
            
            dte = GetService(typeof(SDTE)) as DTE;
            if (dte == null)
                return;

            controller = new Controller();

            buildEvents = dte.Events.BuildEvents;
            buildEvents.OnBuildBegin += OnBuildBegin;

            documentEvents = dte.Events.DocumentEvents;
            documentEvents.DocumentSaved += DocumentSaved;

            var events2 = dte.Events as Events2;
            if (events2 == null)
                return;

            projectItemsEvents = events2.ProjectItemsEvents;
            projectItemsEvents.ItemAdded += ItemAdded;
            projectItemsEvents.ItemRemoved += ItemRemoved;
            projectItemsEvents.ItemRenamed += ItemRenamed;
        }

        private void DocumentSaved(Document document)
        {
           controller.HandleContentChange(document.ProjectItem);
        }

        private void ItemRenamed(ProjectItem projectItem, string oldName)
        {
           controller.HandleNameChange(projectItem);
        }

        private void ItemRemoved(ProjectItem projectItem)
        {
            controller.HandleNameChange(projectItem);
        }

        private void ItemAdded(ProjectItem projectItem)
        {
            controller.HandleNameChange(projectItem);
        }

        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            IEnumerable<Project> projects = null;
            switch (scope)
            {
                case vsBuildScope.vsBuildScopeSolution:
                    projects = dte.Solution.Projects.OfType<Project>();
                    break;
                case vsBuildScope.vsBuildScopeProject:
                    projects = ((object[])dte.ActiveSolutionProjects).OfType<Project>();
                    break;
                default:
                    return;
            }

            controller.RunTemplates(projects);
        }

        protected override int QueryClose(out bool canClose)
        {
            int result = base.QueryClose(out canClose);
            if (!canClose)
                return result;

            if (buildEvents != null)
            {
                buildEvents.OnBuildBegin -= OnBuildBegin;
                buildEvents = null;
            }
            if (documentEvents != null)
            {
                documentEvents.DocumentSaved -= DocumentSaved;
                documentEvents = null;
            }
            if (projectItemsEvents != null)
            {
                projectItemsEvents.ItemAdded -= ItemAdded;
                projectItemsEvents.ItemRemoved -= ItemRemoved;
                projectItemsEvents.ItemRenamed -= ItemRenamed;
            }
            if(controller != null)
                controller.Dispose();

            return result;
        }
    }
}
