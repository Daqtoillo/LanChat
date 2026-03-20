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
            [BlobTrigger("images/{name}", Connection = "AzureWebJobsStorage")] Stream stream,
            string name)
        {
            _logger.LogInformation($"Execution triggered for blob: {name}");
            _logger.LogInformation($"Original Size: {stream.Length} Bytes");

            try
            {
                using Image image = Image.Load(stream);

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(256, 0),
                    Mode = ResizeMode.Max
                }));

                using var outputMemoryStream = new MemoryStream();
                await image.SaveAsync(outputMemoryStream, new JpegEncoder { Quality = 75 });

                _logger.LogInformation($"Compressed Size: {outputMemoryStream.Length} Bytes");

                return outputMemoryStream.ToArray();
            }
            catch(Exception ex)
            {
                _logger.LogError($"Failed to process image {name}. Error: {ex.Message}");
                throw;
            }
        }
    }
}