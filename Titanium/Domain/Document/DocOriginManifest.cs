namespace Titanium.Domain.Document;

public class DocOriginManifest
{
    public string Hostname { get; set; }
    public string Path { get; set; }
    public DateTime Created { get; set; } = DateTime.Now;

}