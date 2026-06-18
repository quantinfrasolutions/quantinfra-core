using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace QuantInfra.Databases.Main.Models.Audit;

[Table("audit_entries", Schema = "audit")]
public class AuditEntry
{
    [Column("audit_entry_id"), Required, Key] public int AuditEntryId { get; set; }
    [Column("dt")] public Instant Dt { get; set; }
    [Column("user_id")] public int UserId { get; set; }
}