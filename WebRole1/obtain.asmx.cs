﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;

namespace WebRole1
{
    /// <summary>
    /// Summary description for obtain
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    [ScriptService]
    public class obtain : System.Web.Services.WebService
    {

        public static Trie trie = new Trie();
        private PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");

        [WebMethod]
        public void GetStorage()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference("pa2");

            CloudBlockBlob blob2 = container.GetBlockBlobReference("newtitles.txt");

            string line = null;
            using (StreamReader sr = new StreamReader(blob2.OpenRead()))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    var count = this.theMemCounter.NextValue();
                    if (count < 900f)
                        break;
                    trie.insertWord(line);
                }
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> Read(string _userinput)
        {
            var json = new WebClient().DownloadString("http://ec2-54-186-72-122.us-west-2.compute.amazonaws.com/player.php?name=" + _userinput);
            _userinput = _userinput.ToLower();
            List<string> answer = new List<string>();
            try
            {
                answer = trie.searchPrefix(_userinput);
            }
            catch (Exception e)
            {
            }
            return answer;
        }
    }
}
