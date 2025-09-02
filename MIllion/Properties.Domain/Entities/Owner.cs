using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Properties.Domain.Common;

namespace Properties.Domain.Entities;

public class Owner : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Address { get; set; }
    
    [Column(TypeName = "nvarchar(MAX)")]
    public string? Photo { get; set; }
    
    [Required]
    public DateTime Birthday { get; set; }
    
    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();
}