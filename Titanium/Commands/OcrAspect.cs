using System.CommandLine;
using System.CommandLine.Invocation;
using Titanium.Adapters.Generators;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;

namespace Titanium.Commands;

public class OcrAspect : TitaniumCommand
{
    public static Option<string> DocumentIdOption = new("--doc", "The id of the document");
    private readonly DocumentProcessor _documentProcessor;
    private readonly OcrAspectGenerator _ocrAspectGenerator;


    public OcrAspect(ConfigManager config,
        OcrAspectGenerator ocrAspectGenerator,
        DocumentProcessor documentProcessor) : base("ocr", "Generate OCR aspects for a document", config)
    {
        _ocrAspectGenerator = ocrAspectGenerator;
        _documentProcessor = documentProcessor;
        DocumentIdOption.AddCompletions(Config.GetDocNames());
    }


    public override List<Option> DefineOptions() => new() { DocumentIdOption };

    public override Task<int> HandleAsync(InvocationContext context)
    {
        string? docId = context.ParseResult.GetValueForOption(DocumentIdOption);
        Doc doc = Config.GetDoc(docId);

        Directory.CreateDirectory(Config.Pathfinder.GetDocAspectPath(Config.CurrentProject, docId, "ocr"));
        List<Aspect> aspects =
            _documentProcessor.ProcessDocument(doc, masterFile =>
                _ocrAspectGenerator.GenerateAspects(doc, masterFile));
        aspects.ForEach(aspect => doc.AddAspect(aspect));
        Config.SaveDoc(doc);

        return Task.FromResult(0);
    }


}