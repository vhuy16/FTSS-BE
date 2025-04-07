using AutoMapper;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Response.Issue;
using FTSS_Model.Entities;
public class IssueProfile : Profile
{
    public IssueProfile()
    {
        // Map từ request sang entity
        CreateMap<AddUpdateIssueRequest, Issue>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDelete, opt => opt.Ignore())
            .ForMember(dest => dest.Solutions, opt => opt.Ignore()); // Bỏ qua Solutions để tránh tạo tự động

        // Sửa ánh xạ SolutionRequest để không tạo thừa solution
        CreateMap<SolutionRequest, Solution>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDelete, opt => opt.Ignore());

        // Map từ entity sang response
        CreateMap<Issue, IssueResponse>()
            .ForMember(dest => dest.IssueCategoryName, opt => opt.MapFrom(src => 
                src.IssueCategory != null ? src.IssueCategory.IssueCategoryName : null))
            .ForMember(dest => dest.Solutions, opt => opt.MapFrom(src => src.Solutions));
            // SolutionCount và ProductCount đã được định nghĩa là computed properties trong IssueResponse

        CreateMap<Solution, SolutionResponse>()
            .ForMember(dest => dest.Products, opt => opt.MapFrom(src => 
                src.SolutionProducts != null ? 
                    src.SolutionProducts
                        .Where(sp => sp != null && sp.Product != null)
                        .Select(sp => sp.Product)
                        .ToList() : 
                    new List<Product>()));

        CreateMap<Product, IssueProductResponse>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.ProductDescription, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ProductImageUrl, opt => opt.MapFrom(src => src.Images.FirstOrDefault().LinkImage));

    }
}