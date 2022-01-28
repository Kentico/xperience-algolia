<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="AlgoliaSearch_Dashboard.aspx.cs" Inherits="Kentico.Xperience.AlgoliaSearch.Pages.AlgoliaSearch_Dashboard"
    Title="Algolia Index - Dashboard" MasterPageFile="~/CMSMasterPages/UI/SimplePage.master" Theme="Default" EnableEventValidation="false" ValidateRequest="false" %>

<%@ Register src="~/CMSAdminControls/UI/UniGrid/UniGrid.ascx" tagname="UniGrid" tagprefix="cms" %>
<%@ Register Namespace="CMS.UIControls.UniGridConfig" TagPrefix="ug" Assembly="CMS.UIControls" %>

<asp:Content ID="cntBody" ContentPlaceHolderID="plcContent" runat="server">
    <cms:LocalizedHeading runat="server" ID="headElem" Text="Registered indexes" Level="3" />
    <cms:UniGrid ID="ugIndexes" runat="server">
        <GridActions>
            <ug:Action Name="view" ExternalSourceName="view" CommandArgument="Name" Caption="View configuration" FontIconClass="icon-eye" FontIconStyle="Allow" />
            <ug:Action Name="rebuild" ExternalSourceName="rebuild" CommandArgument="Name" Caption="Rebuild" FontIconClass="icon-rotate-right" />
        </GridActions>
        <GridColumns>
            <ug:Column Source="Name" Caption="Name" />
            <ug:Column Source="Entries" Caption="Entries" />
            <ug:Column Source="LastBuildTimes" Caption="Build time (seconds)" />
            <ug:Column Source="DataSize" ExternalSourceName="size" Caption="Index size" />
            <ug:Column Source="UpdatedAt" Caption="Last update" />
            <ug:Column Source="CreatedAt" Caption="Date created" />
        </GridColumns>
    </cms:UniGrid>
</asp:Content>
