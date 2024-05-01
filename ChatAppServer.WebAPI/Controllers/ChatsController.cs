using ChatAppServer.WebAPI.Context;
using ChatAppServer.WebAPI.Dtos;
using ChatAppServer.WebAPI.Hubs;
using ChatAppServer.WebAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System;

namespace ChatAppServer.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public sealed class ChatsController(ApplicationDbContext context, IHubContext<ChatHub> hubContext) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            List<User> users = await context.Users.OrderBy(u => u.Name).ToListAsync();

            var allNotReadChats = await context.Chats.Where(c => !c.ReadStatus).ToListAsync();
            await hubContext.Clients.All.SendAsync("AllNotReadChats", allNotReadChats);

            return Ok(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetChats(Guid userId, Guid toUserId, CancellationToken cancellationToken)
        {
            List<Chat> chats = await context.Chats.Where(p =>
                         (p.UserId == userId && p.ToUserId == toUserId) ||
                         (p.UserId == toUserId && p.ToUserId == userId))
                         .OrderBy(o => o.Date).ToListAsync(cancellationToken);

            foreach (Chat chat in chats)
            {
                chat.ReadDate = DateTime.Now;
                chat.ReadStatus = true;
            }
            await context.SaveChangesAsync(cancellationToken);

            var activeUserPair = ChatHub.Users.FirstOrDefault(p => p.Value == toUserId);
            var connectionId = string.Empty;
            if (activeUserPair.Key != null)
            {
                var activeUser = activeUserPair.Value;
                connectionId = activeUserPair.Key;
                await hubContext.Clients.Client(connectionId).SendAsync("ChangeMessages", chats);
            }

            var allNotReadChats = await context.Chats.Where(c => !c.ReadStatus).ToListAsync();
            await hubContext.Clients.All.SendAsync("AllNotReadChats", allNotReadChats);

            return Ok(chats);
        }
        [HttpPost]
        public async Task<IActionResult> SendMessage(SendMessageDto request, CancellationToken cancellationToken)
        {
            Chat chat = new()
            {
                UserId = request.UserId,
                ToUserId = request.ToUserId,
                Message = request.Message,
                Date = DateTime.Now
            };

            await context.Chats.AddAsync(chat);
            await context.SaveChangesAsync(cancellationToken);

            var activeUserPair = ChatHub.Users.FirstOrDefault(p => p.Value == chat.ToUserId);
            var connectionId = string.Empty;
            if (activeUserPair.Key != null)
            {
                var activeUser = activeUserPair.Value;
                connectionId = activeUserPair.Key;
                await hubContext.Clients.Client(connectionId).SendAsync("Messages", chat);
            }

            var allNotReadChats = await context.Chats.Where(c => !c.ReadStatus).ToListAsync();
            await hubContext.Clients.All.SendAsync("AllNotReadChats", allNotReadChats);

            return Ok(chat);
        }
    }
}
