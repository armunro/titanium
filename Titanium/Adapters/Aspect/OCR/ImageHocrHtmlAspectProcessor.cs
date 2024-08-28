using Tesseract;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Paths;

namespace Titanium.Adapters.Aspect.OCR;

[AspectMetadata("ocr", "hocr")]
public class ImageHocrHtmlAspectProcessor : TesseractAspectProcessor
{
    private PathFinder _pathfinder;
    public ImageHocrHtmlAspectProcessor(PathFinder pathfinder)
    {
        _pathfinder = pathfinder;
    }

    public override Domain.DocAspect ProcessAspect(Doc doc, string masterFilePath)
    {
        using TesseractEngine engine = new(_pathfinder.GetTessdataPath(), "eng", EngineMode.Default);
        using Pix? img = Pix.LoadFromFile(masterFilePath);
        using Page? page = engine.Process(img);
        return Domain.DocAspect.NewHtml(masterFilePath, page.GetText(), "ocr.hocr");
    }
}