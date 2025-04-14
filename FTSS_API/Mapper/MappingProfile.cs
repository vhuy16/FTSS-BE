using AutoMapper;
using FTSS_API.Payload.Response.Order;
using FTSS_API.Payload.Response.SetupPackage;
using FTSS_Model.Entities;

namespace FTSS_API.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Order, GetOrderResponse>()
            .ForMember(dest => dest.OderCode, opt => opt.MapFrom(src => src.OrderCode))
            .ForMember(dest => dest.ModifyDate  , opt => opt.MapFrom(src => src.ModifyDate))
            .ForMember(dest => dest.BuyerName, opt => opt.MapFrom(src => src.RecipientName))
            // Map từ Order.Payments sang GetOrderResponse.Payment
            .ForMember(dest => dest.Payment, opt => opt.MapFrom(src => 
                src.Payments != null && src.Payments.Any() ? src.Payments.FirstOrDefault() : null))
            // Map từ Order.User sang GetOrderResponse.userResponse
            .ForMember(dest => dest.userResponse, opt => opt.MapFrom(src => src.User))
            // Map từ Order.SetupPackage sang GetOrderResponse.SetupPackage
            .ForMember(dest => dest.SetupPackage, opt => opt.MapFrom(src => src.SetupPackage));

        CreateMap<SetupPackage, SetupPackageResponse>()
            .ForMember(dest => dest.SetupPackageId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SetupName, opt => opt.MapFrom(src => src.SetupName))
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Price ?? 0))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? "N/A"))
            .ForMember(dest => dest.IsDelete, opt => opt.MapFrom(src => src.IsDelete ?? false))
            .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
            .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
            .ForMember(dest => dest.Size, opt => opt.MapFrom(src =>
                src.SetupPackageDetails
                    .Where(spd => spd.Product != null && 
                                 spd.Product.SubCategory != null && 
                                 spd.Product.SubCategory.Category != null && 
                                 spd.Product.SubCategory.Category.CategoryName == "Bể")
                    .Select(spd => spd.Product.Size)
                    .FirstOrDefault()))
            .ForMember(dest => dest.Products, opt => opt.MapFrom(src => 
                src.SetupPackageDetails));

        CreateMap<SetupPackageDetail, ProductResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Product != null ? src.Product.Id : Guid.Empty))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName ?? "Unknown" : "Unknown"))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price : 0))
            .ForMember(dest => dest.InventoryQuantity, opt => opt.MapFrom(src => src.Product != null ? src.Product.Quantity ?? 0 : 0))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Product != null ? src.Product.Status : "false"))
            .ForMember(dest => dest.IsDelete, opt => opt.MapFrom(src => src.Product != null ? src.Product.IsDelete ?? false : false))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src =>
                src.Product != null && 
                src.Product.SubCategory != null && 
                src.Product.SubCategory.Category != null 
                    ? src.Product.SubCategory.Category.CategoryName ?? "Unknown" 
                    : "Unknown"))
            .ForMember(dest => dest.images, opt => opt.MapFrom(src =>
                src.Product != null && 
                src.Product.Images != null && 
                src.Product.Images.Any(img => img.IsDelete == false)
                    ? src.Product.Images
                        .Where(img => img.IsDelete == false)
                        .OrderBy(img => img.CreateDate)
                        .Select(img => img.LinkImage)
                        .FirstOrDefault() 
                    : "NoImageAvailable"));

        // Map Payment sang PaymentResponse
        CreateMap<Payment, GetOrderResponse.PaymentResponse>()
            .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.Id));

        // Map User sang UserResponse
        CreateMap<User, GetOrderResponse.UserResponse>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.UserName ?? "Unknown"))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? "Unknown"))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber ?? "Unknown"));

        CreateMap<Voucher, GetOrderResponse.VoucherResponse>();

        CreateMap<OrderDetail, GetOrderResponse.OrderDetailCreateResponse>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName ?? "Unknown" : "Unknown"))
            .ForMember(dest => dest.LinkImage, opt => opt.MapFrom(src =>
                src.Product != null && 
                src.Product.Images != null && 
                src.Product.Images.Any()
                    ? src.Product.Images.FirstOrDefault().LinkImage ?? "NoImageAvailable" 
                    : "NoImageAvailable"))
            .ForMember(dest => dest.SubCategoryName, opt => opt.MapFrom(src =>
                src.Product != null && 
                src.Product.SubCategory != null 
                    ? src.Product.SubCategory.SubCategoryName ?? "NoSubCategory" 
                    : "NoSubCategory"))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src =>
                src.Product != null && 
                src.Product.SubCategory != null && 
                src.Product.SubCategory.Category != null 
                    ? src.Product.SubCategory.Category.CategoryName ?? "NoCategory" 
                    : "NoCategory"));
    }
}