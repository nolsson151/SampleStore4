
using System.IO;

using Microsoft.Azure.WebJobs;
using NAudio.Wave;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using SampleStore4.Models;
using System;

namespace SampleStore4_WebJob
{
    public class Functions
    {
        public static void ReadTableEntity(
        [QueueTrigger("samplemaker")] SampleEntity sampleInQueue,
        [Blob("musiclibrary/music/{queueTrigger}")] CloudBlockBlob inputBlob,
        [Blob("musiclibrary/samples/{queueTrigger}")] CloudBlockBlob outputBlob,
        [Table("Samples", "{PartitionKey}", "{RowKey}")] SampleEntity personInTable,
        [Table("Samples")] CloudTable tableBinding,
        TextWriter logger)
        {
            if (personInTable == null) {
                logger.WriteLine("Person not found: PK:{0}, RK:{1}",
                sampleInQueue.PartitionKey, sampleInQueue.RowKey);
            }

            else
            {
                logger.WriteLine("Person found: PK:{0}, RK:{1}, Name:{2}",
                       personInTable.PartitionKey, personInTable.RowKey, personInTable.Title);

                DateTime date;
                date = DateTime.Now;
                var newSample = new SampleEntity()
                {
                    PartitionKey = sampleInQueue.PartitionKey,
                    RowKey = sampleInQueue.RowKey,
                    Mp3Blob = sampleInQueue.Mp3Blob,
                    SampleMp3Blob = sampleInQueue.Mp3Blob,
                    SampleMp3URL = "http://127.0.0.1:10000/devstoreaccount1/musiclibrary/samples/" + sampleInQueue.Mp3Blob,
                    SampleDate = DateTime.Now
                };
                newSample.ETag = "*";
                TableOperation tableop = TableOperation.Merge(newSample);
                tableBinding.Execute(tableop);

                inputBlob.FetchAttributes();
                string test = inputBlob.Metadata["Title"];
                // Open streams to blobs for reading and writing as appropriate.
                // Pass references to application specific methods
                using (Stream input = inputBlob.OpenRead())
                using (Stream output = outputBlob.OpenWrite())
                {
                    CreateSample(input, output, 10);
                    outputBlob.Properties.ContentType = "audio/mpeg3";
                }
                outputBlob.Metadata["Title"] = test;
                outputBlob.SetMetadata();
                logger.WriteLine("GenerateSample() completed...");
            }
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
