using System;
using System.Collections.Generic;
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
        public string[] GetSynonyms(string word)
        {
            string url = string.Format("http://www.cnn.com");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                List<string> synonyms = new List<string>();
                StringBuilder data = new StringBuilder();
                string line;

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {

                    //we know that the synonyms is in the upper-part of the html stream so we do not want to read the entire stream.
                    while ((line = reader.ReadLine()) != null)
                    {
                        var index = line.IndexOf("href=\"");

                        if (index > 0)
                        {
                            string link;
                            index = index + "\"".Length;
                            link = line.Substring(index).Replace("\"", "");
                            if (link.StartsWith("/"))
                                synonyms.Add("http://cnn.com" + link);
                            if (link.Contains(".cnn."))
                                synonyms.Add(link);
                        }

                        //break when we come to the Antonyms section of the page
                        if (line.Contains("</html>"))
                        {
                            break;
                        }
                    }
                }
                return synonyms.ToArray<string>();
            }
            else
            {
                return null;
            }
        }
    }
}
