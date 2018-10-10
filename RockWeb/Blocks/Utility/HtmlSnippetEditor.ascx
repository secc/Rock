<%@ Control Language="C#" AutoEventWireup="true" CodeFile="HtmlSnippetEditor.ascx.cs" Inherits="RockWeb.Blocks.Utility.HtmlSnippetEditor" %>
<asp:UpdatePanel runat="server" ID="upSnippets">
    <ContentTemplate>
        <Rock:RockTextBox runat="server" ID="tbName" Label="Name" Required="true" />
        <Rock:HtmlEditor runat="server" ID="heSnippet" Rows="100" Height="400"/>
        <Rock:BootstrapButton runat="server" ID="btnSave" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
        <Rock:BootstrapButton runat="server" ID="btnCancel" Text="Cancel" OnClick="btnCancel_Click" />
    </ContentTemplate>
</asp:UpdatePanel>

