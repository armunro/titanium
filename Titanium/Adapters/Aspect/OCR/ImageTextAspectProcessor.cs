using Tesseract;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Paths;

namespace Titanium.Adapters.Aspect.OCR;

[AspectMetadata("ocr", "text")]
public class ImageTextAspectProcessor : TesseractAspectProcessor
{
    private PathFinder _pathfinder;
    public ImageTextAspectProcessor(PathFinder pathfinder)
    {
        _pathfinder = pathfinder;
    }

    public override Domain.DocAspect ProcessAspect(Doc doc, string masterFilePath)
    {
        using TesseractEngine engine = new(_pathfinder.GetTessdataPath(), "eng", EngineMode.Default);
        using Pix? img = Pix.LoadFromFile(masterFilePath);
        using Page? page = engine.Process(img);
        return Domain.DocAspect.NewText(masterFilePath, page.GetText(), "ocr.text");
    }
}