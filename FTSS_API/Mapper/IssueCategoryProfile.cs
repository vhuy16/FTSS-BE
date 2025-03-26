using AutoMapper;
using FTSS_API.Payload.Request.IssueCategory;
using FTSS_API.Payload.Response.IssueCategory;
using FTSS_Model.Entities;

public class IssueCategoryProfile : Profile
{
    public IssueCategoryProfile()
    {
        CreateMap<AddUpdateIssueCategoryRequest, IssueCategory>()
            .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsDelete, opt => opt.Ignore());

        CreateMap<IssueCategory, IssueCategoryResponse>();
    }
}