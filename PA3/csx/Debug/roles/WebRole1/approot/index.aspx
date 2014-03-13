<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="index.aspx.cs" Inherits="WebRole1.WebForm1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Oscar Bot</title>
    <script type="text/javascript">
        function startBot()
        {
            WebRole1.admin.StartCrawling();
        }

        function stopBot() {
            WebRole1.admin.StartCrawling();
        }

        function getInfo() {
            var echoElem = document.getElementById("UserURL");
            WebRole1.admin.getInfo(echoElem.value, URLSucceed);
        }

        function URLSucceed(result)
        {
            var URLResult = document.getElementById("URLResults");
            URLResult.innerHTML = result;
        }

        function getstatus() {
            var status = WebRole1.admin.getStatus(statusSucceed);
        }

        function statusSucceed(result) {
            var statusChange = document.getElementById("status");
            statusChange.innerHTML = result;
        }

        function refreshResults() {
            var crawled = WebRole1.admin.getTableLength(crawlsuccess);
            var queueSize = WebRole1.admin.getQueueLength(queuesuccess);
            var errors = WebRole1.admin.getErrorSize(errorsuccess);
        }

        function crawlsuccess(crawlresults) {
            var crawlNum = document.getElementById("crawled");
            crawlNum.innerHTML = crawlresults;
        }

        function queuesuccess(queueresults) {
            var queueNum = document.getElementById("queuesize");
            queueNum.innerHTML = queueresults;
        }

        function errorsuccess(errorresults) {
            var errorNum = document.getElementById("errors");
            errorNum.innerHTML = errorresults;
        }
    </script>
</head>
<body>
    <form id="Form1" runat="server">
        <asp:ScriptManager runat="server" ID="scriptManager">
            <Services>
                <asp:ServiceReference path="admin.asmx" />
            </Services>
        </asp:ScriptManager>
        <div>
            <input id="Start Bot" type="button" value="Start Bot" onclick="startBot()"/>
            <input id="Stop Bot" type="button" onclick="stopBot()" value="Stop Bot" />
        </div>

        <div>
            <input type="button" id="getStatus" value="Update the status of the bot." onclick="getstatus()" />
            <p>The bot is currently: <span id="status"></span></p>
        </div>

        <div>
            <input type="button" value="Refresh results" id="refresh" onclick="refreshResults()"/>
        </div>
    </form>

    <div>
        <p>Number of URLs crawled: <span id="crawled"></span></p>
        <p>Size of Queue: <span id="queuesize"></span></p>
        <p>Number of errors: <span id="errors"></span></p>
    </div>
</body>
</html>
