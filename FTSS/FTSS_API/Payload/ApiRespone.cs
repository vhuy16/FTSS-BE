namespace FTSS_API.Payload;

public class ApiRespone
{
    public string status { get; set; }
 
    public string? message { get; set; }
    
    public object? data { get; set; }
}