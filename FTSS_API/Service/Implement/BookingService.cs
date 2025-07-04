﻿using AutoMapper;
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
using Supabase;
using FTSS_API.Service.Implement.Implement;

namespace FTSS_API.Service.Implement
{
    public class BookingService : BaseService<BookingService>, IBookingService
    {
        private readonly SupabaseUltils _supabaseImageService;
        private readonly IEmailSender _emailSender;
        public IPaymentService _paymentService { get; set; }
        public BookingService(IUnitOfWork<MyDbContext> unitOfWork, IEmailSender emailSender, ILogger<BookingService> logger, IMapper mapper,SupabaseUltils supabaseImageService, IHttpContextAccessor httpContextAccessor, IPaymentService paymentService) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
            _paymentService = paymentService;
            _supabaseImageService = supabaseImageService;
            _emailSender = emailSender;
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

                // Kiểm tra trạng thái Order phải là PROCESSED hoặc NOTDONE
                var allowedStatuses = new[] { OrderStatus.PROCESSED.GetDescriptionFromEnum(), OrderStatus.NOTDONE.GetDescriptionFromEnum() };
                if (!allowedStatuses.Contains(order.Status))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Chỉ có thể phân công kỹ thuật viên khi đơn hàng ở trạng thái PROCESSED hoặc NOTDONE.",
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

                await _unitOfWork.GetRepository<Mission>().InsertAsync(newTask);
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
                // ✅ Không cho phép đặt mới nếu đã có booking với OrderId này mà chưa ở trạng thái DONE, CANCELLED hoặc MISSED
                var existingBookings = await _unitOfWork.GetRepository<Booking>().GetListAsync(
                    predicate: b => b.OrderId == request.OrderId &&
                                    !new[] {
                        BookingStatusEnum.DONE.ToString(),
                        BookingStatusEnum.CANCELLED.ToString(),
                        BookingStatusEnum.MISSED.ToString(),
                        BookingStatusEnum.COMPLETED.ToString(),
                                    }.Contains(b.Status)
                );

                if (existingBookings.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Bạn chỉ có thể đặt một lịch duy nhất cho mỗi đơn hàng. Vui lòng chờ đến khi lịch trình hiện tại hoàn tất hoặc bị hủy.",
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
                // Lấy thông tin booking + liên quan
                var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                    predicate: b => b.Id == bookingId,
                    include: b => b
                        .Include(x => x.User)
                        .Include(x => x.BookingDetails).ThenInclude(bd => bd.ServicePackage)
                        .Include(x => x.Payments)
                        .Include(x => x.Order) // để lấy SetupPackageId
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

                // Truy vấn Mission có BookingId = booking.Id
                var mission = await _unitOfWork.GetRepository<Mission>().SingleOrDefaultAsync(
                    predicate: m => m.BookingId == booking.Id && m.IsDelete == false,
                    include: m => m.Include(mi => mi.MissionImages)
                );

                // Lấy danh sách ảnh kiểu Report từ MissionImage
                var reportImages = mission?.MissionImages?
                    .Where(img => img.IsDelete == false)
                    .Select(img => new MissionImageResponse
                    {
                        Id = img.Id,
                        LinkImage = img.LinkImage,
                        Status = img.Status,
                        CreateDate = img.CreateDate,
                        ModifyDate = img.ModifyDate,
                        IsDelete = img.IsDelete,
                        Type = img.Type
                    }).ToList() ?? new List<MissionImageResponse>();


                // Lấy payment đầu tiên nếu có
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

                // Lấy thông tin SetupPackage nếu có
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

                // Gộp response
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
                    IsAssigned = booking.IsAssigned,
                    MissionId = mission?.Id,
                    CancelReason = mission?.CancelReason,
                    Reason = booking.CancelReason,
                    Images = reportImages,
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


        public async Task<ApiResponse> GetDateUnavailable(DateTime? date)
        {
            try
            {
                // Lấy số lượng kỹ thuật viên đang sẵn sàng (Available)
                var technicianCount = await _unitOfWork.GetRepository<User>()
                    .GetQueryable()
                    .Where(u => u.Role == RoleEnum.Technician.ToString()
                             && u.IsDelete == false
                             && u.Status == UserStatusEnum.Available.GetDescriptionFromEnum())
                    .CountAsync();

                // Danh sách trạng thái hợp lệ cho Booking
                var validBookingStatuses = new[]
                {
            BookingStatusEnum.NOTASSIGN.ToString(),
            BookingStatusEnum.ASSIGNED.ToString(),
            BookingStatusEnum.PROCESSING.ToString()
        };

                // Danh sách trạng thái KHÔNG hợp lệ cho Order (bị loại)
                var excludedOrderStatuses = new[]
                {
            OrderStatus.NOTDONE.ToString(),
            OrderStatus.DONE.ToString(),
            OrderStatus.REPORTED.ToString(),
            OrderStatus.COMPLETED.ToString(),
            OrderStatus.RETURNED.ToString(),
            OrderStatus.RETURNING.ToString(),
            OrderStatus.CANCELLED.ToString()
        };

                // Lấy danh sách ngày từ Booking hợp lệ
                var bookingDates = await _unitOfWork.GetRepository<Booking>()
                    .GetListAsync(
                        predicate: b => b.ScheduleDate != null && validBookingStatuses.Contains(b.Status),
                        selector: b => b.ScheduleDate.Value.Date
                    );

                // Lấy danh sách ngày từ Order hợp lệ
                var orderDates = await _unitOfWork.GetRepository<Order>()
                    .GetListAsync(
                        predicate: o => o.InstallationDate != null && !excludedOrderStatuses.Contains(o.Status),
                        selector: o => o.InstallationDate.Value.Date
                    );

                // Gộp cả 2 danh sách ngày Booking và Order
                var allDates = bookingDates.Concat(orderDates).ToList();

                // Nhóm theo ngày và lọc ra những ngày không khả dụng
                var groupedUnavailableDates = allDates
                    .GroupBy(d => d)
                    .Where(group => group.Count() >= technicianCount)
                    .Select(group => group.Key)
                    .ToList();

                // Nếu truyền vào ngày cụ thể => loại bỏ ngày đó khỏi danh sách nếu có
                if (date.HasValue)
                {
                    groupedUnavailableDates = groupedUnavailableDates
                        .Where(d => d.Date != date.Value.Date)
                        .ToList();
                }

                var responseData = groupedUnavailableDates
                    .Select(d => new GetDateUnavailableResponse { ScheduleDate = d })
                    .ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Lấy danh sách ngày không khả dụng thành công.",
                    data = responseData
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

                    // Lấy MissionId đầu tiên nếu có
                    MissionId = b.Missions.FirstOrDefault()?.Id,

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
                        BankHolder = p.BankHolder ?? "Unknown",
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
                    include: m => m
                        .Include(x => x.User)
                        .Include(x => x.Order)
                        .Include(x => x.MissionImages) // Thêm dòng này
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
                    Services = m.Booking?.BookingDetails?
                        .Select(s => new ServicePackageResponse
                        {
                            Id = s.ServicePackage.Id,
                            ServiceName = s.ServicePackage.ServiceName,
                            Price = s.ServicePackage.Price
                        }).ToList() ?? new(),

                    // ✅ Thêm danh sách hình ảnh nhiệm vụ
                    Images = m.MissionImages
                        .Where(img => img.IsDelete == false)
                        .Select(img => new MissionImageResponse
                        {
                            Id = img.Id,
                            LinkImage = img.LinkImage,
                            Status = img.Status,
                            CreateDate = img.CreateDate,
                            ModifyDate = img.ModifyDate,
                            IsDelete = img.IsDelete,
                            Type = img.Type
                        }).ToList()
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

                Guid? finalOrderId = t.OrderId;
                string? orderCode = t.Order?.OrderCode;
                DateTime? installationDate = t.Order?.InstallationDate;

                if (t.BookingId.HasValue && !t.OrderId.HasValue)
                {
                    var bookingWithOrder = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                        predicate: b => b.Id == t.BookingId,
                        include: b => b.Include(bk => bk.Order)
                    );

                    if (bookingWithOrder?.Order != null)
                    {
                        finalOrderId = bookingWithOrder.Order.Id;
                        orderCode = bookingWithOrder.Order.OrderCode;
                        installationDate = bookingWithOrder.Order.InstallationDate;
                        setupPackageId = bookingWithOrder.Order.SetupPackageId;
                    }
                }
                else if (t.OrderId.HasValue)
                {
                    setupPackageId = t.Order?.SetupPackageId;
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

                // Trả về BookingCode hoặc OrderCode theo yêu cầu
                string responseBookingCode = t.BookingId.HasValue && t.Booking != null ? t.Booking.BookingCode ?? "Không có" : "Không có";
                string responseOrderCode = t.OrderId.HasValue && t.Order != null ? t.Order.OrderCode ?? "Không có" : "Không có";

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
                    Address = string.IsNullOrEmpty(t.Address) ? "Không có" : t.Address,
                    PhoneNumber = string.IsNullOrEmpty(t.PhoneNumber) ? "Không có" : t.PhoneNumber,
                    BookingId = t.BookingId,
                    OrderId = finalOrderId,
                    BookingCode = responseBookingCode,
                    BookingImage = t.BookingId.HasValue && t.Booking != null ? t.Booking.BookingImage ?? "Không có" : "Không có",
                    OrderCode = responseOrderCode,
                    InstallationDate = installationDate,
                    Services = servicePackages,
                    SetupPackage = setupPackage
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
                        status = StatusCodes.Status404NotFound.GetDescriptionFromEnum(),
                        message = "Không tìm thấy nhiệm vụ.",
                        data = null
                    };
                }

                // Danh sách status được phép cập nhật
                var allowedStatuses = new[]
                {
    MissionStatusEnum.Completed.GetDescriptionFromEnum(),
    MissionStatusEnum.NotDone.GetDescriptionFromEnum(),
    MissionStatusEnum.Reported.GetDescriptionFromEnum(),
    MissionStatusEnum.NotStarted.GetDescriptionFromEnum()
};

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    if (!allowedStatuses.Contains(request.Status))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.GetDescriptionFromEnum(),
                            message = "Chỉ được phép cập nhật trạng thái: Chưa xong, Hoàn tất, hoặc Báo cáo.",
                            data = null
                        };
                    }
                }

                // 🟡 Di chuyển phần kiểm tra các trường khác lên trước
                bool isUpdatingOtherFields =
                    (!string.IsNullOrWhiteSpace(request.MissionName) && request.MissionName != mission.MissionName) ||
                    (!string.IsNullOrWhiteSpace(request.MissionDescription) && request.MissionDescription != mission.MissionDescription) ||
                    (request.TechnicianId.HasValue && request.TechnicianId.Value != mission.Userid);

                if (isUpdatingOtherFields)
                {
                    if (mission.Status != MissionStatusEnum.NotStarted.GetDescriptionFromEnum() ||
                        request.Status != MissionStatusEnum.NotStarted.GetDescriptionFromEnum())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.GetDescriptionFromEnum(),
                            message = "Chỉ được phép thay đổi thông tin khác khi nhiệm vụ đang ở trạng thái Chưa bắt đầu.",
                            data = null
                        };
                    }
                }

                // 🟡 Tiếp theo kiểm tra Completed sau
                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    if (request.Status == MissionStatusEnum.Completed.GetDescriptionFromEnum() &&
                        mission.Status != MissionStatusEnum.Done.GetDescriptionFromEnum())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.GetDescriptionFromEnum(),
                            message = "Chỉ được cập nhật sang trạng thái Hoàn tất nếu nhiệm vụ đang ở trạng thái Xong công việc.",
                            data = null
                        };
                    }

                    if (request.Status == MissionStatusEnum.NotStarted.GetDescriptionFromEnum() &&
                        mission.Status != MissionStatusEnum.NotStarted.GetDescriptionFromEnum())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.GetDescriptionFromEnum(),
                            message = "Không được phép cập nhật.",
                            data = null
                        };
                    }
                }

                // Validate MissionName
                if (!string.IsNullOrWhiteSpace(request.MissionName) && request.MissionName.Length < 3)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.GetDescriptionFromEnum(),
                        message = "Tên nhiệm vụ phải có ít nhất 3 ký tự.",
                        data = null
                    };
                }

                // Validate MissionDescription
                if (!string.IsNullOrWhiteSpace(request.MissionDescription) && request.MissionDescription.Length < 10)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.GetDescriptionFromEnum(),
                        message = "Mô tả nhiệm vụ phải có ít nhất 10 ký tự.",
                        data = null
                    };
                }

                // So sánh các trường truyền vào với dữ liệu hiện tại để kiểm tra trùng
                bool isSameName = string.IsNullOrWhiteSpace(request.MissionName) || request.MissionName == mission.MissionName;
                bool isSameDesc = string.IsNullOrWhiteSpace(request.MissionDescription) || request.MissionDescription == mission.MissionDescription;
                bool isSameTech = !request.TechnicianId.HasValue || request.TechnicianId.Value == mission.Userid;
                bool isSameStatus = string.IsNullOrWhiteSpace(request.Status) || request.Status == mission.Status;

                if (isSameName && isSameDesc && isSameTech && isSameStatus)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.GetDescriptionFromEnum(),
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
                            status = StatusCodes.Status400BadRequest.GetDescriptionFromEnum(),
                            message = "Kỹ thuật viên không hợp lệ, không tồn tại hoặc không phải là kỹ thuật viên.",
                            data = null
                        };
                    }


                    mission.Userid = request.TechnicianId;
                }

                // Cập nhật các trường nếu khác
                mission.MissionName = !string.IsNullOrWhiteSpace(request.MissionName) ? request.MissionName : mission.MissionName;
                mission.MissionDescription = !string.IsNullOrWhiteSpace(request.MissionDescription) ? request.MissionDescription : mission.MissionDescription;

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    mission.Status = request.Status;

                    if (mission.OrderId.HasValue && !mission.BookingId.HasValue)
                    {
                        var order = await _unitOfWork.GetRepository<Order>()
                    .SingleOrDefaultAsync(selector: x => x, predicate: o => o.Id == mission.OrderId.Value);

                        if (order != null)
                        {
                            switch (request.Status)
                            {
                                case var s when s == MissionStatusEnum.Cancel.GetDescriptionFromEnum():
                                    order.Status = OrderStatus.CANCELLED.GetDescriptionFromEnum(); break;
                                case var s when s == MissionStatusEnum.NotDone.GetDescriptionFromEnum():
                                    order.Status = OrderStatus.NOTDONE.GetDescriptionFromEnum();
                                    order.IsAssigned = false;
                                    break;
                                case var s when s == MissionStatusEnum.Completed.GetDescriptionFromEnum():
                                    order.Status = OrderStatus.COMPLETED.GetDescriptionFromEnum(); break;
                                case var s when s == MissionStatusEnum.Reported.GetDescriptionFromEnum():
                                    order.Status = OrderStatus.NOTDONE.GetDescriptionFromEnum();
                                    order.IsAssigned = false;
                                    break;
                            }

                            _unitOfWork.GetRepository<Order>().UpdateAsync(order);
                        }
                    }
                    else if (mission.BookingId.HasValue && !mission.OrderId.HasValue)
                    {
                        var booking = await _unitOfWork.GetRepository<Booking>()
                    .SingleOrDefaultAsync(selector: x => x, predicate: b => b.Id == mission.BookingId.Value);
                        if (booking != null)
                        {
                            switch (request.Status)
                            {
                                case var s when s == MissionStatusEnum.Cancel.GetDescriptionFromEnum():
                                    booking.Status = BookingStatusEnum.MISSED.GetDescriptionFromEnum(); break;
                                case var s when s == MissionStatusEnum.NotDone.GetDescriptionFromEnum():
                                    booking.Status = BookingStatusEnum.NOTDONE.GetDescriptionFromEnum();
                                    booking.IsAssigned = false;
                                    break;
                                case var s when s == MissionStatusEnum.Completed.GetDescriptionFromEnum():
                                    booking.Status = BookingStatusEnum.COMPLETED.GetDescriptionFromEnum(); break;
                                case var s when s == MissionStatusEnum.Reported.GetDescriptionFromEnum():
                                    booking.Status = BookingStatusEnum.NOTDONE.GetDescriptionFromEnum();
                                    booking.IsAssigned = false;
                                    break;
                            }

                            _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);
                        }
                    }
                }

                _unitOfWork.GetRepository<Mission>().UpdateAsync(mission);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.GetDescriptionFromEnum(),
                    message = "Cập nhật nhiệm vụ thành công.",
                    data = null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.GetDescriptionFromEnum(),
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

                // Validate reason khi trạng thái là Cancel và NotDone
                if (missionStatus == MissionStatusEnum.Cancel || missionStatus == MissionStatusEnum.NotDone)
                {
                    if (string.IsNullOrWhiteSpace(reason))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Lý do là bắt buộc khi trạng thái là Cancel hoặc NotDone.",
                            data = null
                        };
                    }
                    mission.CancelReason = reason;
                }
                else
                {
                    mission.CancelReason = null;
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
                            IsDelete = false,
                            Type = MissionImageTypeEnum.Cancel.GetDescriptionFromEnum()
                        };

                        // Thêm mới vào cơ sở dữ liệu
                        await _unitOfWork.GetRepository<MissionImage>().InsertAsync(newImage);
                    }
                }

                // Cập nhật trạng thái Mission
                mission.Status = missionStatus.ToString();

                // Cập nhật EndMissionSchedule chỉ khi trạng thái là Done hoặc Cancel
                if (missionStatus == MissionStatusEnum.Done || missionStatus == MissionStatusEnum.Cancel)
                {
                    mission.EndMissionSchedule = TimeUtils.GetCurrentSEATime();
                }

                // Nếu mission liên kết với OrderId và không có BookingId => cập nhật Order
                if (mission.OrderId.HasValue && mission.BookingId == null)
                {
                    string? orderStatus = missionStatus switch
                    {
                        MissionStatusEnum.Processing => OrderStatus.PENDING_DELIVERY.ToString(),
                        MissionStatusEnum.Done => OrderStatus.DONE.ToString(),
                        MissionStatusEnum.Cancel => OrderStatus.CANCELLED.ToString(),
                        MissionStatusEnum.NotDone => OrderStatus.NOTDONE.ToString(),
                        _ => null
                    };

                    if (orderStatus != null)
                    {
                        var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                            predicate: o => o.Id == mission.OrderId.Value && o.IsDelete == false);

                        if (order != null)
                        {
                            order.Status = orderStatus;
                            order.ModifyDate = TimeUtils.GetCurrentSEATime();

                            // ✅ Nếu là NotDone thì cập nhật IsAssigned = false
                            if (missionStatus == MissionStatusEnum.NotDone)
                            {
                                order.IsAssigned = false;
                            }

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
                        MissionStatusEnum.NotDone => BookingStatusEnum.NOTDONE.ToString(),
                        _ => null
                    };

                    if (bookingStatus != null)
                    {
                        var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                            predicate: b => b.Id == mission.BookingId.Value);

                        if (booking != null)
                        {
                            booking.Status = bookingStatus;

                            // ✅ Nếu là NotDone thì cập nhật IsAssigned = false
                            if (missionStatus == MissionStatusEnum.NotDone)
                            {
                                booking.IsAssigned = false;
                            }

                            _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);
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
                Order? orderInfo = null;

                // Trường hợp 1: Có OrderId, không có BookingId
                if (mission.OrderId.HasValue && !mission.BookingId.HasValue)
                {
                    orderInfo = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
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

                    setupPackageResponse = GenerateSetupPackageResponse(orderInfo?.SetupPackage);
                }
                // Trường hợp 2: Có BookingId, không có OrderId
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

                    orderInfo = booking?.Order;
                    setupPackageResponse = GenerateSetupPackageResponse(orderInfo?.SetupPackage);
                }

                // Dịch vụ từ booking nếu có
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

                // Xử lý các giá trị trả về để không null
                string responseBookingCode = mission.BookingId.HasValue && mission.Booking != null
                    ? mission.Booking.BookingCode ?? "Không có"
                    : "Không có";

                string responseOrderCode = mission.OrderId.HasValue && orderInfo != null
                    ? orderInfo.OrderCode ?? "Không có"
                    : "Không có";

                string responseBookingImage = mission.BookingId.HasValue && mission.Booking != null
                    ? mission.Booking.BookingImage ?? "Không có"
                    : "Không có";

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
                    Address = string.IsNullOrEmpty(mission.Address) ? "Không có" : mission.Address,
                    PhoneNumber = string.IsNullOrEmpty(mission.PhoneNumber) ? "Không có" : mission.PhoneNumber,
                    BookingId = mission.BookingId,
                    OrderId = mission.OrderId ?? orderInfo?.Id,
                    BookingCode = responseBookingCode,
                    BookingImage = responseBookingImage,
                    OrderCode = responseOrderCode,
                    InstallationDate = orderInfo?.InstallationDate,
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
                var missionRepo = _unitOfWork.GetRepository<Mission>();

                var booking = await bookingRepo.SingleOrDefaultAsync(
    predicate: b => b.Id == bookingId
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

                // Kiểm tra quyền cập nhật dựa theo vai trò
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
                else if (user.Role == RoleEnum.Manager.GetDescriptionFromEnum())
                {
                    if (booking.IsAssigned == true && booking.Status != BookingStatusEnum.NOTDONE.ToString())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status403Forbidden.ToString(),
                            message = "Booking đã phân công và không ở trạng thái chưa hoàn thành. Không thể cập nhật.",
                            data = null
                        };
                    }
                }

                bool isUpdated = false;

                if (request.ScheduleDate.HasValue)
                {
                    booking.ScheduleDate = request.ScheduleDate;
                    isUpdated = true;
                }

                if (!string.IsNullOrWhiteSpace(request.Address))
                {
                    booking.Address = request.Address;
                    isUpdated = true;
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
                    isUpdated = true;
                }

                if (!isUpdated)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Không có dữ liệu nào được gửi để cập nhật.",
                        data = null
                    };
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
        public async Task<ApiResponse> CancelBooking(Guid bookingId, CancelBookingRequest request)
        {
            try
            {
                // Lấy user đăng nhập
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false
                );

                if (user == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status401Unauthorized.ToString(),
                        message = "Không xác định được người dùng.",
                        data = null
                    };
                }

                var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                    predicate: b => b.Id == bookingId,
                    include: b => b.Include(x => x.Payments)
                                   .Include(x => x.User)
                                   .Include(x => x.Order)
                                   .Include(x => x.Missions)
                );

                if (booking == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Không tìm thấy lịch hẹn.",
                        data = null
                    };
                }

                var isCustomer = user.Role == RoleEnum.Customer.GetDescriptionFromEnum();
                var isManager = user.Role == RoleEnum.Manager.GetDescriptionFromEnum(); // optional nếu có

                if (isCustomer)
                {
                    // ✅ Chỉ được hủy lịch của chính mình
                    if (booking.UserId != user.Id)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status403Forbidden.ToString(),
                            message = "Bạn không có quyền hủy lịch hẹn này.",
                            data = null
                        };
                    }

                    // ✅ Không được hủy nếu đã có nhiệm vụ (đã được phân công)
                    if (booking.Missions != null && booking.Missions.Any())
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Lịch hẹn đã được phân công, không thể hủy.",
                            data = null
                        };
                    }

                    // ✅ Không gửi mail, không cần request.Reason
                    booking.Status = BookingStatusEnum.CANCELLED.GetDescriptionFromEnum();

                    // ✅ Nếu miễn phí => cập nhật order.IsEligible
                    if (booking.TotalPrice == 0 && booking.OrderId.HasValue && booking.Order != null)
                    {
                        booking.Order.IsEligible = true;
                        booking.Order.ModifyDate = TimeUtils.GetCurrentSEATime();
                        _unitOfWork.GetRepository<Order>().UpdateAsync(booking.Order);
                    }

                    _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);
                    bool result = await _unitOfWork.CommitAsync() > 0;

                    return new ApiResponse
                    {
                        status = result ? StatusCodes.Status200OK.ToString() : StatusCodes.Status400BadRequest.ToString(),
                        message = result ? "Hủy lịch hẹn thành công." : "Hủy lịch hẹn thất bại.",
                        data = true
                    };
                }

                // ✅ Nếu là Manager thì giữ nguyên logic ban đầu
                var isPaid = booking.Payments.Any(p => p.PaymentStatus == PaymentStatusEnum.Completed.GetDescriptionFromEnum());

                var email = booking.User?.Email;
                if (!string.IsNullOrEmpty(email))
                {
                    string emailBody = EmailTemplatesUtils.RefundBookingNotificationEmailTemplate(
                        booking.BookingCode ?? "N/A",
                        isPaid,
                        request?.Reason ?? "Không có lý do cụ thể"
                    );

                    await _emailSender.RefundBookingNotificationEmailAsync(email, emailBody);
                }

                booking.Status = BookingStatusEnum.CANCELLED.GetDescriptionFromEnum();
                booking.CancelReason = request.Reason;

                if (booking.TotalPrice == 0 && booking.OrderId.HasValue && booking.Order != null)
                {
                    booking.Order.IsEligible = true;
                    booking.Order.ModifyDate = TimeUtils.GetCurrentSEATime();
                    _unitOfWork.GetRepository<Order>().UpdateAsync(booking.Order);
                }

                _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);
                bool managerResult = await _unitOfWork.CommitAsync() > 0;

                return new ApiResponse
                {
                    status = managerResult ? StatusCodes.Status200OK.ToString() : StatusCodes.Status400BadRequest.ToString(),
                    message = managerResult ? "Hủy lịch hẹn thành công." : "Hủy lịch hẹn thất bại.",
                    data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = $"Đã xảy ra lỗi khi hủy lịch hẹn: {ex.Message}",
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
                    predicate: b => b.OrderId == orderId &&
                                    b.Status == BookingStatusEnum.COMPLETED.GetDescriptionFromEnum(), // Chỉ lấy Booking có Status COMPLETED
                    include: b => b.Include(bk => bk.BookingDetails)
                                   .ThenInclude(bd => bd.ServicePackage)
                );

                if (bookings == null || !bookings.Any())
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status200OK.ToString(),
                        message = "Không tìm thấy lịch sử đặt lịch hoàn tất cho đơn hàng này.",
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

        public async Task<ApiResponse> Confirm(Guid? orderid, Guid? bookingid)
        {
            try
            {
                if (orderid.HasValue)
                {
                    var order = await _unitOfWork.GetRepository<Order>().SingleOrDefaultAsync(
                        predicate: o => o.Id == orderid.Value && o.IsDelete == false);

                    if (order == null)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status404NotFound.ToString(),
                            message = "Không tìm thấy đơn hàng.",
                            data = null
                        };
                    }

                    if (!order.Status.Equals(OrderStatus.DONE.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Chỉ những đơn hàng có trạng thái DONE mới được xác nhận.",
                            data = null
                        };
                    }

                    // Cập nhật trạng thái Order
                    order.Status = OrderStatus.COMPLETED.ToString();
                    _unitOfWork.GetRepository<Order>().UpdateAsync(order);

                    // Cập nhật trạng thái Mission liên quan
                    var missions = await _unitOfWork.GetRepository<Mission>().GetListAsync(
                        predicate: m => m.OrderId == order.Id && m.IsDelete == false);

                    foreach (var mission in missions)
                    {
                        mission.Status = MissionStatusEnum.Completed.ToString();
                        _unitOfWork.GetRepository<Mission>().UpdateAsync(mission);
                    }

                    await _unitOfWork.CommitAsync();

                    return new ApiResponse
                    {
                        status = StatusCodes.Status200OK.ToString(),
                        message = "Xác nhận đơn hàng và nhiệm vụ thành công.",
                        data = null
                    };
                }
                else if (bookingid.HasValue)
                {
                    var booking = await _unitOfWork.GetRepository<Booking>().SingleOrDefaultAsync(
                        predicate: b => b.Id == bookingid.Value);

                    if (booking == null)
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status404NotFound.ToString(),
                            message = "Không tìm thấy booking.",
                            data = null
                        };
                    }

                    if (!booking.Status.Equals(BookingStatusEnum.DONE.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return new ApiResponse
                        {
                            status = StatusCodes.Status400BadRequest.ToString(),
                            message = "Chỉ những booking có trạng thái DONE mới được xác nhận.",
                            data = null
                        };
                    }

                    // Cập nhật trạng thái Booking
                    booking.Status = BookingStatusEnum.COMPLETED.ToString();
                    _unitOfWork.GetRepository<Booking>().UpdateAsync(booking);

                    // Cập nhật trạng thái Mission liên quan
                    var missions = await _unitOfWork.GetRepository<Mission>().GetListAsync(
                        predicate: m => m.BookingId == booking.Id && m.IsDelete == false);

                    foreach (var mission in missions)
                    {
                        mission.Status = MissionStatusEnum.Completed.ToString();
                        _unitOfWork.GetRepository<Mission>().UpdateAsync(mission);
                    }

                    await _unitOfWork.CommitAsync();

                    return new ApiResponse
                    {
                        status = StatusCodes.Status200OK.ToString(),
                        message = "Xác nhận booking và nhiệm vụ thành công.",
                        data = null
                    };
                }
                else
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "Vui lòng cung cấp orderid hoặc bookingid.",
                        data = null
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "Đã xảy ra lỗi khi xác nhận.",
                    data = ex.Message
                };
            }
        }
    }
}
