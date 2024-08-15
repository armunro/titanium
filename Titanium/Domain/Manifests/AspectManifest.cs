namespace Titanium.Domain.Manifests;

public class AspectManifest
{
    public string Name { get; set; }
    public string Variant { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;

}