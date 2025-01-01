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
using File = System.IO.File;

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

            public List<string> UploadToGoogleDriveAsync(List<IFormFile> filesToUpload)
            {
                var allowedExtensions = new List<string> { ".docx", ".pdf", ".mov", ".xlsx", ".mp4", ".jpg", ".txt" };
                var fileUrls = new List<string>();

                var folderId = _configuration["Authentication:GoogleDrive:FolderId"];
                if (string.IsNullOrEmpty(folderId))
                {
                    throw new InvalidOperationException("FolderId is missing in the configuration.");
                }

                GoogleCredential credential;
                var credentialsSection = _configuration.GetSection("Authentication:GoogleDrive:CredentialsPath");
                var credentialsJson = credentialsSection.GetChildren().ToDictionary(x => x.Key, x => x.Value);
                var credentialsJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(credentialsJson);

                try
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(credentialsJsonString)))
                    {
                        credential = GoogleCredential.FromStream(stream)
                            .CreateScoped(new[] { DriveService.ScopeConstants.DriveFile });
                    }

                    var service = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Google Drive Upload Console App"
                    });

                    foreach (var fileToUpload in filesToUpload)
                    {
                        var fileExtension = Path.GetExtension(fileToUpload.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            throw new InvalidOperationException($"Invalid file format: {fileToUpload.FileName}");
                        }

                        var fileMetaData = new Google.Apis.Drive.v3.Data.File()
                        {
                            Name = fileToUpload.FileName,
                            Parents = new List<string> { folderId }
                        };

                        FilesResource.CreateMediaUpload request;
                        using (var stream = fileToUpload.OpenReadStream())
                        {
                            request = service.Files.Create(fileMetaData, stream, fileToUpload.ContentType);
                            request.Fields = "id";
                            request.UploadAsync().Wait();
                        }

                        if (request.ResponseBody == null)
                        {
                            throw new InvalidOperationException($"File upload failed: {fileToUpload.FileName}");
                        }

                        var file = request.ResponseBody;
                        var permission = new Permission()
                        {
                            Type = "anyone",
                            Role = "reader"
                        };
                        service.Permissions.Create(permission, file.Id).Execute();

                        string fileUrl = $"https://drive.google.com/uc?id={file.Id}/view?usp=sharing";
                        fileUrls.Add(fileUrl);
                    }

                    return fileUrls;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading files to Google Drive: {ex.Message}");
                    return null;
                }
            }
        }
    }
}