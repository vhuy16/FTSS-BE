using FTSS_Model.Enum;

namespace FTSS_API.Payload.Request;

public class UpdateUserRequest
{
    public GenderEnum? Gender { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public bool? IsDelete { get; set; }
}