using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using DocumentProcessor.WebForms.Models;

namespace DocumentProcessor.WebForms.Controls
{
    public class DocumentEventArgs : EventArgs
    {
        public DocumentEventArgs(Guid documentId)
        {
            DocumentId = documentId;
        }

        public Guid DocumentId { get; private set; }
    }

    public partial class DocumentList : UserControl
    {
        public event EventHandler RefreshRequested;

        public event EventHandler<DocumentEventArgs> ViewSummaryRequested;

        public event EventHandler<DocumentEventArgs> DeleteRequested;

        public void Bind(IList<Document> documents)
        {
            CountText.Text = documents.Count.ToString();
            EmptyState.Visible = documents.Count == 0;

            DocumentRepeater.DataSource = documents;
            DocumentRepeater.DataBind();
        }

        protected void RefreshButton_Click(object sender, EventArgs e)
        {
            if (RefreshRequested != null)
            {
                RefreshRequested(this, EventArgs.Empty);
            }
        }

        protected void DocumentRepeater_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            Guid documentId;
            if (!Guid.TryParse(Convert.ToString(e.CommandArgument), out documentId))
            {
                return;
            }

            if (e.CommandName == "ViewSummary" && ViewSummaryRequested != null)
            {
                ViewSummaryRequested(this, new DocumentEventArgs(documentId));
            }
            else if (e.CommandName == "Delete" && DeleteRequested != null)
            {
                DeleteRequested(this, new DocumentEventArgs(documentId));
            }
        }

        protected string Encode(string value)
        {
            return HttpUtility.HtmlEncode(value ?? string.Empty);
        }

        protected string FileLabel(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return "FILE";
            }

            var label = extension.TrimStart('.').ToUpperInvariant();
            return label.Length > 0 ? Encode(label) : "FILE";
        }

        protected string FileTagCss(string extension)
        {
            var isPdf = string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase);
            return isPdf ? string.Empty : "file-tag-text";
        }

        protected string StatusCss(object status)
        {
            switch ((DocumentStatus)status)
            {
                case DocumentStatus.Processed:
                    return "status-processed";
                case DocumentStatus.Processing:
                    return "status-processing";
                case DocumentStatus.Failed:
                    return "status-failed";
                default:
                    return "status-pending";
            }
        }

        protected string StatusIcon(object status)
        {
            switch ((DocumentStatus)status)
            {
                case DocumentStatus.Processed:
                    return "bi-check-lg";
                case DocumentStatus.Processing:
                    return "bi-hourglass-split";
                case DocumentStatus.Failed:
                    return "bi-x-lg";
                default:
                    return "bi-clock";
            }
        }

        protected string UploadedText(object uploadedAt)
        {
            var value = (DateTimeOffset)uploadedAt;

            return value == default(DateTimeOffset)
                ? "Upload time unknown"
                : value.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
        }

        protected string SummaryPreview(string summary)
        {
            return string.IsNullOrWhiteSpace(summary) ? "No summary yet." : Encode(summary);
        }

        protected bool HasSummary(string summary)
        {
            return !string.IsNullOrWhiteSpace(summary);
        }
    }
}
