namespace SHARED;

public class CollectionItemDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } 
    public string Code { get; set; }
    public string Description { get; set; }
    public string Symbol { get; set; }
    
    public override bool Equals(object obj)
    {
        if (obj is CollectionItemDto other)
        {
            return Id.HasValue && other.Id.HasValue && Id.Value == other.Id.Value;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id.HasValue ? Id.Value.GetHashCode() : 0;
    }
}