using CSharpVitamins;

namespace Titanium.Domain;

public class PathFinder
{
    private string GetUserHomePath() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public string GetTitaniumHomePath() => Path.Join(GetUserHomePath(), "titanium");

    public string GetConfigPath() => Path.Join(GetTitaniumHomePath(), "config.yaml");

    public string GetProjectPath(string projectName) => Path.Join(GetTitaniumHomePath(), projectName);

    public string GetMasterDirectory(string projectName, ShortGuid docId) =>
        Path.Join(GetProjectPath(projectName), docId, "_masters");

    public string[] GetMasterFiles(string projectName, ShortGuid docId) => Directory.GetFiles(GetMasterDirectory(projectName, docId));

    public string GetDocManifestPath(string projectName, ShortGuid docId) =>
        Path.Join(GetProjectPath(projectName), docId, docId + ".manifest.yaml");
    
    public string GetDocAspecVariantPath(string projectName, ShortGuid docId, string aspectName, string? variant) =>
        Path.Join(GetProjectPath(projectName), docId, aspectName);
    
    
    public string GetDocAspectPath(string projectName, ShortGuid docId, string aspectName) =>
        Path.Join(GetProjectPath(projectName), docId, aspectName);
  
    public string GetAspectFilePath(string masterFileName, string projectName, ShortGuid docId, string aspectName, string fileName,
        string extension, string variant ="")
    {
        string newFilename = (string.IsNullOrWhiteSpace(  variant)) ? fileName : fileName + "-" + variant;
        newFilename = masterFileName + "_" + newFilename;
        return Path.Join(GetDocAspectPath(projectName, docId, aspectName), newFilename + "." + extension);
    }

    public string[] GetAspectFilePaths(string projectName, ShortGuid docId, string aspectName, string variant = "")
    {
        string aspectDirectoryPath  = Path.Join(GetProjectPath(projectName), docId, aspectName);
        return Directory.GetFiles(aspectDirectoryPath, "*." + aspectName, SearchOption.AllDirectories);
    }

    public string GetDocPath(string project, string docId) => Path.Join(GetProjectPath(project), docId);

    public string GetOcrAspectPromptPath()
    {
        return Path.Join(GetTitaniumHomePath(), "_ocr.prompts");
    }

    public string GetLogPath()
    {
        return Path.Join(GetTitaniumHomePath(), "_logs");
    }

    public string GetTessdataPath()
    {
        return Path.Join(GetTitaniumHomePath(), "_tessdata");
    }
}