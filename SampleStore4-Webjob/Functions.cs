
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
        // This class contains the application-specific WebJob code consisting of event-driven
        // methods executed when messages appear in queues with any supporting code.

        // Trigger method  - run when new message detected in queue. "thumbnailmaker" is name of queue.
        // "photogallery" is name of storage container; "images" and "thumbanils" are folder names.
        // "{queueTrigger}" is an inbuilt variable taking on value of contents of message automatically;
        // the other variables are valued automatically.
        public static void GenerateSample(
        [QueueTrigger("samplemaker")] SampleEntity sampleInQueue,
        [Table("Samples", "{PartitionKey}", "{RowKey}")] SampleEntity sampleInTable,
        [Table("Samples")] CloudTable tableBinding, TextWriter logger)
        {
            logger.WriteLine("GenerateSample started\n"+ 
                "Parition key: " + sampleInQueue.PartitionKey+"\n"+
                "Row key: "+sampleInQueue.RowKey);
            BlobStorageService blobStorageService = new BlobStorageService();
            CloudBlobContainer cloudBlobContainer = blobStorageService.getCloudBlobContainer();

            TableOperation tableOperation = TableOperation.Retrieve<SampleEntity>(sampleInQueue.PartitionKey, sampleInQueue.RowKey);
            TableResult tableResult = tableBinding.Execute(tableOperation);
            SampleEntity sampleEntity = (SampleEntity)tableResult.Result;

            var inputBlob = cloudBlobContainer.GetDirectoryReference("music/").GetBlobReference(sampleInTable.Mp3Blob);
            var sampleName = string.Format("{0}{1}{2}", sampleInTable.Title, "-sample", ".mp3");
            sampleInTable.SampleMp3Blob = sampleName;
            var outputBlob = cloudBlobContainer.GetBlockBlobReference("samples/" + sampleName);
            using (Stream input = inputBlob.OpenRead())
            using (Stream output = outputBlob.OpenWrite())
            {
                CreateSample(input, output, 10);
                outputBlob.Properties.ContentType = "audio/mpeg3";
            }
            sampleInTable.SampleDate = DateTime.Now;
            sampleInTable.SampleMp3URL = outputBlob.Uri.ToString();

            TableOperation tableOperation2 = TableOperation.InsertOrReplace(sampleInTable);
            tableBinding.Execute(tableOperation2);

            logger.WriteLine("Sample created: "+ sampleName );
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
