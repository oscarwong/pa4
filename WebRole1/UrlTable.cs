using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkerRole1
{
    public class UrlTable : TableEntity
    {
        public UrlTable(string root, string url)
        {
            this.PartitionKey = root;
            this.RowKey = url;
        }

        public UrlTable() { }

        public string Title { get; set; }
        public string Date { get; set; }
    }
}
