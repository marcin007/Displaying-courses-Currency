<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebApplication1._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="row">
        <div class="col-md-8" id="tables" runat="server">
            Filtrowanie zakresu dat:<br />
            Od <asp:TextBox type="text" id="filter_from" runat="server" />
            do <asp:TextBox type="text" id="filter_to" runat="server" />
            <asp:Button text="Filtruj" onclick="applyDateFilter" runat="server" />
        </div>
        <div class="col-md-4" id="comments" runat="server">
            <div id="plot_area" runat="server"></div>
            <div id="comments_loaded" runat="server">

            </div>
            <div id="comments_post" runat="server">
                Treść komentarza: <asp:TextBox type="text" id="comment_post_text" runat="server"/><br />
                <input type="submit" value="Dodaj komentarz" id="comment_post_button" runat="server" />
                <asp:HiddenField ID="comment_post_id" runat="server" />
            </div>
        </div>
    </div>

</asp:Content>
