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

            if (peekedMessage != null)
            {
                status = Convert.ToBoolean(peekedMessage);
            }
            else
            {
                status = false;
            }

            while (status)
            {
                Thread.Sleep(10000);
                CloudQueueMessage newMessage = queue.PeekMessage();

                if (newMessage != null)
                {
                    status = Convert.ToBoolean(newMessage);
                    if (!status)
                    {
                        break;
                    }
                    else
                    {
                        crawl("http://www.cnn.com/robots.txt");
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

        public string[] crawl(string url)
        {
            List<string> disallow = checkrobot();
            string[] siteData = new string[3];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("unvisitedurls");

            string visit = string.Format(url);

            if (disallow.Contains(visit))
                return null;

        }

        public List<string> checkrobot()
        {
            string check = string.Format("http://www.cnn.com/robots.txt");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(check)
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
