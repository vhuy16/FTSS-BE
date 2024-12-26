﻿using FTSS_API.Constant;
using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response.User;
using FTSS_API.Service.Implement.Implement;
using FTSS_Model.Entities;
using FTSS_Model.Paginate;
using Microsoft.AspNetCore.Mvc;

namespace FTSS_API.Controller;

 [ApiController]
 public class UserController : BaseController<UserController>
 {
     private readonly IUserService _userService;
     public UserController(ILogger<UserController> logger, IUserService userService) : base(logger)
     {
         _userService = userService;
     }
     [HttpPost(ApiEndPointConstant.User.Register)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> CreateNewAccount([FromBody] CreateNewAccountRequest createNewAccountRequest)
     {
         ApiResponse createNewAccountResponse = await _userService.CreateNewAccount(createNewAccountRequest);
         if (createNewAccountResponse == null)
         {
             return Problem(MessageConstant.UserMessage.CreateUserAdminFail);
         }
         return CreatedAtAction(nameof(CreateNewAccount), createNewAccountResponse);
     }
     
     
     [HttpPost(ApiEndPointConstant.User.Login)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
     public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
     {
         var loginResponse = await _userService.Login(loginRequest);
         return StatusCode(int.Parse(loginResponse.status), loginResponse);
     }
     
     // [HttpPost(ApiEndPointConstant.User.LoginCustomer)]
     // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
     // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
     // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
     // public async Task<IActionResult> LoginCustomer([FromBody] LoginRequest loginRequest)
     // {
     //     var loginResponse = await _userService.LoginCustomer(loginRequest);
     //     return StatusCode(int.Parse(loginResponse.status), loginResponse);
     // }
     //
     [HttpDelete(ApiEndPointConstant.User.DeleteUser)]
     [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
     {
         var response = await _userService.DeleteUser(id);
         return StatusCode(int.Parse(response.status), response);
     }
     
     [HttpGet(ApiEndPointConstant.User.GetAllUser)]
     [ProducesResponseType(typeof(IPaginate<GetUserResponse>), StatusCodes.Status200OK)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> GetAllUser([FromQuery] int? page, [FromQuery] int? size)
     {
         int pageNumber = page ?? 1;
         int pageSize = size ?? 10;
         var response = await _userService.GetAllUser(pageNumber, pageSize);
         if(response.data == null)
         {
             response.data = new List<User>();
         }
         return StatusCode(int.Parse(response.status), response);
     }
     
     [HttpGet(ApiEndPointConstant.User.GetUserById)]
     [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> GetUserById([FromRoute] Guid id)
     {
         var response = await _userService.GetUser(id);
         return StatusCode(int.Parse(response.status), response);
     }
     
     [HttpGet(ApiEndPointConstant.User.GetUser)]
     [ProducesResponseType(typeof(GetUserResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> GetUser()
     {
         var response = await _userService.GetUser();
         return StatusCode(int.Parse(response.status), response);
     }
     
     [HttpPut(ApiEndPointConstant.User.UpdateUser)]
     [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> UpdateUser([FromRoute] Guid id, [FromBody] UpdateUserRequest updateUserRequest)
     {
         var response = await _userService.UpdateUser(id, updateUserRequest);
         return StatusCode(int.Parse(response.status), response);
     }
     [HttpPost(ApiEndPointConstant.User.VerifyOtp)]
     [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest verifyOtpRequest)
     {
         // Gọi phương thức dịch vụ để xác thực OTP
         bool isOtpValid = await _userService.VerifyOtp(verifyOtpRequest.UserId, verifyOtpRequest.otpCheck);
     
         return Ok(isOtpValid);
     }
     
     [HttpPost(ApiEndPointConstant.User.ForgotPassword)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
     {
         var response = await _userService.ForgotPassword(request);
     
         return StatusCode(int.Parse(response.status), response);
     }
     
     [HttpPost(ApiEndPointConstant.User.ResetPassword)]
     [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> ResetPassword([FromBody] VerifyAndResetPasswordRequest request)
     {
         var response = await _userService.ResetPassword(request);
     
         return Ok(response);
     }
     
     [HttpPost(ApiEndPointConstant.User.VerifyForgotPassword)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> VerifyForgotPassword([FromBody] VerifyForgotPasswordRequest request)
     {
         var response = await _userService.VerifyForgotPassword(request.userId, request.otp);
     
         return StatusCode(int.Parse(response.status), response);
     }
     [HttpPost(ApiEndPointConstant.User.ChangePassword)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
     [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
     [ProducesErrorResponseType(typeof(ProblemDetails))]
     public async Task<IActionResult> ChangPassword([FromBody] ChangePasswordRequest request)
     {
         var response = await _userService.ChangePassword(request);
     
         return StatusCode(int.Parse(response.status), response);
     }
 }