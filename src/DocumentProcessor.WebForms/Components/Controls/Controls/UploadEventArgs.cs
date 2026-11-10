using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Forms;

namespace DocumentProcessor.WebForms.Components.Controls.Controls
{
    /// <summary>
    /// Payload of <see cref="DocumentUploader.UploadRequested"/>: the browser files that passed
    /// the size and extension checks. Read each one with
    /// <c>file.OpenReadStream(maxAllowedSize)</c> (Storage.MaxFileSizeMegabytes) and use
    /// <c>file.Size</c> in place of the old <c>HttpPostedFile.ContentLength</c>.
    /// </summary>
    public class UploadEventArgs : EventArgs
    {
        public UploadEventArgs(IList<IBrowserFile> files)
        {
            Files = files;
        }

        public IList<IBrowserFile> Files { get; private set; }
    }
}
