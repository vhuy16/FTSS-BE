using Newtonsoft.Json;

namespace FTSS_API.Payload;

public class ApiResponse
{
    public string status { get; set; }
 
    public string? message { get; set; }
    
    public object? data { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string>? listErrorMessage { get; set; }
}