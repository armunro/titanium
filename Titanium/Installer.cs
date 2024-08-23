using Serilog;
using Titanium.Domain;
using Titanium.Domain.Paths;
using Titanium.Samples;

namespace Titanium;

public class Installer
{
    
    private readonly PathFinder _pathFinder;
    private readonly ILogger _logger;
    private readonly PathScaffolder _scaffolder;

    public Installer (PathFinder pathFinder, ILogger logger, PathScaffolder scaffolder)
    {
        
        _pathFinder = pathFinder;
        _logger = logger;
        _scaffolder = scaffolder;
    }



    public void AddCurrentDirectoryToPathVariable()
    {
        string path = Environment.GetEnvironmentVariable("PATH") ?? "";
        string currentDirectory = Directory.GetCurrentDirectory();
        if (!path.Contains(currentDirectory))
        {
            _logger.Information("Added {CurrentDir} to PATH env. variable.", currentDirectory);
            path += $";{currentDirectory}";
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Machine);
            
        }
        
    }

    public void InstallSamples()
    {
        string promptsPath = _scaffolder.ScaffoldPromptsDirectory();
        File.WriteAllText(Path.Join(promptsPath, "Receipt.json.txt"), ReceiptSamples.RECEIPT_MODEL_GPT);
    }

    public async Task InstallTesseractTrainingData(string language)
    {
        await DownloadTessData(_scaffolder.ScaffoldTessdataDirectory(), language);
    }

    private async Task DownloadTessData(string destination,  string language)
    {
        using HttpClient client = new();
        string url = $"https://github.com/tesseract-ocr/tessdata/raw/main/{language}.traineddata"; 
        string filePath = Path.Combine(destination, $"{language}.traineddata");
        _logger.Debug("Http-GET: {Url}", url);

        HttpResponseMessage response = await client.GetAsync(url);
        try
        {
            _logger.Debug("HTTP-Response: {Code}", response.StatusCode.ToString());
            response.EnsureSuccessStatusCode();
            _logger.Debug("Save: {FilePath}", filePath);
            using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream);
        }
        catch (Exception e)
        {
            _logger.Fatal(e,"Couldn't get Tesseract traineddata at {Url}", url);
        }
    }
}