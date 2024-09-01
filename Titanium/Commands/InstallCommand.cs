using System.CommandLine;
using System.CommandLine.Invocation;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;

namespace Titanium.Commands;

[CliCommand("install", "Install tesseract training data and samples")]
public class InstallCommand : CliCommand
{
    // Private fields
    private readonly Installer _installer;

    //Options Arguments
    [CliArgument("lang", "The tesseract language to download.")]
    public static Argument<string> LanguageArg = new(name: "lang", description: "The tesseract language to download.",
        getDefaultValue: () => "");

    public InstallCommand(Installer installer)
    {
        _installer = installer;
    }

    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        string? lang = context.Argument<string>(LanguageArg);
        _installer.InstallTesseractTrainingData(lang).Wait();
        _installer.InstallSamples();
        _installer.AddCurrentDirectoryToPathVariable();
        return Task.FromResult(0);
    }
}