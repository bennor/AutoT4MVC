using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AutoT4MVC
{
    public class T4MVCSettings
    {
        private static readonly string[] settingFilenames =
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

        #region Triggers

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
            var fileNames = item.GetFileNames();

            return settingFilenames
                .Any(settingFilename => fileNames
                    .Any(fileName => fileName.EndsWith(settingFilename, StringComparison.InvariantCultureIgnoreCase)));
        }

        public static bool RequiresCacheInvalidation(ProjectItem item)
        {
            return IsSettingsFile(item);
        }

        #endregion

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

        #region Builders

        public static T4MVCSettings BuildDefault()
        {
            return new T4MVCSettings()
            {
                ControllersFolder = @"\Controllers\",
                ViewsRootFolder = @"\Views\",
                NonQualifiedViewFolders = new List<string>(){
                     @"\DisplayTemplates\",
                     @"\EditorTemplates\"
                 },
                StaticFilesFolders = new List<string>(){
                     @"\Scripts\",
                     @"\Content\"
                 },
                ExcludedStaticFileExtensions = new List<string>(){
                     ".cs",
                     ".cshtml",
                     ".aspx",
                     ".ascx"
                 },
                ExcludedViewExtensions = new List<string>()
                {
                    ".master",
                    ".js",
                    ".css"
                }
            };
        }

        public static T4MVCSettings Build(Project SettingsProject)
        {
            var t4MVCTemplate = SettingsProject.FindProjectItems("T4MVC.tt").FirstOrDefault();

            // No T4MVC Template
            if (t4MVCTemplate == null)
                return null;

            // Find Settings XML
            var settingsProjectItem = SettingsProject.FindProjectItems("T4MVC.tt.settings.xml").FirstOrDefault();

            // Return default settings if no XML settings file found
            if (settingsProjectItem == null)
                return BuildDefault();
            else
                return Build(settingsProjectItem);
        }

        public static T4MVCSettings Build(ProjectItem SettingsProjectItem)
        {
            if (!SettingsProjectItem.Name.Equals("T4MVC.tt.settings.xml", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Not a valid T4MVC Settings File", "SettingsProjectItem");

            var settingsFileName = SettingsProjectItem.FileNames[0];

            if (settingsFileName == null
                    || !File.Exists(settingsFileName))
                return null;

            var settingsXml = File.ReadAllText(settingsFileName);

            var xmlDocument = new XmlDocument();
            try
            {
                xmlDocument.LoadXml(settingsXml);
            }
            catch (XmlException)
            {
                // Unable to load T4MVC XML Settings File
                return null;
            }

            var settings = T4MVCSettings.BuildDefault();

            // Controllers Folder
            var controllersFolderNode = xmlDocument.GetElementsByTagName("ControllersFolder").Cast<XmlNode>().FirstOrDefault();
            if (controllersFolderNode != null)
                settings.ControllersFolder = "\\" + controllersFolderNode.InnerText + "\\";

            // Views Root Folder
            var viewsRootFolderNode = xmlDocument.GetElementsByTagName("ViewsRootFolder").Cast<XmlNode>().FirstOrDefault();
            if (viewsRootFolderNode != null)
                settings.ViewsRootFolder = "\\" + viewsRootFolderNode.InnerText + "\\";

            // Non Qualified View Folders
            var nonQualifiedViewFoldersNode = xmlDocument.GetElementsByTagName("NonQualifiedViewFolders").Cast<XmlNode>().FirstOrDefault();
            if (nonQualifiedViewFoldersNode != null)
                settings.NonQualifiedViewFolders = nonQualifiedViewFoldersNode.ChildNodes.Cast<XmlNode>().Select(n => "\\" + n.InnerText + "\\").ToList();

            // Static Files Folders
            var staticFilesFoldersNode = xmlDocument.GetElementsByTagName("StaticFilesFolders").Cast<XmlNode>().FirstOrDefault();
            if (staticFilesFoldersNode != null)
                settings.StaticFilesFolders = staticFilesFoldersNode.ChildNodes.Cast<XmlNode>().Select(n => "\\" + n.InnerText + "\\").ToList();

            // Excluded Static File Extensions
            var excludedStaticFileExtensionsNode = xmlDocument.GetElementsByTagName("ExcludedStaticFileExtensions").Cast<XmlNode>().FirstOrDefault();
            if (excludedStaticFileExtensionsNode != null)
                settings.ExcludedStaticFileExtensions = excludedStaticFileExtensionsNode.ChildNodes.Cast<XmlNode>().Select(n => n.InnerText).ToList();

            // Excluded View Extensions
            var excludedViewExtensionsNode = xmlDocument.GetElementsByTagName("ExcludedViewExtensions").Cast<XmlNode>().FirstOrDefault();
            if (excludedViewExtensionsNode != null)
                settings.ExcludedViewExtensions = excludedViewExtensionsNode.ChildNodes.Cast<XmlNode>().Select(n => n.InnerText).ToList();

            return settings;
        }

        #endregion
    }
}
