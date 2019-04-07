using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SampleStore4.Models;
using System.Configuration;

namespace SampleStore4.Migrations
{       
    public static class InitialiseSamples
    {
        public static void go()
        {
            const String partitionName = "Samples_Partition_1";

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            CloudTable table = tableClient.GetTableReference("Samples");

            // If table doesn't already exist in storage then create and populate it with some initial values, otherwise do nothing
            if (!table.Exists())
            {
                // Create table if it doesn't exist already
                table.CreateIfNotExists();

                // Create the batch operation.
                TableBatchOperation batchOperation = new TableBatchOperation();

                // Create a sample entity and add it to the table.
                SampleEntity sample1 = new SampleEntity(partitionName, "1");
                sample1.Title = "Song1";
                sample1.Artist = "Artist1";
                sample1.CreatedDate = DateTime.Now;
                sample1.Mp3Blob = null;
                sample1.SampleMp3Blob = null;
                sample1.SampleMp3URL = null;
                sample1.SampleDate = null;

                // Create another sample entity and add it to the table.
                SampleEntity sample2 = new SampleEntity(partitionName, "2");
                sample2.Title = "Song2";
                sample2.Artist = "Artist2";
                sample2.CreatedDate = DateTime.Now;
                sample2.Mp3Blob = null;
                sample2.SampleMp3Blob = null;
                sample2.SampleMp3URL = null;
                sample2.SampleDate = null;

                // Create another sample entity and add it to the table.
                SampleEntity sample3 = new SampleEntity(partitionName, "3");
                sample3.Title = "Song3";
                sample3.Artist = "Artist3";
                sample3.CreatedDate = DateTime.Now;
                sample3.Mp3Blob = null;
                sample3.SampleMp3Blob = null;
                sample3.SampleMp3URL = null;
                sample3.SampleDate = null;

                // Create another sample entity and add it to the table.
                SampleEntity sample4 = new SampleEntity(partitionName, "4");
                sample4.Title = "Song4";
                sample4.Artist = "Artist4";
                sample4.CreatedDate = DateTime.Now;
                sample4.Mp3Blob = null;
                sample4.SampleMp3Blob = null;
                sample4.SampleMp3URL = null;
                sample4.SampleDate = null;

                // Add sample entities to the batch insert operation.
                batchOperation.Insert(sample1);
                batchOperation.Insert(sample2);
                batchOperation.Insert(sample3);
                batchOperation.Insert(sample4);

                // Execute the batch operation.
                table.ExecuteBatch(batchOperation);
            }

        }
    }
}