using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace AutoT4MVC
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidAutoT4MVCPkgString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    public sealed class AutoT4MVCPackage : Package
    {
        private static readonly string[] DocumentNameChangeTriggers = 
        {
            @"\Content\",
            @"\Controllers\",
            @"\Scripts\",
            @"\Views\"
        };
        private static readonly string[] DocumentContentChangeTriggers = 
        {
            @"\Controllers\"
        };

        private DTE dte;
        private BuildEvents buildEvents;
        private DocumentEvents documentEvents;
        private ProjectItemsEvents projectItemsEvents;
        
        protected override void Initialize()
        {
            base.Initialize();
            
            dte = GetService(typeof(SDTE)) as DTE;

            buildEvents = dte.Events.BuildEvents;
            buildEvents.OnBuildBegin += OnBuildBegin;

            documentEvents = dte.Events.DocumentEvents;
            documentEvents.DocumentSaved += DocumentSaved;

            var events2 = dte.Events as Events2;
            if (events2 != null)
            {
                projectItemsEvents = events2.ProjectItemsEvents;
                projectItemsEvents.ItemAdded += ItemAdded;
                projectItemsEvents.ItemRemoved += ItemRemoved;
                projectItemsEvents.ItemRenamed += ItemRenamed;
            }
        }

        private void RunTemplates(params Project[] projects)
        {
            if (projects == null)
                return;

            var t4MvcTemplates = FindProjectItems("T4MVC.tt", projects).ToList();
            foreach (var t4MvcTemplate in t4MvcTemplates)
            {
                if (!t4MvcTemplate.IsOpen)
                    t4MvcTemplate.Open();
                t4MvcTemplate.Save();
            }
        }

        private static bool IsMatch(string fileName, string[] triggerPaths)
        {
            return triggerPaths.Any(p => fileName.IndexOf(p, StringComparison.OrdinalIgnoreCase) > -1);
        }

        private static bool IsMatch(ProjectItem projectItem, string[] triggerPaths)
        {
            if (projectItem == null || triggerPaths == null)
                return false;

            short fileCount = projectItem.FileCount;
            for (short i = 0; i < fileCount; i++)
            {
                try
                {
                    if (IsMatch(projectItem.FileNames[i], triggerPaths))
                        return true;
                }
                catch (COMException) { }
            }

            return false;
        }

        private void DocumentSaved(Document Document)
        {
            var projectItem = Document.ProjectItem;
            if (projectItem == null)
                return;

            if (IsMatch(projectItem, DocumentContentChangeTriggers))
                RunTemplates(projectItem.ContainingProject);
        }

        private void ItemRenamed(ProjectItem ProjectItem, string OldName)
        {
            if(IsMatch(ProjectItem, DocumentNameChangeTriggers))
                RunTemplates(ProjectItem.ContainingProject);
        }

        private void ItemRemoved(ProjectItem ProjectItem)
        {
            if (IsMatch(ProjectItem, DocumentNameChangeTriggers))
                RunTemplates(ProjectItem.ContainingProject);
        }

        private void ItemAdded(ProjectItem ProjectItem)
        {
            if (IsMatch(ProjectItem, DocumentNameChangeTriggers))
                RunTemplates(ProjectItem.ContainingProject);
        }

        private void OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            IEnumerable<Project> projects = null;
            switch (Scope)
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

            RunTemplates(projects.ToArray());
        }

        private IEnumerable<ProjectItem> FindProjectItems(string name, IEnumerable<Project> projects)
        {
            if (projects == null)
                projects = dte.Solution.Projects.OfType<Project>();

            foreach (Project project in projects)
            {
                foreach (var projectItem in FindProjectItems(name, project.ProjectItems))
                    yield return projectItem;
            }
        }

        private static IEnumerable<ProjectItem> FindProjectItems(string name, ProjectItems projectItems)
        {
            foreach (ProjectItem projectItem in projectItems)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(projectItem.Name, name))
                    yield return projectItem;

                if (projectItem.ProjectItems != null)
                {
                    foreach (var subItem in FindProjectItems(name, projectItem.ProjectItems))
                        yield return subItem;
                }
                if (projectItem.SubProject != null)
                {
                    foreach (var subItem in FindProjectItems(name, projectItem.SubProject.ProjectItems))
                        yield return subItem;
                }
            }
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
            return result;
        }
    }
}
