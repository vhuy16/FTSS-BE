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
        public async Task<ApiResponse> AssigningTechnician(Guid technicianid, Guid orderid, AssigningTechnicianRequest request)
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
                    predicate: t => t.Id.Equals(technicianid) &&
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
                    predicate: m => m.Userid.Equals(technicianid) &&
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
                    predicate: o => o.Id.Equals(orderid) && o.IsDelete == false);

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
                // Tạo mới MaintenanceTask
                var newTask = new Mission
                {
                    Id = Guid.NewGuid(),
                    BookingId = null, // Lấy ID của MaintenanceSchedule vừa tạo
                    MissionName = request.MissionName,
                    MissionDescription = request.MissionDescription,
                    Status = MissionStatusEnum.Processing.GetDescriptionFromEnum(),
                    IsDelete = false,
                    MissionSchedule = request.MissionSchedule,
                    Address = order.Address,
                    PhoneNumber = order.PhoneNumber,
                    Userid = technicianid // Gán technicianid từ request vào userid của MaintenanceTask
                };

                await _unitOfWork.Context.Set<Mission>().AddAsync(newTask);
                await _unitOfWork.CommitAsync();

                // Chuẩn bị response
                var response = new AssigningTechnicianResponse
                {
                    Id = newTask.Id,
                    MissionName = newTask.MissionName,
                    MissionDescription = newTask.MissionDescription,
                    Status = newTask.Status,
                    MissionSchedule = newTask.MissionSchedule,
                    TechnicianId = technicianid,
                    TechnicianName = technician.FullName ?? "Unknown",
                    Address = newTask.Address,
                    PhoneNumber = newTask.PhoneNumber,
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

        public async Task<ApiResponse> AssigningTechnicianBooking(Guid bookingid, Guid technicianid, AssignTechBookingRequest request)
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
                            predicate: b => b.Id == bookingid && (b.IsAssigned == false),
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
                    predicate: t => t.Id.Equals(technicianid) &&
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
                    predicate: m => m.Userid == technicianid &&
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

                // Tạo nhiệm vụ mới
                var newTask = new Mission
                {
                    Id = Guid.NewGuid(),
                    BookingId = bookingid,
                    MissionName = request.MissionName,
                    MissionDescription = request.MissionDescription,
                    Status = MissionStatusEnum.NotStarted.GetDescriptionFromEnum(),
                    IsDelete = false,
                    MissionSchedule = booking.ScheduleDate,
                    Userid = technicianid,
                    Address = booking.Address,
                    PhoneNumber = booking.PhoneNumber,
                };

                await _unitOfWork.Context.Set<Mission>().AddAsync(newTask);

                // Cập nhật trạng thái booking đã được gán
                booking.IsAssigned = true;
                _unitOfWork.Context.Set<Booking>().Update(booking);

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
                    TechnicianId = technicianid,
                    TechnicianName = technician.UserName,
                    BookingId = bookingid,
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

        public async Task<ApiResponse> BookingSchedule(List<Guid> serviceid, Guid orderid, BookingScheduleRequest request)
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

                // Kiểm tra ScheduleDate phải trễ hơn thời gian hiện tại theo SEATime
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
                    predicate: o => o.Id.Equals(orderid) &&
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
                // Kiểm tra xem có booking nào trùng ngày không
                var existingBooking = await _unitOfWork.GetRepository<Booking>().GetListAsync(
                    predicate: b => b.OrderId.Equals(orderid) && b.ScheduleDate.Value.Date == request.ScheduleDate.Value.Date);

                if (existingBooking.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "A booking already exists for this order on the selected date.",
                        data = null
                    };
                }

                // Kiểm tra số lượng booking của order này
                var existingBookings = await _unitOfWork.GetRepository<Booking>().GetListAsync(
                    predicate: b => b.OrderId.Equals(orderid));

                // Kiểm tra nếu số lượng booking đã có > 3 thì serviceid không được rỗng
                bool isOverLimit = existingBookings.Count() >= 3;
                if (isOverLimit && (serviceid == null || !serviceid.Any()))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Service package selection is required when there are more than 3 bookings.",
                        data = null
                    };
                }

                // Tính tổng TotalPrice khi có hơn 3 booking và kiểm tra serviceid
                decimal totalPrice = 0;
                List<ServicePackage> selectedServices = new();
                if (isOverLimit)
                {
                    selectedServices = (List<ServicePackage>)await _unitOfWork.GetRepository<ServicePackage>().GetListAsync(
                        predicate: sp => serviceid.Contains(sp.Id) &&
                                         sp.Status.Equals(ServicePackageStatus.Available.GetDescriptionFromEnum()) &&
                                         sp.IsDelete == false);

                    // Kiểm tra nếu có serviceid không hợp lệ
                    var invalidServices = serviceid.Except(selectedServices.Select(sp => sp.Id)).ToList();
                    if (invalidServices.Any())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Invalid servicepackgeid.",
                            data = null
                        };
                    }

                    totalPrice = selectedServices.Sum(sp => sp.Price);
                }

                // Chọn trạng thái booking dựa vào số lượng booking hiện tại
                string bookingStatus = isOverLimit
                    ? BookingStatusEnum.NOTPAID.ToString()
                    : BookingStatusEnum.FREE.ToString();

                // Tạo mới Booking
                var newBooking = new Booking
                {
                    Id = Guid.NewGuid(),
                    ScheduleDate = request.ScheduleDate,
                    Status = bookingStatus, // Đặt status theo yêu cầu
                    Address = request.Address,
                    PhoneNumber = request.PhoneNumber,
                    FullName = request.FullName,
                    TotalPrice = totalPrice,  // Gán totalPrice mới tính được
                    UserId = userId,
                    OrderId = orderid,
                    IsAssigned = false
                };

                await _unitOfWork.Context.Set<Booking>().AddAsync(newBooking);

                // Thêm BookingDetail nếu có serviceid
                if (isOverLimit)
                {
                    var bookingDetails = selectedServices.Select(service => new BookingDetail
                    {
                        Id = Guid.NewGuid(),
                        ServicePackageId = service.Id,
                        BookingId = newBooking.Id
                    }).ToList();

                    await _unitOfWork.Context.Set<BookingDetail>().AddRangeAsync(bookingDetails);
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
                    OrderId = orderid
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
                    FullName = b.User?.FullName ?? "Unknown",
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

        public async Task<ApiResponse> GetListTech()
        {
            try
            {
                // Lấy danh sách các User có Role là Technician và chưa bị xóa
                var technicians = await _unitOfWork.GetRepository<User>().GetListAsync(
                    predicate: u => u.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false);

                // Chuyển đổi dữ liệu sang GetListTechResponse
                var responseList = technicians.Select(t => new GetListTechResponse
                {
                    TechId = t.Id,
                    TechName = t.UserName,
                    FullName = t.FullName
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "List of technicians retrieved successfully.",
                    data = responseList
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving technician list.",
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
