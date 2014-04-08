using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace AutoT4MVC
{
    public static class ProjectItemExtensions
    {
        public static IEnumerable<ProjectItem> FindProjectItems(this Project project, string name)
        {
            return project.ProjectItems.FindProjectItems(name);
        }

        public static IEnumerable<ProjectItem> FindProjectItems(this IEnumerable<Project> projects, string name)
        {
            return projects.SelectMany(project => project.ProjectItems.FindProjectItems(name));
        }

        private static IEnumerable<ProjectItem> FindProjectItems(this ProjectItems projectItems, string name)
        {
            foreach (ProjectItem projectItem in projectItems)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(projectItem.Name, name))
                    yield return projectItem;

                if (projectItem.ProjectItems != null)
                {
                    foreach (var subItem in projectItem.ProjectItems.FindProjectItems(name))
                        yield return subItem;
                }
                if (projectItem.SubProject != null)
                {
                    foreach (var subItem in projectItem.SubProject.ProjectItems.FindProjectItems(name))
                        yield return subItem;
                }
            }
        }

        public static bool HasProject(this ProjectItem projectItem)
        {
            if (projectItem == null)
                throw new ArgumentNullException("projectItem");

            return projectItem.ContainingProject != null
                   && !string.Equals(projectItem.ContainingProject.Name, "Miscellaneous Files",
                                    StringComparison.OrdinalIgnoreCase);
        }

        public static bool MatchesAnyPathFragment(this ProjectItem projectItem, IList<string> pathFragments)
        {
            if (projectItem == null)
                throw new ArgumentNullException("projectItem");

            if (pathFragments == null)
                return false;

            return projectItem.GetFileNames()
                .Any(fileName =>
                    pathFragments.Any(p =>
                        fileName.IndexOf(p, StringComparison.OrdinalIgnoreCase) > -1));
        }

        public static IEnumerable<string> GetFileNames(this ProjectItem item)
        {
            var projectFolderPath = item.HasProject()
                                            ? Path.GetDirectoryName(item.ContainingProject.FullName)
                                            : null;

            return Enumerable.Range(0, item.FileCount).Select(i =>
            {
                try
                {
                    string fileName = item.FileNames[(short)i];

                    if (projectFolderPath != null)
                        fileName = fileName.Replace(projectFolderPath, "");

                    return fileName;
                }
                catch (COMException) { return null; /* Ignore invalid exceptions */ }
            }).Where(f => !string.IsNullOrWhiteSpace(f));
        }
    }
}
