using Tesseract;

namespace Titanium.Domain.Aspect;

public class PixDocAspect : DocAspect<Pix>
{
    public PixDocAspect(string masterName, Pix content, string name, string extension, string variant) : base(masterName,
        content, name, extension, variant)
    {
    }

    public override void Save(string filePath)
    {
        Content.Save(filePath);
    }
}