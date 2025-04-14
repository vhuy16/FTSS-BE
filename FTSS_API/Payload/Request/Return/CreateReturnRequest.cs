namespace FTSS_API.Payload.Request.Return
{
    public class CreateReturnRequest
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; }
        public List<IFormFile> MediaFiles { get; set; } // Danh sách file ảnh hoặc video
    }
}