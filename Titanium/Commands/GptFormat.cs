using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Adapters.Generators;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class GptFormat : TitaniumCommand
{
    readonly GptAspectGenerator _gptAspectGenerator;
    private readonly DocumentProcessor _documentProcessor;

    public static Option<string> DocumentIdOption = new("--doc", "The id of the document.");
    public static Option<string> SourceAspectOption = new("--aspect", "The aspect to use for input.");
    public static Option<string> SourceAspectVariantOption = new("--variant", "The aspect variant to use for input.");
    public static Option<string> SourceAspectExtensionOption = new("--extension", "The The aspect extension");

    public GptFormat(ConfigManager config, GptAspectGenerator gptAspectGenerator,
        DocumentProcessor documentProcessor): base("gpt", "Generate GPT aspects for a document", config)
    {
        _gptAspectGenerator = gptAspectGenerator;
        _documentProcessor = documentProcessor;
    }

    public override List<Option> DefineOptions() => new() { DocumentIdOption, SourceAspectOption, SourceAspectVariantOption, SourceAspectExtensionOption };
    public override Task<int> HandleAsync(InvocationContext context)
    {
        string? docId = context.ParseResult.GetValueForOption(DocumentIdOption);
        string? aspectName = context.ParseResult.GetValueForOption(SourceAspectOption);
        string? variantName = context.ParseResult.GetValueForOption(SourceAspectVariantOption);
        string? extension = context.ParseResult.GetValueForOption(SourceAspectExtensionOption);

        Doc doc = Config.GetDoc(docId);

        List<Aspect> aspects = _documentProcessor.ProcessDocument(doc, masterFilePath =>
        {
            Directory.CreateDirectory(Config.Pathfinder.GetDocAspectPath(Config.CurrentProject, docId, "gpt"));
            _gptAspectGenerator.SetSource(aspectName!, variantName!, extension!);
            List<Aspect> aspects = _gptAspectGenerator.GenerateAspects(doc, masterFilePath);
            return aspects;
        });

        aspects.ForEach(aspect => doc.AddAspect(aspect));
        Config.SaveDoc(doc);
        return  Task.FromResult(0);
    }


}