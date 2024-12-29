using System.Linq.Expressions;
using System.Text.RegularExpressions;
using AutoMapper;
using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response;
using FTSS_API.Payload.Response.User;
using FTSS_API.Utils;
using FTSS_Model.Context;
using FTSS_Model.Entities;
using FTSS_Model.Enum;
using FTSS_Repository.Interface;

namespace FTSS_API.Service.Implement.Implement;

public class UserService : BaseService<UserService>, IUserService
{
    public readonly IEmailSender _emailService;

    public UserService(IUnitOfWork<MyDbContext> unitOfWork, ILogger<UserService> logger, IMapper mapper,
        IHttpContextAccessor httpContextAccessor, IEmailSender emailSender) : base(unitOfWork, logger, mapper,
        httpContextAccessor)
    {
        _emailService = emailSender;
    }

        public async Task<ApiResponse> CreateNewAccount(CreateNewAccountRequest createNewAccountRequest)
{
    _logger.LogInformation($"Creating new customer account for {createNewAccountRequest.UserName}");

    string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    if (!Regex.IsMatch(createNewAccountRequest.Email, emailPattern))
    {
        return new ApiResponse
        {
            status = StatusCodes.Status400BadRequest.ToString(),
            message = MessageConstant.PatternMessage.EmailIncorrect,
            data = null
        };
    }

    string phonePattern = @"^0\d{9}$";
    if (!Regex.IsMatch(createNewAccountRequest.PhoneNumber, phonePattern))
    {
        return new ApiResponse
        {
            status = StatusCodes.Status400BadRequest.ToString(),
            message = MessageConstant.PatternMessage.PhoneIncorrect,
            data = null
        };
    }

    var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
        predicate: m => m.UserName.Equals(createNewAccountRequest.UserName) || m.Email.Equals(createNewAccountRequest.Email)
    );

    // Nếu tài khoản đã tồn tại với trạng thái khác "Unavailable", báo lỗi
    if (user != null && user.Status != UserStatusEnum.Unavailable.GetDescriptionFromEnum())
    {
        return new ApiResponse
        {
            status = StatusCodes.Status400BadRequest.ToString(),
            message = MessageConstant.UserMessage.AccountExisted,
            data = null
        };
    }

    // Nếu tài khoản tồn tại và có trạng thái "Unavailable", bỏ qua kiểm tra và tạo tài khoản mới
    if (user != null && user.Status == UserStatusEnum.Unavailable.GetDescriptionFromEnum())
    {
        _logger.LogInformation($"Reactivating account for {createNewAccountRequest.UserName}");
    }

    User newUser = new User
    {
        UserName = createNewAccountRequest.UserName,
        Password = PasswordUtil.HashPassword(createNewAccountRequest.Password),
        FullName = createNewAccountRequest.FullName,
        Email = createNewAccountRequest.Email,
        CreateDate = TimeUtils.GetCurrentSEATime(),
        ModifyDate = TimeUtils.GetCurrentSEATime(),
        Role = RoleEnum.Customer.GetDescriptionFromEnum(),
        Status = UserStatusEnum.Unavailable.GetDescriptionFromEnum(),
        Address = createNewAccountRequest.Address,
        PhoneNumber = createNewAccountRequest.PhoneNumber,
        Gender = createNewAccountRequest.Gender.GetDescriptionFromEnum()
    };

    try
    {
        await _unitOfWork.GetRepository<User>().InsertAsync(newUser);
        bool isSuccessfully = await _unitOfWork.CommitAsync() > 0;

        if (isSuccessfully)
        {
            var createNewAccountResponse = new CreateNewAccountResponse
            {
                Id = newUser.Id,
                Username = newUser.UserName,
                Email = newUser.Email,
                FullName = newUser.FullName,
                Gender = EnumUtil.ParseEnum<GenderEnum>(newUser.Gender),
                PhoneNumber = newUser.PhoneNumber,
                Address = newUser.Address,
            };

            string otp = OtpUltil.GenerateOtp();
            var otpRecord = new Otp
            {
                Id = Guid.NewGuid(),  // Sử dụng Guid.NewGuid() để tạo một GUID duy nhất
                UserId = newUser.Id,
                OtpCode = otp,
                CreateDate = TimeUtils.GetCurrentSEATime(),
                ExpiresAt = TimeUtils.GetCurrentSEATime().AddMinutes(10),
                IsValid = true
            };

            await _unitOfWork.GetRepository<Otp>().InsertAsync(otpRecord);
            await _unitOfWork.CommitAsync();
            // Send OTP email
            await _emailService.SendVerificationEmailAsync(newUser.Email, otp);

            // Optionally, handle OTP expiration as discussed
            ScheduleOtpCancellation(otpRecord.Id, TimeSpan.FromMinutes(10));

            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Customer account created successfully",
                data = createNewAccountResponse
            };
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error creating customer account for {createNewAccountRequest.UserName}");
        return new ApiResponse
        {
            status = StatusCodes.Status500InternalServerError.ToString(),
            message = "Failed to create customer account due to database error",
            data = null
        };
    }

    return new ApiResponse
    {
        status = StatusCodes.Status500InternalServerError.ToString(),
        message = "Failed to create customer account",
        data = null
    };
}


    public async Task<ApiResponse> Login(LoginRequest loginRequest)
    {
        // Define the search filter for the user
        Expression<Func<User, bool>> searchFilter = p =>
            p.UserName.Equals(loginRequest.Username) &&
            p.Password.Equals(PasswordUtil.HashPassword(loginRequest.Password)) &&
            (p.IsDelete == false);

        // Retrieve the user based on the search filter
        User user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(predicate: searchFilter);
        if (user == null)
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status401Unauthorized.ToString(),
                message = MessageConstant.LoginMessage.InvalidUsernameOrPassword,
                data = null
            };
        }

        if (user.Status.Equals(UserStatusEnum.Unavailable.GetDescriptionFromEnum()))
        {
            string otp = OtpUltil.GenerateOtp();
            var otpRecord = new Otp
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OtpCode = otp,
                CreateDate = TimeUtils.GetCurrentSEATime(),
                ExpiresAt = TimeUtils.GetCurrentSEATime().AddMinutes(10),
                IsValid = true
            };
            await _unitOfWork.GetRepository<Otp>().InsertAsync(otpRecord);
            await _unitOfWork.CommitAsync();

            // Send OTP email
            await _emailService.SendVerificationEmailAsync(user.Email, otp);

            // Optionally, handle OTP expiration as discussed
            ScheduleOtpCancellation(otpRecord.Id, TimeSpan.FromMinutes(10));
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),

                data = new GetUserResponse()
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Username = user.UserName,
                    FullName = user.FullName,
                    Gender = user.Gender,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                }
            };
        }

        // Create the login response
        RoleEnum role = EnumUtil.ParseEnum<RoleEnum>(user.Role);
        Tuple<string, Guid> guildClaim = new Tuple<string, Guid>("userID", user.Id);
        var token = JwtUtil.GenerateJwtToken(user, guildClaim);

        // Create the login response object
        var loginResponse = new LoginResponse()
        {
            RoleEnum = role.ToString(),
            UserId = user.Id,
            UserName = user.UserName,
            token = token // Assign the generated token
        };

        // Return a success response
        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Login successful.",
            data = loginResponse
        };
    }

    public Task<ApiResponse> DeleteUser(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> GetAllUser(int page, int size)
    {
        var users = await _unitOfWork.GetRepository<User>().GetPagingListAsync(
            selector: u => new GetUserResponse()
            {
                UserId = u.Id,
                FullName = u.FullName,
                Username = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Gender = u.Gender,
                Role = u.Role,
                Address = u.Address,
            },
            predicate: u => u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()),
            page: page,
            size: size);

        int totalItems = users.Total;
        int totalPages = (int)Math.Ceiling((double)totalItems / size);
        if (users == null)
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "Users retrieved successfully.",
                data = new LinkedList<CartItem>()
            };
        }
        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Users retrieved successfully.",
            data = users
        };
    }

    public async Task<ApiResponse> GetUser()
    {
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);

        if (userId == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "User ID not found.",
                data = null
            };
        }

        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            selector: u => new GetUserResponse()
            {
                UserId = u.Id,
                Username = u.UserName,
                Email = u.Email,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Gender = u.Gender,
                Role = u.Role,
                Address = u.Address,
            },
            predicate: u => u.Id.Equals(userId.Value) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

        if (user == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "User not found.",
                data = null
            };
        }

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "User retrieved successfully.",
            data = user
        };
    }

    public async Task<ApiResponse> GetUser(Guid id)
    {
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            selector: u => new GetUserResponse()
            {
                UserId = u.Id,
                Email = u.Email,
                Username = u.UserName,
                FullName = u.FullName,
                PhoneNumber = u.PhoneNumber,
                Gender = u.Gender,
                Role = u.Role,
                Address = u.Address,
            },
            predicate: u => u.Id.Equals(id) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

        if (user == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = MessageConstant.UserMessage.UserNotExist,
                data = null
            };
        }

        return new ApiResponse
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "User retrieved successfully.",
            data = user
        };
    }

    public async Task<ApiResponse> UpdateUser(Guid id, UpdateUserRequest updateUserRequest)
    {
        string phonePattern = @"^0\d{9}$";
        if (updateUserRequest.PhoneNumber != null && !Regex.IsMatch(updateUserRequest.PhoneNumber, phonePattern))
        {
            return new ApiResponse
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = MessageConstant.PatternMessage.PhoneIncorrect,
                data = null
            };
        }
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(id) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

        if (user == null)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = MessageConstant.UserMessage.UserNotExist,
                data = null
            };
        }

        user.FullName = string.IsNullOrEmpty(updateUserRequest.FullName) ? user.FullName : updateUserRequest.FullName;
        user.PhoneNumber = string.IsNullOrEmpty(updateUserRequest.PhoneNumber) ? user.PhoneNumber : updateUserRequest.PhoneNumber;
        user.Gender = updateUserRequest.Gender.HasValue ? updateUserRequest.Gender.GetDescriptionFromEnum() : user.Gender.GetDescriptionFromEnum();
        user.ModifyDate = TimeUtils.GetCurrentSEATime();
        user.Address = string.IsNullOrEmpty(updateUserRequest.Address) ? user.Address : updateUserRequest.Address;

        _unitOfWork.GetRepository<User>().UpdateAsync(user);

        bool isSuccessful = await _unitOfWork.CommitAsync() > 0;

        if (isSuccessful)
        {
            return new ApiResponse
            {
                status = StatusCodes.Status200OK.ToString(),
                message = "User updated successfully.",
                data = user
            };
        }
        else
        {
            return new ApiResponse
            {
                status = StatusCodes.Status500InternalServerError.ToString(),
                message = "Failed to update the user.",
                data = null
            };
        }
    }

    public async Task<string> CreateTokenByEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            throw new ArgumentException("Username cannot be null or empty", nameof(email));
        }
        var account = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: p => p.Email.Equals(email)
        );
        if (account == null) throw new BadHttpRequestException("Account not found");
        var guidClaim = new Tuple<string, Guid>("userId", account.Id);
        var token = JwtUtil.GenerateJwtToken(account, guidClaim);
        // _logger.LogInformation($"Token: {token} ");
        return token;
    }

    public Task<bool> GetAccountByEmail(string email)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> VerifyOtp(Guid UserId, string otpCheck)
    {
        var otp = await _unitOfWork.GetRepository<Otp>()
            .SingleOrDefaultAsync(predicate: p => p.OtpCode.Equals(otpCheck) && p.UserId.Equals(UserId));
        if (otp != null && TimeUtils.GetCurrentSEATime() < otp.ExpiresAt && otp.IsValid == true)
        {
            var user = await _unitOfWork.GetRepository<User>()
                .SingleOrDefaultAsync(predicate: u => u.Id.Equals(UserId));
            if (user != null)
            {
                user.Status = UserStatusEnum.Available.GetDescriptionFromEnum();
                _unitOfWork.GetRepository<User>().UpdateAsync(user);
                _unitOfWork.GetRepository<Otp>().DeleteAsync(otp); // Delete the OTP record


                await _unitOfWork.CommitAsync();
                return true;
            }
        }

        return false;
    }

    public Task<ApiResponse> CreateNewUserAccountByGoogle(GoogleAuthResponse response)
    {
        throw new NotImplementedException();
    }

    public async Task<ApiResponse> ForgotPassword(ForgotPasswordRequest request)
    {
        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(request.Email, emailPattern))
        {
            throw new BadHttpRequestException(MessageConstant.PatternMessage.EmailIncorrect);
        }

        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Email.Equals(request.Email) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));

        if (user == null)
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = MessageConstant.UserMessage.AccountNotExist,
                data = false
            };
        }

        string otp = OtpUltil.GenerateOtp();
        var otpRecord = new Otp
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OtpCode = otp,
            CreateDate = TimeUtils.GetCurrentSEATime(),
            ExpiresAt = TimeUtils.GetCurrentSEATime().AddMinutes(10),
            IsValid = true
        };
        await _unitOfWork.GetRepository<Otp>().InsertAsync(otpRecord);
        await _unitOfWork.CommitAsync();
        await _emailService.SendVerificationEmailAsync(user.Email, otp);
        ScheduleOtpCancellation(otpRecord.Id, TimeSpan.FromMinutes(10));
        return new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "send otp successful",
            data = user.Id
        };
    }

    public async Task<ApiResponse> ResetPassword(VerifyAndResetPasswordRequest request)
    {
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
        if (user == null)
        {
            throw new BadHttpRequestException(MessageConstant.UserMessage.UserNotExist);
        }

        if (!request.NewPassword.Equals(request.ComfirmPassword))
        {
            throw new BadHttpRequestException("Mật khẩu xác nhận không trùng");
        }

        user.Password = PasswordUtil.HashPassword(request.NewPassword);
        user.ModifyDate = TimeUtils.GetCurrentSEATime();
        _unitOfWork.GetRepository<User>().UpdateAsync(user);

        await _unitOfWork.CommitAsync();
        return new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Reset successful",
            data = true
        };
    }

    public async Task<ApiResponse> VerifyForgotPassword(Guid userId, string otp)
    {
        Otp? otpExist = await _unitOfWork.GetRepository<Otp>().SingleOrDefaultAsync(
            predicate: o => o.OtpCode.Equals(otp) && o.UserId.Equals(userId));
        if (otpExist == null)
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Otp không tồn tại",
                data = null
            };
        }
        if (TimeUtils.GetCurrentSEATime() > otpExist.ExpiresAt && otpExist.IsValid != true)
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Otp hết hạn",
                data = null
            };
        }
        _unitOfWork.GetRepository<Otp>().DeleteAsync(otpExist);


        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: o => o.Id.Equals(userId) && o.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()) && o.Role.Equals(RoleEnum.Customer.GetDescriptionFromEnum()));
        RoleEnum role = EnumUtil.ParseEnum<RoleEnum>(user.Role);
        Tuple<string, Guid> guildClaim = new Tuple<string, Guid>("userID", user.Id);
        var token = JwtUtil.GenerateJwtToken(user, guildClaim);
        var loginResponse = new LoginResponse()
        {
            RoleEnum = role.ToString(),
            UserId = user.Id,
            UserName = user.UserName,
            token = token
        };
        return new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Xác thực thành công",
            data = loginResponse
        };
    }

    public async Task<ApiResponse> ChangePassword(ChangePasswordRequest request)
    {
        Guid? userId = UserUtil.GetAccountId(_httpContextAccessor.HttpContext);
        var user = await _unitOfWork.GetRepository<User>().SingleOrDefaultAsync(
            predicate: u => u.Id.Equals(userId) && u.Status.Equals(UserStatusEnum.Available.GetDescriptionFromEnum()));
        if (user == null)
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status404NotFound.ToString(),
                message = "Bạn cần đăng nhập",
                data = false
            };
        }
        if (!user.Password.Equals(PasswordUtil.HashPassword(request.OldPassword)))
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Mật khẩu cũ không chính xác",
                data = false
            };
        }
        if (!request.NewPassword.Equals(request.ConfirmPassword))
        {
            return new ApiResponse()
            {
                status = StatusCodes.Status400BadRequest.ToString(),
                message = "Mật khẩu xác nhận không trùng khớp",
                data = false
            };
        }
        user.Password = PasswordUtil.HashPassword(request.NewPassword);
        _unitOfWork.GetRepository<User>().UpdateAsync(user);
        await _unitOfWork.CommitAsync();

        return new ApiResponse()
        {
            status = StatusCodes.Status200OK.ToString(),
            message = "Đổi mật khẩu thành công",
            data = true
        };

    }
    

    private async Task ScheduleOtpCancellation(Guid otpId, TimeSpan delay)
    {
        await Task.Delay(delay);

        var otpRepository = _unitOfWork.GetRepository<Otp>();
        var otp = await otpRepository.SingleOrDefaultAsync(predicate: p => p.Id.Equals(otpId));

        if (otp != null && otp.IsValid && TimeUtils.GetCurrentSEATime() >= otp.ExpiresAt)
        {
            otp.IsValid = false;
            otpRepository.UpdateAsync(otp);
            await _unitOfWork.CommitAsync();
        }
    }
}