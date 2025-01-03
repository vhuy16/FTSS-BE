namespace FTSS_API.Utils;

public class SupabaseUltils
{
    public async Task<List<string>> SendImagesAsync(List<IFormFile> images, Supabase.Client client)
    {
        var urls = new List<string>();

        foreach (var image in images)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await image.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                var bucket = client.Storage.From("FTSS");
                var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                Console.WriteLine($"Uploading file: {fileName}");

                await bucket.Upload(imageBytes, fileName);
                Console.WriteLine($"File uploaded: {fileName}");

                var publicUrl = bucket.GetPublicUrl(fileName);
                Console.WriteLine($"Generated public URL: {publicUrl}");

                urls.Add(publicUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {image.FileName}: {ex.Message}");
            }


        }

        return urls;
    }
}