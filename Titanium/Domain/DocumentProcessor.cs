using Serilog;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Paths;

namespace Titanium.Domain;

public class DocumentProcessor
{
    private readonly ConfigManager _config;
    private readonly PathFinder _pathFinder;
    private readonly ILogger _logger;

    public DocumentProcessor(ConfigManager config, PathFinder pathFinder, ILogger logger)
    {
        _config = config;
        _pathFinder = pathFinder;
        _logger = logger;
    }

    public List<Aspect.Aspect> ProcessDocument(Doc doc, Func<string, List<Aspect.Aspect>> processor)
    {
        List<Aspect.Aspect> aspects = new();
        
        string[] masterFiles = _pathFinder.GetMasterFiles(_config.CurrentProject, doc.Id);
        foreach (string masterFile in masterFiles)
        {
            _logger.Information($"Processing Master: {masterFile}");
            List<Aspect.Aspect> baseAspects = processor.Invoke(masterFile);
            baseAspects.ForEach(aspect =>
            {
                string path = _pathFinder.GetAspectFilePath(Path.GetFileName(aspect.MasterName), _config.CurrentProject, doc.Id,
                    aspect.Name, doc.Name,
                    aspect.Extension, aspect.Variant);
                aspect.Save(path);
                aspects.Add(aspect);
            });
        }

        return aspects;
    }
}