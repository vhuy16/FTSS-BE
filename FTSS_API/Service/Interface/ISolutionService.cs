using FTSS_API.Payload;
using FTSS_API.Payload.Request.Solution;

namespace FTSS_API.Service.Interface;

public interface ISolutionService
{
    Task<ApiResponse> CreateSolution(AddUpdateSolutionRequest request);
    Task<ApiResponse> GetAllSolutions();
    Task<ApiResponse> GetSolutionById(Guid id);
    Task<ApiResponse> UpdateSolution(Guid id, AddUpdateSolutionRequest request);
    Task<ApiResponse> DeleteSolution(Guid id);
}