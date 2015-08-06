
<%@ Page Title="Orleans in Azure" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Async="true" Inherits="WebRole1._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>CG Azure Starting Point</h1>
        <p class="lead">Trying to run the Silos</p>
        <p>
            <asp:TextBox ID="TextBox1" runat="server" Height="341px" OnTextChanged="TextBox1_TextChanged" Width="670px"></asp:TextBox>
        </p>
        <p>
            <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Start!" />
        </p>
    </div>

</asp:Content>
