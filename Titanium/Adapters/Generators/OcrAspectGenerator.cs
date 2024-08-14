using Tesseract;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Document;

namespace Titanium.Adapters.Generators;

public class OcrAspectGenerator : IAspectGenerator
{
    readonly ConfigManager _config;
    readonly PathFinder _pathfinder;
    private const string AspectName = "ocr";

    public OcrAspectGenerator(
        ConfigManager config,
        PathFinder pathfinder)
    {
        _config = config;
        _pathfinder = pathfinder;
    }

    public List<BaseAspect> GenerateAspects(Doc doc, string masterFilePath)
    {
        List<BaseAspect> aspects = new();
        string aspectBase = _pathfinder.GetDocAspectPath(_config.CurrentProject, doc.Id, AspectName);
        Directory.CreateDirectory(aspectBase);
        string masterName = Path.GetFileNameWithoutExtension(masterFilePath);
        try
        {
            using TesseractEngine engine = new(_pathfinder.GetTessdataPath(), "eng", EngineMode.Default);
            using Pix? img = Pix.LoadFromFile(masterFilePath);
            using Page? page = engine.Process(img);

            aspects.Add(BaseAspect.NewText(masterName, page.GetText(), AspectName));
            aspects.Add(BaseAspect.NewXml(masterName, page.GetAltoText(0), "alto"));
            aspects.Add(BaseAspect.NewHtml(masterName, page.GetHOCRText(0), "hocr"));
            aspects.Add(BaseAspect.NewPix(masterName, page.GetThresholdedImage(), "thresholded"));
        }
        catch (Exception e)
        {
            aspects.Add(BaseAspect.NewOcrErrorText(masterName, e));
        }

        return aspects;
    }
}