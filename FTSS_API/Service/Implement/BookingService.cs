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
using FTSS_API.Payload.Request.Pay;
using FTSS_API.Payload.Response.Pay.Payment;
using Azure;

namespace FTSS_API.Service.Implement
{
    public class BookingService : BaseService<BookingService>, IBookingService
    {
        public IPaymentService _paymentService { get; set; }
        public BookingService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<BookingService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor, IPaymentService paymentService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _paymentService = paymentService;
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
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }
                // Kiểm tra điều kiện nhập vào
                if (!string.IsNullOrWhiteSpace(request.MissionName) && request.MissionName.Length < 3)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Tên nhiệm vụ phải có ít nhất 3 ký tự.",
                        data = null
                    };
                }

                if (!string.IsNullOrWhiteSpace(request.MissionDescription) && request.MissionDescription.Length < 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Mô tả nhiệm vụ phải có ít nhất 10 ký tự.",
                        data = null
                    };
                }
                if (request.MissionSchedule == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Lịch trình nhiệm vụ không được để trống.",
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
                        message = "Lịch trình nhiệm vụ phải sau thời gian hiện tại.",
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
                        message = "Kỹ thuật viên không hợp lệ, không tồn tại hoặc không phải là kỹ thuật viên.",
                        data = null
                    };
                }
                // Kiểm tra technician đã có mission vào ngày này chưa
                //var existingMission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                //    predicate: m => m.Userid.Equals(request.TechnicianId) &&
                //                    m.MissionSchedule.Value.Date == request.MissionSchedule.Value.Date &&
                //                    m.IsDelete == false);

                //if (existingMission != null)
                //{
                //    return new ApiResponse
                //    {
                //        status = StatusCodes.Status400BadRequest.ToString(),
                //        message = $"Kỹ thuật viên đã được phân công nhiệm vụ vào ngày {request.MissionSchedule.Value.Date:dd/MM/yyyy}.",
                //        data = null
                //    };
                //}
                // Truy vấn Order theo orderid để lấy Address và PhoneNumber
                var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                    predicate: o => o.Id.Equals(request.OrderId) && o.IsDelete == false);

                if (order == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Đơn hàng không tồn tại hoặc đã bị xóa.",
                        data = null
                    };
                }

                // ✅ Kiểm tra Address có chứa "Hồ Chí Minh" không
                if (string.IsNullOrWhiteSpace(order.Address) ||
                    !order.Address.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Chỉ có thể phân công đơn hàng ở khu vực Hồ Chí Minh.",
                        data = null
                    };
                }

                // Kiểm tra trạng thái Order phải là PROCESSED
                if (!order.Status.Equals(OrderStatus.PROCESSED.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Trạng thái đơn hàng phải là PROCESSED trước khi phân công kỹ thuật viên.",
                        data = null
                    };
                }

                // ✅ Kiểm tra Order đã được phân công chưa
                if (order.IsAssigned == true)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Đơn hàng đã được phân công cho kỹ thuật viên.",
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
                    TechnicianName = technician.FullName ?? "Không xác định",
                    Address = newTask.Address,
                    PhoneNumber = newTask.PhoneNumber,
                    OrderId = newTask.OrderId
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Phân công kỹ thuật viên thành công.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi phân công kỹ thuật viên.",
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
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }

                // Kiểm tra điều kiện nhập vào
                if (!string.IsNullOrWhiteSpace(request.MissionName) && request.MissionName.Length < 3)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Tên nhiệm vụ phải có ít nhất 3 ký tự.",
                        data = null
                    };
                }

                if (!string.IsNullOrWhiteSpace(request.MissionDescription) && request.MissionDescription.Length < 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Mô tả nhiệm vụ phải có ít nhất 10 ký tự.",
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
                        message = "Booking không tồn tại hoặc đã được phân công.",
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
                        message = "Kỹ thuật viên không hợp lệ, không tồn tại hoặc không phải là kỹ thuật viên.",
                        data = null
                    };
                }

                // Kiểm tra kỹ thuật viên đã có nhiệm vụ trong ngày đó chưa
                //var assignedMission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                //    predicate: m => m.Userid == request.TechnicianId &&
                //                    m.MissionSchedule.HasValue &&
                //                    m.MissionSchedule.Value.Date == booking.ScheduleDate.Value.Date);

                //if (assignedMission != null)
                //{
                //    return new ApiResponse
                //    {
                //        status = StatusCodes.Status400BadRequest.ToString(),
                //        message = $"Kỹ thuật viên đã được phân công nhiệm vụ vào ngày {booking.ScheduleDate.Value.Date:dd/MM/yyyy}.",
                //        data = null
                //    };
                //}
                // ✅ Kiểm tra Booking đã được phân công chưa
                if (booking.IsAssigned == true)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Booking đã được phân công cho kỹ thuật viên.",
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
                    UserName = bookingUser?.UserName ?? "Không xác định",
                    FullName = bookingUser?.FullName ?? "Không xác định",
                    Address = newTask.Address,
                    PhoneNumber = newTask.PhoneNumber
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Phân công kỹ thuật viên cho booking thành công.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi phân công kỹ thuật viên cho booking.",
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
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }

                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Địa chỉ không được để trống.",
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
                        message = "Ngày lịch trình không được để trống.",
                        data = null
                    };
                }

                var currentSEATime = TimeUtils.GetCurrentSEATime();
                if (request.ScheduleDate <= currentSEATime)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Ngày lịch trình phải sau thời gian hiện tại.",
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
                        message = "Đơn hàng không tồn tại hoặc chưa hoàn thành.",
                        data = null
                    };
                }

                // Cho phép đặt lại nếu booking cũ ở trạng thái REFUNDED hoặc REFUNDING
                var disallowedStatuses = new[] {
                                BookingStatusEnum.NOTPAID.GetDescriptionFromEnum(),
                                BookingStatusEnum.PAID.GetDescriptionFromEnum(),
                                BookingStatusEnum.FREE.GetDescriptionFromEnum()
                            };

                var existingBooking = await _unitOfWork.GetRepository<Booking>().GetListAsync(
                    predicate: b => b.OrderId.Equals(request.OrderId) &&
                                    b.ScheduleDate.Value.Date == request.ScheduleDate.Value.Date &&
                                    disallowedStatuses.Contains(b.Status) // Chỉ chặn nếu có status khác REFUNDED, REFUNDING
                );

                if (existingBooking.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Đơn hàng đã có booking vào ngày này.",
                        data = null
                    };
                }

                // Kiểm tra số lượng booking đã có của order này
                bool isEligibleForFreeBooking = order.IsEligible == true;

                // Xác định trạng thái booking
                string bookingStatus = isEligibleForFreeBooking
                    ? BookingStatusEnum.FREE.ToString()
                    : BookingStatusEnum.NOTPAID.ToString();

                // Tính TotalPrice nếu không miễn phí
                decimal totalPrice = 0;
                List<ServicePackage> selectedServices = new();
                List<Guid> serviceIds = request.ServiceIds?.Select(s => s.ServiceId).ToList() ?? new List<Guid>();

                if (isEligibleForFreeBooking)
                {
                    // Nếu là Booking FREE, lấy tất cả ServicePackage có trạng thái Available
                    selectedServices = (List<ServicePackage>)await _unitOfWork.GetRepository<ServicePackage>().GetListAsync(
                        predicate: sp => sp.Status.Equals(ServicePackageStatus.Available.GetDescriptionFromEnum()) &&
                                         sp.IsDelete == false);

                    totalPrice = 0; // FREE booking luôn có giá 0
                }
                else if (serviceIds.Any())
                {

                    // Nếu không FREE, chỉ lấy các ServicePackage theo danh sách serviceIds
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
                            message = "ID gói dịch vụ không hợp lệ.",
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
                    BookingCode = GenerateBookingCode(),
                    UserId = userId,
                    OrderId = request.OrderId,
                    IsAssigned = false
                };

                await _unitOfWork.GetRepository<Booking>().InsertAsync(newBooking);

                // Thêm BookingDetail cho Booking
                if (selectedServices.Any())
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
                if (!isEligibleForFreeBooking)
                {
                    var paymentRequest = new CreatePaymentRequest
                    {
                        BookingId = newBooking.Id,
                        PaymentMethod = request.PaymentMethod // Cần nhận PaymentMethod từ request
                    };

                    var paymentResponse = await _paymentService.CreatePayment(paymentRequest);

                    // Kiểm tra null và status code
                    if (paymentResponse == null || paymentResponse.status != StatusCodes.Status200OK.ToString())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status500InternalServerError.ToString(),
                            message = "Đặt lịch thành công nhưng lỗi thanh toán",
                            data = paymentResponse?.data
                        };
                    }

                    // Lấy URL từ response.data (không cần cast)
                    string paymentUrl = string.Empty;
                    if (paymentResponse.data is Dictionary<string, object> dict)
                    {
                        paymentUrl = dict.TryGetValue("PaymentURL", out var url) ? url.ToString() : "";
                    }
                    else if (paymentResponse.data != null)
                    {
                        // Dùng reflection nếu cần
                        var type = paymentResponse.data.GetType();
                        var prop = type.GetProperty("PaymentURL") ?? type.GetProperty("paymentUrl");
                        paymentUrl = prop?.GetValue(paymentResponse.data)?.ToString() ?? "";
                    }

                    // Chuẩn bị respons
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
                        OrderId = request.OrderId,
                        Url = paymentUrl,
                        BookingCode = newBooking.BookingCode,
                        PaymentMethod = request.PaymentMethod,
                    };

                    return new ApiResponse
                    {
                        status = StatusCodes.Status201Created.ToString(),
                        message = "Đặt lịch booking thành công.",
                        data = response
                    };
                }

                // Trả về response cho booking FREE
                var freeBookingResponse = new BookingScheduleResponse
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
                    OrderId = request.OrderId,
                    BookingCode = newBooking.BookingCode,
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status201Created.ToString(),
                    message = "Đặt lịch booking thành công.",
                    data = freeBookingResponse
                };


            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi đặt lịch booking.",
                    data = ex.Message
                };
            }

        }

        public async Task<ApiResponse> GetBookingById(Guid bookingId)
        {
            try
            {
                var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                    predicate: b => b.Id == bookingId,
                    include: b => b.Include(x => x.User)
              .Include(x => x.BookingDetails)
                  .ThenInclude(bd => bd.ServicePackage)
              .Include(x => x.Missions)
                );

                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy booking.",
                        data = null
                    };
                }

                var response = new GetBookingById
                {
                    Id = booking.Id,
                    ScheduleDate = booking.ScheduleDate,
                    Status = booking.Status,
                    MissionStatus = booking.Missions.FirstOrDefault(m => m.IsDelete == false)?.Status,
                    UserId = booking.UserId,
                    UserName = booking.User?.UserName ?? "Không xác định",
                    FullName = booking.User?.FullName ?? "Không xác định",
                    Address = booking.Address,
                    PhoneNumber = booking.PhoneNumber,
                    TotalPrice = booking.TotalPrice,
                    BookingCode = booking.BookingCode,
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
                    message = "Thành công",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = $"Đã xảy ra lỗi: {ex.Message}",
                    data = null
                };
            }
        }
        public async Task<ApiResponse> GetBookingById(string bookingCode)
        {
            try
            {
                var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                    predicate: b => b.BookingCode == bookingCode,
                    include: b => b.Include(x => x.User)
                                   .Include(x => x.BookingDetails)
                                   .ThenInclude(bd => bd.ServicePackage)
                );

                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy booking.",
                        data = null
                    };
                }

                var response = new GetBookingById
                {
                    Id = booking.Id,
                    ScheduleDate = booking.ScheduleDate,
                    Status = booking.Status,
                    MissionStatus = booking.Missions.FirstOrDefault(m => m.IsDelete == false)?.Status,
                    UserId = booking.UserId,
                    UserName = booking.User?.UserName ?? "Không xác định",
                    FullName = booking.User?.FullName ?? "Không xác định",
                    Address = booking.Address,
                    PhoneNumber = booking.PhoneNumber,
                    TotalPrice = booking.TotalPrice,
                    BookingCode = booking.BookingCode,
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
                    message = "Thành công",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = $"Đã xảy ra lỗi: {ex.Message}",
                    data = null
                };
            }
        }

        public async Task<ApiResponse> GetDateUnavailable()
        {
            try
            {
                var unavailableDates = await _unitOfWork.GetRepository<Mission>()
                    .GetListAsync(
                        predicate: m => m.MissionSchedule != null && (m.IsDelete == false || m.IsDelete == null),
                        selector: m => m.MissionSchedule.Value.Date
                    );

                var groupedUnavailableDates = unavailableDates
                    .GroupBy(date => date)
                    .Where(group => group.Count() >= 5)
                    .Select(group => new GetDateUnavailableResponse { ScheduleDate = group.Key })
                    .ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy danh sách ngày không khả dụng thành công.",
                    data = groupedUnavailableDates
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy danh sách ngày không khả dụng.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetListBookingForManager(int pageNumber, int pageSize, string? status,string? missionstatus, bool? isAscending, bool? isAssigned)
        {
            try
            {
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
                        message = "Bạn không có quyền thực hiện thao tác này.",
                        data = null
                    };
                }

                Expression<Func<Booking, bool>> filter = b =>
                    (string.IsNullOrEmpty(status) || b.Status == status) &&
                    (isAssigned == null || b.IsAssigned == isAssigned);

                Func<IQueryable<Booking>, IOrderedQueryable<Booking>> orderBy = query =>
                    isAscending == true ? query.OrderBy(b => b.ScheduleDate) : query.OrderByDescending(b => b.ScheduleDate);

                var bookings = await _unitOfWork.GetRepository<Booking>().GetPagingListAsync(
                    predicate: filter,
                    orderBy: orderBy,
                    include: b => b.Include(x => x.User)
                                   .Include(x => x.Missions),
                    page: pageNumber,
                    size: pageSize
                );

                var response = bookings.Items.Select(b => new GetListBookingForManagerResponse
                {
                    Id = b.Id,
                    ScheduleDate = b.ScheduleDate,
                    Status = b.Status,
                    MissionStatus = b.Missions.FirstOrDefault(m => m.IsDelete == false)?.Status,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    bookingCode = b.BookingCode,
                    TotalPrice = b.TotalPrice,
                    UserId = b.User?.Id,
                    UserName = b.User?.UserName ?? "Không xác định",
                    FullName = b.FullName,
                    OrderId = b.OrderId,
                    IsAssigned = b.IsAssigned
                }).Where(r => string.IsNullOrEmpty(missionstatus) || r.MissionStatus == missionstatus).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy danh sách booking thành công.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy danh sách booking.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetListBookingForUser(int pageNumber, int pageSize, string? status, string? missionstatus, bool? isAscending)
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
                        message = "Bạn không có quyền thực hiện thao tác này.",
                        data = null
                    };
                }

                // Lấy danh sách booking phân trang
                var bookingPaginate = await _unitOfWork.GetRepository<Booking>().GetPagingListAsync(
                    predicate: b => b.UserId == userId && (string.IsNullOrEmpty(status) || b.Status == status),
                    orderBy: isAscending == true
                        ? q => q.OrderBy(b => b.ScheduleDate)
                        : q => q.OrderByDescending(b => b.ScheduleDate),
                    include: q => q.Include(b => b.BookingDetails)
                                   .ThenInclude(bd => bd.ServicePackage)
                                   .Include(b => b.Missions),
                    page: pageNumber,
                    size: pageSize
                );

                var response = bookingPaginate.Items.Select(b => new GetListBookingForUserResponse
                {
                    Id = b.Id,
                    ScheduleDate = b.ScheduleDate,
                    Status = b.Status,
                    MissionStatus = b.Missions.FirstOrDefault(m => m.IsDelete == false)?.Status,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    BookingCode = b.BookingCode,
                    TotalPrice = b.TotalPrice,
                    OrderId = b.OrderId,
                    IsAssigned = b.IsAssigned,
                    Services = b.BookingDetails.Select(bd => new ServicePackageResponse
                    {
                        Id = bd.ServicePackage.Id,
                        ServiceName = bd.ServicePackage.ServiceName,
                        Price = bd.ServicePackage.Price
                    }).ToList()
                }).Where(r => string.IsNullOrEmpty(missionstatus) || r.MissionStatus == missionstatus).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy danh sách booking thành công.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy danh sách booking.",
                    data = ex.Message
                };
            }
        }


        public async Task<ApiResponse> GetListMissionForManager(int pageNumber, int pageSize, string? status, bool? isAscending)
        {
            try
            {
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
                        message = "Bạn không có quyền thực hiện thao tác này.",
                        data = null
                    };
                }

                Expression<Func<Mission, bool>> filter = m =>
                    (string.IsNullOrEmpty(status) || m.Status == status) &&
                    (m.IsDelete == false || m.IsDelete == null);

                Func<IQueryable<Mission>, IOrderedQueryable<Mission>> orderBy = query =>
                    isAscending == true ? query.OrderBy(m => m.MissionSchedule) : query.OrderByDescending(m => m.MissionSchedule);

                var missions = await _unitOfWork.GetRepository<Mission>().GetPagingListAsync(
                    predicate: filter,
                    orderBy: orderBy,
                    include: m => m.Include(x => x.User)
                                   .Include(x => x.Order)
                                   .Include(x => x.Booking),
                    page: pageNumber,
                    size: pageSize
                );

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
                    OrderId = m.OrderId,
                    TechnicianId = m.Userid,
                    TechnicianName = m.User?.FullName ?? "Không xác định",
                    OrderCode = m.Order?.OrderCode ?? "Không có",
                    BookingCode = m.Booking?.BookingCode ?? "Không có"
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy danh sách nhiệm vụ thành công.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy danh sách nhiệm vụ.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetListTaskTech(int pageNumber, int pageSize, string? status, bool? isAscending)
        {
            Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);

            // Kiểm tra quyền truy cập
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
                    message = "Bạn không có quyền thực hiện thao tác này.",
                    data = null
                };
            }

            var taskRepo = _unitOfWork.GetRepository<Mission>();

            var query = taskRepo.GetListAsync(
                                     predicate: t => t.Userid == userId &&
                                                     (string.IsNullOrEmpty(status) || t.Status == status) &&
                                                     t.IsDelete == false,
                                     include: t => t.Include(x => x.User)
                                                    .Include(x => x.Booking)
                                                    .Include(x => x.Order)
                                 );

            var taskList = await query;

            taskList = isAscending == true
                ? taskList.OrderBy(x => x.MissionSchedule).ToList()
                : taskList.OrderByDescending(x => x.MissionSchedule).ToList();

            var paginatedList = taskList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Lấy FullName theo điều kiện
            var response = new List<GetListTaskTechResponse>();

            foreach (var t in paginatedList)
            {
                string fullName = "Không xác định";

                if (t.BookingId.HasValue && !t.OrderId.HasValue)
                {
                    // Nếu BookingId có giá trị và OrderId là null, lấy User từ Booking
                    var bookingUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        predicate: u => u.Id == t.Booking!.UserId
                    );
                    fullName = bookingUser?.FullName ?? fullName;
                }
                else if (t.OrderId.HasValue && !t.BookingId.HasValue)
                {
                    // Nếu OrderId có giá trị và BookingId là null, lấy User từ Order
                    var orderUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        predicate: u => u.Id == t.Order!.UserId
                    );
                    fullName = orderUser?.FullName ?? fullName;
                }
                else if (t.User != null)
                {
                    // Nếu không thuộc hai trường hợp trên, lấy FullName từ Mission.User
                    fullName = t.User.FullName;
                }

                response.Add(new GetListTaskTechResponse
                {
                    Id = t.Id,
                    MissionName = t.MissionName,
                    MissionDescription = t.MissionDescription,
                    Status = t.Status,
                    IsDelete = t.IsDelete,
                    MissionSchedule = t.MissionSchedule,
                    FullName = fullName,
                    Address = t.Address,
                    PhoneNumber = t.PhoneNumber
                });
            }

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Lấy danh sách công việc thành công.",
                data = response
            };
        }

        public async Task<ApiResponse> GetListTech(GetListTechRequest request)
        {
            try
            {
                DateTime? requestedDate = request.ScheduleDate?.Date;

                var allTechnicians = await _unitOfWork.GetRepository<User>().GetListAsync(
                    predicate: u => u.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

                var missionsOnRequestedDate = await _unitOfWork.GetRepository<Mission>().GetListAsync(
                    predicate: m => m.MissionSchedule.Value.Date == request.ScheduleDate.Value.Date &&
                                    (m.IsDelete == false || m.IsDelete == null) &&
                                    (m.Status == MissionStatusEnum.Processing.GetDescriptionFromEnum() ||
                                     m.Status == MissionStatusEnum.NotStarted.GetDescriptionFromEnum()));

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
                    message = "Lấy danh sách kỹ thuật viên khả dụng thành công.",
                    data = availableTechnicians
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy danh sách kỹ thuật viên.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetServicePackage(int pageNumber, int pageSize, bool? isAscending)
        {
            var servicePackages = await _unitOfWork.GetRepository<ServicePackage>().GetListAsync(
                predicate: sp => sp.Status == ServicePackageStatus.Available.ToString() && sp.IsDelete == false,
                orderBy: isAscending == true ? sp => sp.OrderBy(x => x.ServiceName) : sp => sp.OrderByDescending(x => x.ServiceName)
            );

            var paginatedList = servicePackages
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = paginatedList.Select(sp => new GetServicePackageResponse
            {
                Id = sp.Id,
                ServiceName = sp.ServiceName,
                Price = sp.Price
            }).ToList();

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Lấy danh sách gói dịch vụ khả dụng thành công.",
                data = response
            };
        }

        public async Task<ApiResponse> UpdateMission(Guid missionid, UpdateMissionRequest request)
        {
            try
            {
                var mission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                    predicate: m => m.Id.Equals(missionid) && m.IsDelete == false);

                if (mission == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy nhiệm vụ.",
                        data = null
                    };
                }

                if (!string.IsNullOrWhiteSpace(request.MissionName) && request.MissionName.Length < 3)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Tên nhiệm vụ phải có ít nhất 3 ký tự.",
                        data = null
                    };
                }

                if (!string.IsNullOrWhiteSpace(request.MissionDescription) && request.MissionDescription.Length < 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Mô tả nhiệm vụ phải có ít nhất 10 ký tự.",
                        data = null
                    };
                }

                if (request.MissionSchedule.HasValue)
                {
                    var currentSEATime = TimeUtils.GetCurrentSEATime();
                    if (request.MissionSchedule <= currentSEATime)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Lịch trình nhiệm vụ phải sau thời gian hiện tại.",
                            data = null
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.Address) && !request.Address.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Chỉ cho phép nhiệm vụ ở khu vực Hồ Chí Minh.",
                        data = null
                    };
                }

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && request.PhoneNumber.Length < 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Số điện thoại phải có ít nhất 10 chữ số.",
                        data = null
                    };
                }

                bool isTechnicianChanged = request.TechnicianId.HasValue && request.TechnicianId.Value != mission.Userid;
                bool isScheduleChanged = request.MissionSchedule.HasValue && request.MissionSchedule.Value.Date != mission.MissionSchedule.Value.Date;

                if (isTechnicianChanged)
                {
                    var technician = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        predicate: t => t.Id.Equals(request.TechnicianId.Value) &&
                                        t.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                        t.IsDelete == false &&
                                        t.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()));

                    if (technician == null)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Kỹ thuật viên không hợp lệ, không tồn tại hoặc không phải là kỹ thuật viên.",
                            data = null
                        };
                    }

                    if (!isScheduleChanged)
                    {
                        var existingMissionForNewTech = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                            predicate: m => m.Userid.Equals(request.TechnicianId) &&
                                            m.MissionSchedule.Value.Date == mission.MissionSchedule.Value.Date &&
                                            m.IsDelete == false);

                        if (existingMissionForNewTech != null)
                        {
                            return new ApiResponse
                            {
                                status = StatusCodes.Status400BadRequest.ToString(),
                                message = $"Kỹ thuật viên đã được phân công nhiệm vụ vào ngày {mission.MissionSchedule.Value.Date:dd/MM/yyyy}.",
                                data = null
                            };
                        }
                    }

                    mission.Userid = request.TechnicianId;
                }

                if (isScheduleChanged && !isTechnicianChanged)
                {
                    var existingMissionForOldTech = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                        predicate: m => m.Userid.Equals(mission.Userid) &&
                                        m.MissionSchedule.Value.Date == request.MissionSchedule.Value.Date &&
                                        m.IsDelete == false);

                    if (existingMissionForOldTech != null)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = $"Kỹ thuật viên đã được phân công nhiệm vụ vào ngày {request.MissionSchedule.Value.Date:dd/MM/yyyy}.",
                            data = null
                        };
                    }
                }

                if (isTechnicianChanged && isScheduleChanged)
                {
                    var existingMissionForNewTech = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                        predicate: m => m.Userid.Equals(request.TechnicianId) &&
                                        m.MissionSchedule.Value.Date == request.MissionSchedule.Value.Date &&
                                        m.IsDelete == false);

                    if (existingMissionForNewTech != null)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = $"Kỹ thuật viên đã được phân công nhiệm vụ vào ngày {request.MissionSchedule.Value.Date:dd/MM/yyyy}.",
                            data = null
                        };
                    }

                    mission.Userid = request.TechnicianId;
                }

                mission.MissionName = !string.IsNullOrWhiteSpace(request.MissionName) ? request.MissionName : mission.MissionName;
                mission.MissionDescription = !string.IsNullOrWhiteSpace(request.MissionDescription) ? request.MissionDescription : mission.MissionDescription;
                mission.MissionSchedule = request.MissionSchedule ?? mission.MissionSchedule;
                mission.Address = !string.IsNullOrWhiteSpace(request.Address) ? request.Address : mission.Address;
                mission.PhoneNumber = !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber : mission.PhoneNumber;

                _unitOfWork.GetRepository<Mission>().UpdateAsync(mission);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Cập nhật nhiệm vụ thành công.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi cập nhật nhiệm vụ.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> UpdateStatusMission(Guid missionId, string status)
        {
            try
            {
                var mission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                    predicate: m => m.Id.Equals(missionId) && m.IsDelete == false);

                if (mission == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy nhiệm vụ.",
                        data = null
                    };
                }

                if (!Enum.TryParse(status, out MissionStatusEnum missionStatus))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Trạng thái không hợp lệ.",
                        data = null
                    };
                }

                // Cập nhật trạng thái của nhiệm vụ
                mission.Status = missionStatus.ToString();

                // Chỉ cập nhật Order nếu Mission có OrderId và không có BookingId
                if (mission.OrderId.HasValue && mission.BookingId == null)
                {
                    string? orderStatus = missionStatus switch
                    {
                        MissionStatusEnum.Processing => OrderStatus.PENDING_DELIVERY.ToString(),
                        MissionStatusEnum.Done => OrderStatus.COMPLETED.ToString(),
                        MissionStatusEnum.Cancel => OrderStatus.CANCELLED.ToString(),
                        _ => null
                    };

                    if (orderStatus != null)
                    {
                        var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                            predicate: o => o.Id == mission.OrderId.Value && o.IsDelete == false);

                        if (order != null)
                        {
                            order.Status = orderStatus;
                            order.ModifyDate = DateTime.UtcNow;
                            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
                        }
                    }
                }

                _unitOfWork.GetRepository<Mission>().UpdateAsync(mission);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Cập nhật trạng thái nhiệm vụ thành công.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi cập nhật trạng thái nhiệm vụ.",
                    data = ex.Message
                };
            }
        }


        private string GenerateBookingCode()
        {
            var now = DateTime.UtcNow; // Lấy thời gian hiện tại theo UTC để tránh trùng lặp
            string timestamp = now.ToString("yyyyMMddHHmmssfff"); // VD: 20250322153045999
            string randomLetters = GenerateRandomLetters(7).Trim(); // Loại bỏ khoảng trắng
            string lastThreeDigits = timestamp[^3..]; // Lấy 3 số cuối của timestamp

            return $"{randomLetters}{lastThreeDigits}"; // VD: ABC999
        }
        private string GenerateRandomLetters(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var random = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        }

        public async Task<ApiResponse> GetMissionById(Guid missionid)
        {
            try
            {
                // Tìm mission theo ID, include Booking và Order để truy vấn nhanh hơn
                var mission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                    predicate: m => m.Id == missionid && (m.IsDelete == false || m.IsDelete == null),
                    include: m => m.Include(x => x.Booking).Include(x => x.Order)
                );

                if (mission == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy nhiệm vụ.",
                        data = null
                    };
                }

                string fullName = "Không xác định";

                if (mission.BookingId.HasValue && !mission.OrderId.HasValue)
                {
                    // Nếu BookingId có giá trị và OrderId là null, lấy User từ Booking
                    var bookingUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        predicate: u => u.Id == mission.Booking!.UserId
                    );
                    fullName = bookingUser?.FullName ?? fullName;
                }
                else if (mission.OrderId.HasValue && !mission.BookingId.HasValue)
                {
                    // Nếu OrderId có giá trị và BookingId là null, lấy User từ Order
                    var orderUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        predicate: u => u.Id == mission.Order!.UserId
                    );
                    fullName = orderUser?.FullName ?? fullName;
                }
                else if (mission.User != null)
                {
                    // Nếu không thuộc hai trường hợp trên, lấy FullName từ Mission.User
                    fullName = mission.User.FullName;
                }

                // Tạo response
                var response = new GetListTaskTechResponse
                {
                    Id = mission.Id,
                    MissionName = mission.MissionName,
                    MissionDescription = mission.MissionDescription,
                    Status = mission.Status,
                    IsDelete = mission.IsDelete,
                    MissionSchedule = mission.MissionSchedule,
                    FullName = fullName, // Giá trị FullName theo logic trên
                    Address = mission.Address,
                    PhoneNumber = mission.PhoneNumber
                };

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy thông tin nhiệm vụ thành công.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy thông tin nhiệm vụ.",
                    data = ex.Message
                };
            }
        }
        public async Task<ApiResponse> UpdateBooking(Guid bookingId, UpdateBookingRequest request)
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
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }
                var bookingRepo = _unitOfWork.GetRepository<Booking>();
                var bookingDetailRepo = _unitOfWork.GetRepository<BookingDetail>();
                var serviceRepo = _unitOfWork.GetRepository<ServicePackage>();

                var booking = await bookingRepo.SingleOrDefaultAsync(
                    predicate: b => b.Id == bookingId && b.IsAssigned == false);

                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy booking hoặc booking đã được phân công.",
                        data = null
                    };
                }
                if (booking.UserId != userId)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status403Forbidden.ToString(),
                        message = "Bạn không có quyền cập nhật booking này.",
                        data = null
                    };
                }

                // Cập nhật các trường nếu được truyền
                if (request.ScheduleDate.HasValue)
                {
                    if (request.ScheduleDate <= TimeUtils.GetCurrentSEATime())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Ngày lịch trình phải sau thời gian hiện tại.",
                            data = null
                        };
                    }
                    booking.ScheduleDate = request.ScheduleDate;
                }

                if (!string.IsNullOrWhiteSpace(request.Address))
                {
                    booking.Address = request.Address;
                }

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    string phonePattern = @"^0\d{9}$";
                    if (!Regex.IsMatch(request.PhoneNumber, phonePattern))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Số điện thoại không hợp lệ.",
                            data = null
                        };
                    }
                    booking.PhoneNumber = request.PhoneNumber;
                }

                if (!string.IsNullOrWhiteSpace(request.FullName))
                {
                    booking.FullName = request.FullName;
                }

                bookingRepo.UpdateAsync(booking);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Cập nhật booking thành công.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi cập nhật booking.",
                    data = ex.Message
                };
            }
        }
        public async Task<ApiResponse> CancelBooking(Guid bookingId)
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
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }

                var bookingRepo = _unitOfWork.GetRepository<Booking>();
                var orderRepo = _unitOfWork.GetRepository<Order>();

                var booking = await bookingRepo.SingleOrDefaultAsync(
                    predicate: b => b.Id == bookingId,
                    include: b => b.Include(x => x.Order)
                );

                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy booking.",
                        data = null
                    };
                }

                // ❌ Không cho hủy nếu không phải booking của người dùng hiện tại
                if (booking.UserId != userId)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status403Forbidden.ToString(),
                        message = "Bạn không có quyền hủy booking này.",
                        data = null
                    };
                }

                // ❌ Không cho hủy nếu đã được assigned
                if (booking.IsAssigned == true)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Booking đã được phân công. Không thể hủy.",
                        data = null
                    };
                }

                // ✅ Cập nhật trạng thái
                booking.Status = BookingStatusEnum.REFUNDING.GetDescriptionFromEnum();
                

                // ✅ Nếu miễn phí => cập nhật order.IsEligible
                if (booking.TotalPrice == 0 && booking.OrderId.HasValue)
                {
                    booking.Status = BookingStatusEnum.REFUNDED.GetDescriptionFromEnum();
                    var order = booking.Order;
                    if (order != null)
                    {
                        order.IsEligible = true;
                        order.ModifyDate = DateTime.Now;
                        orderRepo.UpdateAsync(order);
                    }
                }

                bookingRepo.UpdateAsync(booking);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Hủy booking thành công.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = $"Đã xảy ra lỗi khi hủy booking: {ex.Message}",
                    data = null
                };
            }
        }
        public async Task<ApiResponse> UpdateBookingStatus(Guid bookingid)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role.Equals(RoleEnum.Manager.GetDescriptionFromEnum()));

                if (user == null)
                {
                    throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
                }

                var bookingRepo = _unitOfWork.GetRepository<Booking>();

                var booking = await bookingRepo.SingleOrDefaultAsync(
                    predicate: b => b.Id == bookingid,
                    include: b => b.Include(b => b.Order)
                );

                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy booking.",
                        data = null
                    };
                }

                // Chỉ cho phép cập nhật nếu trạng thái hiện tại là REFUNDING
                if (booking.Status != BookingStatusEnum.REFUNDING.GetDescriptionFromEnum())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Chỉ được cập nhật booking có trạng thái REFUNDING.",
                        data = null
                    };
                }

                // Cập nhật trạng thái thành REFUNDED
                booking.Status = BookingStatusEnum.REFUNDED.GetDescriptionFromEnum();

                bookingRepo.UpdateAsync(booking);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Cập nhật trạng thái booking thành công.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = $"Đã xảy ra lỗi khi cập nhật trạng thái booking: {ex.Message}",
                    data = null
                };
            }
        }
    }
}
