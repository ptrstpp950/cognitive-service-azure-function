#r "Newtonsoft.Json"
#r "Microsoft.Azure.Storage.Blob"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.Storage.Blob;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    log.LogInformation("C# HTTP trigger function processed a request.");

    dynamic data = await req.Content.ReadAsAsync<object>();
    string photoBase64String = data.photoBase64;
    Uri uri = await UploadBlobAsync(photoBase64String);
    return (ActionResult)new OkObjectResult($"Hello, {name}");//req.CreateResponse(HttpStatusCode.OK, uri);
}

public static async Task<Uri> UploadBlobAsync(string photoBase64String)
{
  var match = new Regex(
    $@"^data\:(?<type>image\/(jpg|gif|png));base64,(?<data>[A-Z0-9\+\/\=]+)$",
    RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase)
    .Match(photoBase64String);
 
  string contentType = match.Groups["type"].Value;
  string extension = contentType.Split('/')[1];
  string fileName = $"{Guid.NewGuid().ToString()}.{extension}";
  byte[] photoBytes = Convert.FromBase64String(match.Groups["data"].Value);
 
  CloudStorageAccount storageAccount =
  CloudStorageAccount.Parse(ConfigurationManager.AppSettings["BlobConnectionString"]);
  CloudBlobClient client = storageAccount.CreateCloudBlobClient();
  CloudBlobContainer container = client.GetContainerReference("img");
 
  await container.CreateIfNotExistsAsync(
    BlobContainerPublicAccessType.Blob,
    new BlobRequestOptions(),
    new OperationContext());
 
  CloudBlockBlob blob = container.GetBlockBlobReference(fileName);
  blob.Properties.ContentType = contentType;
 
  using (Stream stream = new MemoryStream(photoBytes, 0, photoBytes.Length))
  {
    await blob.UploadFromStreamAsync(stream).ConfigureAwait(false);
  }
 
  return blob.Uri;
}