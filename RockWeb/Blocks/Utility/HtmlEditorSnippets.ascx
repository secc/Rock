<%@ Control Language="C#" AutoEventWireup="true" CodeFile="HtmlEditorSnippets.ascx.cs" Inherits="RockWeb.Blocks.Utility.HtmlEditorSnippets" %>

<asp:Panel ID="pnlModalHeader" runat="server" Visible="false">
    <h3 class="modal-title">
        <asp:Literal ID="lTitle" runat="server"></asp:Literal>
        <span class="js-cancel-file-button cursor-pointer pull-right" style="opacity: .5">&times;</span>
    </h3>
    
</asp:Panel>

<div class="snippets-wrapper clearfix">
    <Rock:Grid ID="gSnippets" runat="server" AllowSorting="true" RowItemText="snippet" TooltipField="Description">
        <Columns>
            <Rock:ReorderField Visible="false" />
            <Rock:RockBoundField
                DataField="Id"
                HeaderText="Id"
                SortExpression="Id"
                ItemStyle-Wrap="false"
                ItemStyle-HorizontalAlign="Right"
                HeaderStyle-HorizontalAlign="Right" />
            <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
            <Rock:RockBoundField DataField="Content" HeaderText="Content" SortExpression="Content" />
            <Rock:SecurityField TitleField="Id" />
            <Rock:DeleteField  />
        </Columns>
    </Rock:Grid>
</div>

<asp:Panel ID="pnlModalFooterActions" CssClass="modal-footer" runat="server" Visible="false">
    <div class="row">
        <div class="actions">
            <a class="btn btn-primary js-ok-button">OK</a>
        </div>
    </div>
</asp:Panel>
