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
        private event EventHandler<ProjectEventArgs> Update;

        private IDisposable _projectSubscription;
        private readonly T4MVCSettingsCache _settingsCache;

        public Controller()
        {
            _settingsCache = new T4MVCSettingsCache();

            _projectSubscription = Observable.FromEventPattern<ProjectEventArgs>(e => Update += e, e => Update -= e)
                                            .Select(e => e.EventArgs.Project)
                                            .GroupBy(p => p)
                                            .SelectMany(g => g.Throttle(TimeSpan.FromSeconds(1)))
                                            .Subscribe(RunTemplate);
        }

        public void HandleNameChange(ProjectItem item)
        {
            var reloadSettings = T4MVCSettings.RequiresCacheInvalidation(item);
            
            var settings = _settingsCache.GetSettings(item, reloadSettings);

            if (settings == null)
                return;

            if (settings.TriggerOnNameChange(item))
                Update(this, new ProjectEventArgs(item.ContainingProject));
        }

        public void HandleContentChange(ProjectItem item)
        {
            var reloadSettings = T4MVCSettings.RequiresCacheInvalidation(item);

            var settings = _settingsCache.GetSettings(item, reloadSettings);

            if (settings == null)
                return;

            if (settings.TriggerOnContentChange(item))
                Update(this, new ProjectEventArgs(item.ContainingProject));
        }

        public void HandleProjectUnload(Project project)
        {
            _settingsCache.InvalidateSettings(project);
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
            if (_projectSubscription != null)
            {
                _projectSubscription.Dispose();
                _projectSubscription = null;
            }
        }
    }
}
