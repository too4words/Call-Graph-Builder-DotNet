
<%@ Page Title="Orleans in Azure" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Async="true" Inherits="WebRole1._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>
			CG Azure Starting Point</h1>
        <p class="lead">Trying to run the Silos</p>
        <p>
            <asp:TextBox ID="TextBox1" runat="server" Height="250px" Width="670px" TextMode="MultiLine" OnTextChanged="TextBox1_TextChanged"></asp:TextBox>
        </p>
        <p>
            <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Test Solution" />
        	<asp:Button ID="Button3" runat="server" OnClick="Button3_Click" Text="TestSource" />
            <asp:Button ID="Button4" runat="server" OnClick="Button4_Click" Text="Test Test" />
        	<asp:Button ID="Button5" runat="server" OnClick="Button5_Click" Text="QueryCallees" />
        </p>
		<p>
            <asp:Button ID="Button6" runat="server" OnClick="Button6_Click" Text="RandomQueries" />
            <asp:Button ID="Button7" runat="server" OnClick="Button7_Click" Text="RemoveGrains" />
        	<asp:Button ID="Button8" runat="server" OnClick="Button8_Click" Text="Stats" />
        </p>
		<p>
            <asp:TextBox ID="TextBoxPathPrefix" runat="server" Width="675px">C;N2;0</asp:TextBox>
        </p>
		<p>
            <asp:TextBox ID="TextBoxPath" runat="server" Width="680px" OnTextChanged="TextBoxPath_TextChanged">LongGeneratedTest2</asp:TextBox>
        </p>
		<p>
            <asp:TextBox ID="TextRandomQueryInput" runat="server">C;N;100;50;1</asp:TextBox>
        </p>
    </div>

</asp:Content>
