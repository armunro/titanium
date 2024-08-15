using OpenAI.VectorStores;
using Serilog;
using Titanium.Domain;
using Titanium.Domain.Config;
using Titanium.Samples;

namespace Titanium;

public class Installer
{
    private readonly ConfigManager _config;
    private readonly PathFinder _pathFinder;
    private readonly ILogger _logger;

    public Installer (ConfigManager config, PathFinder pathFinder, ILogger logger)
    {
        _config = config;
        _pathFinder = pathFinder;
        _logger = logger;
    }

    public void Install(string language)
    {
        InstallTesseractTrainingData(language).Wait();
        InstallSamples();
    }

    private void InstallSamples()
    {
        string promptsPath = _pathFinder.GetOcrAspectPromptPath();
        if (!Path.Exists(promptsPath))
            Directory.CreateDirectory(promptsPath);
        File.WriteAllText(Path.Join(promptsPath, "Receipt.json.txt"), ReceiptSamples.RECEIPT_MODEL_GPT);
    }

    private async Task InstallTesseractTrainingData(string language)
    {
        string tessdataPath = _pathFinder.GetTessdataPath();
        if (!Path.Exists(tessdataPath))
            Directory.CreateDirectory(tessdataPath);
        await DownloadTessData(language);
    }

    private async Task DownloadTessData(string language)
    {
        using HttpClient client = new();
        string url = $"https://github.com/tesseract-ocr/tessdata/raw/main/{language}.traineddata"; 
        string filePath = Path.Combine(_pathFinder.GetTessdataPath(), $"{language}.traineddata");
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