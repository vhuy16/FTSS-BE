namespace FTSS_API.Payload.Request.SetupPackage;

public class UpdateSetupPackageRequest
{
    public string? SetupName { get; set; }
    public string? Description { get; set; }
    public IFormFile? ImageFile { get; set; }
    
    /// <summary>
    /// JSON string containing the list of products with their quantities
    /// Format: [{"ProductId":"guid","Quantity":1},{"ProductId":"guid","Quantity":2}]
    /// </summary>
    public string? ProductItemsJson { get; set; }
}
