using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoT4MVC
{
    public class T4MVCSettings
    {
        private static readonly string[] SettingsFileNames =
        {
            @"\T4MVC.tt.settings.xml",
            @"\T4MVC.tt.settings.t4"
        };

        public string ControllersFolder { get; set; }
        public string ViewsRootFolder { get; set; }

        public List<string> NonQualifiedViewFolders { get; set; }
        public List<string> StaticFilesFolders { get; set; }
        public List<string> ExcludedStaticFileExtensions { get; set; }
        public List<string> ExcludedViewExtensions { get; set; }

        private bool TriggerAlways(ProjectItem item)
        {
            return IsSettingsFile(item);
        }

        public bool TriggerOnNameChange(ProjectItem item)
        {
            if (TriggerAlways(item))
                return true;

            var contentType = GetContentType(item);

            switch (contentType)
            {
                case T4ContentType.Controller:
                case T4ContentType.View:
                case T4ContentType.StaticContent:
                    return true;
                default:
                    return false;
            }
        }

        public bool TriggerOnContentChange(ProjectItem item)
        {
            if (TriggerAlways(item))
                return true;

            var contentType = GetContentType(item);

            switch (contentType)
            {
                case T4ContentType.Controller:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsSettingsFile(ProjectItem item)
        {
            return item.GetFileNames()
                .Any(fileName => SettingsFileNames
                    .Any(settingFilename => fileName.EndsWith(settingFilename, StringComparison.InvariantCultureIgnoreCase)));
        }

        public static bool RequiresCacheInvalidation(ProjectItem item)
        {
            return IsSettingsFile(item);
        }

        private enum T4ContentType
        {
            Controller,
            View,
            StaticContent,
            Unknown
        }

        private T4ContentType GetContentType(ProjectItem item)
        {
            var fileNames = item.GetFileNames();

            foreach (var fileName in fileNames)
            {
                if (fileName.IndexOf(this.ControllersFolder, StringComparison.InvariantCultureIgnoreCase) >= 0
                        && fileName.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase))
                    return T4ContentType.Controller;

                if (fileName.IndexOf(this.ViewsRootFolder, StringComparison.InvariantCultureIgnoreCase) >= 0
                        && ExcludedViewExtensions.All(ee => !fileName.EndsWith(ee, StringComparison.InvariantCultureIgnoreCase))
                        && NonQualifiedViewFolders.All(nq => fileName.IndexOf(nq, StringComparison.InvariantCultureIgnoreCase) < 0))
                    return T4ContentType.View;

                if (StaticFilesFolders.Any(sff => fileName.StartsWith(sff, StringComparison.InvariantCultureIgnoreCase))
                        && ExcludedStaticFileExtensions.All(ee => !fileName.EndsWith(ee, StringComparison.InvariantCultureIgnoreCase)))
                    return T4ContentType.StaticContent;
            }
            return T4ContentType.Unknown;
        }
    }
}
