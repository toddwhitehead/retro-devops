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
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("$web");
            
                BlobContinuationToken blobContinuationToken = null;

                do
                {
                    // Get a list of the blobs in the container.
                    // Specifiy the useFlatBlobListing option so we get subfolders too
                    var results = await cloudBlobContainer.ListBlobsSegmentedAsync(
                        prefix            : "uploads",
                        useFlatBlobListing: true, 
                        blobListingDetails: BlobListingDetails.All,
                        maxResults        : null,
                        currentToken      : blobContinuationToken,
                        options           : null,
                        operationContext  : null
                    );

                    // Get the value of the continuation token returned by the listing call.
                    blobContinuationToken = results.ContinuationToken;

                    string filename = "";

                    foreach (IListBlobItem item in results.Results)
                    {                    
                            filename = System.IO.Path.GetFileName(item.Uri.LocalPath);
                            if(filename.EndsWith(".d64"))
                            {
                                diskList.Add(new C64Disk{displayName = filename, fileName = filename, uri = item.Uri.ToString()});
                            }
                    }
                } while (blobContinuationToken != null); // Loop while the continuation token is not null.

                var json = JsonConvert.SerializeObject(diskList, Formatting.Indented);

                // write a blob to the container
                CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference("uploads/fileList.json");
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

        public string uri { get; set; }
    }

}
