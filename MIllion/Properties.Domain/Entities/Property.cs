using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Properties.Domain.Common;

namespace Properties.Domain.Entities;

public class Property : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Address { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string CodeInternal { get; set; }
    
    [Required]
    public int Year { get; set; }
    
    public int OwnerId { get; set; }
    
    [ForeignKey(nameof(OwnerId))]
    public virtual Owner Owner { get; set; }
    
    public virtual ICollection<PropertyTrace> PropertyTraces { get; set; } = new List<PropertyTrace>();
    public virtual ICollection<PropertyImage> PropertyImages { get; set; } = new List<PropertyImage>();
}