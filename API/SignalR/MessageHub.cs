using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRe;
        public IUserRepository _userRepository { get; }
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _presenceHub;
        public MessageHub(
        IMessageRepository messageRe, 
        IUserRepository iuser,
        IMapper mapper,
        IHubContext<PresenceHub> presenceHub)
        {
            _mapper = mapper;
            _userRepository = iuser;
            _messageRe = messageRe;
            _presenceHub = presenceHub;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"]; 
            var groupName = GetGroupName(Context.User.Getusername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);

            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _messageRe
                .GetMessageThread(Context.User.Getusername(), otherUser);

            await Clients.Group(groupName).SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var username = Context.User.Getusername();

            if (username == createMessageDto.RecipentUsername.ToLower())
                throw new HubException("You cannot send messages to yourself!");

            var sender = await _userRepository.GetUserByUsernameAsync(username);
            var recipent = await _userRepository.GetUserByUsernameAsync(createMessageDto.RecipentUsername);

            if (recipent == null) throw new HubException("not found user");

            var message = new Message
            {
                Sender = sender,
                Recipent = recipent,
                SenderUsername = sender.UserName,
                RecipentUsername = recipent.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recipent.UserName);

            var group = await _messageRe.GetMessageGroup(groupName);

            if(group.Connections.Any(x => x.UserName == recipent.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await PresenceTracker.GetConnectionsForUser(recipent.UserName);
                if (connections != null)
                {
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived",
                    new {username = sender.UserName, knownAs = sender.KnownAs});
                }
            }

            _messageRe.AddMessage(message);

            if (await _messageRe.SaveAllAsync())
            {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            }
        }
        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await _messageRe.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.Getusername());

            if (group == null)
            {
                group = new Group(groupName);
                _messageRe.AddGroup(group);
            }

            group.Connections.Add(connection);

            if( await _messageRe.SaveAllAsync()) return group;

            throw new HubException("Failed to add to group");
        }

        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await _messageRe.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _messageRe.RemoveConnection(connection);

            if (await _messageRe.SaveAllAsync()) return group;

            throw new HubException("Failed to remove from group");
        }
    }
}