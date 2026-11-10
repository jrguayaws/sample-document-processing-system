<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DocumentProcessor.WebForms._Default" %>

<%@ Register Src="~/Controls/DocumentUploader.ascx" TagPrefix="dp" TagName="DocumentUploader" %>
<%@ Register Src="~/Controls/DocumentList.ascx" TagPrefix="dp" TagName="DocumentList" %>

<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Document Processor</asp:Content>

<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
    <div class="app-grid">
        <section>
            <%-- Outside the UpdatePanel on purpose: a file input cannot be posted through an
                 asynchronous postback, so uploading is a full round trip. --%>
            <dp:DocumentUploader ID="Uploader" runat="server" OnUploadRequested="Uploader_UploadRequested" />
        </section>

        <section>
            <asp:UpdatePanel ID="ListPanel" runat="server" UpdateMode="Conditional">
                <ContentTemplate>
                    <dp:DocumentList ID="Documents" runat="server"
                        OnRefreshRequested="Documents_RefreshRequested"
                        OnViewSummaryRequested="Documents_ViewSummaryRequested"
                        OnDeleteRequested="Documents_DeleteRequested" />

                    <asp:Panel ID="SummaryModal" runat="server" Visible="false" TabIndex="-1"
                        CssClass="modal fade show d-block modal-backdrop-tint">
                        <div class="modal-dialog">
                            <div class="modal-content">
                                <div class="modal-header">
                                    <h6 class="modal-title"><asp:Literal ID="SummaryModalTitle" runat="server" /></h6>
                                    <asp:LinkButton ID="SummaryModalDismiss" runat="server" CssClass="btn-close"
                                        OnClick="CloseSummaryModal_Click" />
                                </div>
                                <div class="modal-body">
                                    <p><asp:Literal ID="SummaryModalBody" runat="server" /></p>
                                </div>
                                <div class="modal-footer">
                                    <asp:LinkButton ID="SummaryModalClose" runat="server" CssClass="btn btn-sm btn-secondary"
                                        OnClick="CloseSummaryModal_Click" Text="Close" />
                                </div>
                            </div>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="DeleteModal" runat="server" Visible="false" TabIndex="-1"
                        CssClass="modal fade show d-block modal-backdrop-tint">
                        <div class="modal-dialog modal-sm">
                            <div class="modal-content">
                                <div class="modal-header">
                                    <h6 class="modal-title">Delete document?</h6>
                                    <asp:LinkButton ID="DeleteModalDismiss" runat="server" CssClass="btn-close"
                                        OnClick="CancelDelete_Click" />
                                </div>
                                <div class="modal-body">
                                    <p class="mb-0"><strong><asp:Literal ID="DeleteModalFileName" runat="server" /></strong></p>
                                </div>
                                <div class="modal-footer">
                                    <asp:LinkButton ID="CancelDeleteButton" runat="server" CssClass="btn btn-sm btn-secondary"
                                        OnClick="CancelDelete_Click" Text="Cancel" />
                                    <asp:LinkButton ID="ConfirmDeleteButton" runat="server" CssClass="btn btn-sm btn-danger"
                                        OnClick="ConfirmDelete_Click" Text="Delete" />
                                </div>
                            </div>
                        </div>
                    </asp:Panel>
                </ContentTemplate>
            </asp:UpdatePanel>

            <asp:UpdateProgress ID="ListProgress" runat="server" AssociatedUpdatePanelID="ListPanel" DisplayAfter="300">
                <ProgressTemplate>
                    <div class="update-progress"><span class="spinner-border"></span></div>
                </ProgressTemplate>
            </asp:UpdateProgress>
        </section>
    </div>
</asp:Content>
