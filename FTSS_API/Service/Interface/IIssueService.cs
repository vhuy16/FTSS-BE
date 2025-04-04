using FTSS_API.Payload;
using FTSS_API.Payload.Request;

namespace FTSS_API.Service.Interface;

public interface IIssueService
{
    Task<ApiResponse> CreateIssue(AddUpdateIssueRequest request);
    Task<ApiResponse> GetAllIssues();
    Task<ApiResponse> GetIssue(Guid id);
    Task<ApiResponse> UpdateIssue(Guid id, AddUpdateIssueRequest request);
    Task<ApiResponse> DeleteIssue(Guid id);
}
