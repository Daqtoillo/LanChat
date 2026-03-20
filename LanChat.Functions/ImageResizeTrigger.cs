using System.IO;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace LanChat.Functions
{
    public class ImageResizeTrigger
    {
        private readonly ILogger<ImageResizeTrigger> _logger;

        public ImageResizeTrigger(ILogger<ImageResizeTrigger> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ImageResizeTrigger))]
        [BlobOutput("thumbnails/{name}", Connection = "AzureWebJobsStorage")]
        public async Task<byte[]> Run(
            [BlobTrigger("images/{name}", Connection = "AzureWebJobsStorage")] byte[] imageBytes,
            string name)
        {
            _logger.LogInformation($"Execution triggered for blob: {name}");
            _logger.LogInformation($"Original Size: {imageBytes.Length} Bytes");

            try
            {
                using var inputStream = new MemoryStream(imageBytes);

                using Image image = Image.Load(inputStream);

                image.Mutate(x => x.Resize(256, 0));

                using var outputMemoryStream = new MemoryStream();
                await image.SaveAsync(outputMemoryStream, new JpegEncoder { Quality = 75 });

                _logger.LogInformation($"Compressed Size: {outputMemoryStream.Length} Bytes");

                return outputMemoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to process image {name}. Error: {ex.Message}");
                throw;
            }
        }
    }
}