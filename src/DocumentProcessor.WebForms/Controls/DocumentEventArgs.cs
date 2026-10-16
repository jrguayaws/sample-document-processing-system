using System;

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
}
