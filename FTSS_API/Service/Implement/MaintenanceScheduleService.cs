using AutoMapper;
using FTSS_API.Payload;
using FTSS_API.Payload.Request.MaintenanceSchedule;
using FTSS_API.Payload.Response.MaintenanceSchedule;
using FTSS_API.Service.Interface;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;
using Microsoft.EntityFrameworkCore;

namespace FTSS_API.Service.Implement
{
    public class MaintenanceScheduleService : BaseService<MaintenanceScheduleService>, IMaintenanceScheduleService
    {
        public MaintenanceScheduleService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<MaintenanceScheduleService> logger, IMapper mapper, IHttpContextAccessor httpContextAccessor) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {
        }
        public async Task<ApiResponse> AssigningTechnician(Guid technicianid, Guid userid, AssigningTechnicianRequest request)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }
                // Kiểm tra request có đầy đủ dữ liệu không
                if (string.IsNullOrWhiteSpace(request.TaskName))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "TaskName name cannot be empty.",
                        data = null
                    };
                }

                if (string.IsNullOrWhiteSpace(request.TaskDescription))
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "TaskDescription name cannot be empty.",
                        data = null
                    };
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
                if (request.ScheduleDate == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "ScheduleDate name cannot be empty.",
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

                // Kiểm tra user (Customer) có tồn tại và hợp lệ không
                var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userid) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.IsDelete == false &&
                                    u.Role.Equals(RoleEnum.Customer.GetDescriptionFromEnum()));

                if (user == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status400BadRequest.ToString(),
                        message = "User is not valid, does not exist, or is not a Customer.",
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

                // Tạo mới MaintenanceSchedule
                var newSchedule = new MaintenanceSchedule
                {
                    Id = Guid.NewGuid(),
                    UserId = userid,
                    ScheduleDate = request.ScheduleDate,
                    Status = MaintenanceScheduleStatusEnum.Available.GetDescriptionFromEnum()
                };

                await _unitOfWork.Context.Set<MaintenanceSchedule>().AddAsync(newSchedule);
                await _unitOfWork.CommitAsync(); // Lưu vào DB để có ID sử dụng tiếp

                // Tạo mới MaintenanceTask
                var newTask = new MaintenanceTask
                {
                    Id = Guid.NewGuid(),
                    MaintenanceScheduleId = newSchedule.Id, // Lấy ID của MaintenanceSchedule vừa tạo
                    TaskName = request.TaskName,
                    TaskDescription = request.TaskDescription,
                    Status = TaskStatusEnum.Processing.GetDescriptionFromEnum(),
                    IsDelete = false,
                    Address = request.Address,
                    Userid = technicianid // Gán technicianid từ request vào userid của MaintenanceTask
                };

                await _unitOfWork.Context.Set<MaintenanceTask>().AddAsync(newTask);
                await _unitOfWork.CommitAsync();

                // Chuẩn bị response
                var response = new AssigningTechnicianResponse
                {
                    Id = newTask.Id,
                    MaintenanceScheduleId = newSchedule.Id,
                    TaskName = newTask.TaskName,
                    TaskDescription = newTask.TaskDescription,
                    Status = newTask.Status,
                    TechnicianId = technician.Id,
                    TechnicianUserName = technician.FullName ?? "Unknown",
                    UserId = user.Id,
                    UserFullName = user.FullName ?? "Unknown",
                    Address = newTask.Address,
                    ScheduleDate = newSchedule.ScheduleDate
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

        public async Task<ApiResponse> CancelTask(Guid id)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum() || u.Role == RoleEnum.Technician.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }

                // Tìm MaintenanceTask cần hủy
                var task = await _unitOfWork.GetRepository<MaintenanceTask>().SingleOrDefaultAsync(
                    predicate: t => t.Id.Equals(id) && t.IsDelete == false
                );

                if (task == null)
                {
                    return new ApiResponse
                    {
                        status = StatusCodes.Status404NotFound.ToString(),
                        message = "Task not found.",
                        data = null
                    };
                }

                // Cập nhật trạng thái thành "Cancel"
                task.Status = TaskStatusEnum.Cancel.GetDescriptionFromEnum();
                _unitOfWork.GetRepository<MaintenanceTask>().UpdateAsync(task);
                await _unitOfWork.CommitAsync();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Task has been cancelled successfully.",
                    data = new
                    {
                        task.Id,
                        task.TaskName,
                        task.Status
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while cancelling the task.",
                    data = ex.Message
                };
            }
        }


        public async Task<ApiResponse> GetListTask(int pageNumber, int pageSize, string? status, bool? isAscending)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    (u.Role == RoleEnum.Admin.GetDescriptionFromEnum() || u.Role == RoleEnum.Manager.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }
                // Lấy danh sách các MaintenanceTask hợp lệ (chưa bị xóa)
                var query = _unitOfWork.Context.Set<MaintenanceTask>()
                    .Where(t => t.IsDelete == false);

                // Lọc theo status nếu có
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(t => t.Status == status);
                }

                // Sắp xếp theo ScheduleDate (tăng/giảm dần)
                query = isAscending == true
                    ? query.OrderBy(t => t.MaintenanceSchedule.ScheduleDate)
                    : query.OrderByDescending(t => t.MaintenanceSchedule.ScheduleDate);

                // Phân trang
                var totalRecords = await query.CountAsync();
                var tasks = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(t => t.MaintenanceSchedule)
                    .Include(t => t.User) // Technician
                    .Include(t => t.MaintenanceSchedule.User) // User của MaintenanceSchedule
                    .ToListAsync();

                // Chuyển dữ liệu sang response
                var response = tasks.Select(t => new AssigningTechnicianResponse
                {
                    Id = t.Id,
                    MaintenanceScheduleId = t.MaintenanceScheduleId,
                    TaskName = t.TaskName,
                    TaskDescription = t.TaskDescription,
                    TechnicianId = t.Userid ?? Guid.Empty,
                    TechnicianUserName = t.User != null ? t.User.UserName : "Unknown",
                    Status = t.Status,
                    UserId = t.MaintenanceSchedule.UserId,
                    UserFullName = t.MaintenanceSchedule.User.FullName,
                    Address = t.Address,
                    ScheduleDate = t.MaintenanceSchedule.ScheduleDate
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Task list retrieved successfully.",
                    data = new
                    {
                        TotalRecords = totalRecords,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        Tasks = response
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving the task list.",
                    data = ex.Message
                };
            }
        }

        public async Task<ApiResponse> GetListTaskTech(int pageNumber, int pageSize, string? status, bool? isAscending)
        {
            try
            {
                // Lấy UserId từ HttpContext
                Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
                var userr = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
                    predicate: u => u.Id.Equals(userId) &&
                                    u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) &&
                                    u.Role.Equals(RoleEnum.Technician.GetDescriptionFromEnum()));

                if (userr == null)
                {
                    throw new BadHttpRequestException("You don't have permission to do this.");
                }

                // Lấy danh sách nhiệm vụ thuộc về Technician này
                var query = _unitOfWork.Context.Set<MaintenanceTask>()
                    .Where(t => t.IsDelete == false && t.Userid == userId);

                // Lọc theo status nếu có
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(t => t.Status == status);
                }

                // Sắp xếp theo ScheduleDate
                query = isAscending == true
                    ? query.OrderBy(t => t.MaintenanceSchedule.ScheduleDate)
                    : query.OrderByDescending(t => t.MaintenanceSchedule.ScheduleDate);

                // Phân trang
                var totalRecords = await query.CountAsync();
                var tasks = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(t => t.MaintenanceSchedule)
                    .Include(t => t.User) // Technician
                    .Include(t => t.MaintenanceSchedule.User) // User của MaintenanceSchedule
                    .ToListAsync();

                // Chuyển dữ liệu sang response
                var response = tasks.Select(t => new AssigningTechnicianResponse
                {
                    Id = t.Id,
                    MaintenanceScheduleId = t.MaintenanceScheduleId,
                    TaskName = t.TaskName,
                    TaskDescription = t.TaskDescription,
                    TechnicianId = t.Userid ?? Guid.Empty,
                    TechnicianUserName = t.User != null ? t.User.UserName : "Unknown",
                    Status = t.Status,
                    UserId = t.MaintenanceSchedule.UserId,
                    UserFullName = t.MaintenanceSchedule.User.FullName,
                    Address = t.Address,
                    ScheduleDate = t.MaintenanceSchedule.ScheduleDate
                }).ToList();

                return new ApiResponse
                {
                    status = StatusCodes.Status200OK.ToString(),
                    message = "Task list retrieved successfully.",
                    data = new
                    {
                        TotalRecords = totalRecords,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        Tasks = response
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse
                {
                    status = StatusCodes.Status500InternalServerError.ToString(),
                    message = "An error occurred while retrieving the task list.",
                    data = ex.Message
                };
            }
        }

    }
}
