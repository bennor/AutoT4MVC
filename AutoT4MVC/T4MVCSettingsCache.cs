using EnvDTE;
using System.Collections.Concurrent;

namespace AutoT4MVC
{
    public class T4MVCSettingsCache
    {
        private readonly ConcurrentDictionary<Project, T4MVCSettings> _projectT4MVCSettings;

        public T4MVCSettingsCache()
        {
            _projectT4MVCSettings = new ConcurrentDictionary<Project, T4MVCSettings>();
        }

        public T4MVCSettings GetSettings(Project project)
        {
            T4MVCSettings settings;

            // Try from Cache
            if (_projectT4MVCSettings.TryGetValue(project, out settings))
                return settings;

            // Building Settings
            settings = T4MVCSettingsBuilder.Build(project);

            // Cache
            _projectT4MVCSettings.TryAdd(project, settings);

            return settings;
        }
        public T4MVCSettings GetSettings(Project project, bool forceReload)
        {
            if (forceReload)
                InvalidateSettings(project);

            return GetSettings(project);
        }

        public T4MVCSettings GetSettings(ProjectItem projectItem)
        {
            if (projectItem.HasProject())
                return GetSettings(projectItem.ContainingProject);
            else
                return null;
        }
        public T4MVCSettings GetSettings(ProjectItem projectItem, bool forceReload)
        {
            if (forceReload)
                InvalidateSettings(projectItem.ContainingProject);

            return GetSettings(projectItem);
        }

        public bool InvalidateSettings(Project project)
        {
            T4MVCSettings settings;

            return _projectT4MVCSettings.TryRemove(project, out settings);
        }
    }
}
