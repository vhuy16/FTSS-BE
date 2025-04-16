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
using FTSS_API.Payload.Response.SetupPackage;
using static FTSS_API.Payload.Response.Order.GetOrderResponse;

namespace FTSS_API.Service.Implement
{
    public class BookingService : BaseService<BookingService>, IBookingService
    {
        private readonly SupabaseUltils _supabaseImageService;
        public IPaymentService _paymentService { get; set; }
        public BookingService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<BookingService> logger, IMapper mapper,SupabaseUltils supabaseImageService, IHttpContextAccessor httpContextAccessor, IPaymentService paymentService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _paymentService = paymentService;
            _supabaseImageService = supabaseImageService;
            
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
                    MissionSchedule = order.InstallationDate,
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
                booking.Status = BookingStatusEnum.ASSIGNED.GetDescriptionFromEnum();
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
                var disallowedStatuses = new[]
                    {
                        PaymentStatusEnum.Completed.ToString(),
                        PaymentStatusEnum.Cancelled.ToString(),
                        PaymentStatusEnum.Processing.ToString()
                    };

                var existingPayments = await _unitOfWork.GetRepository<Payment>().GetListAsync(
                    predicate: p => p.Booking.OrderId == request.OrderId &&
                                    p.Booking.ScheduleDate.Value.Date == request.ScheduleDate.Value.Date &&
                                    disallowedStatuses.Contains(p.PaymentStatus)
                );

                if (existingPayments.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Đơn hàng đã có thanh toán cho booking vào ngày này.",
                        data = null
                    };
                }

                // Kiểm tra số lượng booking đã có của order này
                bool isEligibleForFreeBooking = order.IsEligible == true;

                // ✅ Đặt trạng thái booking mặc định là NOTASSIGN trong mọi trường hợp
                string bookingStatus = BookingStatusEnum.NOTASSIGN.ToString();

                // Tính TotalPrice nếu không miễn phí
                decimal totalPrice = 0;
                List<ServicePackage> selectedServices = new();
                List<Guid> serviceIds = request.ServiceIds?.Select(s => s.ServiceId).ToList() ?? new List<Guid>();

                if (isEligibleForFreeBooking)
                {

                    // Nếu là Booking FREE, lấy tất cả ServicePackage có trạng thái Available
                    selectedServices = (List<ServicePackage>)await _unitOfWork.GetRepository<ServicePackage>()
                        .GetListAsync(
                            predicate: sp =>
                                sp.Status.Equals(ServicePackageStatus.Available.GetDescriptionFromEnum()) &&
                                sp.IsDelete == false);


                    totalPrice = 0; // FREE booking luôn có giá 0
                }
                else if (serviceIds.Any())
                {


                    // Nếu không FREE, chỉ lấy các ServicePackage theo danh sách serviceIds
                    selectedServices = (List<ServicePackage>)await _unitOfWork.GetRepository<ServicePackage>()
                        .GetListAsync(
                            predicate: sp => serviceIds.Contains(sp.Id) &&
                                             sp.Status.Equals(ServicePackageStatus.Available
                                                 .GetDescriptionFromEnum()) &&
                                             sp.IsDelete == false);

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
                    FullName = order.RecipientName,
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

                    await _unitOfWork.GetRepository<BookingDetail>().InsertRangeAsync(bookingDetails);
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

                    // Lấy URL từ response.data (chỉ áp dụng cho booking không FREE)
                    string paymentUrl = string.Empty;
                    if (paymentResponse.data is Dictionary<string, object> dict)
                    {
                        paymentUrl = dict.TryGetValue("PaymentURL", out var url) ? url.ToString() : "";
                    }
                    else if (paymentResponse.data != null)
                    {
                        var type = paymentResponse.data.GetType();
                        var prop = type.GetProperty("PaymentURL") ?? type.GetProperty("paymentUrl");
                        paymentUrl = prop?.GetValue(paymentResponse.data)?.ToString() ?? "";
                    }

                    // Chuẩn bị response cho booking không FREE
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
                        Url = paymentUrl, // Chỉ có URL khi không FREE
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

                else
                {
                    // Tạo payment cho booking FREE nhưng không cần URL
                    var paymentRequest = new CreatePaymentRequest
                    {
                        BookingId = newBooking.Id,
                        PaymentMethod = PaymenMethodEnum.FREE.GetDescriptionFromEnum() // Gán một giá trị đặc biệt để nhận diện
                    };

                    var paymentResponse = await _paymentService.CreatePayment(paymentRequest);

                    if (paymentResponse == null || paymentResponse.status != StatusCodes.Status200OK.ToString())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status500InternalServerError.ToString(),
                            message = "Đặt lịch thành công nhưng lỗi tạo payment cho booking miễn phí",
                            data = paymentResponse?.data
                        };
                    }

                    // Chuẩn bị response cho booking FREE (không có Url)
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
                        // Không có Url ở đây
                    };

                    return new ApiResponse
                    {
                        status = StatusCodes.Status201Created.ToString(),
                        message = "Đặt lịch booking thành công.",
                        data = freeBookingResponse
                    };
                }

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
                    include: b => b
                        .Include(x => x.User)
                        .Include(x => x.BookingDetails).ThenInclude(bd => bd.ServicePackage)
                        .Include(x => x.Missions).ThenInclude(mm => mm.MissionImages)
                        .Include(x => x.Payments)
                        .Include(x => x.Order) // Include Order để truy cập SetupPackageId
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

                // Lấy thông tin payment đầu tiên nếu có
                var payment = booking.Payments.FirstOrDefault();
                var paymentResponse = payment != null
                    ? new PaymentResponse
                    {
                        PaymentId = payment.Id,
                        PaymentMethod = payment.PaymentMethod ?? "Không xác định",
                        PaymentStatus = payment.PaymentStatus ?? "Không xác định",
                        BankHolder = payment.BankHolder ?? "Unknown",
                        BankName = payment.BankName ?? "Unknown",
                        BankNumber = payment.BankNumber ?? "Unknown",
                    }
                    : new PaymentResponse();

                // Khởi tạo biến chứa SetupPackageResponse
                SetupPackageResponse? setupPackageResponse = null;

                if (booking.Order != null && booking.Order.SetupPackageId.HasValue)
                {
                    var setup = await _unitOfWork.GetRepository<SetupPackage>().SingleOrDefaultAsync(
                        predicate: s => s.Id == booking.Order.SetupPackageId && s.IsDelete == false,
                        include: s => s.Include(sp => sp.SetupPackageDetails)
                                       .ThenInclude(spd => spd.Product)
                                           .ThenInclude(p => p.Images)
                                       .Include(sp => sp.SetupPackageDetails)
                                           .ThenInclude(spd => spd.Product.SubCategory)
                    );

                    if (setup != null)
                    {
                        setupPackageResponse = new SetupPackageResponse
                        {
                            SetupPackageId = setup.Id,
                            SetupName = setup.SetupName,
                            Description = setup.Description,
                            TotalPrice = setup.Price,
                            CreateDate = setup.CreateDate,
                            ModifyDate = setup.ModifyDate,
                            Size = setup.SetupPackageDetails.FirstOrDefault()?.Product?.Size,
                            images = setup.Image ?? "",
                            IsDelete = setup.IsDelete ?? false,
                            Products = setup.SetupPackageDetails.Select(spd => new ProductResponse
                            {
                                Id = spd.Product.Id,
                                ProductName = spd.Product.ProductName,
                                Price = spd.Price ?? spd.Product.Price,
                                Quantity = spd.Quantity,
                                InventoryQuantity = spd.Product.Quantity,
                                Status = spd.Product.Status,
                                IsDelete = spd.Product.IsDelete,
                                CategoryName = spd.Product.SubCategory?.SubCategoryName ?? "Không rõ",
                                images = spd.Product?.Images?
                        .Where(img => img.IsDelete == false)
                        .OrderBy(img => img.CreateDate)
                        .Select(img => img.LinkImage)
                        .FirstOrDefault() ?? "NoImageAvailable"
                            }).ToList()
                        };
                    }
                }

                var response = new GetBookingById
                {
                    Id = booking.Id,
                    ScheduleDate = booking.ScheduleDate,
                    Status = booking.Status,
                    UserId = booking.UserId,
                    UserName = booking.User?.UserName ?? "Không xác định",
                    FullName = booking.FullName ?? "Không xác định",
                    Address = booking.Address,
                    PhoneNumber = booking.PhoneNumber,
                    TotalPrice = booking.TotalPrice,
                    BookingCode = booking.BookingCode,
                    OrderId = booking.OrderId,
                    ImageLinks = booking.Missions
                        .SelectMany(m => m.MissionImages)
                        .Where(mi => mi.IsDelete == false)
                        .Select(mi => mi.LinkImage)
                        .ToList(),
                    IsAssigned = booking.IsAssigned,
                    Services = booking.BookingDetails.Select(bd => new ServicePackageResponse
                    {
                        Id = bd.ServicePackage.Id,
                        ServiceName = bd.ServicePackage.ServiceName,
                        Price = bd.ServicePackage.Price
                    }).ToList(),
                    Payment = paymentResponse,
                    SetupPackage = setupPackageResponse
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
                                   .Include(x => x.BookingDetails).ThenInclude(bd => bd.ServicePackage)
                                   .Include(x => x.Order) // cần để lấy SetupPackageId
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

                // Lấy thông tin setup package từ Order nếu có
                SetupPackageResponse? setupPackageResponse = null;

                if (booking.Order != null && booking.Order.SetupPackageId.HasValue)
                {
                    var setup = await _unitOfWork.GetRepository<SetupPackage>().SingleOrDefaultAsync(
                        predicate: s => s.Id == booking.Order.SetupPackageId && s.IsDelete == false,
                        include: s => s.Include(sp => sp.SetupPackageDetails)
                                       .ThenInclude(spd => spd.Product)
                                           .ThenInclude(p => p.Images)
                                       .Include(sp => sp.SetupPackageDetails)
                                           .ThenInclude(spd => spd.Product.SubCategory)
                    );

                    if (setup != null)
                    {
                        setupPackageResponse = new SetupPackageResponse
                        {
                            SetupPackageId = setup.Id,
                            SetupName = setup.SetupName,
                            Description = setup.Description,
                            TotalPrice = setup.Price,
                            CreateDate = setup.CreateDate,
                            ModifyDate = setup.ModifyDate,
                            Size = setup.SetupPackageDetails.FirstOrDefault()?.Product?.Size,
                            images = setup.Image ?? "",
                            IsDelete = setup.IsDelete ?? false,
                            Products = setup.SetupPackageDetails.Select(spd => new ProductResponse
                            {
                                Id = spd.Product.Id,
                                ProductName = spd.Product.ProductName,
                                Price = spd.Price ?? spd.Product.Price,
                                Quantity = spd.Quantity,
                                InventoryQuantity = spd.Product.Quantity,
                                Status = spd.Product.Status,
                                IsDelete = spd.Product.IsDelete,
                                CategoryName = spd.Product.SubCategory?.SubCategoryName ?? "Không rõ",
                                images = spd.Product?.Images?
                                        .Where(img => img.IsDelete == false)
                                        .OrderBy(img => img.CreateDate)
                                        .Select(img => img.LinkImage)
                                        .FirstOrDefault() ?? "NoImageAvailable"
                            }).ToList()
                        };
                    }
                }

                var response = new GetBookingById
                {
                    Id = booking.Id,
                    ScheduleDate = booking.ScheduleDate,
                    Status = booking.Status,
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
                    }).ToList(),
                    SetupPackage = setupPackageResponse
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

        public async Task<ApiResponse> GetListBookingForManager(int pageNumber, int pageSize, string? status, string? paymentstatus, string? bookingcode, bool? isAscending, bool? isAssigned)
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
                    (isAssigned == null || b.IsAssigned == isAssigned) &&
                    (string.IsNullOrEmpty(bookingcode) || b.BookingCode.Contains(bookingcode)) &&
                    (string.IsNullOrEmpty(paymentstatus) || b.Payments.Any(p => p.PaymentStatus == paymentstatus));

                Func<IQueryable<Booking>, IOrderedQueryable<Booking>> orderBy = query =>
                    isAscending == true ? query.OrderBy(b => b.ScheduleDate) : query.OrderByDescending(b => b.ScheduleDate);

                var bookings = await _unitOfWork.GetRepository<Booking>().GetPagingListAsync(
                    predicate: filter,
                    orderBy: orderBy,
                    include: b => b.Include(x => x.User)
                                   .Include(x => x.Missions)
                                   .Include(x => x.Payments),
                    page: pageNumber,
                    size: pageSize
                );

                var response = bookings.Items.Select(b => new GetListBookingForManagerResponse
                {
                    Id = b.Id,
                    ScheduleDate = b.ScheduleDate,
                    Status = b.Status,
                    Address = b.Address,
                    PhoneNumber = b.PhoneNumber,
                    bookingCode = b.BookingCode,
                    TotalPrice = b.TotalPrice,
                    UserId = b.User?.Id,
                    UserName = b.User?.UserName ?? "Không xác định",
                    FullName = b.FullName,
                    OrderId = b.OrderId,
                    IsAssigned = b.IsAssigned,
                    Payment = b.Payments.Select(p => new PaymentResponse
                    {
                        PaymentId = p.Id,
                        PaymentMethod = p.PaymentMethod ?? "Không xác định",
                        PaymentStatus = p.PaymentStatus ?? "Không xác định"
                    }).FirstOrDefault() ?? new PaymentResponse()
                }).ToList();

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
        public async Task<ApiResponse> GetListBookingForUser(int pageNumber, int pageSize, string? status, string? paymentstatus, string? bookingcode, bool? isAscending)
        {
            try
            {
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

                Expression<Func<Booking, bool>> filter = b =>
                    b.UserId == userId &&
                    (string.IsNullOrEmpty(status) || b.Status == status) &&
                    (string.IsNullOrEmpty(bookingcode) || b.BookingCode.Contains(bookingcode)) &&
                    (string.IsNullOrEmpty(paymentstatus) || b.Payments.Any(p => p.PaymentStatus == paymentstatus));

                Func<IQueryable<Booking>, IOrderedQueryable<Booking>> orderBy = query =>
                    isAscending == true ? query.OrderBy(b => b.ScheduleDate) : query.OrderByDescending(b => b.ScheduleDate);

                var bookingPaginate = await _unitOfWork.GetRepository<Booking>().GetPagingListAsync(
                    predicate: filter,
                    orderBy: orderBy,
                    include: q => q.Include(b => b.BookingDetails)
                                   .ThenInclude(bd => bd.ServicePackage)
                                   .Include(b => b.Missions)
                                   .Include(b => b.Payments),
                    page: pageNumber,
                    size: pageSize
                );

                var response = bookingPaginate.Items.Select(b => new GetListBookingForUserResponse
                {
                    Id = b.Id,
                    ScheduleDate = b.ScheduleDate,
                    Status = b.Status,
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
                    }).ToList(),
                    Payment = b.Payments.Select(p => new PaymentResponse
                    {
                        PaymentId = p.Id,
                        PaymentMethod = p.PaymentMethod ?? "Không xác định",
                        PaymentStatus = p.PaymentStatus ?? "Không xác định",
                        BankHolder =p.BankHolder ?? "Unknown",
                        BankName = p.BankName ?? "Unknown",
                        BankNumber = p.BankNumber ?? "Unknown",
                    }).FirstOrDefault() ?? new PaymentResponse()
                }).ToList();

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
               .Include(x => x.Booking)
                   .ThenInclude(b => b.BookingDetails)
                       .ThenInclude(bd => bd.ServicePackage),
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
                    EndMissionSchedule = m.EndMissionSchedule,
                    Address = m.Address,
                    PhoneNumber = m.PhoneNumber,
                    CancelReason = m.CancelReason,
                    BookingId = m.BookingId,
                    OrderId = m.OrderId,
                    TechnicianId = m.Userid,
                    TechnicianName = m.User?.FullName ?? "Không xác định",
                    OrderCode = m.Order?.OrderCode ?? "Không có",
                    BookingCode = m.Booking?.BookingCode ?? "Không có",
                    FullName = m.Booking != null
        ? m.Booking.FullName
        : (m.Order?.RecipientName ?? "Không có"),
                    BookingImage = m.Booking?.BookingImage,
                    InstallationDate = m.Order?.InstallationDate,
                    Services = m.Booking?.BookingDetails
             ?.Select(s => new ServicePackageResponse
             {
                 Id = s.ServicePackage.Id,
                 ServiceName = s.ServicePackage.ServiceName,
                 Price = s.ServicePackage.Price
             }).ToList() ?? new()
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
                include: m => m.Include(x => x.User)
                               .Include(x => x.Order)
                               .Include(x => x.Booking)
                                   .ThenInclude(b => b.BookingDetails)
                                       .ThenInclude(bd => bd.ServicePackage)
            );

            var taskList = await query;

            taskList = isAscending == true
                ? taskList.OrderBy(x => x.MissionSchedule).ToList()
                : taskList.OrderByDescending(x => x.MissionSchedule).ToList();

            var paginatedList = taskList
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new List<GetListTaskTechResponse>();

            foreach (var t in paginatedList)
            {
                string fullName = "Không xác định";
                if (t.BookingId.HasValue && !t.OrderId.HasValue)
                {
                    var bookingUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        predicate: u => u.Id == t.Booking!.UserId);
                    fullName = bookingUser?.FullName ?? fullName;
                }
                else if (t.OrderId.HasValue && !t.BookingId.HasValue)
                {
                    fullName = t.Order?.RecipientName ?? fullName;
                }
                else if (t.User != null)
                {
                    fullName = t.User.FullName;
                }

                // Lấy các dịch vụ từ Booking nếu có
                var servicePackages = new List<ServicePackageResponse>();
                if (t.Booking != null)
                {
                    foreach (var detail in t.Booking.BookingDetails)
                    {
                        servicePackages.Add(new ServicePackageResponse
                        {
                            Id = detail.ServicePackage.Id,
                            ServiceName = detail.ServicePackage.ServiceName,
                            Price = detail.ServicePackage.Price
                        });
                    }
                }

                // Lấy SetupPackage nếu có
                SetupPackageResponse? setupPackage = null;
                Guid? setupPackageId = null;

                if (t.OrderId.HasValue && !t.BookingId.HasValue)
                {
                    setupPackageId = t.Order?.SetupPackageId;
                }
                else if (t.BookingId.HasValue && !t.OrderId.HasValue)
                {
                    var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                        predicate: b => b.Id == t.BookingId,
                        include: b => b.Include(bk => bk.Order)
                    );
                    setupPackageId = booking?.Order?.SetupPackageId;
                }

                if (setupPackageId.HasValue)
                {
                    var setup = await _unitOfWork.GetRepository<SetupPackage>().SingleOrDefaultAsync(
                        predicate: s => s.Id == setupPackageId && s.IsDelete == false,
                        include: s => s.Include(sp => sp.SetupPackageDetails)
                                       .ThenInclude(spd => spd.Product)
                    );

                    if (setup != null)
                    {
                        setupPackage = new SetupPackageResponse
                        {
                            SetupPackageId = setup.Id,
                            SetupName = setup.SetupName,
                            Description = setup.Description,
                            TotalPrice = setup.Price,
                            CreateDate = setup.CreateDate,
                            ModifyDate = setup.ModifyDate,
                            Size = setup.SetupPackageDetails.FirstOrDefault()?.Product?.Size,
                            images = setup.Image ?? "",
                            IsDelete = setup.IsDelete ?? false,
                            Products = setup.SetupPackageDetails.Select(spd => new ProductResponse
                            {
                                Id = spd.Product.Id,
                                ProductName = spd.Product.ProductName,
                                Price = spd.Price ?? spd.Product.Price,
                                Quantity = spd.Quantity,
                                InventoryQuantity = spd.Product.Quantity,
                                Status = spd.Product.Status,
                                IsDelete = spd.Product.IsDelete,
                                CategoryName = spd.Product.SubCategory?.SubCategoryName ?? "Không rõ",
                                images = spd.Product?.Images?
                        .Where(img => img.IsDelete == false)
                        .OrderBy(img => img.CreateDate)
                        .Select(img => img.LinkImage)
                        .FirstOrDefault() ?? "NoImageAvailable"
                            }).ToList()
                        };
                    }
                }

                response.Add(new GetListTaskTechResponse
                {
                    Id = t.Id,
                    MissionName = t.MissionName,
                    MissionDescription = t.MissionDescription,
                    Status = t.Status,
                    IsDelete = t.IsDelete,
                    MissionSchedule = t.MissionSchedule,
                    EndMissionSchedule = t.EndMissionSchedule,
                    CancelReason = t.CancelReason,
                    FullName = fullName,
                    Address = t.Address,
                    PhoneNumber = t.PhoneNumber,
                    BookingId = t.BookingId,
                    OrderId = t.OrderId,
                    BookingCode = t.Booking?.BookingCode,
                    BookingImage = t.Booking?.BookingImage,
                    OrderCode = t.Order?.OrderCode,
                    InstallationDate = t.Order?.InstallationDate,
                    Services = servicePackages,
                    SetupPackage = setupPackage!
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

        public async Task<ApiResponse> UpdateMission(Guid missionId, UpdateMissionRequest request)
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

                // Validate MissionName
                if (!string.IsNullOrWhiteSpace(request.MissionName) && request.MissionName.Length < 3)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Tên nhiệm vụ phải có ít nhất 3 ký tự.",
                        data = null
                    };
                }

                // Validate MissionDescription
                if (!string.IsNullOrWhiteSpace(request.MissionDescription) && request.MissionDescription.Length < 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Mô tả nhiệm vụ phải có ít nhất 10 ký tự.",
                        data = null
                    };
                }

                // So sánh các trường truyền vào với dữ liệu hiện tại để kiểm tra trùng
                bool isSameName = string.IsNullOrWhiteSpace(request.MissionName) || request.MissionName == mission.MissionName;
                bool isSameDesc = string.IsNullOrWhiteSpace(request.MissionDescription) || request.MissionDescription == mission.MissionDescription;
                bool isSameTech = !request.TechnicianId.HasValue || request.TechnicianId.Value == mission.Userid;

                if (isSameName && isSameDesc && isSameTech)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Vui lòng thay đổi thông tin để cập nhật.",
                        data = null
                    };
                }

                // Validate và xử lý TechnicianId nếu có truyền
                if (request.TechnicianId.HasValue && request.TechnicianId.Value != mission.Userid)
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

                    var existingMission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                        predicate: m => m.Userid.Equals(request.TechnicianId) &&
                                        m.MissionSchedule.Value.Date == mission.MissionSchedule.Value.Date &&
                                        m.IsDelete == false);

                    if (existingMission != null)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = $"Kỹ thuật viên đã có nhiệm vụ vào ngày {mission.MissionSchedule.Value.Date:dd/MM/yyyy}.",
                            data = null
                        };
                    }

                    mission.Userid = request.TechnicianId;
                }

                // Cập nhật các field được phép
                mission.MissionName = !string.IsNullOrWhiteSpace(request.MissionName) ? request.MissionName : mission.MissionName;
                mission.MissionDescription = !string.IsNullOrWhiteSpace(request.MissionDescription) ? request.MissionDescription : mission.MissionDescription;

                // Cập nhật mission
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


        public async Task<ApiResponse> UpdateStatusMission(Guid missionId, string status, Supabase.Client client, List<IFormFile>? ImageLinks, string? reason = null)
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
            throw new BadHttpRequestException("Bạn không có quyền thực hiện thao tác này.");
        }

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

        // Kiểm tra technician chỉ được cập nhật mission của chính mình
        if (mission.Userid == null || mission.Userid != userId)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status403Forbidden.ToString(),
                message = "Bạn không được phép cập nhật trạng thái nhiệm vụ này.",
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

        // Validate reason khi trạng thái là Cancel
        if (missionStatus == MissionStatusEnum.Cancel)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "Lý do hủy nhiệm vụ là bắt buộc khi trạng thái là Cancel.",
                    data = null
                };
            }
            mission.CancelReason = reason; // Lưu lý do hủy
        }
        else
        {
            mission.CancelReason = null; // Xóa lý do nếu trạng thái không phải Cancel
        }

        // Update images (logic giống UpdateProduct)
        if (ImageLinks != null && ImageLinks.Any())
        {
            // Validate file ảnh
            if (ImageLinks.Any(file => file == null || file.Length == 0))
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status400BadRequest.ToString(),
                    message = "File ảnh không hợp lệ hoặc trống.",
                    data = null
                };
            }

            // Xóa tất cả ảnh hiện tại của mission
            var existingImages = await _unitOfWork.GetRepository<MissionImage>()
                .GetListAsync(predicate: i => i.MissionId.Equals(missionId));
            foreach (var img in existingImages)
            {
                _unitOfWork.GetRepository<MissionImage>().DeleteAsync(img);
            }

            // Gửi ảnh lên Supabase và lấy URL
            var imageUrls = await _supabaseImageService.SendImagesAsync(ImageLinks, client);

            // Thêm ảnh mới
            foreach (var imageUrl in imageUrls)
            {
                var newImage = new MissionImage
                {
                    Id = Guid.NewGuid(),
                    MissionId = missionId,
                    LinkImage = imageUrl,
                    Status = "Active", // Giả định trạng thái mặc định
                    CreateDate = TimeUtils.GetCurrentSEATime(),
                    ModifyDate = TimeUtils.GetCurrentSEATime(),
                    IsDelete = false
                };

                // Thêm mới vào cơ sở dữ liệu
                await _unitOfWork.GetRepository<MissionImage>().InsertAsync(newImage);
            }
        }

        // Cập nhật trạng thái Mission
        mission.Status = missionStatus.ToString();

        // Nếu mission liên kết với OrderId và không có BookingId => cập nhật Order
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

        // Nếu mission liên kết với BookingId và không có OrderId => cập nhật Booking
        if (mission.BookingId.HasValue && mission.OrderId == null)
        {
            string? bookingStatus = missionStatus switch
            {
                MissionStatusEnum.Processing => BookingStatusEnum.PROCESSING.ToString(),
                MissionStatusEnum.Done => BookingStatusEnum.DONE.ToString(),
                MissionStatusEnum.Cancel => BookingStatusEnum.MISSED.ToString(),
                _ => null
            };

            if (bookingStatus != null)
            {
                var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                    predicate: b => b.Id == mission.BookingId.Value);

                if (booking != null)
                {
                    booking.Status = bookingStatus;
                    _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);
                }
            }
        }
        mission.EndMissionSchedule = TimeUtils.GetCurrentSEATime();
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
                var mission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                    predicate: m => m.Id == missionid && (m.IsDelete == false || m.IsDelete == null),
                    include: m => m.Include(x => x.Booking)
                                   .ThenInclude(b => b.BookingDetails)
                                       .ThenInclude(bd => bd.ServicePackage)
                                   .Include(x => x.User)
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

                // Xác định FullName
                string fullName = "Không xác định";
                if (mission.BookingId.HasValue && !mission.OrderId.HasValue)
                {
                    var bookingUser = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                        predicate: u => u.Id == mission.Booking!.UserId
                    );
                    fullName = bookingUser?.FullName ?? fullName;
                }
                else if (mission.OrderId.HasValue && !mission.BookingId.HasValue)
                {
                    var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                        predicate: o => o.Id == mission.OrderId
                    );
                    fullName = order?.RecipientName ?? fullName;
                }
                else if (mission.User != null)
                {
                    fullName = mission.User.FullName;
                }

                // Chuẩn bị biến chứa SetupPackageResponse
                SetupPackageResponse setupPackageResponse = null;

                // Trường hợp 1: Có OrderId, không có BookingId → lấy setup từ Order trực tiếp
                if (mission.OrderId.HasValue && !mission.BookingId.HasValue)
                {
                    var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                        predicate: o => o.Id == mission.OrderId,
                        include: o => o.Include(ord => ord.SetupPackage)
                                       .ThenInclude(sp => sp.SetupPackageDetails)
                                           .ThenInclude(spd => spd.Product)
                                               .ThenInclude(p => p.Images)
                                       .Include(ord => ord.SetupPackage)
                                           .ThenInclude(sp => sp.SetupPackageDetails)
                                               .ThenInclude(spd => spd.Product.SubCategory)
                                                   .ThenInclude(sc => sc.Category)
                    );

                    setupPackageResponse = GenerateSetupPackageResponse(order?.SetupPackage);
                }
                // Trường hợp 2: Có BookingId, không có OrderId → lấy OrderId từ Booking
                else if (mission.BookingId.HasValue && !mission.OrderId.HasValue)
                {
                    var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                        predicate: b => b.Id == mission.BookingId,
                        include: b => b.Include(bk => bk.Order)
                                       .ThenInclude(ord => ord.SetupPackage)
                                           .ThenInclude(sp => sp.SetupPackageDetails)
                                               .ThenInclude(spd => spd.Product)
                                                   .ThenInclude(p => p.Images)
                                       .Include(bk => bk.Order)
                                           .ThenInclude(ord => ord.SetupPackage)
                                               .ThenInclude(sp => sp.SetupPackageDetails)
                                                   .ThenInclude(spd => spd.Product.SubCategory)
                                                       .ThenInclude(sc => sc.Category)
                    );

                    setupPackageResponse = GenerateSetupPackageResponse(booking?.Order?.SetupPackage);
                }

                // Lấy danh sách dịch vụ nếu có Booking
                var servicePackages = new List<ServicePackageResponse>();
                if (mission.Booking != null)
                {
                    foreach (var detail in mission.Booking.BookingDetails)
                    {
                        servicePackages.Add(new ServicePackageResponse
                        {
                            Id = detail.ServicePackage.Id,
                            ServiceName = detail.ServicePackage.ServiceName,
                            Price = detail.ServicePackage.Price
                        });
                    }
                }

                // Tạo phản hồi
                var response = new GetListTaskTechResponse
                {
                    Id = mission.Id,
                    MissionName = mission.MissionName,
                    MissionDescription = mission.MissionDescription,
                    Status = mission.Status,
                    IsDelete = mission.IsDelete,
                    MissionSchedule = mission.MissionSchedule,
                    EndMissionSchedule = mission.EndMissionSchedule,
                    CancelReason = mission.CancelReason,
                    FullName = fullName,
                    Address = mission.Address,
                    PhoneNumber = mission.PhoneNumber,
                    BookingId = mission.BookingId,
                    OrderId = mission.OrderId,
                    BookingCode = mission.Booking?.BookingCode,
                    BookingImage = mission.Booking?.BookingImage,
                    Services = servicePackages,
                    SetupPackage = setupPackageResponse
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
        private SetupPackageResponse GenerateSetupPackageResponse(SetupPackage setup)
        {
            if (setup == null) return null;

            return new SetupPackageResponse
            {
                SetupPackageId = setup.Id,
                SetupName = setup.SetupName,
                ModifyDate = setup.ModifyDate,
                Size = setup.SetupPackageDetails?
                    .FirstOrDefault(spd => spd.Product?.SubCategory?.Category?.CategoryName == "Bể")
                    ?.Product?.Size,
                CreateDate = setup.CreateDate,
                TotalPrice = setup.Price ?? 0,
                Description = setup.Description ?? "N/A",
                IsDelete = setup.IsDelete ?? false,
                images = setup.Image ?? "",
                Products = setup.SetupPackageDetails?.Select(spd => new ProductResponse
                {
                    Id = spd.Product?.Id ?? Guid.Empty,
                    ProductName = spd.Product?.ProductName ?? "Unknown",
                    Quantity = spd.Quantity,
                    Price = spd.Product?.Price ?? 0,
                    InventoryQuantity = spd.Product?.Quantity ?? 0,
                    Status = spd.Product?.Status ?? "false",
                    IsDelete = setup.IsDelete ?? false,
                    CategoryName = spd.Product?.SubCategory?.Category?.CategoryName ?? "Unknown",
                    images = spd.Product?.Images?
                        .Where(img => img.IsDelete == false)
                        .OrderBy(img => img.CreateDate)
                        .Select(img => img.LinkImage)
                        .FirstOrDefault() ?? "NoImageAvailable"
                }).ToList() ?? new List<ProductResponse>()
            };
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
                                    u.IsDelete == false);

                if (user == null)
                {
                    throw new BadHttpRequestException("Tài khoản không hợp lệ.");
                }

                var bookingRepo = _unitOfWork.GetRepository<Booking>();

                var booking = await bookingRepo.SingleOrDefaultAsync(
                    predicate: b => b.Id == bookingId);

                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy booking.",
                        data = null
                    };
                }

                // Nếu là Customer thì chỉ được cập nhật booking của mình và chưa phân công
                if (user.Role == RoleEnum.Customer.GetDescriptionFromEnum())
                {
                    if (booking.UserId != userId)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status403Forbidden.ToString(),
                            message = "Bạn không có quyền cập nhật booking này.",
                            data = null
                        };
                    }

                    if (booking.IsAssigned == true)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status403Forbidden.ToString(),
                            message = "Booking đã được phân công. Không thể cập nhật.",
                            data = null
                        };
                    }
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
                booking.Status = BookingStatusEnum.CANCELLED.GetDescriptionFromEnum();
                

                // ✅ Nếu miễn phí => cập nhật order.IsEligible
                if (booking.TotalPrice == 0 && booking.OrderId.HasValue)
                {
                    booking.Status = BookingStatusEnum.CANCELLED.GetDescriptionFromEnum();
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

        public Task<ApiResponse> UpdateBookingStatus(Guid bookingid)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResponse> GetHistoryOrder(Guid orderId)
        {
            try
            {
                var bookings = await _unitOfWork.GetRepository<Booking>().GetListAsync(
                    predicate: b => b.OrderId == orderId,
                    include: b => b.Include(bk => bk.BookingDetails)
                                   .ThenInclude(bd => bd.ServicePackage)
                );

                if (bookings == null || !bookings.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status200OK.ToString(),
                        message = "Không tìm thấy lịch sử đặt lịch cho đơn hàng này.",
                        data = null
                    };
                }

                var response = bookings.Select(b => new GetHistoryOrderResponse
                {
                    ScheduleDate = b.ScheduleDate,
                    Services = b.BookingDetails.Select(d => new ServicePackageResponse
                    {
                        Id = d.ServicePackage.Id,
                        ServiceName = d.ServicePackage.ServiceName,
                        Price = d.ServicePackage.Price
                    }).ToList()
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy lịch sử đơn hàng thành công.",
                    data = response
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi lấy lịch sử đơn hàng.",
                    data = ex.Message
                };
            }
        }

    }
}
