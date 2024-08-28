using Tesseract;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Paths;

namespace Titanium.Adapters.Aspect.OCR;

[AspectMetadata("ocr", "alto")]
public class ImageAltoXmlAspectProcessor : TesseractAspectProcessor
{
    private PathFinder _pathfinder;
    public ImageAltoXmlAspectProcessor(PathFinder pathfinder)
    {
        _pathfinder = pathfinder;
    }

    public override DocAspect ProcessAspect(Doc doc, string masterFilePath)
    {
        using TesseractEngine engine = new(_pathfinder.GetTessdataPath(), "eng", EngineMode.Default);
        using Pix? img = Pix.LoadFromFile(masterFilePath);
        using Page? page = engine.Process(img);
        return DocAspect.NewXml(masterFilePath, page.GetText(), "ocr.alto"); 
    }
}