﻿namespace FTSS_API.Payload.Request;

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set;} = null!;
    public string ConfirmPassword { get; set; } = null!;
}