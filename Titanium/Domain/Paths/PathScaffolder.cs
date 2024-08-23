using Serilog;
using Titanium.Domain.Config;

namespace Titanium.Domain.Paths;

public class PathScaffolder
{
    private readonly PathFinder _pathFinder;
    private readonly ConfigManager _config;
    private readonly ILogger _logger;

    public PathScaffolder(PathFinder pathFinder, ConfigManager config, ILogger logger)
    {
        _pathFinder = pathFinder;
        _config = config;
        _logger = logger;
    }

 
    public string ScaffoldDocumentAspectDirectory(string configCurrentProject, string docId, string aspectName)
    {
        string docAspectPath = _pathFinder.GetDocAspectPath(_config.CurrentProject, docId, aspectName);
        Directory.CreateDirectory(docAspectPath);
        return docAspectPath;
    }

    public string ScaffoldProjectDirectory(string projectName)
    {
        string projectPath = _pathFinder.GetProjectPath(projectName);
        Directory.CreateDirectory(projectName);
        return projectPath;
    }

    public string ScaffoldDocumentDirectory(string docProject, string docId)
    {
        string docBasePath = _pathFinder.GetDocPath(docProject, docId);
        string docMastersPath = _pathFinder.GetMasterDirectory(docProject, docId);
        Directory.CreateDirectory(docBasePath);
        Directory.CreateDirectory(docMastersPath);
        return docBasePath;
    }
    
    
    public string ScaffoldTessdataDirectory()
    {
        string tessdataPath = _pathFinder.GetTessdataPath();
        Directory.CreateDirectory(tessdataPath);
        return tessdataPath;
    }


    public string ScaffoldPromptsDirectory()
    {
        string promptsPath = _pathFinder.GetOcrAspectPromptPath();
        Directory.CreateDirectory(promptsPath);
        return promptsPath;
    }
}