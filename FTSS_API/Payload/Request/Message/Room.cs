using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FTSS_API.Models;

[Table("room")]
public class Room : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("manager_id")]
    public Guid ManagerId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}