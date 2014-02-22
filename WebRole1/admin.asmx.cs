﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services;

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
        HashSet<string> visited = new HashSet<string>();

        [WebMethod]
        public void StartCrawling() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");
            queue.CreateIfNotExists();

            CloudQueue unvisitedQueue = queueClient.GetQueueReference("unvisitedurls");
            unvisitedQueue.CreateIfNotExists();

            CloudQueueMessage message = new CloudQueueMessage("true");
            queue.AddMessage(message);

            
        }

        public void ClearIndex()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");
            queue.CreateIfNotExists();

            CloudQueueMessage message = queue.GetMessage();
            message.SetMessageContent("false");
            queue.UpdateMessage(message, TimeSpan.FromSeconds(0.0), MessageUpdateFields.Content | MessageUpdateFields.Visibility);
        }

        [WebMethod]
        public void initialRobot()
        {
            string check = string.Format("http://www.cnn.com/robots.txt");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(check);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string line;

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Sitemap:"))
                    {
                        int index = line.IndexOf("http://");
                        crawlRobot(line.Substring(index));
                    }
                }
            }
        }

        public void crawlRobot(string url)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue unvisitedQueue = queueClient.GetQueueReference("unvisitedurls");
            unvisitedQueue.CreateIfNotExists();

            List<string> disallow = checkrobot();
            string line; 

            string check = string.Format(url);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(check);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (StreamReader reader = new StreamReader(response.GetResponseStream())) 
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(".xml"))
                    {
                        int index = line.IndexOf("http://");
                        string capture = line.Substring(index);
                        int endIndex = capture.IndexOf("</loc>");
                        crawlRobot(line.Substring(index, endIndex));
                    }
                    else if (line.Contains(".html"))
                    {
                        int index = line.IndexOf("http://");
                        if (line.Contains(".cnn."))
                        {
                            string capture = line.Substring(index);
                            int endIndex = capture.IndexOf("</loc>");
                            string urlCapture = line.Substring(index, endIndex);
                            foreach (string compare in disallow)
                            {
                                if (urlCapture.Contains(compare))
                                    continue;
                                else
                                {
                                    if (visited.Contains(urlCapture))
                                    {
                                        continue;
                                    }
                                    CloudQueueMessage message = new CloudQueueMessage(urlCapture);
                                    unvisitedQueue.AddMessage(message);
                                    visited.Add(urlCapture);
                                }
                            }                          
                        }
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
                        disallow.Add("http://www.money.cnn.com" + line.Substring(index));
                    }
                }
            }
            return disallow;
        }
    }
}
