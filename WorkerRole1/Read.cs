using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using System.IO;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Table;

namespace WorkerRole1
{
    public class Read : RoleEntryPoint
    {
        HashSet<string> visited = new HashSet<string>();

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("WorkerRole1 entry point called", "Information");

            Boolean status = false;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");
            CloudQueueMessage peekedMessage = queue.PeekMessage();

            CloudQueue unreadurls = queueClient.GetQueueReference("unvisitedurls");
            List<string> disallow = checkrobot();

            if (peekedMessage != null)
            {
                status = Convert.ToBoolean(peekedMessage.AsString);
            }
            else
            {
                status = false;
            }

            while (status)
            {
                Thread.Sleep(10000);
                CloudQueueMessage newmessage = queue.PeekMessage();

                if (newmessage != null)
                {
                    status = Convert.ToBoolean(newmessage.AsString);
                    if (!status)
                    {
                        break;
                    }
                    else
                    {
                        CloudQueueMessage unread = unreadurls.PeekMessage();
                        string url;
                        if (unread != null)
                        {
                            url = unread.AsString;
                        }
                        else
                        {
                            break;
                        }
                        Boolean test = false;
                        if (unread != null && !visited.Contains(url))
                        {
                            test = true;
                            foreach (string prohibited in disallow)
                            {
                                if (url.StartsWith(prohibited))
                                    test = false;
                            }
                        }
                        if (!test)
                        {
                            CloudQueueMessage retrievedmessage = queue.GetMessage();
                            unreadurls.DeleteMessage(retrievedmessage);
                            continue;
                        }
                        visited.Add(url);
                        string[] data = crawl(url, disallow);
                        addToTable(data);
                        CloudQueueMessage deletemessage = queue.GetMessage();
                        if (deletemessage != null)
                            unreadurls.DeleteMessage(deletemessage);
                        else
                            break;
                    }
                }

                Trace.TraceInformation("Working", "Information");
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();


        }

        public void addToTable(string[] data)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();
            UrlTable entry = new UrlTable("CNN", data[0]);
            entry.Title = data[1];
            if (data[2] != null)
                entry.Date = data[2];
            else
                entry.Date = "N/A";
        }

        public string[] crawl(string url, List<string> disallow)
        {
            string[] siteData = new string[3];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("unvisitedurls");
            siteData[0] = url;

            string visit = string.Format(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string line;
                Boolean title = true;
                Boolean publish = true;

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        var index = line.IndexOf("href=\"");
                        Boolean hasTitle = line.Contains("<title>");
                        Boolean hasDate = line.Contains("itemprop=\"datePublished\"");

                        if (hasTitle && title)
                        {
                            string pageTitle = line.Substring(7);
                            siteData[1] = (System.Web.HttpUtility.HtmlDecode(line.Substring(7, pageTitle.Length - 8)));
                            title = false;
                        }

                        if (hasDate && publish)
                        {
                            string date = (line.Substring("<meta content=\"".Length));
                            date = date.Substring(0, 10);
                            siteData[2] = date;
                            publish = false;
                        }

                        if (index > 0)
                        {
                            string temp;
                            string link = "";
                            index = index + "href=\"".Length;
                            temp = line.Substring(index);
                            var endIndex = temp.IndexOf("\"");
                            if (endIndex > 0)
                            {
                                link = line.Substring(index, endIndex);
                            }
                            if (link.StartsWith("/"))
                                visited.Add("http://www.cnn.com" + link);
                            else if (link.Contains(".cnn."))
                                visited.Add(link);
                        }

                        if (line.Contains("</html>"))
                        {
                            break;
                        }
                    }
                }
            }
            return siteData;
        }

        public List<string> checkrobot()
        {
            string check = string.Format("http://www.cnn.com/robots.txt");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(check);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            List<string> disallow = new List<string>();
            string line;

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Disallow:"))
                    {
                        int index = line.IndexOf("/");
                        disallow.Add("http://www.cnn.com" + line.Substring(index));
                    }
                }
            }

            check = string.Format("http://www.money.cnn.com/robots.txt");
            request = (HttpWebRequest)WebRequest.Create(check);
            response = (HttpWebResponse)request.GetResponse();

            line = "";

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Disallow:"))
                    {
                        int index = line.IndexOf("/");
                        disallow.Add("http://www.money.cnn.com" + line.Substring(index));
                    }
                }
            }
            return disallow;
        }
    }

}
