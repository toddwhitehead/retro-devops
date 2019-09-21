using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Host;


namespace retrodevsops.disks
{
    public static class DiskImageTrigger
    {


        [FunctionName("DiskImageTrigger")]

        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest  req,
            ILogger log)
        {
            log.LogInformation("C64 Disk List file generator function");

            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            

            // Retrieve the connection string for use with the application. 
            string storageConnectionString = Environment.GetEnvironmentVariable("CONNECT_STR");            
            
            // Check whether the connection string can be parsed.
            CloudStorageAccount storageAccount;
            var diskList = new List<C64Disk>();

            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                // If the connection string is valid, proceed with operations against Blob storage here.
                    log.LogInformation("Get a list of blobs in the container");
            
                // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();            
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("c64-disks");
            
                BlobContinuationToken blobContinuationToken = null;

                do
                {
                    var results = await cloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                    // Get the value of the continuation token returned by the listing call.
                    blobContinuationToken = results.ContinuationToken;

                    string filename = "";

                    foreach (IListBlobItem item in results.Results)
                    {                    
                    // if (item.Uri.IsFile) {
                            //filename = System.IO.Path.GetFileName(item.Uri.LocalPath);
                            filename = System.IO.Path.GetFileName(item.Uri.LocalPath);
                            if(filename.EndsWith(".d64"))
                            {
                                diskList.Add(new C64Disk{displayName = filename, fileName = item.Uri.ToString()});
                            }
                    //  }     
                    }
                } while (blobContinuationToken != null); // Loop while the continuation token is not null.

                var json = JsonConvert.SerializeObject(diskList, Formatting.Indented);

                // write a blob to the container
                CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference("fileList.json");
                blob.UploadTextAsync(json.ToString()).Wait();
            }
            else
            {
                // Otherwise, let the user know that they need to define the environment variable.
                log.LogInformation(
                    "A connection string has not been defined in the system environment variables. " +
                    "Add an environment variable named 'CONNECT_STR' with your storage " +
                    "connection string as a value.");
            }
            return (ActionResult)new OkObjectResult($"{diskList.Count} files written to fileList.json");
               
        }
    }

    public class C64Disk
    {
        public string displayName { get; set; }
        public string fileName { get; set; }
    }

}
