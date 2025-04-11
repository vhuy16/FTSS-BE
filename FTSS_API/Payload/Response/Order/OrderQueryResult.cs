namespace FTSS_API.Payload.Response.Order;

public class OrderQueryResult
{
    public FTSS_Model.Entities.Order Order { get; set; }
    public UserInfo User { get; set; }
    public VoucherInfo Voucher { get; set; }
    public List<PaymentInfo> Payments { get; set; }
    public List<OrderDetailInfo> OrderDetails { get; set; }
    public SetupPackageInfo SetupPackage { get; set; }
}

public class UserInfo
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
}

public class VoucherInfo
{
    public string VoucherCode { get; set; }
    public string DiscountType { get; set; }
    public decimal Discount { get; set; }
    public decimal? MaximumOrderValue { get; set; }
}

public class PaymentInfo
{
    public Guid Id { get; set; }
    public string PaymentMethod { get; set; }
    public string PaymentStatus { get; set; }
    public string BankHolder { get; set; }
    public string BankName { get; set; }
    public string BankNumber { get; set; }
}

public class OrderDetailInfo
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public ProductInfo Product { get; set; }
}

public class ProductInfo
{
    public string ProductName { get; set; }
    public string SubCategoryName { get; set; }
    public string CategoryName { get; set; }
    public string Image { get; set; }
}

public class SetupPackageInfo
{
    public Guid SetupPackageId { get; set; }
    public string SetupName { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public bool IsDelete { get; set; }
    public string Size { get; set; }
    public List<SetupPackageProductInfo> Products { get; set; }
}

public class SetupPackageProductInfo
{
    public Guid Id { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public int InventoryQuantity { get; set; }
    public string Status { get; set; }
    public string CategoryName { get; set; }
    public string Image { get; set; }
}
