<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DocumentUploader.ascx.cs" Inherits="DocumentProcessor.WebForms.Controls.DocumentUploader" %>

<h1 class="upload-title">Upload a document and I'll summarize it.</h1>
<p class="upload-subtitle">
    <asp:Literal ID="FileTypeSummaryText" runat="server" />
    &middot; up to <asp:Literal ID="MaxFileSizeText" runat="server" /> MB
</p>

<div class="dropzone">
    <%-- The file input is stretched over the whole zone by .dropzone-input, which is what
         makes dropping files onto it work without any script. --%>
    <asp:FileUpload ID="FilePicker" runat="server" AllowMultiple="true"
        CssClass="dropzone-input" ToolTip="Drag files here or click to browse" />
    <div class="dropzone-icon"><i class="bi bi-cloud-arrow-up"></i></div>
    <p class="dropzone-hint">Drag files here</p>
    <span class="btn-brand">Browse files</span>
</div>

<%-- Filled in by script once the user picks files: the server does not know what is in
     the file input until the form is posted. --%>
<ul id="selectedList" class="selected-list"></ul>

<div id="uploadActions" class="d-flex gap-2 mt-3" style="display: none;">
    <%-- A LinkButton rather than a Button: the busy state needs a spinner element inside the
         control, and posting through __doPostBack means suppressing a second click cannot
         stop the first one from reaching the server. --%>
    <asp:LinkButton ID="UploadButton" runat="server" CssClass="btn-brand" OnClick="UploadButton_Click">
        <span id="uploadIdleLabel">Upload</span>
        <span id="uploadBusyLabel" style="display: none;">
            <span class="spinner-border spinner-border-sm me-1"></span>Uploading...
        </span>
    </asp:LinkButton>
    <button type="button" id="clearButton" class="btn-quiet">Clear</button>
</div>

<%-- Summarizing happens inside the postback, so the wait can be long. Say so. --%>
<p id="uploadStatus" class="upload-status" style="display: none;"></p>

<asp:Repeater ID="NoticeRepeater" runat="server">
    <ItemTemplate>
        <div class="notice notice-<%# Eval("CssSuffix") %>">
            <i class="bi <%# Eval("IconClass") %>"></i>
            <span><%# System.Web.HttpUtility.HtmlEncode(Eval("Text") as string) %></span>
        </div>
    </ItemTemplate>
</asp:Repeater>

<script type="text/javascript">
    (function () {
        var picker = document.getElementById('<%= FilePicker.ClientID %>');
        var uploadButton = document.getElementById('<%= UploadButton.ClientID %>');
        var list = document.getElementById('selectedList');
        var actions = document.getElementById('uploadActions');
        var clearButton = document.getElementById('clearButton');
        var idleLabel = document.getElementById('uploadIdleLabel');
        var busyLabel = document.getElementById('uploadBusyLabel');
        var status = document.getElementById('uploadStatus');
        var submitting = false;

        function formatSize(bytes) {
            var units = ['B', 'KB', 'MB', 'GB'];
            var size = bytes;
            var unit = 0;
            while (size >= 1024 && unit < units.length - 1) {
                size = size / 1024;
                unit++;
            }
            return (Math.round(size * 100) / 100) + ' ' + units[unit];
        }

        function render() {
            var files = picker.files;
            list.innerHTML = '';

            for (var i = 0; i < files.length; i++) {
                var item = document.createElement('li');
                item.className = 'selected-item';
                var label = document.createElement('small');
                label.appendChild(document.createTextNode(files[i].name + ' (' + formatSize(files[i].size) + ')'));
                item.appendChild(label);
                list.appendChild(item);
            }

            actions.style.display = files.length > 0 ? '' : 'none';
            idleLabel.textContent = files.length === 1
                ? 'Upload 1 file'
                : 'Upload ' + files.length + ' files';
        }

        function reset() {
            picker.value = '';
            render();
        }

        // The page is replaced wholesale by the postback, so this busy state only has to
        // survive until the response arrives. Nothing needs to undo it.
        function startUpload() {
            if (submitting) {
                return false;
            }

            submitting = true;
            idleLabel.style.display = 'none';
            busyLabel.style.display = '';
            uploadButton.className = 'btn-brand is-busy';
            clearButton.disabled = true;

            var count = picker.files.length;
            status.textContent = 'Uploading ' + count + (count === 1 ? ' file' : ' files')
                + '. Each document is summarized before the page comes back, so this can take'
                + ' a while. Leaving the page cancels it.';
            status.style.display = '';
        }

        picker.onchange = render;
        clearButton.onclick = reset;

        // Returning false from here would cancel the LinkButton's postback, which is exactly
        // what should happen on a second click.
        uploadButton.onclick = startUpload;
    })();
</script>
