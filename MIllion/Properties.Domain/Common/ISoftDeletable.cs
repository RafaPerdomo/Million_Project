namespace Properties.Domain.Common;

public interface ISoftDeletable
{
    bool IsActive { get; set; }
}
