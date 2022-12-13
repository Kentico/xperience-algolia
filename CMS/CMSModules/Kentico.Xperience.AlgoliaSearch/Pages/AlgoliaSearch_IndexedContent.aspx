<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="AlgoliaSearch_IndexedContent.aspx.cs" Inherits="Kentico.Xperience.Algolia.Pages.AlgoliaSearch_IndexedContent"
    Title="Algolia search - Indexed content" MasterPageFile="~/CMSMasterPages/UI/SimplePage.master" Theme="Default" EnableEventValidation="false" ValidateRequest="false" %>

<%@ Register src="~/CMSAdminControls/UI/UniGrid/UniGrid.ascx" tagname="UniGrid" tagprefix="cms" %>
<%@ Register Namespace="CMS.UIControls.UniGridConfig" TagPrefix="ug" Assembly="CMS.UIControls" %>

<asp:Content ID="cntBody" ContentPlaceHolderID="plcContent" runat="server">
    <div style="margin-bottom: 30px">
        <cms:LocalizedHeading runat="server" ResourceString="Indexed properties" Level="3" />
        <cms:UniGrid ID="ugProperties" runat="server">
            <GridColumns>
                <ug:Column Source="Name" Caption="Property" />
                <ug:Column Source="Searchable" Caption="Searchable" ExternalSourceName="#yesno" />
                <ug:Column Source="Retrievable" Caption="Retrievable" ExternalSourceName="#yesno" />
                <ug:Column Source="Facetable" Caption="Facetable" ExternalSourceName="#yesno" />
                <ug:Column Source="Source" Caption="Source" CssClass="filling-column" />
            </GridColumns>
        </cms:UniGrid>
    </div>
    <div>
        <cms:LocalizedHeading runat="server" ResourceString="Indexed paths" Level="3" />
        <cms:UniGrid ID="ugIncludedContent" runat="server">
            <GridColumns>
                <ug:Column Source="Path" Caption="Path" />
                <ug:Column Source="Cultures" Caption="Cultures" />
                <ug:Column Source="PageTypes" Caption="Page types" />
            </GridColumns>
        </cms:UniGrid>
    </div>
</asp:Content>
