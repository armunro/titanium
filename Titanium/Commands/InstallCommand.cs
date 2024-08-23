using System.CommandLine;
using System.CommandLine.Invocation;

namespace Titanium.Commands;

public class InstallCommand : TitaniumCommand
{
    
    private readonly Installer _installer;
    public static Argument<string> LanguageArg = new(name: "lang",  description: "The tesseract language to download.",getDefaultValue:() => "");

    public InstallCommand(Installer installer) : base("install", "Installs core dependancies")
    {
        
        _installer = installer;
    }


    public override List<Argument> DefineArguments() => new() { LanguageArg };
    

    protected override Task<int> HandleAsync(InvocationContext context)
    {
        string? lang = context.ParseResult.GetValueForArgument(LanguageArg);
        _installer.InstallTesseractTrainingData(lang).Wait();
        _installer.InstallSamples();
        _installer.AddCurrentDirectoryToPathVariable();
        return Task.FromResult(0);
    }
}