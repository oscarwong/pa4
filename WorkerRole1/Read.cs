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
using System.Web;

namespace WorkerRole1
{
    public class Read : RoleEntryPoint
    {
        HashSet<string> visited = new HashSet<string>();

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("WorkerRole1 entry point called", "Information");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");
            CloudQueue unreadurls = queueClient.GetQueueReference("unvisitedurls");
            unreadurls.CreateIfNotExists();
            CloudQueue error = queueClient.GetQueueReference("errors");
            error.CreateIfNotExists();
            CloudQueue lastten = queueClient.GetQueueReference("lastten");
            lastten.CreateIfNotExists();
            List<string> disallow = checkrobot();

            string status = "false";
           

            while (true)
            {
                Thread.Sleep(500);
                CloudQueueMessage peekedMessage = queue.PeekMessage();

                if (peekedMessage != null)
                {
                    status = peekedMessage.AsString;
                }
                else
                {
                    status = "false";
                }

                if (status == "false")
                {
                    if (unreadurls != null)
                    {
                        unreadurls.Clear();
                        error.Clear();
                        lastten.Clear();
                        deleteTable();
                    }
                }
                else if (status == "run")
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
                        CloudQueueMessage retrievedmessage = unreadurls.GetMessage();
                        unreadurls.DeleteMessage(retrievedmessage);
                        continue;
                    }
                    visited.Add(url);
                    string[] data;
                    try
                    {
                        data = crawl(url, disallow);
                    }
                    catch (Exception e)
                    {
                        CloudQueueMessage errormessage = new CloudQueueMessage(url);
                        error.AddMessage(errormessage);
                        continue;
                    }
                    lastten.FetchAttributes();
                    int? cachedMessageCount = lastten.ApproximateMessageCount;
                    if (cachedMessageCount == 10)
                    {
                        CloudQueueMessage retrievedmessage = lastten.GetMessage();
                        lastten.DeleteMessage(retrievedmessage);
                    }
                    CloudQueueMessage lasttenmessage = new CloudQueueMessage(url);
                    lastten.AddMessage(lasttenmessage);
                    addToTable(data);
                    CloudQueueMessage deletemessage = unreadurls.GetMessage();
                    if (deletemessage != null)
                        unreadurls.DeleteMessage(deletemessage);
                    else
                        break;
                }
                else if (status == "start")
                {
                    //initialRobot();
                    CloudQueueMessage message = queue.GetMessage();
                    message.SetMessageContent("run");
                    queue.UpdateMessage(message, TimeSpan.FromSeconds(0.0), MessageUpdateFields.Content | MessageUpdateFields.Visibility);
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

        public void initialRobot()
        {
            string check = string.Format("http://www.cnn.com/robots.txt");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(check);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string line;

            List<string> disallow = checkrobot();
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Sitemap:"))
                    {
                        int index = line.IndexOf("http://");
                        crawlRobot(line.Substring(index), disallow);
                    }
                }
            }
        }

        public void crawlRobot(string url, List<string> disallow)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue unvisitedQueue = queueClient.GetQueueReference("unvisitedurls");
            unvisitedQueue.CreateIfNotExists();

            CloudQueue error = queueClient.GetQueueReference("errors");
            error.CreateIfNotExists();

            string line;


            string check = string.Format(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(check);
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.ServicePoint.ConnectionLimit = 24;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {

                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        if (line.Contains(".xml") && line.Contains("http://"))
                        {
                            int index = line.IndexOf("http://");
                            string capture = line.Substring(index);
                            int endIndex = capture.IndexOf("</loc>");
                            crawlRobot(line.Substring(index, endIndex), disallow);
                        }
                        else if (line.Contains(".html") && line.Contains(".cnn.") && line.Contains("http://"))
                        {
                            Boolean test = true;
                            int index = line.IndexOf("http://");
                            string capture = line.Substring(index);
                            int endIndex = capture.IndexOf("</loc>");
                            string urlCapture = line.Substring(index, endIndex);
                            foreach (string compare in disallow)
                            {
                                if (urlCapture.StartsWith(compare))
                                {
                                    test = false;
                                    continue;
                                }
                            }
                            if (test)
                            {
                                CloudQueueMessage message = new CloudQueueMessage(urlCapture);
                                unvisitedQueue.AddMessage(message);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        CloudQueueMessage errormessage = new CloudQueueMessage(url);
                        error.AddMessage(errormessage);
                        continue;
                    }
                }
            }
        }

        public void deleteTable()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");

            TableQuery<UrlTable> query = new TableQuery<UrlTable>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "CNN"));

            foreach (UrlTable entity in table.ExecuteQuery(query))
            {
                if (entity != null)
                {
                    TableOperation deleteOperation = TableOperation.Delete(entity);

                    table.Execute(deleteOperation);
                }
                else
                {
                    break;
                }
            }
        }

        public void addToTable(string[] data)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();

            string[] split = data[1].Split(new Char[] { ' ', ',', ':', ';', '-', '\'' });
            foreach (string s in split)
            {
                if (s.Trim() != "")
                {
                    UrlTable entry = new UrlTable(s.ToLower(), HttpUtility.UrlEncode(data[0]));
                    entry.Title = data[1];
                    if (data[2] != null)
                        entry.Date = data[2];
                    else
                        entry.Date = "N/A";
                    TableOperation insertOperation = TableOperation.InsertOrReplace(entry);
                    try {
                        table.Execute(insertOperation);
                    } catch (Exception e) 
                    {}
                }
            }
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
                        Boolean hasDate = (line.Contains("itemprop=\"datePublished\"") && line.Contains("content=\""));

                        if (hasTitle && title)
                        {
                            string pageTitle = line.Substring(line.IndexOf("<title>"));
                            pageTitle = pageTitle.Substring(7);
                            int end = pageTitle.IndexOf("- CNN");
                            if (end > 0)
                                siteData[1] = (pageTitle.Substring(0, end));
                            else
                                siteData[1] = ((pageTitle.Substring(0, pageTitle.Length - 8)));
                            title = false;
                        }

                        if (hasDate && publish)
                        {
                            string date = (line.Substring("<meta content=\"".Length));
                            date = date.Substring(0, 10);
                            siteData[2] = date;
                            publish = false;
                        }

                        if (index > 0 && !line.Contains(".xml"))
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
                            {
                                CloudQueueMessage message = new CloudQueueMessage("http://www.cnn.com" + link);
                                queue.AddMessage(message);
                            }
                            else if (link.Contains(".cnn."))
                            {
                                CloudQueueMessage message = new CloudQueueMessage(link);
                                queue.AddMessage(message);
                            }      
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
                        disallow.Add("http://money.cnn.com" + line.Substring(index));
                    }
                }
            }
            return disallow;
        }
    }

}
