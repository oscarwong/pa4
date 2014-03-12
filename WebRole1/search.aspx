<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="search.aspx.cs" Inherits="WebRole1.search" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="//netdna.bootstrapcdn.com/bootstrap/3.1.0/css/bootstrap.min.css">
    <meta charset="utf-8">
    <title>Oscar's search interface</title>
    <script type="text/javascript>
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
                },
                error: function (xhr, status, error) {
                    var err = eval("(" + xhr.responseText + ")");
                    alert(err.Message);
                }
            });
        };

         function callback(data) {
            alert("callback");
            $('#result1').html(JSON.stringify(data));
        };

        function getjson() {
            var userinput = $("#input").val();
            alert("basketball players");
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

        function autocomplete(data) {
            var availableTags = data.d;
            $("#tags").autocomplete({
                source: availableTags
            });
        };
    </script>
    <script src="http://code.jquery.com/ui/1.9.2/jquery-ui.js"></script>
</head>
<body>
    <div class="row">
        <div class="col-lg-6 col-lg-offset-3">
            <div class="input-group">
                <input type="text" class="form-control" name="input" id="input" value="" onkeyup="testJson()" />
                <span class="input-group-btn">
                    <button class="btn btn-default" type="button" onclick="testJson();getjson();">Go!</button>
                </span>
            </div>
        </div>
    </div>

    <div class="row">
        <div class="col-md-2 col-md-offset-5 container">
            <div id="result"></div>
        </div>
    </div>

    <div class="row">
        <div class="col-md-2 col-md-offset-5 container">
            <div id="result1"></div>
        </div>
    </div>

    <div class="ui-widget">
        <label for="tags"></label>
        <input id="tags">
    </div>

    <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.10.2/jquery.min.js"></script>
    <script src="//netdna.bootstrapcdn.com/bootstrap/3.1.0/js/bootstrap.min.js"></script>
</body>
</html>
