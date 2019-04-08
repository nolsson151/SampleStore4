
using System.IO;

using Microsoft.Azure.WebJobs;
using NAudio.Wave;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using SampleStore4;
using System;
using SampleStore4.Models;

namespace SampleStore4_WebJob
{
    

    public class Functions
    {
        
        public static void ReadTableEntity(
        [QueueTrigger("samplemaker")] SampleEntity sampleInQueue,
        [Table("Samples", "{PartitionKey}", "{RowKey}")] SampleEntity sampleInTable,
        [Table("Samples")] CloudTable tableBinding, TextWriter logger)
        {
            logger.WriteLine("Samplemaker starter");
            BlobStorageService bs = new BlobStorageService();
            CloudBlobContainer bc = bs.getCloudBlobContainer();

            TableOperation tableOperation = TableOperation.Retrieve<SampleEntity>(sampleInQueue.PartitionKey, sampleInQueue.RowKey);
            TableResult tableResult = tableBinding.Execute(tableOperation);
            SampleEntity sampleEntity = (SampleEntity)tableResult.Result;

            var blob = bc.GetDirectoryReference("music/").GetBlobReference(sampleInTable.Mp3Blob);
            string sampleName = string.Format("{0}{1}", Guid.NewGuid(), ".mp3");
            sampleInTable.SampleMp3Blob = sampleName;
            var blob2 = bc.GetBlockBlobReference("musiclibrary/samples/" + sampleName);
            using (Stream input = blob.OpenRead())
            using (Stream output = blob2.OpenWrite())
            {
                CreateSample(input, output, 10);
                blob2.Properties.ContentType = "audio/mpeg3";
            }
            sampleInTable.SampleDate = DateTime.Now;
            sampleInTable.SampleMp3URL = blob2.Uri.ToString();

            TableOperation tableOperation2 = TableOperation.InsertOrReplace(sampleInTable);
            tableBinding.Execute(tableOperation2);

            logger.WriteLine("Sample createed");


            //if (sampleInTable == null)
            //{
            //    logger.WriteLine("Person not found: PK:{0}, RK:{1}",
            //    sampleInQueue.PartitionKey, sampleInQueue.RowKey);
            //}

            //else
            //{
            //    logger.WriteLine("Person found: PK:{0}, RK:{1}, Name:{2}",
            //           sampleInTable.PartitionKey, sampleInTable.RowKey, sampleInTable.Title);

            //    DateTime date;
            //    date = DateTime.Now;
            //    var newSample = new SampleEntity()
            //    {
            //        PartitionKey = sampleInQueue.PartitionKey,
            //        RowKey = sampleInQueue.RowKey,
            //        Mp3Blob = sampleInQueue.Mp3Blob,
            //        SampleMp3Blob = sampleInQueue.Mp3Blob,
            //        SampleMp3URL = "http://127.0.0.1:10000/devstoreaccount1/musiclibrary/samples/" + sampleInQueue.Mp3Blob,
            //        SampleDate = DateTime.Now
            //    };
            //    newSample.ETag = "*";
            //    TableOperation tableop = TableOperation.Merge(newSample);
            //    tableBinding.Execute(tableop);

            //    There is still problem here, if using this it must be modified.
            //    inputBlob.FetchAttributes();
            //    string test = inputBlob.Metadata["Title"];
            //    Open streams to blobs for reading and writing as appropriate.
            //    Pass references to application specific methods
            //    using (Stream input = inputBlob.OpenRead())
            //        using (Stream output = outputBlob.OpenWrite())
            //        {
            //            CreateSample(input, output, 10);
            //            outputBlob.Properties.ContentType = "audio/mpeg3";
            //        }
            //    outputBlob.Metadata["Title"] = test;
            //    outputBlob.SetMetadata();
            //    logger.WriteLine("GenerateSample() completed...");
            //}
        }

        private static void CreateSample(Stream input, Stream output, int duration)
        {
            using (var reader = new Mp3FileReader(input, wave => new NLayer.NAudioSupport.Mp3FrameDecompressor(wave)))
            {
                Mp3Frame frame;
                frame = reader.ReadNextFrame();
                int frameTimeLength = (int)(frame.SampleCount / (double)frame.SampleRate * 1000.0);
                int framesRequired = (int)(duration / (double)frameTimeLength * 1000.0);

                int frameNumber = 0;
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    frameNumber++;

                    if (frameNumber <= framesRequired)
                    {
                        output.Write(frame.RawData, 0, frame.RawData.Length);
                    }
                    else break;
                }
            }
        }
    }




}
