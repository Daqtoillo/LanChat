using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using LanChat.Server.Data;
using LanChat.Common.Models;
using LanChat.Server.Services;
using System.Text.Json;

namespace LanChat.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ChatDbContext _context;
        private readonly BlobService _blobService;
        private readonly RedisCacheService _cacheService;

        private const string ChatHistoryKey = "recent_chat_history";

        public ChatController(ChatDbContext context, BlobService blobService, RedisCacheService cacheService)
        {
            _context = context;
            _blobService = blobService;
            _cacheService = cacheService;
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<ChatMessage>>> GetHistory()
        {
            var messages = await _cacheService.GetCacheDataAsync<List<ChatMessage>>(ChatHistoryKey);

            if(messages == null)
            {
                messages = await _context.Messages
                    .OrderByDescending(m => m.SentAt)
                    .Take(50)
                    .ToListAsync();

                messages.Reverse();

                await _cacheService.SetCacheDataAsync(ChatHistoryKey, messages, TimeSpan.FromMinutes(5));
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

            var userMessages = await _context.Messages
                           .Where(m => m.Sender == username)
                           .OrderByDescending(m => m.SentAt)
                           .ToListAsync();

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
