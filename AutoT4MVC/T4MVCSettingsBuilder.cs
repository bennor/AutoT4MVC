using EnvDTE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace AutoT4MVC
{
    public static class T4MVCSettingsBuilder
    {
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

        public static T4MVCSettings Build(Project settingsProject)
        {
            var t4MVCTemplate = settingsProject.FindProjectItems("T4MVC.tt").FirstOrDefault();

            // No T4MVC Template
            if (t4MVCTemplate == null)
                return null;

            // Find Settings XML
            var settingsProjectItem = settingsProject.FindProjectItems("T4MVC.tt.settings.xml").FirstOrDefault();

            // Return default settings if no XML settings file found
            if (settingsProjectItem == null)
                return BuildDefault();
            else
                return Build(settingsProjectItem);
        }

        public static T4MVCSettings Build(ProjectItem settingsProjectItem)
        {
            if (!settingsProjectItem.Name.Equals("T4MVC.tt.settings.xml", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Not a valid T4MVC Settings File", "SettingsProjectItem");

            var settingsFileName = settingsProjectItem.FileNames[0];

            if (settingsFileName == null
                    || !File.Exists(settingsFileName))
                return null;

            XDocument xDocument;
            try
            {
                xDocument = XDocument.Load(settingsFileName);
            }
            catch (XmlException)
            {
                // Unable to load T4MVC XML Settings File
                return null;
            }

            var settings = T4MVCSettingsBuilder.BuildDefault();

            // Controllers Folder
            var controllersFolderValue = xDocument.Descendants("ControllersFolder").Select(e => string.Format(@"\{0}\", e.Value)).FirstOrDefault();
            if (controllersFolderValue != null)
                settings.ControllersFolder = controllersFolderValue;

            // Views Root Folder
            var viewsRootFolderValue = xDocument.Descendants("ViewsRootFolder").Select(e => string.Format(@"\{0}\", e.Value)).FirstOrDefault();
            if (viewsRootFolderValue != null)
                settings.ViewsRootFolder = viewsRootFolderValue;

            // Non Qualified View Folders
            var nonQualifiedViewFoldersNode = xDocument.Descendants("NonQualifiedViewFolders").FirstOrDefault();
            if (nonQualifiedViewFoldersNode != null)
                settings.NonQualifiedViewFolders = nonQualifiedViewFoldersNode.Elements("ViewFolder").Select(e => string.Format(@"\{0}\", e.Value)).ToList();

            // Static Files Folders
            var staticFilesFoldersNode = xDocument.Descendants("StaticFilesFolders").FirstOrDefault();
            if (staticFilesFoldersNode != null)
                settings.StaticFilesFolders = staticFilesFoldersNode.Elements("FileFolder").Select(e => string.Format(@"\{0}\", e.Value)).ToList();

            // Excluded Static File Extensions
            var excludedStaticFileExtensionsNode = xDocument.Descendants("ExcludedStaticFileExtensions").FirstOrDefault();
            if (excludedStaticFileExtensionsNode != null)
                settings.ExcludedStaticFileExtensions = excludedStaticFileExtensionsNode.Elements("Extension").Select(e => e.Value).ToList();

            // Excluded View Extensions
            var excludedViewExtensionsNode = xDocument.Descendants("ExcludedViewExtensions").FirstOrDefault();
            if (excludedViewExtensionsNode != null)
                settings.ExcludedViewExtensions = excludedViewExtensionsNode.Elements("Extension").Select(e => e.Value).ToList();

            return settings;
        }
    }
}
