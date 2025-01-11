using FTSS_API.Payload;
using FTSS_API.Payload.Request.Product;

namespace FTSS_API.Service.Implement.Implement;

public interface IProductService
{
    Task<ApiResponse> CreateProduct(CreateProductRequest createProductRequest, Supabase.Client client);

    Task<ApiResponse> GetListProduct(int page, int size, bool? isAscending, string? SubcategoryName,
        string? productName,
        string? cateName,
        string? status,
        decimal? minPrice,
        decimal? maxPrice);

    Task<ApiResponse> GetAllProduct(int page, int size, bool? isAscending, string? SubcategoryName,
        string? productName,
        string? cateName,

        decimal? minPrice,
        decimal? maxPrice);

    Task<ApiResponse> GetListProductBySubCategoryId(Guid subCateId, int page, int size);
//Task<bool> UpdateProduct(Guid ProID, UpdateProductRequest updateProductRequest);
    Task<ApiResponse> UpdateProduct(Guid productId, UpdateProductRequest updateProductRequest, Supabase.Client client);
    Task<bool> DeleteProduct(Guid productId);
    Task<ApiResponse> GetProductById(Guid productId);

    Task<ApiResponse> EnableProduct(Guid productId);

    Task<ApiResponse> UpImageForDescription(IFormFile formFile);
}