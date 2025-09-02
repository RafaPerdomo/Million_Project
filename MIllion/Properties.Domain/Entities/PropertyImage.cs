using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Properties.Domain.Common;

namespace Properties.Domain.Entities;

public class PropertyImage : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(MAX)")]
    public string File { get; set; }
    
    public int PropertyId { get; set; }
    
    [ForeignKey(nameof(PropertyId))]
    public virtual Property Property { get; set; }
}