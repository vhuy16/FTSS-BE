namespace FTSS_API.Payload;

public class ApiResponse
{
    public string status { get; set; }
 
    public string? message { get; set; }
    
    public object? data { get; set; }
}