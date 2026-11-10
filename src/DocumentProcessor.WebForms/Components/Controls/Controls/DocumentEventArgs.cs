using System;

namespace DocumentProcessor.WebForms.Components.Controls.Controls
{
    /// <summary>
    /// Event payload raised by the DocumentList component for the View Summary and Delete actions.
    /// Moved from the legacy Controls/DocumentList.ascx.cs.
    /// </summary>
    public class DocumentEventArgs : EventArgs
    {
        public DocumentEventArgs(Guid documentId)
        {
            DocumentId = documentId;
        }

        public Guid DocumentId { get; private set; }
    }
}
