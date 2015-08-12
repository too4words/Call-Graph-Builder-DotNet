
<%@ Page Title="Orleans in Azure" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Async="true" Inherits="WebRole1._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>
			CG Azure Starting Point</h1>
        <p class="lead">Trying to run the Silos</p>
        <p>
            <asp:TextBox ID="TextBox1" runat="server" Height="341px" Width="670px" TextMode="MultiLine" OnTextChanged="TextBox1_TextChanged"></asp:TextBox>
        </p>
        <p>
            <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Test Solution" />
        	<asp:Button ID="Button3" runat="server" OnClick="Button3_Click" Text="TestSource" />
            <asp:Button ID="Button4" runat="server" OnClick="Button4_Click" Text="Test Test" />
        	<asp:Button ID="Button5" runat="server" OnClick="Button5_Click" Text="Button" />
        </p>
		<p>
            <asp:TextBox ID="TextBoxPathPrefix" runat="server" Width="675px">c:\Temp\solutions</asp:TextBox>
        </p>
		<p>
            <asp:TextBox ID="TextBoxPath" runat="server" Width="680px">ConsoleApplication1\ConsoleApplication1.sln</asp:TextBox>
        </p>
    </div>

</asp:Content>
