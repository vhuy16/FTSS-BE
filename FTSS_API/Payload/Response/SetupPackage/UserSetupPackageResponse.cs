using FTSS_API.Payload.Response.SetupPackage;

public class UserSetupPackageResponse
{
    public Guid UserId { get; set; } // ID của User
    public string UserName { get; set; } // Tên User
    public List<SetupPackageResponse> SetupPackages { get; set; } // Danh sách SetupPackage của User

    public UserSetupPackageResponse()
    {
        SetupPackages = new List<SetupPackageResponse>();
    }
}
