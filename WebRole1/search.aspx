﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="search.aspx.cs" Inherits="WebRole1.search" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="//netdna.bootstrapcdn.com/bootstrap/3.1.0/css/bootstrap.min.css">
    <meta charset="utf-8">
    <title>Oscar's search interface</title>
    <link rel="stylesheet" href="//code.jquery.com/ui/1.10.4/themes/smoothness/jquery-ui.css">
    <script src="//code.jquery.com/jquery-1.9.1.js"></script>
    <script src="//code.jquery.com/ui/1.10.4/jquery-ui.js"></script>
    <script type="text/javascript">
        function testJson() {
            var userinput = $("#input").val();
            $.ajax({
                type: "POST",
                url: "obtain.asmx/Read",
                data: '{_userinput:"' + userinput + '"}',
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (msg) {
                    $("#result").html(msg.d.toString());
                }
            });
        };

        function pageLoad() {
            WebRole1.obtain.GetStorage();
        }

        $(document).ready(function () {
            $("#input").autocomplete({
                source: function (request, response) {
                    $.ajax({
                        type: "POST",
                        url: "obtain.asmx/Read",
                        data: '{_userinput:"' + $("#input").val() + '"}',
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function (data) {
                            try {
                                var datafromServer = data.d;
                            } catch (err) { alert(err); }
                            response(datafromServer)
                        }
                    });
                }
            });
        });

        function callback(data) {
            $('#result1').html(JSON.stringify(data));
        };

        function getjson() {
            var userinput = $("#input").val();
            $.ajax({
                type: "POST",
                url: "http://ec2-54-186-72-122.us-west-2.compute.amazonaws.com/player.php",
                data: { name: userinput },
                contentType: "application/json; charset=utf-8",
                dataType: "jsonp",
                success: callback,
                error: function (xhr, status, error) {
                    var err = eval("(" + xhr.responseText + ")");
                    alert(err.Message);
                }
            });
        };

        function getInfo()
        {
            $(".container").empty();
            var keyword = $("#input").val();
            WebRole1.admin.findKeyword(keyword, infoSuccess);
        }

        function infoSuccess(result)
        {
            $.each(result, function (index, value) {
                if (result[index] = "Keyword not found") {
                    $('.container').append("<div>Result not found</div>");
                } else {
                    $('.container').append("<div><a href=\"" + result[index] + "\">" + result[index] + "</a></div>");
                }
            });
        }
    </script>
</head>
<body>
    <form action="form1" runat="server">
        <asp:ScriptManager runat="server" ID="scriptManager">
            <Services>
                <asp:ServiceReference path="obtain.asmx" />
                <asp:ServiceReference Path="admin.asmx" />
            </Services>
        </asp:ScriptManager>
        <div class="row">
            <div class="col-lg-6 col-lg-offset-3">
                <div class="input-group">
                    <input type="text" class="form-control" name="input" id="input" placeholder="Search here..."/>
                    <span class="input-group-btn">
                        <button class="btn btn-default" type="button" onclick="getInfo();getjson()">Go!</button>
                    </span>
                </div>
            </div>
        </div>
    </form>

    <div class="row">
        <div class="col-md-offset-2 col-md-5 container">
            <div id="result"></div>
        </div>
    </div>

    <div class="row">
        <div class="col-md-offset-5 col-md-5 container1">
            <div id="result1"></div>
        </div>
    </div>

    <script type='text/javascript' src='http://ads1.qadabra.com/t?id=579ac763-00a2-4fd1-b55a-e9557bd09af3&size=120x600'></script>
    <script src="//netdna.bootstrapcdn.com/bootstrap/3.1.0/js/bootstrap.min.js"></script>
</body>
</html>
