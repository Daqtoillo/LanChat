using LanChat.Common.Dtos;
using LanChat.Common.Models;
using LanChat.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace LanChat.Server.Hubs
{
    public class ChatHub : Hub
    {
        private readonly CosmosDbService _cosmosService;
        private readonly BlobService _blobService;

        public ChatHub(CosmosDbService cosmosService, BlobService  blobService)
        {
            _cosmosService = cosmosService;
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

            await _cosmosService.AddMessageAsync(msgEntity);

            if (!string.IsNullOrWhiteSpace(msgEntity.ProfilePictureUrl))
                msgEntity.ProfilePictureUrl = _blobService.GetSecureImageUrl(msgEntity.ProfilePictureUrl);

            if (!string.IsNullOrWhiteSpace(msgEntity.AttachmentUrl))
                msgEntity.AttachmentUrl = _blobService.GetSecureImageUrl(msgEntity.AttachmentUrl);

            await Clients.All.SendAsync("ReceiveMessage", msgEntity);
        }

        public async Task DeleteMessage(string messageId, string requesterName)
        {
            try
            {
                var messages = await _cosmosService.GetMessagesByUserAsync(requesterName);
                var messageToDelete = messages.FirstOrDefault(m => m.Id == messageId);

                if (messageToDelete == null) return;

                if (!string.IsNullOrWhiteSpace(messageToDelete.AttachmentUrl))
                {
                    await _blobService.DeleteFileAsync(messageToDelete.AttachmentUrl);
                }

                await _cosmosService.DeleteMessageAsync(messageId, requesterName);

                await Clients.All.SendAsync("MessageDeleted", messageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete message: {ex.Message}");
            }
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
