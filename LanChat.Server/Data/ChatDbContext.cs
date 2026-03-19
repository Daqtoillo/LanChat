using Microsoft.EntityFrameworkCore;
using LanChat.Common.Models;

namespace LanChat.Server.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

        public DbSet<ChatMessage> Messages { get; set; }
    }
}
