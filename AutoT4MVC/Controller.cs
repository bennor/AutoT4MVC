using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using System.Reactive.Linq;
using VSLangProj;

namespace AutoT4MVC
{
    public sealed class Controller : IDisposable
    {
        private static readonly string[] NameChangePathFragments = 
        {
            @"\Assets\",
            @"\Content\",
            @"\Controllers\",
            @"\CSS\",
            @"\Images\",
            @"\JS\",
            @"\Scripts\",
            @"\Styles\",
            @"\Views\"
        };

        private static readonly string[] ContentChangePathFragments = 
        {
            @"\Controllers\",
            @"\T4MVC.tt.settings.t4",
            @"\T4MVC.tt.settings.xml"
        };

        private event EventHandler<ProjectEventArgs> Update;

        private IDisposable projectSubscription;

        public Controller()
        {
            projectSubscription = Observable.FromEventPattern<ProjectEventArgs>(e => Update += e, e => Update -= e)
                                            .Select(e => e.EventArgs.Project)
                                            .GroupBy(p => p)
                                            .SelectMany(g => g.Throttle(TimeSpan.FromSeconds(1)))
                                            .Subscribe(RunTemplate);
        }

        private void TryTriggerUpdate(ProjectItem projectItem, IList<string> pathFragments)
        {
            if (Update == null || projectItem == null)
                return;

            if(projectItem.HasProject() && projectItem.MatchesAnyPathFragment(pathFragments))
                Update(this, new ProjectEventArgs(projectItem.ContainingProject));
        }

        public void HandleNameChange(ProjectItem item)
        {
            TryTriggerUpdate(item, NameChangePathFragments);
        }

        public void HandleContentChange(ProjectItem item)
        {
            TryTriggerUpdate(item, ContentChangePathFragments);
        }
        
        public void RunTemplates(IEnumerable<Project> projects)
        {
            if (projects == null)
                return;

            var templates = projects.Where(p => p != null).FindProjectItems("T4MVC.tt");
            foreach (var template in templates)
            {
                var templateVsProjectItem = template.Object as VSProjectItem;
                if (templateVsProjectItem != null)
                {
                    templateVsProjectItem.RunCustomTool();
                }
                else
                {
                    if (!template.IsOpen)
                        template.Open();
                    template.Save();
                }
            }
        }
        public void RunTemplate(Project project)
        {
            if (project == null)
                return;

            RunTemplates(new[] { project });
        }

        public void Dispose()
        {
            if (projectSubscription != null)
            {
                projectSubscription.Dispose();
                projectSubscription = null;
            }
        }
    }
}
