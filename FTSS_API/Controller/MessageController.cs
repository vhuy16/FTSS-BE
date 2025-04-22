using Microsoft.AspNetCore.Mvc;
using Supabase;
using Supabase.Interfaces;
using FTSS_API.Utils;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Request.Message;
using System.Security.Claims;
using FTSS_API.Models;
using FTSS_Model.Context;
using Microsoft.AspNetCore.Authorization;
using FTSS_Repository.Interface;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using Supabase.Postgrest;
using Supabase.Postgrest.Interfaces;

namespace FTSS_API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly Supabase.Client _supabase;
    private readonly IUnitOfWork<MyDbContext> _unitOfWork;
    private readonly ILogger<ChatController> _logger;

    public ChatController(Supabase.Client supabase, IUnitOfWork<MyDbContext> unitOfWork, ILogger<ChatController> logger)
    {
        _supabase = supabase;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(Guid? roomId, int page = 1, int size = 50)
    {
        try
        {
            var userId = UserUtil.GetAccountId(HttpContext);
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized access attempt: Invalid user.");
                return Unauthorized("Invalid user.");
            }

            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found or not available.");
                return Unauthorized("User not found or not available.");
            }

            if (user.Role != RoleEnum.Customer.GetDescriptionFromEnum() && user.Role != RoleEnum.Manager.GetDescriptionFromEnum())
            {
                _logger.LogWarning($"User {user.UserName} with role {user.Role} attempted to access messages.");
                return Forbid("Only Customers and Managers can access messages.");
            }

            IPostgrestTable<Message> query = _supabase.From<Message>();
            if (roomId.HasValue)
            {
                query = query.Filter("room_id", Constants.Operator.Equals, roomId.Value.ToString());
            }

            if (user.Role == RoleEnum.Customer.GetDescriptionFromEnum())
            {
                query = query.Filter("user_id", Constants.Operator.Equals, user.Id.ToString());
            }

            var response = await query
                .Order("id", Constants.Ordering.Ascending)
                .Range((page - 1) * size, page * size - 1)
                .Get();

            var messages = response.Models.ToList();
            var messageDtos = new List<MessageDto>();

            foreach (var message in messages)
            {
                var customer = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id == message.UserId && u.Role == RoleEnum.Customer.GetDescriptionFromEnum());

                messageDtos.Add(new MessageDto
                {
                    Id = message.Id,
                    RoomId = message.RoomId,
                    UserId = message.UserId,
                    Username = message.Username,
                    CustomerName = customer?.UserName ?? "Unknown",
                    Role = message.Role,
                    Text = message.Text,
                    Timestamp = TimeUtils.ConvertToSEATime(message.Timestamp) // Đã chuẩn hóa trong TimeUtils
                });
            }

            DateTime? latestMessageTime = messages.Any()
                ? TimeUtils.ConvertToSEATime(messages.Max(m => m.Timestamp))
                : null;

            var result = new
            {
                Messages = messageDtos,
                LatestMessageTime = latestMessageTime
            };

            _logger.LogInformation($"Retrieved {messages.Count} messages for user {user.UserName} in room {roomId?.ToString() ?? "all"}.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages.");
            return StatusCode(500, "An error occurred while retrieving messages.");
        }
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Text) || request.Text.Length > 500)
            {
                return BadRequest("Text is required and must be less than 500 characters.");
            }

            if (request.RoomId == null)
            {
                return BadRequest("RoomId is required.");
            }

            var userId = UserUtil.GetAccountId(HttpContext);
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized access attempt: Invalid user.");
                return Unauthorized("Invalid user.");
            }

            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found or not available.");
                return Unauthorized("User not found or not available.");
            }

            if (user.Role != RoleEnum.Customer.GetDescriptionFromEnum() && user.Role != RoleEnum.Manager.GetDescriptionFromEnum())
            {
                _logger.LogWarning($"User {user.UserName} with role {user.Role} attempted to send a message.");
                return Forbid("Only Customers and Managers can send messages.");
            }

            var message = new Message
            {
                UserId = user.Id,
                RoomId = request.RoomId,
                Username = user.UserName,
                Role = user.Role,
                Text = request.Text,
                Timestamp = TimeUtils.GetCurrentSEATimeAsUtc() // Lưu UTC
            };

            var response = await _supabase.From<Message>().Insert(message);
            _logger.LogInformation($"Message sent by {user.UserName} ({user.Role}) in room {request.RoomId}: {request.Text}");
            return Ok(response.Models.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message.");
            return StatusCode(500, "An error occurred while sending the message.");
        }
    }

    [HttpPost("rooms")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        try
        {
            var userId = UserUtil.GetAccountId(HttpContext);
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized access attempt: Invalid user.");
                return Unauthorized("Invalid user.");
            }

            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found or not available.");
                return Unauthorized("User not found or not available.");
            }

            if (user.Role != RoleEnum.Customer.GetDescriptionFromEnum())
            {
                _logger.LogWarning($"User {user.UserName} with role {user.Role} attempted to create a room.");
                return Forbid("Only Customers can create rooms.");
            }

            var manager = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id == request.ManagerId && u.Role == RoleEnum.Manager.GetDescriptionFromEnum() && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            if (manager == null)
            {
                _logger.LogWarning($"Manager {request.ManagerId} not found or not available.");
                return BadRequest("Manager not found or not available.");
            }

            IPostgrestTable<Room> query = _supabase.From<Room>();
            query = query.Filter("customer_id", Constants.Operator.Equals, user.Id.ToString())
                         .Filter("manager_id", Constants.Operator.Equals, request.ManagerId.ToString());

            var existingRoom = await query.Get();
            if (existingRoom.Models.Any())
            {
                return Ok(existingRoom.Models.First());
            }

            var room = new Room
            {
                Id = Guid.NewGuid(),
                CustomerId = user.Id,
                ManagerId = request.ManagerId,
                CreatedAt = TimeUtils.GetCurrentSEATimeAsUtc() // Lưu UTC
            };

            var response = await _supabase.From<Room>().Insert(room);
            _logger.LogInformation($"Room created: {room.Id} between Customer {user.UserName} and Manager {manager.UserName}");
            return Ok(response.Models.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room.");
            return StatusCode(500, "An error occurred while creating the room.");
        }
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        try
        {
            var userId = UserUtil.GetAccountId(HttpContext);
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized access attempt: Invalid user.");
                return Unauthorized("Invalid user.");
            }

            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found or not available.");
                return Unauthorized("User not found or not available.");
            }

            if (user.Role != RoleEnum.Customer.GetDescriptionFromEnum() && user.Role != RoleEnum.Manager.GetDescriptionFromEnum())
            {
                _logger.LogWarning($"User {user.UserName} with role {user.Role} attempted to access rooms.");
                return Forbid("Only Customers and Managers can access rooms.");
            }

            IPostgrestTable<Room> query = _supabase.From<Room>();
            if (user.Role == RoleEnum.Customer.GetDescriptionFromEnum())
            {
                query = query.Filter("customer_id", Constants.Operator.Equals, user.Id.ToString());
            }
            else
            {
                query = query.Filter("manager_id", Constants.Operator.Equals, user.Id.ToString());
            }

            var response = await query.Get();
            var rooms = response.Models;

            var roomDtos = new List<RoomDto>();
            foreach (var room in rooms)
            {
                var manager = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id == room.ManagerId);

                var customer = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id == room.CustomerId);

                var latestMessage = await _supabase.From<Message>()
                    .Filter("room_id", Constants.Operator.Equals, room.Id.ToString())
                    .Order("timestamp", Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                DateTime? latestMessageTime = latestMessage.Models.Any()
                    ? TimeUtils.ConvertToSEATime(latestMessage.Models.First().Timestamp)
                    : null;

                roomDtos.Add(new RoomDto
                {
                    Id = room.Id,
                    CustomerId = room.CustomerId,
                    ManagerId = room.ManagerId,
                    ManagerName = manager?.UserName ?? "Unknown",
                    CustomerName = customer?.UserName ?? "Unknown",
                    CreatedAt = TimeUtils.ConvertToSEATime(room.CreatedAt), // Đã chuẩn hóa trong TimeUtils
                    LatestMessageTime = latestMessageTime
                });
            }

            _logger.LogInformation($"Retrieved {roomDtos.Count} rooms for user {user.UserName}.");
            return Ok(roomDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rooms.");
            return StatusCode(500, "An error occurred while retrieving rooms.");
        }
    }

    [HttpGet("users/managers")]
    public async Task<IActionResult> GetManagers()
    {
        try
        {
            var userId = UserUtil.GetAccountId(HttpContext);
            if (!userId.HasValue)
            {
                _logger.LogWarning("Unauthorized access attempt: Invalid user.");
                return Unauthorized("Invalid user.");
            }

            var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found or not available.");
                return Unauthorized("User not found or not available.");
            }

            if (user.Role != RoleEnum.Customer.GetDescriptionFromEnum())
            {
                _logger.LogWarning($"User {user.UserName} with role {user.Role} attempted to access managers.");
                return Forbid("Only Customers can access managers list.");
            }

            var managers = await _unitOfWork.GetRepository<User>().GetListAsync(
                predicate: u => u.Role == RoleEnum.Manager.GetDescriptionFromEnum() && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            var managerDtos = managers.Select(m => new ManagerDto
            {
                Id = m.Id,
                UserName = m.UserName
            }).ToList();

            _logger.LogInformation($"Retrieved {managerDtos.Count} available managers for user {user.UserName}.");
            return Ok(managerDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving managers.");
            return StatusCode(500, "An error occurred while retrieving managers.");
        }
    }
}

public class RoomDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ManagerId { get; set; }
    public string ManagerName { get; set; }
    public string CustomerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LatestMessageTime { get; set; }
}

public class ManagerDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
}

public class MessageDto
{
    public long Id { get; set; }
    public Guid? RoomId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string CustomerName { get; set; }
    public string Role { get; set; }
    public string Text { get; set; }
    public DateTime Timestamp { get; set; }
}