
<%@ Page Title="Orleans in Azure" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Async="true" Inherits="WebRole1._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>
			CG Azure Starting Point</h1>
        <p class="lead">Trying to run the Silos</p>
        <p>
            <asp:TextBox ID="TextBox1" runat="server" Height="250px" Width="670px" TextMode="MultiLine"></asp:TextBox>
        </p>
        <p>
            <asp:Button ID="ButtonTestSolution" runat="server" OnClick="ButtonTestSolution_Click" Text="Test Solution" />
        	<asp:Button ID="ButtonTestSource" runat="server" OnClick="ButtonTestSource_Click" Text="Test Source" />
            <asp:Button ID="ButtonTestTest" runat="server" OnClick="ButtonTestTest_Click" Text="Test Test" />
			<asp:Button ID="ButtonQueryStatus" runat="server" OnClick="ButtonQueryStatus_Click" Text="Query Status" />
        	<asp:Button ID="ButtonQueryCallees" runat="server" OnClick="ButtonQueryCallees_Click" Text="Query Callees" />
        </p>
		<p>
            <asp:Button ID="ButtonRandomQueries" runat="server" OnClick="ButtonRandomQueries_Click" Text="Random Queries" />
            <asp:Button ID="ButtonRemoveGrains" runat="server" OnClick="ButtonRemoveGrains_Click" Text="Remove Grains" />
        	<asp:Button ID="ButtonStats" runat="server" OnClick="ButtonStats_Click" Text="Stats" />
        </p>
		<p>
            <asp:TextBox ID="TextBoxPathPrefix" runat="server" Width="675px">C;N2;0</asp:TextBox>
        </p>
		<p>
            <asp:TextBox ID="TextBoxPath" runat="server" Width="680px">LongGeneratedTest1</asp:TextBox>
        </p>
		<p>
            <asp:TextBox ID="TextRandomQueryInput" runat="server">C;N;10;50;1;Main</asp:TextBox>
        </p>
    </div>

</asp:Content>
