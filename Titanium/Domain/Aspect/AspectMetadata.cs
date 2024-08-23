using System.ComponentModel.Composition;

namespace Titanium.Domain.Aspect;

[MetadataAttribute]
public class AspectMetadataAttribute :Attribute
{
    public AspectMetadataAttribute()
    {
        
    }
    public AspectMetadataAttribute(string type, string variant)
    {
        Type = type;
        Variant = variant;
    }
    public string Type { get; set; }
    public string Variant { get; set; }

    public static AspectMetadataAttribute Parse(string raw)
    {
        var parts = raw.Split('.');
        return new AspectMetadataAttribute(parts[0], parts[1]);
    }
}