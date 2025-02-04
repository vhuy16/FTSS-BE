﻿using FTSS_Model.Enum;

namespace FTSS_API.Payload.Response.User;

public class CreateNewAccountResponse
{
    
    public Guid? Id { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public GenderEnum? Gender { get; set; }
    public string? PhoneNumber { get; set; }
}