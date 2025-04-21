namespace FTSS_API.Payload.Response.User;

public class GetUserResponse
{
    public Guid? UserId { get; set; }
    public string Username { get; set; }
    public string? FullName { get; set; }
    public string Address { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? Role {  get; set; }
    public bool? IsDeleted { get; set; }
    public string? CityId  {get; set; }
    public string? DistrictId {get; set; }
    public string? BankName {get; set; }
    public string? BankNumber {get; set; }
    public string? BankHolder {get; set; }
}