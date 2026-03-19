using Microsoft.AspNetCore.Mvc;
using LanChat.Server.Services;
using LanChat.Server.Models;
using Azure.Storage.Blobs.Models;

namespace LanChat.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly BlobService _blobService;
        
        public ImagesController(BlobService blobService)
        {
            _blobService = blobService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadProfilePicture([FromForm] ImageUploadRequest request)
        {
            if(request.File == null || request.File.Length == 0)
            {
                return BadRequest("No file uploaded");
            }            
            
            using var stream = request.File.OpenReadStream();

            string rawUrl = await _blobService.UploadFileAsync(stream, request.File.FileName, request.UploaderName, request.File.ContentType);

            string sasUrl = _blobService.GetSecureImageUrl(rawUrl);

            return Ok(new { RawUrl = rawUrl, SasUrl = sasUrl });
        }

        [HttpGet("audit")]
        public async Task<IActionResult> GetImageMetadata([FromQuery] string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
                return BadRequest("URL is missing.");

            string? uploader = await _blobService.GetFileUploaderAsync(rawUrl);

            if (uploader == null)
                return NotFound("Blob not found or metadata is missing");

            return Ok(new { FileUrl = rawUrl, UploadedBy = uploader });
        }

        [HttpPost("tier")]
        public async Task<IActionResult> UpdateBlobTier([FromQuery] string rawUrl, [FromQuery] string tierName)
        {
            if (string.IsNullOrWhiteSpace(rawUrl) || string.IsNullOrWhiteSpace(tierName))
                return BadRequest("URL and Tier name are required.");

            AccessTier targetTier = tierName.ToLower() switch
            {
                "cool" => AccessTier.Cool,
                "archive" => AccessTier.Archive,
                "cold" => AccessTier.Cold,
                _ => AccessTier.Hot
            };

            bool success = await _blobService.ChangeAccessTierAsync(rawUrl, targetTier);

            if (success)
                return Ok(new { Message = $"Successfully initiated move to {targetTier} tier." });

            return BadRequest($"Failed to change access tier. (Note: Azurite emulator does not support this operation)");
        }

        [HttpPost("overwrite-locked")]
        public async Task<IActionResult> OverwriteLockedBlob([FromQuery] string rawUrl, [FromQuery] string localFilePath)
        {
            if (string.IsNullOrWhiteSpace(rawUrl) || string.IsNullOrWhiteSpace(localFilePath))
                return BadRequest("Url and local file path are required.");

            bool success = await _blobService.OverwriteBlobWithLeaseAsync(rawUrl, localFilePath);

            if (success)
                return Ok(new { Message = $"Successfully acquired lease, overwritten blob, and released lease" });

            return Conflict("Failed to overwrite blob. It may be locked by another process (Concurrency control active).");
        }
    }
}
