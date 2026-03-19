using LanChat.Common.Dtos;
using LanChat.Common.Models;
using LanChat.Server.Data;
using LanChat.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace LanChat.Server.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatDbContext _context;
        private readonly BlobService _blobService;

        public ChatHub(ChatDbContext context, BlobService  blobService)
        {
            _context = context;
            _blobService = blobService;
        }

        public async Task SendMessage(SendMessageDto messageDto)
        {
            var msgEntity = new ChatMessage
            {
                Sender = messageDto.Sender,
                Content = messageDto.Content,
                ProfilePictureUrl = messageDto.ProfilePictureUrl,
                AttachmentUrl = messageDto.AttachmentUrl,
                AttachmentName = messageDto.AttachmentName,
                IsImageAttachment = messageDto.IsImageAttachment,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(msgEntity);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(msgEntity.ProfilePictureUrl))
                msgEntity.ProfilePictureUrl = _blobService.GetSecureImageUrl(msgEntity.ProfilePictureUrl);

            if (!string.IsNullOrWhiteSpace(msgEntity.AttachmentUrl))
                msgEntity.AttachmentUrl = _blobService.GetSecureImageUrl(msgEntity.AttachmentUrl);

            await Clients.All.SendAsync("ReceiveMessage", msgEntity);
        }

        public async Task DeleteMessage(int messageId, string requesterName)
        {
            var message = await _context.Messages.FindAsync(messageId);

            if (message == null) return;

            if (message.Sender != requesterName) return;

            if(!string.IsNullOrWhiteSpace(message.AttachmentUrl))
                await _blobService.DeleteFileAsync(message.AttachmentUrl);

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            await Clients.All.SendAsync("MessageDeleted", messageId);
        }

        public override async Task OnConnectedAsync()
        {
            string logMessage = $"Client Connected: ConnectionID: {Context.ConnectionId}";
            await _blobService.LogSecurityEventAsync(logMessage);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string reason = exception != null ? $"Crashed: {exception.Message}" : "Disconnected Gracefully";
            string logMessage = $"Client Disconnected. ConnectionID {Context.ConnectionId} - {reason}";

            await _blobService.LogSecurityEventAsync(logMessage);
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}
