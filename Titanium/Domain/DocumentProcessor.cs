using Serilog;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Document;

namespace Titanium.Domain;

public class DocumentProcessor
{
    private readonly ConfigManager _config;
    private readonly PathFinder _pathFinder;
    private readonly ILogger _logger;
    public Action<BaseAspect> DocumentCompletedCallback { get; set; }

    public DocumentProcessor(ConfigManager config, PathFinder pathFinder, ILogger logger)
    {
        _config = config;
        _pathFinder = pathFinder;
        _logger = logger;
    }

    public List<BaseAspect> ProcessDocument(Doc doc, Func<string, List<BaseAspect>> processor)
    {
        List<BaseAspect> aspects = new();
        string[] masterFiles = _pathFinder.GetMasterFiles(_config.CurrentProject, doc.Id);
        foreach (string masterFile in masterFiles)
        {
            _logger.Information($"Processing Master: {masterFile}");
            List<BaseAspect> baseAspects = processor.Invoke(masterFile);
            baseAspects.ForEach(aspect =>
            {
                string path = _pathFinder.GetAspectFilePath(aspect.MasterName, _config.CurrentProject, doc.Id,
                    aspect.Name, doc.Name,
                    aspect.Extension, aspect.Variant);
                _logger.Information($"Save Aspect -> {path}");
                aspect.Save(path);
                aspects.Add(aspect);
            });
        }

        return aspects;
    }
}