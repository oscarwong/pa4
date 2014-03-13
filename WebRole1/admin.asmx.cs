﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using WorkerRole1;

namespace WebRole1
{
    /// <summary>
    /// Summary description for admin
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    [ScriptService]
    public class admin : System.Web.Services.WebService
    {
        private static Dictionary<string, List<string>> cache = new Dictionary<string, List<string>>();

        [WebMethod]
        public void StartCrawling() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");
            queue.CreateIfNotExists();

            CloudQueue error = queueClient.GetQueueReference("errors");
            error.CreateIfNotExists();

            CloudQueue lastten = queueClient.GetQueueReference("lastten");
            lastten.CreateIfNotExists();

            CloudQueueMessage message = new CloudQueueMessage("start");
            queue.AddMessage(message);

            CloudQueue unvisitedQueue = queueClient.GetQueueReference("unvisitedurls");
            unvisitedQueue.CreateIfNotExists();
        }

        [WebMethod]
        public void ClearIndex()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");
            queue.CreateIfNotExists();

            CloudQueue unvisitedQueue = queueClient.GetQueueReference("unvisitedurls");
            unvisitedQueue.CreateIfNotExists();

            CloudQueue error = queueClient.GetQueueReference("errors");
            error.CreateIfNotExists();

            CloudQueue lastten = queueClient.GetQueueReference("lastten");
            lastten.CreateIfNotExists();

            unvisitedQueue.Clear();
            error.Clear();
            lastten.Clear();

            CloudQueueMessage message = queue.GetMessage();
            message.SetMessageContent("false");
            queue.UpdateMessage(message, TimeSpan.FromSeconds(0.0), MessageUpdateFields.Content | MessageUpdateFields.Visibility);

            queue.DeleteMessage(message);
        }

        [WebMethod]
        public int? getQueueLength()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("unvisitedurls");

            queue.FetchAttributes();

            return queue.ApproximateMessageCount;
        }

        [WebMethod]
        public string getInfo(string url)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");

            TableOperation retrieveOperation = TableOperation.Retrieve<WorkerRole1.UrlTable>("CNN", HttpUtility.UrlEncode(url));

            TableResult retrievedResult = table.Execute(retrieveOperation);

            if (retrievedResult.Result != null)
                return ((WorkerRole1.UrlTable)retrievedResult.Result).Title + " - Date published: " + ((WorkerRole1.UrlTable)retrievedResult.Result).Date;
            else
                return "URL not found";
        }

        [WebMethod]
        public List<string> findKeyword(string keyword)
        {
            if (cache.ContainsKey(keyword))
                return cache[keyword];
            List<string> answer = new List<string>();
            keyword = keyword.ToLower();
            string[] split = keyword.Split(new Char[] { ' ' });
            foreach (string s in split)
                getWords(s, answer);

            if (answer.Count > 0)
            {
                cache.Add(keyword, answer);
                return answer;
            }
            else
            {
                answer.Add("Keyword not found");
                cache.Add(keyword, answer);
                return answer;
            }
        }

        private List<string> getWords(string keyword, List<string> answer)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");

            TableQuery<WorkerRole1.UrlTable> query = new TableQuery<UrlTable>().Where
                (TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, keyword));

            foreach (WorkerRole1.UrlTable entity in table.ExecuteQuery(query))
            {
                answer.Add(HttpUtility.UrlDecode(entity.RowKey));
            }
            return answer;
        }

        [WebMethod]
        public int getTableLength()
        {
            int length = 0;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");

            TableQuery<UrlTable> query = new TableQuery<UrlTable>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "CNN"));

            foreach (UrlTable entity in table.ExecuteQuery(query))
            {
                if (entity != null)
                {
                    length++;
                }
                else
                {
                    break;
                }
            }

            return length;
        }

        [WebMethod]
        public string getStatus()
        {
            string status;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");

            CloudQueueMessage peekedMessage = queue.PeekMessage();

            if (peekedMessage == null)
                status = "stop";
            else
                status = peekedMessage.AsString;
            if (status == "run")
                return "crawling the website.";
            else if (status == "start")
                return "initializing and crawling the sitemap. Please wait another 7-10 hours.";
            else
                return "stopped.";
        }

        [WebMethod]
        public int? getErrorSize()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("errors");

            queue.FetchAttributes();

            return queue.ApproximateMessageCount;
        }

        [WebMethod]
        public List<string> crawl(string url)
        {
            List<string> siteData = new List<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("unvisitedurls");
            siteData.Add(url);

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
                                siteData.Add(pageTitle.Substring(0, end));
                            else
                                siteData.Add((pageTitle.Substring(0, pageTitle.Length - 8)));
                            title = false;
                        }

                        if (hasDate && publish)
                        {
                            string date = (line.Substring("<meta content=\"".Length));
                            date = date.Substring(0, 10);
                            siteData.Add(date);
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
    }
}
