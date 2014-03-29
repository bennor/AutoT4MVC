using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace AutoT4MVC
{
    public class Options : DialogPage
    {
        private bool _runOnSave = true;
        private bool _runOnBuild = true;

        public const string CategoryName = "AutoT4MVC";
        public const string PageName = "General";

        [Category("General")]
        [DisplayName("Run on save")]
        [Description("Run T4MVC templates when files in the Controllers, Views, Scripts and Content folders are saved, created or deleted.")]
        [DefaultValue(true)]
        public bool RunOnSave
        {
            get { return _runOnSave; }
            set { _runOnSave = value; }
        }

        [Category("General")]
        [DisplayName("Run on build")]
        [Description("Run T4MVC templates when building.")]
        [DefaultValue(true)]
        public bool RunOnBuild
        {
            get { return _runOnBuild; }
            set { _runOnBuild = value; }
        }
    }
}