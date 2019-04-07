using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace SampleStore4.Models

{
    public class Sample
    {
        /// <summary>
        /// Sample ID
        /// </summary>
        [Key]
        public string SampleID { get; set; }

        /// <summary>
        /// Title of the sample
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Name of the sample artist
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// Creation date/time of entity
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Name of the uploaded blob in blob storage
        /// </summary>
        public string Mp3Blob { get; set; }

        /// <summary>
        /// Name of the sample blob in blob storage
        /// </summary>
        public string SampleMp3Blob { get; set; }

        /// <summary>
        /// Web service resource URL of mp3 sample
        /// </summary>
        public String SampleMp3URL { get; set; }

        /// <summary>
        /// Creation date/time of sample
        /// </summary>
        public DateTime? SampleDate { get; set; }
    }
}