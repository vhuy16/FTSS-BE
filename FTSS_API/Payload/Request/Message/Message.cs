
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FTSS_API.Models;

[Table("messages")]
public class Message : BaseModel
{
    [PrimaryKey("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("room_id")]
    public Guid? RoomId { get; set; }

    [Column("username")]
    public string Username { get; set; }

    [Column("role")]
    public string Role { get; set; }

    [Column("text")]
    public string Text { get; set; }
    
    [Column("file_urls")]
    public List<string>? FileUrls { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; }
}