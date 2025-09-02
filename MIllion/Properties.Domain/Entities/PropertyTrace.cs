using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Properties.Domain.Common;

namespace Properties.Domain.Entities;

public class PropertyTrace : BaseEntity
{
    [Required]
    public DateTime DateSale { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Tax { get; set; }
    
    public int PropertyId { get; set; }
    
    [ForeignKey(nameof(PropertyId))]
    public virtual Property Property { get; set; }
}