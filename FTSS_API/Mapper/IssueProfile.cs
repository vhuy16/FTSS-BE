using AutoMapper;
using FTSS_API.Payload.Request;
using FTSS_API.Payload.Request.Solution;
using FTSS_API.Payload.Response.Issue;
using FTSS_Model.Entities;

public class IssueProfile : Profile
{
    public IssueProfile()
    {
        // Map từ request sang entity (chỉ map các trường cơ bản, không map Solutions vì đã xử lý riêng)
        CreateMap<AddUpdateIssueRequest, Issue>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDelete, opt => opt.Ignore())
            .ForMember(dest => dest.Solutions, opt => opt.Ignore()) // Không map từ JSON
            .ForMember(dest => dest.IssueImage, opt => opt.Ignore());

        // Map từ SolutionRequest (sau khi parse từ JSON) sang entity
        CreateMap<AddUpdateSolutionRequest, Solution>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDelete, opt => opt.Ignore());

        // Map từ entity sang response
        CreateMap<Issue, IssueResponse>()
            .ForMember(dest => dest.IssueCategoryName, opt => opt.MapFrom(src =>
                src.IssueCategory != null ? src.IssueCategory.IssueCategoryName : null))
            .ForMember(dest => dest.Solutions, opt => opt.MapFrom(src => src.Solutions));

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
