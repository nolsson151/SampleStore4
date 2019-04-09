
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
        private const String musicDirectory = "music/";
        private const String sampleDirectory = "samples/";

        // This class contains the application-specific WebJob code consisting of event-driven
        // methods executed when messages appear in queues with any supporting code.
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

            // Operation to retrieve a SampleEntity using partitionName and id from the sample in the queue.
            // Operation is then executed and the result is assigned to a SampleEntity named sampleEntity. 
            TableOperation tableOperation = TableOperation.Retrieve<SampleEntity>(sampleInQueue.PartitionKey, sampleInQueue.RowKey);
            TableResult tableResult = tableBinding.Execute(tableOperation);
            SampleEntity sampleEntity = (SampleEntity)tableResult.Result;
            
            // InputBlob in music storage is referenced and used as the the audio to be turned into a sample.
            // Title is then taken from the sampleEntity in the table and assigned to sampleName which will then be used
            // as the name of the sample file. This is name will also then be assigned to the table element of 
            // SampleMp3Blob.
            var inputBlob = cloudBlobContainer.GetDirectoryReference(musicDirectory).GetBlobReference(sampleInTable.Mp3Blob);
            string sampleName = string.Format("{0}{1}", sampleInTable.Title, "-sample.mp3");
            sampleInTable.SampleMp3Blob = sampleName;

            // OutputBlob is is created in sample blob storage and is assigned the name sampleName. Using streams,
            // the audio from the blob in music storage is read and and used in CreateSample to create a sample and 
            // is saved to the sample blob is sample storage. SampleDate is set to the time at the moment of call 
            // and the SampleMp3URL is set to the exact location of the sample in blob sotrage.
            var outputBlob = cloudBlobContainer.GetBlockBlobReference(sampleDirectory + sampleName);
            using (Stream input = inputBlob.OpenRead())
            using (Stream output = outputBlob.OpenWrite())
            {
                CreateSample(input, output, 10);
                outputBlob.Properties.ContentType = "audio/mpeg3";
            }
            sampleInTable.SampleDate = DateTime.Now;
            sampleInTable.SampleMp3URL = outputBlob.Uri.ToString();

            // Another table opertaion that that will update the table with the new SampleMp3Blob, SampleDate,
            // and the SampleMp3URL. 
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
