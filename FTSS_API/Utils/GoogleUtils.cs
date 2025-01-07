using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTSS_API.Utils
{
    public class GoogleUtils
    {
        public class GoogleDriveService
        {
            private readonly IConfiguration _configuration;

            public GoogleDriveService(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public async Task<string> UploadToGoogleDriveAsync(IFormFile fileToUpload)
            {
                // Danh sách định dạng tệp được phép
                var allowedExtensions = new List<string> { ".docx", ".pdf", ".mov", ".xlsx", ".mp4", ".jpg", ".txt" };

                try
                {
                    // Kiểm tra file đầu vào
                    if (fileToUpload == null)
                        throw new ArgumentNullException(nameof(fileToUpload), "Tệp tải lên không được null.");

                    var fileExtension = Path.GetExtension(fileToUpload.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                        throw new InvalidOperationException(
                            $"Định dạng tệp '{fileExtension}' không được phép. Chỉ các định dạng sau được hỗ trợ: {string.Join(", ", allowedExtensions)}");

                    // Đọc cấu hình thư mục Google Drive
                    var folderId = _configuration["Authentication:GoogleDrive:FolderId"];
                    if (string.IsNullOrEmpty(folderId))
                        throw new InvalidOperationException("FolderId không được cấu hình.");

                    // Đọc thông tin xác thực Google Drive
                    var credentialsSection = _configuration.GetSection("Authentication:GoogleDrive:CredentialsPath");
                    var credentialsJson = credentialsSection.GetChildren().ToDictionary(x => x.Key, x => x.Value);
                    var credentialsJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(credentialsJson);

                    GoogleCredential credential;
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentialsJsonString)))
                    {
                        credential = GoogleCredential.FromStream(stream)
                            .CreateScoped(new[] { DriveService.ScopeConstants.DriveFile });
                    }

                    // Khởi tạo dịch vụ Google Drive
                    var service = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Google Drive Upload Console App"
                    });

                    if (service == null)
                        throw new InvalidOperationException("Dịch vụ Google Drive chưa được khởi tạo.");

                    // Tạo metadata cho file
                    var fileMetaData = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = fileToUpload.FileName,
                        Parents = new List<string> { folderId }
                    };

                    // Tải file lên Google Drive
                    using (var stream = fileToUpload.OpenReadStream())
                    {
                        if (stream == null)
                            throw new InvalidOperationException("Luồng dữ liệu của tệp tải lên không hợp lệ.");

                        var request = service.Files.Create(fileMetaData, stream, fileToUpload.ContentType);
                        request.Fields = "id";
                        await request.UploadAsync();

                        var file = request.ResponseBody;
                        if (file == null)
                            throw new InvalidOperationException($"Không thể tải lên tệp: {fileToUpload.FileName}.");

                        // Trả về URL của file trên Google Drive
                        string fileUrl = $"https://drive.google.com/file/d/{file.Id}/view?usp=sharing";
                        return fileUrl;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading file to Google Drive: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
