﻿namespace FTSS_API.Payload.Response.User;

public class LoginResponse
{
    public string token {  get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string RoleEnum { get; set; }
}