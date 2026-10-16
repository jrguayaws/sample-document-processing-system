<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="DocumentList.ascx.cs" Inherits="DocumentProcessor.WebForms.Controls.DocumentList" %>

<div class="list-header">
    <h2 class="list-title">
        Documents <span class="list-title-count">(<asp:Literal ID="CountText" runat="server" />)</span>
    </h2>
    <asp:LinkButton ID="RefreshButton" runat="server" CssClass="btn-quiet" OnClick="RefreshButton_Click">
        <i class="bi bi-arrow-clockwise"></i>Refresh
    </asp:LinkButton>
</div>

<asp:PlaceHolder ID="EmptyState" runat="server" Visible="false">
    <p class="empty-state mb-0">No documents yet</p>
</asp:PlaceHolder>

<asp:Repeater ID="DocumentRepeater" runat="server" OnItemCommand="DocumentRepeater_ItemCommand">
    <ItemTemplate>
        <article class="doc-card">
            <span class="file-tag <%# FileTagCss(Eval("FileExtension") as string) %>">
                <%# FileLabel(Eval("FileExtension") as string) %>
            </span>

            <div class="doc-card-body">
                <div class="doc-card-heading">
                    <span class="doc-name" title="<%# Encode(Eval("OriginalFileName") as string) %>">
                        <%# Encode(Eval("OriginalFileName") as string) %>
                    </span>
                    <span class="status-pill <%# StatusCss(Eval("Status")) %>">
                        <i class="bi <%# StatusIcon(Eval("Status")) %>"></i><%# Eval("Status") %>
                    </span>
                </div>
                <p class="doc-meta">
                    <i class="bi bi-clock"></i><%# UploadedText(Eval("UploadedAt")) %>
                </p>
                <p class="doc-summary"><%# SummaryPreview(Eval("Summary") as string) %></p>
            </div>

            <div class="doc-actions">
                <asp:LinkButton runat="server" CssClass="btn-icon btn-icon-view" ToolTip="View summary"
                    CommandName="ViewSummary" CommandArgument='<%# Eval("Id") %>'
                    Enabled='<%# HasSummary(Eval("Summary") as string) %>'><i class="bi bi-eye"></i></asp:LinkButton>
                <asp:LinkButton runat="server" CssClass="btn-icon btn-icon-delete" ToolTip="Delete"
                    CommandName="Delete" CommandArgument='<%# Eval("Id") %>'><i class="bi bi-trash3"></i></asp:LinkButton>
            </div>
        </article>
    </ItemTemplate>
</asp:Repeater>
