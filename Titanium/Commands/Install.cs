using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class Install : TitaniumCommand
{
    
    private readonly Installer _installer;
    public static Option<string> LanguageOption = new("--lang", "The tesseract language to download.");

    public Install(ConfigManager config,
        Installer installer) : base("install", "Installs core dependancies", config)
    {
        
        _installer = installer;
    }


    public override List<Option> DefineOptions() => new() { LanguageOption };

    public override Task<int> HandleAsync(InvocationContext context)
    {
        string? lang = context.ParseResult.GetValueForOption(LanguageOption);
        _installer.Install(lang!);
        return Task.FromResult(0);
    }
}