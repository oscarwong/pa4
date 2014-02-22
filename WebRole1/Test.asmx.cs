using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for Test
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class Test : System.Web.Services.WebService
    {

        [WebMethod]

        public string[] GetUrls()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("unvisitedurls");
            queue.CreateIfNotExists();

            List<string> disallow = checkrobot();
            HashSet<string> visited = new HashSet<string>();

            string url = string.Format("http://www.cnn.com");

            if (disallow.Contains(url))
            {
                return null;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                List<string> synonyms = new List<string>();
                string line;

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {

                    while ((line = reader.ReadLine()) != null)
                    {
                        var index = line.IndexOf("href=\"");
                            
                        if (index > 0)
                        {
                            string temp;
                            string link = "";
                            index = index + "href=\"".Length;
                            temp = line.Substring(index);
                            var endIndex = temp.IndexOf("\"");
                            if (endIndex > 0) {
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
                return visited.ToArray<string>();
            }
            else
            {
                return null;
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
                    if (line.StartsWith("Disallow:")) {
                        int index = line.IndexOf("/");
                        disallow.Add("http://www.cnn.com" + line.Substring(index));
                    }
                }
            }

            return disallow;
        }

    }
}
