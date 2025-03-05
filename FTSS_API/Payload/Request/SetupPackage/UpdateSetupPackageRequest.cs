namespace FTSS_API.Payload.Request.SetupPackage;

public class UpdateSetupPackageRequest
{
    public Guid SetupPackageId { get; set; }  // ID của SetupPackage cần cập nhật
    public string SetupName { get; set; }  // Tên mới của SetupPackage
    public string Description { get; set; }  // Mô tả mới
    
    public string Status {get; set;}
    public IFormFile? ImageFile { get; set; }  // Ảnh mới (nếu có)
    public List<UpdateProductSetupItem> ProductList { get; set; }  // Danh sách sản phẩm cần cập nhật

    public UpdateSetupPackageRequest()
    {
        ProductList = new List<UpdateProductSetupItem>();
    }
}

public class UpdateProductSetupItem
{
    public Guid ProductId { get; set; }  // ID của sản phẩm
    public int Quantity { get; set; }  // Số lượng sản phẩm
}