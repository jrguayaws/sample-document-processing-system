<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="ErrorPage.aspx.cs" Inherits="DocumentProcessor.WebForms.ErrorPage" %>

<asp:Content ID="Title" ContentPlaceHolderID="TitleContent" runat="server">Something went wrong</asp:Content>

<asp:Content ID="Body" ContentPlaceHolderID="MainContent" runat="server">
    <div class="empty-state">
        <h1><asp:Literal ID="HeadingText" runat="server" /></h1>
        <p><asp:Literal ID="MessageText" runat="server" /></p>
        <a href="<%= ResolveUrl("~/") %>" class="btn-brand">Back to documents</a>
    </div>
</asp:Content>
