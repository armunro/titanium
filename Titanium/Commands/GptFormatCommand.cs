using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Adapters.Generators;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Document;

namespace Titanium.Commands;

public class GptFormatCommand : ICommandHandler
{
    private readonly ConfigManager _config;
    readonly GptAspectGenerator _gptAspectGenerator;
    private readonly DocumentProcessor _documentProcessor;

    public static Option<string> DocumentIdOption = new("--doc", "The id of the document.");
    public static Option<string> SourceAspectOption = new("--aspect", "The aspect to use for input.");
    public static Option<string> SourceAspectVariantOption = new("--variant", "The aspect variant to use for input.");
    public static Option<string> SourceAspectExtensionOption = new("--extension", "The The aspect extension");

    public GptFormatCommand(ConfigManager config, GptAspectGenerator gptAspectGenerator,
        DocumentProcessor documentProcessor)
    {
        _config = config;
        _gptAspectGenerator = gptAspectGenerator;
        _documentProcessor = documentProcessor;
    }
    public int Invoke(InvocationContext context)
    {
        string? docId = context.ParseResult.GetValueForOption(DocumentIdOption);
        string? aspectName = context.ParseResult.GetValueForOption(SourceAspectOption);
        string? variantName = context.ParseResult.GetValueForOption(SourceAspectVariantOption);
        string? extension = context.ParseResult.GetValueForOption(SourceAspectExtensionOption);

        Doc doc = _config.GetDoc(docId);

        List<BaseAspect> aspects = _documentProcessor.ProcessDocument(doc, masterFilePath =>
        {
            Directory.CreateDirectory(_config.Pathfinder.GetDocAspectPath(_config.CurrentProject, docId, "gpt"));
            _gptAspectGenerator.SetSource(aspectName!, variantName!, extension!);
            List<BaseAspect> aspects = _gptAspectGenerator.GenerateAspects(doc, masterFilePath);
            return aspects;
        });

        aspects.ForEach(aspect => doc.AddAspect(aspect));
        _config.SaveDoc(doc);
        return 0;
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        return Task.FromResult(Invoke(context));
    }
}