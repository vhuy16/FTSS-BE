using AutoMapper;
using FTSS_API.Service.Implement.Implement;
using FTSS_API.Service.Interface;
using FTSS_Model.Context;
using FTSS_Repository.Interface;

namespace FTSS_API.Service.Implement
{
    public class SubCategoryService : BaseService<SubCategoryService>, ISubCategoryService
    {
        public SubCategoryService(IUnitOfWork<MyDbContext> unitOfWork,
            ILogger<SubCategoryService> logger,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor
            ) : base(unitOfWork, logger, mapper, httpContextAccessor)
        {

        }
    }
}
