<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="search.aspx.cs" Inherits="WebRole1.search" %>

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
            var name = JSON.stringify(data[1]);
            name = name.replace(/\"/g, "");
            name = name.replace(/\//g, '');

            var gp = JSON.stringify(data[2]);
            gp = gp.replace(/\"/g, "");
            gp = gp.replace(/\//g, '');

            var fgp = JSON.stringify(data[3]);
            fgp = fgp.replace(/\"/g, "");
            fgp = fgp.replace(/\//g, '');

            var tpp = JSON.stringify(data[4]);
            tpp = tpp.replace(/\"/g, "");
            tpp = tpp.replace(/\//g, '');

            var ftp = JSON.stringify(data[5]);
            ftp = ftp.replace(/\"/g, "");
            ftp = ftp.replace(/\//g, '');

            var ppg = JSON.stringify(data[6]);
            ppg = ppg.replace(/\"/g, "");
            ppg = ppg.replace(/\//g, '');
            $('.container1').append("<table class=\"table table-bordered\"><tr><th>Name</th><th>GP</th><th>FGP</th><th>TPP</th><th>FTP</th><th>PPG</th></tr><tr><td>" + name + "</td><td>" + gp + "</td><td>" + fgp + "</td><td>" + tpp + "</td><td>" + ftp + "</td><td>" + ppg + "</td></tr></table>");
        };

        function getjson() {
            var userinput = $("#input").val();
            $(".container").empty();
            $(".container1").empty();
            $.ajax({
                type: "POST",
                url: "http://ec2-54-186-72-122.us-west-2.compute.amazonaws.com/player.php",
                data: { name: userinput },
                contentType: "application/json; charset=utf-8",
                dataType: "jsonp",
                success: callback,
                error: function () {
                    $('.container1').append("<div>No basketball player(s) found with search query</div>");
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
                if (result[index] === "Keyword not found") {
                    $('.container').append("<div>No URLs found with keyword</div>");
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
        </div>
    </div>

    <div class="row">
        <div class="col-md-offset-5 col-md-5 container1">
        </div>
    </div>

    <script type='text/javascript' src='http://ads1.qadabra.com/t?id=579ac763-00a2-4fd1-b55a-e9557bd09af3&size=120x600'></script>
    <script src="//netdna.bootstrapcdn.com/bootstrap/3.1.0/js/bootstrap.min.js"></script>
</body>
</html>
