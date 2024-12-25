using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response;
using Microsoft.AspNetCore.Identity.Data;
using ForgotPasswordRequest = FTSS_API.Payload.Request.ForgotPasswordRequest;
using LoginRequest = FTSS_API.Payload.Request.LoginRequest;

namespace FTSS_API.Service.Implement.Implement;

public interface IUserService
{
    Task<ApiResponse> CreateNewAccount(CreateNewAccountRequest createNewAccountRequest);
    Task<ApiResponse> Login(LoginRequest loginRequest);
    Task<ApiResponse> DeleteUser(Guid id);
    Task<ApiResponse> GetAllUser(int page, int size);
    Task<ApiResponse> GetUser();
    Task<ApiResponse> GetUser(Guid id);
    Task<ApiResponse> UpdateUser(Guid id, UpdateUserRequest updateUserRequest);
    Task<string> CreateTokenByEmail(string email);

    Task<bool> GetAccountByEmail(string email);
    Task<bool> VerifyOtp(Guid UserId, string otpCheck);
    Task<ApiResponse> CreateNewUserAccountByGoogle(GoogleAuthResponse response);
    Task<ApiResponse> ForgotPassword(ForgotPasswordRequest request);
    Task<ApiResponse> ResetPassword(VerifyAndResetPasswordRequest request);
    Task<ApiResponse> VerifyForgotPassword(Guid userId, string otp);
    Task<ApiResponse> ChangePassword(ChangePasswordRequest request);
}

