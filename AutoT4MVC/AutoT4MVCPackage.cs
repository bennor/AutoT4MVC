using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
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
        private DTE dte;
        private BuildEvents buildEvents;
        
        protected override void Initialize()
        {
            base.Initialize();
            
            dte = GetService(typeof(SDTE)) as DTE;
            buildEvents = dte.Events.BuildEvents;
            buildEvents.OnBuildBegin += OnBuildBegin;
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

            var t4MvcTemplates = FindProjectItems("T4MVC.tt", projects).ToList();
            foreach (var t4MvcTemplate in t4MvcTemplates)
            {
                if (!t4MvcTemplate.IsOpen)
                    t4MvcTemplate.Open();
                t4MvcTemplate.Save();
            }
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
                if (projectItem.Name == name)
                    yield return projectItem;

                if (projectItem.ProjectItems != null)
                {
                    foreach (var subItem in FindProjectItems(name, projectItem.ProjectItems))
                        yield return projectItem;
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
            if (canClose && buildEvents != null)
            {
                buildEvents.OnBuildBegin -= OnBuildBegin;
                buildEvents = null;
            }
            return result;
        }

    }
}
