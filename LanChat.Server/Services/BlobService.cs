using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Components.Forms;
using System.Text;

namespace LanChat.Server.Services
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient containerClient;
        private const string containerName = "images";

        public BlobService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;

            containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string originalFileName, string uploaderName, string contentType)
        { 
            string extension = Path.GetExtension(originalFileName);
            string uniqueName = $"{Guid.NewGuid()}{extension}";

            BlobClient blobClient = containerClient.GetBlobClient(uniqueName);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },

                Metadata = new Dictionary<string, string>
                {
                    {"UploadedBy", uploaderName}
                }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions);

            return blobClient.Uri.AbsoluteUri;
        }
        public string GetSecureImageUrl(string rawBlobUrl)
        {
            var blobUri = new Uri(rawBlobUrl);
            string blobName = blobUri.Segments.Last();

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            return sasUri.ToString();
        }

        public async Task<bool> DeleteFileAsync(string rawBlobUrl)
        {
            if (string.IsNullOrWhiteSpace(rawBlobUrl)) return false;

            try
            {
                var blobUri = new Uri(rawBlobUrl);
                string blobName = blobUri.Segments.Last();

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                var response = await blobClient.DeleteIfExistsAsync();

                return response.Value;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetFileUploaderAsync(string rawBlobUrl)
        {
            if (string.IsNullOrWhiteSpace(rawBlobUrl)) return null;

            try
            {
                var blobUri = new Uri(rawBlobUrl);
                string blobName = blobUri.Segments.Last();

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                BlobProperties properties = await blobClient.GetPropertiesAsync();

                if (properties.Metadata.TryGetValue("UploadedBy", out string uploaderName))
                    return uploaderName;

                return "Unknown";
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> UploadJsonExportAsync(string jsonContent, string exportFileName)
        {
            BlobClient blobClient = containerClient.GetBlobClient(exportFileName);

            byte[] byteArray = Encoding.UTF8.GetBytes(jsonContent);

            using var stream = new MemoryStream(byteArray);

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/json"
                }
            };

            await blobClient.UploadAsync(stream, uploadOptions);

            return blobClient.Uri.AbsoluteUri;
        }

        public async Task LogSecurityEventAsync(string logMessage)
        {
            try
            {
                string logFileName = $"security-logs/log-{DateTime.UtcNow:yyyy-MM}.txt";

                AppendBlobClient appendBlobClient = containerClient.GetAppendBlobClient(logFileName);

                await appendBlobClient.CreateIfNotExistsAsync();

                string formattedLog = $"[{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss} UTC] {logMessage}{Environment.NewLine}";
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(formattedLog));

                await appendBlobClient.AppendBlockAsync(stream);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"FATAL: Could not write to Azure Security Log. {ex.Message}");
            }
        }

        public async Task<bool> ChangeAccessTierAsync(string rawBlobUrl, AccessTier newTier)
        {
            if (string.IsNullOrWhiteSpace(rawBlobUrl)) return false;

            try
            {
                var blobUri = new Uri(rawBlobUrl);
                string blobName = blobUri.Segments.Last();

                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                await blobClient.SetAccessTierAsync(newTier);

                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Tier change has failed: {ex.Message} (Note: Expected behaviour if using a local Azurite emulator)");
                return false;
            }
        }

        public async Task<bool> OverwriteBlobWithLeaseAsync(string rawBlobUrl, string localFilePath)
        {
            if (string.IsNullOrWhiteSpace(rawBlobUrl) || string.IsNullOrWhiteSpace(localFilePath)) return false;

            try
            {
                var blobUri = new Uri(rawBlobUrl);
                string blobName = blobUri.Segments.Last();
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                BlobLeaseClient leaseClient = blobClient.GetBlobLeaseClient();
                
                BlobLease lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(15));

                using var stream = File.OpenRead(localFilePath);

                var uploadOptions = new BlobUploadOptions
                {
                    Conditions = new BlobRequestConditions
                    {
                        LeaseId = lease.LeaseId
                    }
                };

                await blobClient.UploadAsync(stream, uploadOptions);

                await leaseClient.ReleaseAsync();

                return true;
            }
            catch(Azure.RequestFailedException ex) when (ex.Status == 412)
            {
                Console.WriteLine($"Lease operation has failed: {ex.Message}");
                return false;
            } 
        }
    }
}
