namespace FTSS_API.Payload.Response.Book
{
    public class GetListTechResponse
    {
        public Guid TechId { get; set; }

        public string TechName { get; set; } = null!;

        public string FullName { get; set; } = null!;
    }
}
