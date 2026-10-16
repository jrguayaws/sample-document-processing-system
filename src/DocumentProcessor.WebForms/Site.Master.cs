using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Web;
using System.Web.UI;
using DocumentProcessor.WebForms.Data;

namespace DocumentProcessor.WebForms
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Stamp the stylesheet with its last write time so browsers pick up CSS edits instead of a cached copy.
            var cssPath = Server.MapPath("~/Content/Site.css");
            SiteStylesheet.Href = "~/Content/Site.css?v=" + File.GetLastWriteTimeUtc(cssPath).Ticks;

            RuntimePillText.Text = Server.HtmlEncode(FrameworkName());

            var info = HttpContext.Current.Application[Global.DatabaseInfoKey] as DatabaseInfo;

            if (info == null)
            {
                DatabasePillText.Text = "Database not resolved";
                CredentialSourceText.Text = "unavailable";
                ProviderNameText.Text = "no database";
                return;
            }

            var isPostgres = info.Provider == DatabaseProvider.PostgreSql;

            DatabasePill.Attributes["class"] = isPostgres ? "db-pill db-pill-postgres" : "db-pill";
            DatabasePillIcon.Attributes["class"] = isPostgres ? "bi bi-rocket-takeoff-fill" : "bi bi-database";

            DatabasePillText.Text = Server.HtmlEncode(info.DisplayName + " · " + info.LocationLabel);
            CredentialSourceText.Text = Server.HtmlEncode(info.CredentialSource);
            ProviderNameText.Text = Server.HtmlEncode(info.DisplayName);
        }

        /// <summary>The framework the app was compiled for, e.g. ".NET Framework 4.8".</summary>
        private static string FrameworkName()
        {
            var target = typeof(SiteMaster).Assembly.GetCustomAttribute<TargetFrameworkAttribute>();
            return target != null && !string.IsNullOrEmpty(target.FrameworkDisplayName)
                ? target.FrameworkDisplayName
                : ".NET Framework";
        }
    }
}
