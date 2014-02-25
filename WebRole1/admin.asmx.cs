﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
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
    public class admin : System.Web.Services.WebService
    {

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

            CloudQueueMessage message = new CloudQueueMessage("run");
            queue.AddMessage(message);

            CloudQueue unvisitedQueue = queueClient.GetQueueReference("unvisitedurls");
            unvisitedQueue.CreateIfNotExists();

            initialRobot();
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
        public int getTableLength()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");

            table.
        }
    }
}
