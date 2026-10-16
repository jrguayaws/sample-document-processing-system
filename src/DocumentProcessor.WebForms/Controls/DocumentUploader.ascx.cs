using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using DocumentProcessor.WebForms.Configuration;
using DocumentProcessor.WebForms.Models;
using DocumentProcessor.WebForms.Services;

namespace DocumentProcessor.WebForms.Controls
{
    public class UploadEventArgs : EventArgs
    {
        public UploadEventArgs(IList<HttpPostedFile> files)
        {
            Files = files;
        }

        public IList<HttpPostedFile> Files { get; private set; }
    }

    public partial class DocumentUploader : UserControl
    {
        private readonly List<Notification> _notifications = new List<Notification>();

        /// <summary>
        /// Raised once the posted files have passed size and extension checks. The page
        /// stores them and runs the pipeline; this control only deals with the form.
        /// </summary>
        public event EventHandler<UploadEventArgs> UploadRequested;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack)
            {
                return;
            }

            var extensions = AppSettings.StorageAllowedExtensions
                .Select(extension => extension.TrimStart('.').ToUpperInvariant());

            FileTypeSummaryText.Text = Server.HtmlEncode(string.Join(", ", extensions));
            MaxFileSizeText.Text = AppSettings.StorageMaxFileSizeMegabytes.ToString();
        }

        /// <summary>Adds a message shown under the dropzone. Call any time before PreRender.</summary>
        public void Notify(string text, NotificationLevel level)
        {
            _notifications.Add(new Notification(text, level));
        }

        protected override void OnPreRender(EventArgs e)
        {
            // Bound this late so the page can add messages from its own handlers, not just
            // from the upload click below.
            NoticeRepeater.DataSource = _notifications;
            NoticeRepeater.DataBind();

            base.OnPreRender(e);
        }

        protected void UploadButton_Click(object sender, EventArgs e)
        {
            var accepted = new List<HttpPostedFile>();
            var maxFiles = AppSettings.StorageMaxFilesPerUpload;
            var maxBytes = AppSettings.StorageMaxFileSizeBytes;
            var allowed = AppSettings.StorageAllowedExtensions;

            var posted = FilePicker.PostedFiles
                .Where(file => file != null && !string.IsNullOrEmpty(file.FileName))
                .ToList();

            if (posted.Count == 0)
            {
                Notify("Choose at least one file to upload.", NotificationLevel.Warning);
                return;
            }

            if (posted.Count > maxFiles)
            {
                Notify(
                    string.Format("Only the first {0} files were taken from this upload.", maxFiles),
                    NotificationLevel.Warning);

                posted = posted.Take(maxFiles).ToList();
            }

            foreach (var file in posted)
            {
                var name = Path.GetFileName(file.FileName);

                if (file.ContentLength > maxBytes)
                {
                    Notify(
                        string.Format("'{0}' exceeds {1} MB", name, AppSettings.StorageMaxFileSizeMegabytes),
                        NotificationLevel.Warning);
                    continue;
                }

                var extension = Path.GetExtension(name).ToLowerInvariant();

                if (!allowed.Contains(extension) || !DocumentTextExtractor.CanExtract(extension))
                {
                    Notify(
                        string.Format("'{0}' is not a supported file type", name),
                        NotificationLevel.Warning);
                    continue;
                }

                accepted.Add(file);
            }

            if (accepted.Count > 0 && UploadRequested != null)
            {
                UploadRequested(this, new UploadEventArgs(accepted));
            }
        }
    }
}
