using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.Book;
using FTSS_API.Payload.Request.MaintenanceSchedule;
using FTSS_API.Payload.Response.Book;
using FTSS_API.Payload.Response.MaintenanceSchedule;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Model.Paginate;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace FTSS_API.Service.Implement
{
    public class BookingService : BaseService<BookingService>, IBookingService
    {
        public BookingService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<BookingService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }
        public async Task<ApiResponse> AssigningTechnician(AssigningTechnicianRequest request)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && u.IsDelete == false &&
                                    (u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }
                // Kiểm tra request có đầy đủ dữ liệu không
                if (string.IsNullOrWhiteSpace(request.MissionName))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "MissionName name cannot be empty.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(request.MissionDescription))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "MissionDescription name cannot be empty.",
                        data = null
                    };
                }
                if (request.MissionSchedule == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "MissionSchedule cannot be empty.",
                        data = null
                    };
                }
                
                // Kiểm tra ScheduleDate phải trễ hơn thời gian hiện tại theo SEATime
                var currentSEATime = TimeUtils.GetCurrentSEATime();
                if (request.MissionSchedule <= currentSEATime)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "MissionSchedule must be later than the current SEATime.",
                        data = null
                    };
                }

                // Kiểm tra technician có tồn tại và hợp lệ không
                var technician = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: t => t.Id.Equals(request.TechnicianId) &&
                                    t.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    t.IsDelete == false &&
                                    t.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()));

                if (technician == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Technician is not valid, does not exist, or is not a Technician.",
                        data = null
                    };
                }
                // Kiểm tra technician đã có mission vào ngày này chưa
                var existingMission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                    predicate: m => m.Userid.Equals(request.TechnicianId) &&
                                    m.MissionSchedule.Value.Date == request.MissionSchedule.Value.Date &&
                                    m.IsDelete == false);

                if (existingMission != null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = $"Technician has already been assigned a mission on {request.MissionSchedule.Value.Date:dd/MM/yyyy}.",
                        data = null
                    };
                }
                // Truy vấn Order theo orderid để lấy Address và PhoneNumber
                var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                    predicate: o => o.Id.Equals(request.OrderId) && o.IsDelete == false);

                if (order == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Order not found or has been deleted.",
                        data = null
                    };
                }
                // Kiểm tra trạng thái Order phải là CONFIRMED
                if (!order.Status.Equals(OrderStatus.CONFIRMED.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Order status must be CONFIRMED before assigning a technician.",
                        data = null
                    };
                }
                // ✅ Kiểm tra Order đã được phân công chưa
                if (order.IsAssigned == true)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Order has already been assigned to a technician.",
                        data = null
                    };
                }
                // Tạo mới MaintenanceTask
                var newTask = new Mission
                {
                    Id = Guid.NewGuid(),
                    BookingId = null, 
                    MissionName = request.MissionName,
                    MissionDescription = request.MissionDescription,
                    Status = MissionStatusEnum.NotStarted.GetDescriptionFromEnum(),
                    IsDelete = false,
                    MissionSchedule = request.MissionSchedule,
                    Address = order.Address,
                    PhoneNumber = order.PhoneNumber,
                    Userid = request.TechnicianId, // Gán technicianid từ request vào userid của MaintenanceTask
                    OrderId = order.Id,
                };

                await _unitOfWork.Context.Set<Mission>().AddAsync(newTask);
                // ✅ Cập nhật trạng thái IsAssigned của Order thành true
                order.IsAssigned = true;
                _unitOfWork.GetRepository<Order>().UpdateAsync(order);
                await _unitOfWork.CommitAsync();

                // Chuẩn bị response
                var response = new AssigningTechnicianResponse
                {
                    Id = newTask.Id,
                    MissionName = newTask.MissionName,
                    MissionDescription = newTask.MissionDescription,
                    Status = newTask.Status,
                    MissionSchedule = newTask.MissionSchedule,
                    TechnicianId = request.TechnicianId,
                    TechnicianName = technician.FullName ?? "Unknown",
                    Address = newTask.Address,
                    PhoneNumber = newTask.PhoneNumber,
                    OrderId = newTask.OrderId
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Technician assigned successfully.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while assigning technician.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> AssigningTechnicianBooking(AssignTechBookingRequest request)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role.Equals(RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }

                // Kiểm tra request có đủ dữ liệu không
                if (string.IsNullOrWhiteSpace(request.MissionName))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "MissionName cannot be empty.",
                        data = null
                    };
                }
                if (string.IsNullOrWhiteSpace(request.MissionDescription))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "MissionDescription cannot be empty.",
                        data = null
                    };
                }

                // Kiểm tra Booking có tồn tại không
                var booking = await _unitOfWork.GetRepository<Booking>()
                        .SingleOrDefaultAsync(
                            predicate: b => b.Id == request.BookingId && (b.IsAssigned == false),
                            include: b => b.Include(x => x.User));
                // Load thông tin User của Booking

                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Booking does not exist or has already been assigned.",
                        data = null
                    };
                }

                // Kiểm tra technician có tồn tại và hợp lệ không
                var technician = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: t => t.Id.Equals(request.TechnicianId) &&
                                    t.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    t.IsDelete == false &&
                                    t.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()));

                if (technician == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Technician is not valid, does not exist, or is not a Technician.",
                        data = null
                    };
                }

                // Kiểm tra kỹ thuật viên đã có nhiệm vụ trong ngày đó chưa
                var assignedMission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                    predicate: m => m.Userid == request.TechnicianId &&
                                    m.MissionSchedule.HasValue &&
                                    m.MissionSchedule.Value.Date == booking.ScheduleDate.Value.Date);

                if (assignedMission != null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = $"Technician has already been assigned a mission on {booking.ScheduleDate.Value.Date:dd/MM/yyyy}.",
                        data = null
                    };
                }
                // ✅ Kiểm tra Booking đã được phân công chưa
                if (booking.IsAssigned == true)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Booking has already been assigned to a technician.",
                        data = null
                    };
                }
                // Tạo nhiệm vụ mới
                var newTask = new Mission
                {
                    Id = Guid.NewGuid(),
                    BookingId = request.BookingId,
                    MissionName = request.MissionName,
                    MissionDescription = request.MissionDescription,
                    Status = MissionStatusEnum.NotStarted.GetDescriptionFromEnum(),
                    IsDelete = false,
                    MissionSchedule = booking.ScheduleDate,
                    Userid = request.TechnicianId,
                    Address = booking.Address,
                    PhoneNumber = booking.PhoneNumber,
                };

                await _unitOfWork.Context.Set<Mission>().AddAsync(newTask);

                // Cập nhật trạng thái booking đã được gán
                booking.IsAssigned = true;
                _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);

                await _unitOfWork.CommitAsync();

                // **Lấy thông tin User từ Booking của Mission**
                var missionWithBooking = await _unitOfWork.GetRepository<Mission>()
                    .SingleOrDefaultAsync(
                        predicate: m => m.Id == newTask.Id,
                        include: query => query.Include(m => m.Booking)
                                               .ThenInclude(b => b.User));

                var bookingUser = missionWithBooking?.Booking?.User;

                // Chuẩn bị response
                var response = new AssignTechBookingResponse
                {
                    Id = newTask.Id,
                    MissionName = newTask.MissionName,
                    MissionDescription = newTask.MissionDescription,
                    Status = newTask.Status,
                    MissionSchedule = newTask.MissionSchedule,
                    TechnicianId = request.TechnicianId,
                    TechnicianName = technician.UserName,
                    BookingId = request.BookingId,
                    UserId = bookingUser?.Id ?? Guid.Empty,
                    UserName = bookingUser?.UserName ?? "Unknown",
                    FullName = bookingUser?.FullName ?? "Unknown",
                    Address = newTask.Address,
                    PhoneNumber = newTask.PhoneNumber
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Technician assigned to booking successfully.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while assigning technician to booking.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> BookingSchedule(BookingScheduleRequest request)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role.Equals(RoleEnum.Customer.GetDescriptionFromEnum()));

                if (user == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }

                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Address name cannot be empty.",
                        data = null
                    };
                }

                string phonePattern = @"^0\d{9}$";
                if (!Regex.IsMatch(request.PhoneNumber, phonePattern))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = MessageConstant.PatternMessage.PhoneIncorrect,
                        data = null
                    };
                }

                if (request.ScheduleDate == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "ScheduleDate cannot be empty.",
                        data = null
                    };
                }

                var currentSEATime = TimeUtils.GetCurrentSEATime();
                if (request.ScheduleDate <= currentSEATime)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "ScheduleDate must be later than the current SEATime.",
                        data = null
                    };
                }

                // Kiểm tra đơn hàng có tồn tại và có trạng thái COMPLETED không
                var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                    predicate: o => o.Id.Equals(request.OrderId) &&
                                    o.Status.Equals(OrderStatus.COMPLETED.ToString()) &&
                                    o.IsDelete == false);

                if (order == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Order does not exist or is not COMPLETED.",
                        data = null
                    };
                }

                // Kiểm tra xem đã có booking nào cùng ngày với order chưa
                var existingBooking = await _unitOfWork.GetRepository<Booking>().GetListAsync(
                        predicate: b => b.OrderId.Equals(request.OrderId) &&
                                        b.ScheduleDate.Value.Date == request.ScheduleDate.Value.Date);

                bool hasBookingSameDate = existingBooking.Any();

                if (hasBookingSameDate)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Order already has a booking on this date.",
                        data = null
                    };
                }


                // Kiểm tra số lượng booking đã có của order này
                var existingBookings = await _unitOfWork.GetRepository<Booking>().GetListAsync(
                    predicate: b => b.OrderId.Equals(request.OrderId));

                bool isFirstBooking = existingBookings.Count() == 0;
                bool isEligibleForFreeBooking = order.IsEligible == true && isFirstBooking;

                // Xác định trạng thái booking
                string bookingStatus = isEligibleForFreeBooking ? BookingStatusEnum.FREE.ToString() : BookingStatusEnum.NOTPAID.ToString();

                // Tính TotalPrice nếu không miễn phí
                decimal totalPrice = 0;
                List<ServicePackage> selectedServices = new();
                List<Guid> serviceIds = request.ServiceIds?.Select(s => s.ServiceId).ToList() ?? new List<Guid>();

                if (!isEligibleForFreeBooking) // Nếu không được miễn phí, phải tính tiền
                {
                    // Lấy danh sách ServicePackage từ serviceIds
                    selectedServices = (List<ServicePackage>)await _unitOfWork.GetRepository<ServicePackage>().GetListAsync(
                        predicate: sp => serviceIds.Contains(sp.Id) &&
                                         sp.Status.Equals(ServicePackageStatus.Available.GetDescriptionFromEnum()) &&
                                         sp.IsDelete == false);

                    // Kiểm tra nếu có serviceId không hợp lệ
                    var invalidServices = serviceIds.Except(selectedServices.Select(sp => sp.Id)).ToList();
                    if (invalidServices.Any())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Invalid service package ID(s).",
                            data = invalidServices
                        };
                    }

                    // Tính tổng giá dịch vụ
                    totalPrice = selectedServices.Sum(sp => sp.Price);
                }

                // Tạo mới Booking
                var newBooking = new Booking
                {
                    Id = Guid.NewGuid(),
                    ScheduleDate = request.ScheduleDate,
                    Status = bookingStatus,
                    Address = request.Address,
                    PhoneNumber = request.PhoneNumber,
                    FullName = request.FullName,
                    TotalPrice = totalPrice,
                    UserId = userId,
                    OrderId = request.OrderId,
                    IsAssigned = false
                };

                await _unitOfWork.GetRepository<Booking>().InsertAsync(newBooking);

                // Nếu không miễn phí, thêm BookingDetail
                if (!isEligibleForFreeBooking && selectedServices.Any())
                {
                    var bookingDetails = selectedServices.Select(service => new BookingDetail
                    {
                        Id = Guid.NewGuid(),
                        ServicePackageId = service.Id,
                        BookingId = newBooking.Id
                    }).ToList();

                    await _unitOfWork.Context.Set<BookingDetail>().AddRangeAsync(bookingDetails);
                }

                // Nếu là lần đầu tiên booking và IsEligible == true, cập nhật lại IsEligible của Order thành false
                if (isEligibleForFreeBooking)
                {
                    order.IsEligible = false;
                    _unitOfWork.GetRepository<Order>().UpdateAsync(order);
                }

                await _unitOfWork.CommitAsync();

                // Chuẩn bị response
                var response = new BookingScheduleResponse
                {
                    Id = newBooking.Id,
                    ScheduleDate = newBooking.ScheduleDate,
                    Status = newBooking.Status,
                    Address = newBooking.Address,
                    PhoneNumber = newBooking.PhoneNumber,
                    TotalPrice = newBooking.TotalPrice,
                    UserId = user.Id,
                    UserName = user.UserName,
                    FullName = newBooking.FullName,
                    OrderId = request.OrderId
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Booking scheduled successfully.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while booking the schedule.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetBookingById(Guid bookingId)
        {
            try
            {
                // Truy vấn Booking từ DB với thông tin User và BookingDetails
                var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                    predicate: b => b.Id == bookingId,
                    include: b => b.Include(x => x.User)
                                   .Include(x => x.BookingDetails)
                                   .ThenInclude(bd => bd.ServicePackage)
                );

                // Kiểm tra nếu không tìm thấy booking
                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Booking not found.",
                        data = null
                    };
                }

                // Chuyển đổi dữ liệu Booking sang response
                var response = new GetBookingById
                {
                    Id = booking.Id,
                    ScheduleDate = booking.ScheduleDate,
                    Status = booking.Status,
                    Address = booking.Address,
                    PhoneNumber = booking.PhoneNumber,
                    TotalPrice = booking.TotalPrice,
                    OrderId = booking.OrderId,
                    IsAssigned = booking.IsAssigned,
                    Services = booking.BookingDetails.Select(bd => new ServicePackageResponse
                    {
                        Id = bd.ServicePackage.Id,
                        ServiceName = bd.ServicePackage.ServiceName,
                        Price = bd.ServicePackage.Price
                    }).ToList()
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Success",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = $"An error occurred: {ex.Message}",
                    data = null
                };
            }
        }

        public async Task<ApiResponse> GetDateUnavailable()
        {
            try
            {
                // Đếm số lượng mission theo từng ngày MissionSchedule
                var unavailableDates = await _unitOfWork.GetRepository<Mission>()
                    .GetListAsync(
                        predicate: m => m.MissionSchedule != null && (m.IsDelete == false || m.IsDelete == null),
                        selector: m => m.MissionSchedule.Value.Date // Lấy chỉ ngày (không lấy giờ phút giây)
                    );

                // Nhóm theo ngày và lọc ra những ngày có từ 5 mission trở lên
                var groupedUnavailableDates = unavailableDates
                    .GroupBy(date => date)
                    .Where(group => group.Count() >= 5)
                    .Select(group => new GetDateUnavailableResponse { ScheduleDate = group.Key })
                    .ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "List of unavailable dates retrieved successfully.",
                    data = groupedUnavailableDates
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving unavailable dates.",
                    data = ex.Message
                };
            }
        }


        public async Task<ApiResponse> GetListBookingForManager(int pageNumber, int pageSize, string? status, bool? isAscending, bool? isAssigned)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role.Equals(RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status403Forbidden.ToString(),
                        message = "You don't have permission to do this.",
                        data = null
                    };
                }

                // Lọc danh sách Booking dựa trên điều kiện
                Expression<Func<Booking, bool>> filter = b =>
                    (string.IsNullOrEmpty(status) || b.Status == status) &&
                    (isAssigned == null || b.IsAssigned == isAssigned);

                // Sắp xếp tăng hoặc giảm dần theo ngày đặt lịch
                Func<IQueryable<Booking>, IOrderedQueryable<Booking>> orderBy = query =>
                    isAscending == true ? query.OrderBy(b => b.ScheduleDate) : query.OrderByDescending(b => b.ScheduleDate);

                // Lấy danh sách Booking theo trang, bao gồm thông tin User
                var bookings = await _unitOfWork.GetRepository<Booking>().GetPagingListAsync(
                    predicate: filter,
                    orderBy: orderBy,
                    include: b => b.Include(x => x.User),
                    page: pageNumber,
                    size: pageSize
                );

                // Chuyển đổi sang response
                var response = bookings.Items.Select(b => new GetListBookingForManagerResponse
                {
                    Id = b.Id,
                    ScheduleDate = b.ScheduleDate,
                    Status = b.Status,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    TotalPrice = b.TotalPrice,
                    UserId = b.User?.Id,
                    UserName = b.User?.UserName ?? "Unknown",
                    FullName = b.FullName,
                    OrderId = b.OrderId,
                    IsAssigned = b.IsAssigned
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "List of bookings retrieved successfully.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving the list of bookings.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetListBookingForUser(int pageNumber, int pageSize, string? status, bool? isAscending)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role.Equals(RoleEnum.Customer.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status403Forbidden.ToString(),
                        message = "You don't have permission to do this.",
                        data = null
                    };
                }

                // Lấy danh sách booking của Customer đó
                var bookings = await _unitOfWork.GetRepository<Booking>().GetPagingListAsync(
                    predicate: b => b.UserId == userId && (string.IsNullOrEmpty(status) || b.Status == status),
                    orderBy: q => isAscending == true ? q.OrderBy(b => b.ScheduleDate) : q.OrderByDescending(b => b.ScheduleDate),
                    page: pageNumber,
                    size: pageSize
                );

                var response = bookings.Items.Select(b => new GetListBookingForUserResponse
                {
                    Id = b.Id,
                    ScheduleDate = b.ScheduleDate,
                    Status = b.Status,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    TotalPrice = b.TotalPrice,
                    OrderId = b.OrderId,
                    IsAssigned = b.IsAssigned
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Booking list retrieved successfully.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving the booking list.",
                    data = ex.Message // Trả về lỗi để debug, nhưng có thể log lại thay vì hiển thị trực tiếp
                };
            }
        }


        public async Task<ApiResponse> GetListMissionForManager(int pageNumber, int pageSize, string? status, bool? isAscending)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role.Equals(RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status403Forbidden.ToString(),
                        message = "You don't have permission to do this.",
                        data = null
                    };
                }

                // Lọc danh sách Mission dựa trên điều kiện
                Expression<Func<Mission, bool>> filter = m =>
                    (string.IsNullOrEmpty(status) || m.Status == status) &&
                    (m.IsDelete == false || m.IsDelete == null);

                // Sắp xếp tăng hoặc giảm dần theo lịch trình nhiệm vụ
                Func<IQueryable<Mission>, IOrderedQueryable<Mission>> orderBy = query =>
                    isAscending == true ? query.OrderBy(m => m.MissionSchedule) : query.OrderByDescending(m => m.MissionSchedule);

                // Lấy danh sách Mission theo trang, bao gồm thông tin của Technician (User)
                var missions = await _unitOfWork.GetRepository<Mission>().GetPagingListAsync(
                    predicate: filter,
                    orderBy: orderBy,
                    include: m => m.Include(x => x.User),
                    page: pageNumber,
                    size: pageSize
                );

                // Chuyển đổi sang response
                var response = missions.Items.Select(m => new GetListMissionForManagerResponse
                {
                    Id = m.Id,
                    MissionName = m.MissionName,
                    MissionDescription = m.MissionDescription,
                    Status = m.Status,
                    MissionSchedule = m.MissionSchedule,
                    Address = m.Address,
                    PhoneNumber = m.PhoneNumber,
                    BookingId = m.BookingId,
                    TechnicianId = m.Userid, 
                    TechnicianName = m.User?.FullName ?? "Unknown" // Lấy FullName của Technician nếu có
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "List of missions retrieved successfully.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving the list of missions.",
                    data = ex.Message
                };
            }
        }


        public async Task<ApiResponse> GetListTaskTech(int pageNumber, int pageSize, string? status, bool? isAscending)
        {
            // Lấy UserId từ HttpContext
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
            var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                predicate: u => u.Id.Equals(userId) &&
                                u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                u.IsDelete == false &&
                                u.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()));

            if (userr == null)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status403Forbidden.ToString(),
                    message = "You don't have permission to do this.",
                    data = null
                };
            }

            // Kiểm tra xem Mission có thuộc tính TechnicianId không
            var taskRepo = _unitOfWork.GetRepository<Mission>();

            // Áp dụng include riêng để tránh lỗi kiểu dữ liệu
            var query = taskRepo.GetListAsync(
                                 predicate: t => t.Userid == userId &&
                                                 (string.IsNullOrEmpty(status) || t.Status == status) &&
                                                 t.IsDelete == false, // Chỉ lấy mission chưa bị xóa
                                 include: t => t.Include(x => x.User)
                             );

            var taskList = await query;

            // Sắp xếp sau khi lấy dữ liệu (tránh lỗi kiểu dữ liệu)
            taskList = isAscending == true
                ? taskList.OrderBy(x => x.MissionSchedule).ToList()
                : taskList.OrderByDescending(x => x.MissionSchedule).ToList();

            // Phân trang dữ liệu
            var paginatedList = taskList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Chuyển đổi danh sách sang GetListTaskTechResponse
            var response = paginatedList.Select(t => new GetListTaskTechResponse
            {
                Id = t.Id,
                MissionName = t.MissionName,
                MissionDescription = t.MissionDescription,
                Status = t.Status,
                IsDelete = t.IsDelete,
                MissionSchedule = t.MissionSchedule,
                FullName = t.User?.FullName ?? "Unknown",
                Address = t.Address,
                PhoneNumber = t.PhoneNumber
            }).ToList();

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "List of tasks retrieved successfully.",
                data = response
            };
        }

        public async Task<ApiResponse> GetListTech(GetListTechRequest request)
        {
            try
            {
                // Lấy ngày từ request (nếu có)
                DateTime? requestedDate = request.ScheduleDate?.Date;

                // Lấy danh sách tất cả kỹ thuật viên có Role là Technician và chưa bị xóa
                var allTechnicians = await _unitOfWork.GetRepository<User>().GetListAsync(
                    predicate: u => u.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

                // Lấy danh sách nhiệm vụ có MissionSchedule trong ngày được request
                var missionsOnRequestedDate = await _unitOfWork.GetRepository<Mission>().GetListAsync(
                    predicate: m => m.MissionSchedule.Value.Date == request.ScheduleDate.Value.Date &&
                                    (m.IsDelete == false || m.IsDelete == null));

                // Lọc ra các kỹ thuật viên chưa có nhiệm vụ trong ngày đó
                var availableTechnicians = allTechnicians
                    .Where(tech => !missionsOnRequestedDate.Any(m => m.Userid == tech.Id))
                    .Select(t => new GetListTechResponse
                    {
                        TechId = t.Id,
                        TechName = t.UserName,
                        FullName = t.FullName
                    })
                    .ToList();


                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "List of available technicians retrieved successfully.",
                    data = availableTechnicians
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving the technician list.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetServicePackage(int pageNumber, int pageSize, bool? isAscending)
        {
            // Lấy danh sách gói dịch vụ từ repository với điều kiện lọc
            var servicePackages = await _unitOfWork.GetRepository<ServicePackage>().GetListAsync(
                predicate: sp => sp.Status == ServicePackageStatus.Available.ToString() && sp.IsDelete == false,
                orderBy: isAscending == true ? sp => sp.OrderBy(x => x.ServiceName) : sp => sp.OrderByDescending(x => x.ServiceName)
            );

            // Phân trang dữ liệu
            var paginatedList = servicePackages
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Chuyển đổi danh sách sang GetServicePackageResponse
            var response = paginatedList.Select(sp => new GetServicePackageResponse
            {
                Id = sp.Id,
                ServiceName = sp.ServiceName,
                Price = sp.Price
            }).ToList();

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "List of available service packages retrieved successfully.",
                data = response
            };
        }

        public async Task<ApiResponse> UpdateStatusMission(Guid id, string status)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()));

                if (user == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status403Forbidden.ToString(),
                        message = "You don't have permission to update this mission.",
                        data = null
                    };
                }

                // Kiểm tra Mission có tồn tại không
                var mission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                    predicate: m => m.Id.Equals(id) && m.IsDelete == false);

                if (mission == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Mission not found.",
                        data = null
                    };
                }

                // Kiểm tra status có hợp lệ không
                if (!Enum.TryParse(status, true, out MissionStatusEnum validStatus))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Invalid status. Allowed values: Done, Cancel, Processing, NotStarted.",
                        data = null
                    };
                }

                // Cập nhật Status
                mission.Status = validStatus.ToString();
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Mission status updated successfully.",
                    data = new { mission.Id, mission.Status }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while updating mission status.",
                    data = ex.Message
                };
            }
        }

    }
}
