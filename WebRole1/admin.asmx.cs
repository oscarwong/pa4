using Microsoft.WindowsAzure.Storage;
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

        [WebMethod]
        public void StartCrawling() {
            initialRobot();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");
            queue.CreateIfNotExists();

            CloudQueue unvisitedQueue = queueClient.GetQueueReference("unvisitedurls");
            unvisitedQueue.CreateIfNotExists();

            CloudQueueMessage message = new CloudQueueMessage("true");
            queue.AddMessage(message);
            
        }

        [WebMethod]
        public void ClearIndex()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("commands");
            CloudQueue unvisitedQueue = queueClient.GetQueueReference("unvisitedurls");
            unvisitedQueue.CreateIfNotExists();
            queue.CreateIfNotExists();

            CloudQueueMessage message = queue.GetMessage();
            message.SetMessageContent("false");
            queue.UpdateMessage(message, TimeSpan.FromSeconds(0.0), MessageUpdateFields.Content | MessageUpdateFields.Visibility);

            unvisitedQueue.Delete();

            CloudQueue newqueue = queueClient.GetQueueReference("unvisitedurls");
            newqueue.CreateIfNotExists();
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

        public int? getQueueLengeth()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("unvisitedurls");

            queue.FetchAttributes();

            return queue.ApproximateMessageCount;
        }
    }
}
