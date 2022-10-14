<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="AlgoliaSearch_Preview.aspx.cs" Inherits="Kentico.Xperience.Algolia.Pages.AlgoliaSearch_Preview"
    Title="Algolia search - Search preview" MasterPageFile="~/CMSMasterPages/UI/SimplePage.master" Theme="Default" EnableEventValidation="false" ValidateRequest="false" %>

<%@ Register Src="~/CMSAdminControls/UI/Pager/UIPager.ascx" TagName="UIPager" TagPrefix="cms" %>

<asp:Content ID="cntBody" ContentPlaceHolderID="plcContent" runat="server">
    <asp:Panel ID="searchPnl" runat="server">
        <div class="form-horizontal form-filter">
            <div class="form-group">
                <div class="filter-form-label-cell">
                    <cms:LocalizedLabel CssClass="control-label" runat="server" ID="lblSearchFor" AssociatedControlID="txtSearchFor"
                        DisplayColon="true" Text="Search for" />
                </div>
                <div class="filter-form-value-cell">
                    <cms:CMSTextBox runat="server" ID="txtSearchFor" MaxLength="1000" />
                </div>
            </div>
            <div class="form-group form-group-buttons">
                <div class="filter-form-buttons-cell">
                    <cms:LocalizedButton runat="server" ID="btnSearch" ButtonStyle="Primary" OnClick="btnSearch_Click" Text="Search" />
                </div>
            </div>
        </div>
        <asp:Label CssClass="control-label" runat="server" ID="lblNoResults" Text="No results." EnableViewState="false" Visible="false" />
        <br />
        <cms:BasicRepeater runat="server" ID="repSearchResults">
            <ItemTemplate>
                <div style="padding:10px">
                    <%# GetResultString(Container.DataItem) %>
                </div>
            </ItemTemplate>
            <AlternatingItemTemplate>
                <div style="padding:10px;background-color:#eee">
                    <%# GetResultString(Container.DataItem) %>
                </div>
            </AlternatingItemTemplate>
        </cms:BasicRepeater>
    </asp:Panel>
</asp:Content>