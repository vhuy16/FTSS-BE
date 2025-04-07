using FTSS_API.Payload;
using FTSS_API.Payload.Request;
using Supabase;

namespace FTSS_API.Service.Interface;

public interface IIssueService
{
    Task<ApiResponse> CreateIssue(AddUpdateIssueRequest request, Client client);

    Task<ApiResponse> GetAllIssues(int page, int size, bool? isAscending, Guid? issueCategoryId = null,
        string issueTitle = null);
    Task<ApiResponse> GetIssueById(Guid id);
    Task<ApiResponse> UpdateIssue(Guid id, AddUpdateIssueRequest request);
    Task<ApiResponse> DeleteIssue(Guid id);
}
