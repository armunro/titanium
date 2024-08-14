namespace Titanium.Domain.Document;

public class DocManifest
{
    public string Id { get; set; }
    public string Project { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public DocOriginManifest Origin { get; set; }
}