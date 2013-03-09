using System;
using System.Linq;
using EnvDTE;

namespace AutoT4MVC
{
    public class ProjectEventArgs : EventArgs
    {
        public Project Project { get; private set; }   
 
        public ProjectEventArgs(Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            Project = project;
        }
    }
}