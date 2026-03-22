using LanChat.Common.Models;
using LanChat.Server.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LanChat.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly CosmosDbService _cosmosService;
        private readonly BlobService _blobService;
        private readonly RedisCacheService _cacheService;

        private const string ChatHistoryKey = "recent_chat_history";

        public ChatController(CosmosDbService cosmosService, BlobService blobService, RedisCacheService cacheService)
        {
            _cosmosService = cosmosService;
            _blobService = blobService;
            _cacheService = cacheService;
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<ChatMessage>>> GetHistory()
        {
            List<ChatMessage> messages = null;

            try
            {
                messages = await _cacheService.GetCacheDataAsync<List<ChatMessage>>(ChatHistoryKey);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Redis Cache Bypass: {ex.Message}");
            }

            if(messages == null)
            {
                messages = await _cosmosService.GetRecentMessagesAsync();

                messages.Reverse();

                try
                {
                    await _cacheService.SetCacheDataAsync(ChatHistoryKey, messages, TimeSpan.FromMinutes(5));
                }
                catch { }
            }

            foreach(var msg in messages)
            {
                if (!string.IsNullOrWhiteSpace(msg.ProfilePictureUrl))
                    msg.ProfilePictureUrl = _blobService.GetSecureImageUrl(msg.ProfilePictureUrl);

                if (!string.IsNullOrWhiteSpace(msg.AttachmentUrl))
                    msg.AttachmentUrl = _blobService.GetSecureImageUrl(msg.AttachmentUrl);
            }

            return Ok(messages);
        }

        [HttpGet("export/{username}")]
        public async Task<IActionResult> ExportHistory(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return BadRequest("Username is required.");

            var userMessages = await _cosmosService.GetMessagesByUserAsync(username);

            if (!userMessages.Any()) return NotFound("No messages found to export.");

            string jsonContent = JsonSerializer.Serialize(userMessages, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string fileName = $"{username}-history-{DateTime.UtcNow:yyyyMMddHHmmss}.json";

            string rawUrl = await _blobService.UploadJsonExportAsync(jsonContent, fileName);

            string sasUrl = _blobService.GetSecureImageUrl(rawUrl);

            return Ok(new { DownloadUrl = sasUrl });
        }
    }
}
