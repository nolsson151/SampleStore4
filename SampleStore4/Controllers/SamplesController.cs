using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using SampleStore4.Models;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace SampleStore4.Controllers
{
    public class SamplesController : ApiController
    {
        private const String partitionName = "Samples_Partition_1";

        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;
        private BlobStorageService blobStorageService = new BlobStorageService();
        private CloudQueueService cloudQueueService = new CloudQueueService();

        public SamplesController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference("Samples");
        }

        /// <summary>
        /// Get all samples
        /// </summary>
        /// <returns></returns>
        // GET: api/Samples
        public IEnumerable<Sample> Get()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));
            List<SampleEntity> entityList = new List<SampleEntity>(table.ExecuteQuery(query));

            // Basically create a list of Sample from the list of SampleEntity with a 1:1 object relationship, filtering data as needed
            IEnumerable<Sample> sampleList = from e in entityList
                                             select new Sample()
                                             {
                                                 SampleID = e.RowKey,
                                                 Title = e.Title,
                                                 Artist = e.Artist,
                                                 CreatedDate = e.CreatedDate,
                                                 Mp3Blob = e.Mp3Blob,
                                                 SampleMp3Blob = e.SampleMp3Blob,
                                                 SampleMp3URL = e.SampleMp3URL,
                                                 SampleDate = e.SampleDate
                                             };
            return sampleList;
        }

        // GET: api/Samples/5
        /// <summary>
        /// Get a sample
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult GetSample(string id)
        {
            // Create a retrieve operation that takes a sample entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiatte
            if (getOperationResult.Result == null) return NotFound();
            else
            {
                SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;
                Sample s = new Sample()
                {
                    SampleID = sampleEntity.RowKey,
                    Title = sampleEntity.Title,
                    Artist = sampleEntity.Artist,
                    CreatedDate = sampleEntity.CreatedDate,
                    Mp3Blob = sampleEntity.Mp3Blob,
                    SampleMp3Blob = sampleEntity.SampleMp3Blob,
                    SampleMp3URL = sampleEntity.SampleMp3URL,
                    SampleDate = sampleEntity.SampleDate
                };
                return Ok(s);
            }
        }

        //POST: api/Samples
        /// <summary>
        /// Create a new sample in table
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        [SwaggerResponse(HttpStatusCode.Created)]
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostSample(Sample sample)
        {
            
            
                // Create new SampleEntity from Sample object 
                SampleEntity sampleEntity = new SampleEntity()
                {
                    RowKey = getNewMaxRowKeyValue(),
                    PartitionKey = partitionName,
                    Title = sample.Title,
                    Artist = sample.Artist,
                    SampleMp3URL = sample.SampleMp3URL,
                    CreatedDate = DateTime.Now,
                    Mp3Blob = null,
                    SampleMp3Blob = null,
                    SampleDate = null
                };
                // Create insert operation for SampleEntity
                var insertOperation = TableOperation.Insert(sampleEntity);

                // Execute insert operation
                table.Execute(insertOperation);

                // Return HTTP status 201 Created code with details in response body
                return CreatedAtRoute("DefaultApi", new { id = sampleEntity.RowKey }, sampleEntity);
            
        }

        // PUT: api/Samples/5
        /// <summary>
        /// Update a sample in table
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        [SwaggerResponse(HttpStatusCode.NoContent)]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSample(string id, Sample sample)
        {
            if (id != sample.SampleID)
            {
                return BadRequest();
            }

            // Create a retrieve operation that takes a sample entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // Assign the result to a SampleEntity object.
            SampleEntity updateEntity = (SampleEntity)retrievedResult.Result;

            updateEntity.Title = sample.Title;
            updateEntity.Artist = sample.Artist;
            //updateEntity.CreatedDate = sample.CreatedDate;


            // Create the TableOperation that inserts the sample entity.
            // Note semantics of InsertOrReplace() which are consistent with PUT
            // See: https://stackoverflow.com/questions/14685907/difference-between-insert-or-merge-entity-and-insert-or-replace-entity
            var updateOperation = TableOperation.InsertOrReplace(updateEntity);

            // Execute the insert operation.
            table.Execute(updateOperation);

            return StatusCode(HttpStatusCode.NoContent);
        }
        // PUT: api/Samples/5/blob
        /// <summary>
        /// Puts a blob to a sample in table
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(void))]
        [HttpPut]
        [Route("api/samples/{id}/blob")]
        public async Task<IHttpActionResult> PutSampleBlob(string id)
        {
            Stream stream = await Request.Content.ReadAsStreamAsync();
            if(stream.Length < 1)
            {
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            TableOperation tableOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);
            TableResult tableResult = table.Execute(tableOperation);
            SampleEntity sampleEntity = (SampleEntity)tableResult.Result;
            if(sampleEntity == null)
            {
                return StatusCode(HttpStatusCode.NotFound);
            }

            deleteBlob(sampleEntity);

            string name = string.Format("{0}{1}", sampleEntity.Title, ".mp3");
            string path = "music/" + name;
            var blob = getContainer().GetBlockBlobReference(path);

            blob.Properties.ContentType = Request.Content.Headers.ContentType.ToString();
            blob.UploadFromStream(stream);

            CloudQueue cloudQueue = getSampleQueue();
            var queueMessage = new SampleEntity(partitionName, id);
            cloudQueue.AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(queueMessage)));
            sampleEntity.Mp3Blob = name;
            var update = TableOperation.InsertOrReplace(sampleEntity);
            table.Execute(update);
            return StatusCode(HttpStatusCode.Created);


        }

        // DELETE: api/Samples/5
        /// <summary>
        /// Delete a sample
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult DeleteSample(string id)
        {
            // Create a retrieve operation that takes a product entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result == null) return NotFound();
            else
            {
                SampleEntity deleteEntity = (SampleEntity)retrievedResult.Result;
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);

                return Ok(retrievedResult.Result);
            }
        }

        public void deleteBlob(SampleEntity sampleEntity)
        {
            string musicFolder = "music/" + sampleEntity.Mp3Blob;
            string sampleFolder = "muisclibrary/samples/" + sampleEntity.SampleMp3Blob;
            
            if(sampleEntity.Mp3Blob != "")
            {
                var musicBlob = getContainer().GetBlobReference(musicFolder);
                musicBlob.DeleteIfExists(); 
            }
            if(sampleEntity.SampleMp3Blob != "")
            {
                var sampleBlob = getContainer().GetBlobReference(sampleFolder);
                sampleBlob.DeleteIfExists();
            }
        }

        private String getNewMaxRowKeyValue()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            int maxRowKeyValue = 0;
            foreach (SampleEntity entity in table.ExecuteQuery(query))
            {
                int entityRowKeyValue = Int32.Parse(entity.RowKey);
                if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
            }
            maxRowKeyValue++;
            return maxRowKeyValue.ToString();
        }

        private CloudBlobContainer getContainer()
        {
            return blobStorageService.getCloudBlobContainer();
        }

        private CloudQueue getSampleQueue()
        {
            return cloudQueueService.getCloudQueue();
        }


    }
}
