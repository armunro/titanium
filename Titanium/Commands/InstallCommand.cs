using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class InstallCommand : ICommandHandler
{
    private readonly ConfigManager _config;
    private readonly Installer _installer;
    public static Option<string> LanguageOption = new("--lang", "The tesseract language to download.");

    public InstallCommand(ConfigManager config,
        Installer installer)
    {
        _config = config;
        _installer = installer;
    }

    public int Invoke(InvocationContext context)
    {
        string? lang = context.ParseResult.GetValueForOption(LanguageOption);
        _installer.Install(lang!);
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        return Task.FromResult(Invoke(context));
    }
}