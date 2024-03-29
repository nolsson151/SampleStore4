﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SampleStore4.Models
{
    public class SampleEntity : TableEntity
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string Mp3Blob { get; set; }
        public string SampleMp3Blob { get; set; }
        public string SampleMp3URL { get; set; }
        public DateTime? SampleDate { get; set; }

        public SampleEntity(string partitionKey, string sampleID)
        {
            PartitionKey = partitionKey;
            RowKey = sampleID;
        }

        public SampleEntity() { }
    }
}