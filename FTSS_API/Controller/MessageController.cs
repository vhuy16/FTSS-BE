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
    private readonly SupabaseUltils _supabaseImageService;

    public ChatController(Supabase.Client supabase, IUnitOfWork<MyDbContext> unitOfWork,
        SupabaseUltils supabaseImageService, ILogger<ChatController> logger)
    {
        _supabase = supabase;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _supabaseImageService = supabaseImageService;
    }

    /// <summary>
    /// Lấy danh sách tin nhắn trong một phòng chat hoặc tất cả tin nhắn của người dùng.
    /// </summary>
    /// <param name="roomId">ID của phòng chat để lọc tin nhắn (tùy chọn).</param>
    /// <param name="page">Số trang của danh sách tin nhắn (mặc định: 1).</param>
    /// <param name="size">Số lượng tin nhắn mỗi trang (mặc định: 50).</param>
    /// <returns>Trả về danh sách tin nhắn (MessageDto) và thời gian tin nhắn mới nhất.</returns>
    /// <response code="200">Lấy danh sách tin nhắn thành công.</response>
    /// <response code="401">Người dùng không hợp lệ hoặc không có trạng thái Available.</response>
    /// <response code="403">Người dùng không phải Customer hoặc Manager, hoặc không có quyền truy cập phòng.</response>
    /// <response code="404">Phòng chat không tồn tại.</response>
    /// <response code="500">Lỗi hệ thống khi truy xuất tin nhắn.</response>
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
                predicate: u =>
                    u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found or not available.");
                return Unauthorized("User not found or not available.");
            }

            if (user.Role != RoleEnum.Customer.GetDescriptionFromEnum() &&
                user.Role != RoleEnum.Manager.GetDescriptionFromEnum())
            {
                _logger.LogWarning($"User {user.UserName} with role {user.Role} attempted to access messages.");
                return Forbid("Only Customers and Managers can access messages.");
            }

            // Kiểm tra quyền truy cập phòng chat nếu roomId được cung cấp
            if (roomId.HasValue)
            {
                var room = await _supabase.From<Room>()
                    .Filter("id", Constants.Operator.Equals, roomId.Value.ToString())
                    .Single();
                if (room == null)
                {
                    _logger.LogWarning($"Room {roomId} not found.");
                    return NotFound("Room not found.");
                }

                // Kiểm tra xem user là Customer hoặc Manager của phòng
                if (room.CustomerId != userId.Value && room.ManagerId != userId.Value)
                {
                    _logger.LogWarning($"User {user.UserName} does not have access to room {roomId}.");
                    return Forbid("You do not have access to this room.");
                }
            }

            IPostgrestTable<Message> query = _supabase.From<Message>();
            if (roomId.HasValue)
            {
                query = query.Filter("room_id", Constants.Operator.Equals, roomId.Value.ToString());
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

                // Tạo danh sách Media từ FileUrls
                var mediaList = new List<Media>();
                if (message.FileUrls != null && message.FileUrls.Any())
                {
                    var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var videoExtensions = new[] { ".mp4", ".mov", ".avi" };

                    foreach (var url in message.FileUrls)
                    {
                        var extension = Path.GetExtension(url).ToLower();
                        var mediaType = imageExtensions.Contains(extension) ? "image" :
                            videoExtensions.Contains(extension) ? "video" : "unknown";

                        mediaList.Add(new Media
                        {
                            Url = url,
                            Type = mediaType
                        });
                    }
                }

                messageDtos.Add(new MessageDto
                {
                    Id = message.Id,
                    RoomId = message.RoomId,
                    UserId = message.UserId,
                    Username = message.Username,
                    CustomerName = customer?.UserName ?? "Unknown",
                    Role = message.Role,
                    Text = message.Text,
                    Timestamp = message.Timestamp, // Timestamp đã là SEA
                    Media = mediaList.Any() ? mediaList : null
                });
            }

            DateTime? latestMessageTime = messages.Any()
                ? messages.Max(m => m.Timestamp)
                : null;

            var result = new
            {
                Messages = messageDtos,
                LatestMessageTime = latestMessageTime
            };

            _logger.LogInformation(
                $"Retrieved {messages.Count} messages for user {user.UserName} in room {roomId?.ToString() ?? "all"}.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages.");
            return StatusCode(500, "An error occurred while retrieving messages.");
        }
    }

    /// <summary>
    /// Gửi một tin nhắn mới (văn bản hoặc media) vào một phòng chat.
    /// </summary>
    /// <param name="request">Thông tin tin nhắn, bao gồm nội dung văn bản, file media, và RoomId.</param>
    /// <returns>Trả về thông tin tin nhắn vừa gửi.</returns>
    /// <response code="200">Tin nhắn được gửi thành công.</response>
    /// <response code="400">Thiếu nội dung/file, nội dung quá dài, RoomId không hợp lệ, hoặc file không đúng định dạng/kích thước.</response>
    /// <response code="401">Người dùng không hợp lệ hoặc không có trạng thái Available.</response>
    /// <response code="403">Người dùng không phải Customer hoặc Manager.</response>
    /// <response code="500">Lỗi hệ thống khi gửi tin nhắn hoặc tải file.</response>
    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromForm] MessageRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Text) && (request.Files == null || !request.Files.Any()))
            {
                return BadRequest("Either text or at least one file is required.");
            }

            if (!string.IsNullOrEmpty(request.Text) && request.Text.Length > 500)
            {
                return BadRequest("Text must be less than 500 characters.");
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
                predicate: u =>
                    u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found or not available.");
                return Unauthorized("User not found or not available.");
            }

            if (user.Role != RoleEnum.Customer.GetDescriptionFromEnum() &&
                user.Role != RoleEnum.Manager.GetDescriptionFromEnum())
            {
                _logger.LogWarning($"User {user.UserName} with role {user.Role} attempted to send a message.");
                return Forbid("Only Customers and Managers can send messages.");
            }

            List<string> fileUrls = new List<string>();
            if (request.Files != null && request.Files.Any())
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov", ".avi" };
                const long maxFileSize = 10 * 1024 * 1024; // 10MB mỗi file
                const int maxFiles = 5; // Giới hạn số lượng file tối đa

                if (request.Files.Count > maxFiles)
                {
                    return BadRequest($"Cannot upload more than {maxFiles} files at a time.");
                }

                foreach (var file in request.Files)
                {
                    var fileExtension = Path.GetExtension(file.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        return BadRequest(
                            "Only image (.jpg, .jpeg, .png, .gif) or video (.mp4, .mov, .avi) files are allowed.");
                    }

                    if (file.Length > maxFileSize)
                    {
                        return BadRequest("Each file size must be less than 10MB.");
                    }
                }

                // Upload file bằng _supabaseImageService
                fileUrls = await _supabaseImageService.SendImagesAsync(request.Files, _supabase);
                if (fileUrls == null || fileUrls.Count != request.Files.Count)
                {
                    return BadRequest("Failed to upload one or more files.");
                }
            }

            var message = new Message
            {
                UserId = user.Id,
                RoomId = request.RoomId,
                Username = user.UserName,
                Role = user.Role,
                Text = request.Text ?? "",
                Timestamp = TimeUtils.GetCurrentSEATimeAsUtc(),
                FileUrls = fileUrls.Any() ? fileUrls : null
            };

            var response = await _supabase.From<Message>().Insert(message);
            _logger.LogInformation(
                $"Message sent by {user.UserName} ({user.Role}) in room {request.RoomId}: {request.Text} with {fileUrls.Count} files");
            return Ok(response.Models.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message.");
            return StatusCode(500, "An error occurred while sending the message.");
        }
    }

    /// <summary>
    /// Tạo một phòng chat mới giữa Customer và Manager.
    /// </summary>
    /// <param name="request">Thông tin yêu cầu, bao gồm ID của Manager.</param>
    /// <returns>Trả về thông tin phòng chat vừa tạo hoặc phòng hiện có.</returns>
    /// <response code="200">Phòng chat được tạo thành công hoặc phòng hiện có được trả về.</response>
    /// <response code="400">Manager không hợp lệ hoặc không có trạng thái Available.</response>
    /// <response code="401">Người dùng không hợp lệ hoặc không có trạng thái Available.</response>
    /// <response code="403">Người dùng không phải Customer.</response>
    /// <response code="500">Lỗi hệ thống khi tạo phòng chat.</response>
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
                predicate: u =>
                    u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
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
                predicate: u =>
                    u.Id == request.ManagerId && u.Role == RoleEnum.Manager.GetDescriptionFromEnum() &&
                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
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
            _logger.LogInformation(
                $"Room created: {room.Id} between Customer {user.UserName} and Manager {manager.UserName}");
            return Ok(response.Models.FirstOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room.");
            return StatusCode(500, "An error occurred while creating the room.");
        }
    }

    /// <summary>
    /// Lấy danh sách các phòng chat của người dùng (Customer hoặc Manager).
    /// </summary>
    /// <returns>Trả về danh sách các phòng chat (RoomDto) với thông tin tin nhắn mới nhất.</returns>
    /// <response code="200">Lấy danh sách phòng chat thành công.</response>
    /// <response code="401">Người dùng không hợp lệ hoặc không có trạng thái Available.</response>
    /// <response code="403">Người dùng không phải Customer hoặc Manager.</response>
    /// <response code="500">Lỗi hệ thống khi truy xuất danh sách phòng chat.</response>
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

        if (user.Role != RoleEnum.Customer.GetDescriptionFromEnum() &&
            user.Role != RoleEnum.Manager.GetDescriptionFromEnum())
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
            var latestMessage = await _supabase.From<Message>()
                .Filter("room_id", Constants.Operator.Equals, room.Id.ToString())
                .Order("timestamp", Constants.Ordering.Descending)
                .Limit(1)
                .Get();

            if (!latestMessage.Models.Any())
            {
                continue;
            }

            var manager = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id == room.ManagerId);

            var customer = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id == room.CustomerId);

            // Log giá trị thô để debug
            DateTime rawTimestamp = latestMessage.Models.First().Timestamp;
            _logger.LogInformation($"GetRooms - Raw Timestamp for Room {room.Id}: {rawTimestamp}, Kind: {rawTimestamp.Kind}");

            // Đảm bảo thời gian là UTC trước khi chuyển đổi
            DateTime utcTimestamp = rawTimestamp.Kind == DateTimeKind.Utc
                ? rawTimestamp
                : DateTime.SpecifyKind(rawTimestamp, DateTimeKind.Utc);

            // Chuyển đổi từ UTC sang SEA
            DateTime? latestMessageTime = rawTimestamp;

            roomDtos.Add(new RoomDto
            {
                Id = room.Id,
                CustomerId = room.CustomerId,
                ManagerId = room.ManagerId,
                ManagerName = manager?.UserName ?? "Unknown",
                CustomerName = customer?.UserName ?? "Unknown",
                CreatedAt = room.CreatedAt,
                LatestMessageTime = latestMessageTime
            });
        }

        roomDtos = roomDtos.OrderByDescending(r => r.LatestMessageTime).ToList();

        _logger.LogInformation($"Retrieved {roomDtos.Count} rooms for user {user.UserName}.");
        return Ok(roomDtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving rooms.");
        return StatusCode(500, "An error occurred while retrieving rooms.");
    }
}
    /// <summary>
    /// Lấy danh sách các Manager hiện có và đang hoạt động.
    /// </summary>
    /// <returns>Trả về danh sách các Manager (ManagerDto).</returns>
    /// <response code="200">Lấy danh sách Manager thành công.</response>
    /// <response code="401">Người dùng không hợp lệ hoặc không có trạng thái Available.</response>
    /// <response code="403">Người dùng không phải Customer.</response>
    /// <response code="500">Lỗi hệ thống khi truy xuất danh sách Manager.</response>
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
                predicate: u =>
                    u.Id == userId.Value && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
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
                predicate: u =>
                    u.Role == RoleEnum.Manager.GetDescriptionFromEnum() &&
                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
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
    public string? Text { get; set; }
    public List<Media>? Media { get; set; }
    public DateTime Timestamp { get; set; }
}

public class Media
{
    public string Url { get; set; }
    public string Type { get; set; }
}