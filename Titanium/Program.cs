using System.ClientModel;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Windows.Input;
using Autofac;
using OpenAI;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Tesseract;
using Titanium;
using Titanium.Adapters.Generators;
using Titanium.Commands;
using Titanium.Domain;
using Titanium.Domain.Config;

ContainerBuilder builder = new();
builder.RegisterType<ConfigManager>().AsSelf().SingleInstance();
builder.RegisterType<AddProjectCommand>().Named<ICommandHandler>(nameof(AddProjectCommand)).SingleInstance();
builder.RegisterType<ListProjectCommand>().Named<ICommandHandler>(nameof(ListProjectCommand)).SingleInstance();
builder.RegisterType<UseProjectCommand>().Named<ICommandHandler>(nameof(UseProjectCommand)).SingleInstance();
builder.RegisterType<GptFormatCommand>().Named<ICommandHandler>(nameof(GptFormatCommand)).SingleInstance();
builder.RegisterType<AddDocCommand>().Named<ICommandHandler>(nameof(AddDocCommand)).SingleInstance();
builder.RegisterType<OcrAspectCommands>().Named<ICommandHandler>(nameof(OcrAspectCommands)).SingleInstance();
builder.RegisterType<ConfigCommand>().Named<ICommandHandler>(nameof(ConfigCommand)).SingleInstance();
builder.RegisterType<ViewDocCommand>().Named<ICommandHandler>(nameof(ViewDocCommand)).SingleInstance();
builder.RegisterType<SGuidCommandHandler>().Named<ICommandHandler>(nameof(SGuidCommandHandler)).SingleInstance();
builder.RegisterType<InstallCommand>().Named<ICommandHandler>(nameof(InstallCommand)).SingleInstance();
builder.RegisterType<OcrAspectGenerator>().AsSelf();
builder.RegisterType<GptAspectGenerator>().AsSelf();
builder.RegisterType<DocumentProcessor>().AsSelf();
builder.RegisterType<Installer>().AsSelf();
builder.RegisterType<PathFinder>().AsSelf().SingleInstance();
builder.RegisterType<ApiKeyCredential>().AsSelf();


builder.Register<OpenAIClient>((c, p) => new(new ApiKeyCredential(c.Resolve<ConfigManager>().RootConfig.OpenAIApiKey)));


builder.Register<ILogger>((c, p) =>
{
    return new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.File(Path.Combine(c.Resolve<PathFinder>().GetLogPath(), "log-.txt"),
            rollingInterval: RollingInterval.Day)
        .WriteTo.Console(theme: AnsiConsoleTheme.Code)
        .CreateLogger();
}).SingleInstance();
IContainer container = builder.Build();

RootCommand rootCommand = new();
Command project = new("project", "Create a project.");
Command addProject = new("add", "Add a project to the solution.");
Command listProject = new("list", "List all projects.");
Command useProject = new("use", "Use a project.");
Command docAspect = new("aspect", "Generate an aspect of a document.");
Command docAspectOcr = new("ocr", "Generate a plain-text aspect using OCR (OCR)");
Command docAspectGptTranslate = new("gpt", "Generate a text JSON aspect.");
Command docView = new("view", "View a document.");
Command config = new("config", "Manage configuration.");
Command configGet = new Command("get", "Get a specific configuration key value");
Command document = new("doc", "Manage documents in the current project.");
Command addDocument = new("add", "Add a document to the current project.");
Command install = new Command("install", "Installs core dependancies");



//Project
project.AddCommand(addProject);
project.AddCommand(listProject);
project.AddCommand(useProject);
project.Handler = container.ResolveNamed<ICommandHandler>(nameof(ListProjectCommand));

//Project - List 
listProject.Handler = container.ResolveNamed<ICommandHandler>(nameof(ListProjectCommand));

//Project - Add
addProject.AddOption(AddProjectCommand.ProjectNameOption);
addProject.Handler = container.ResolveNamed<ICommandHandler>(nameof(AddProjectCommand));

//Project - Use
useProject.AddOption(UseProjectCommand.ProjectNameOption);
useProject.Handler = container.ResolveNamed<ICommandHandler>(nameof(UseProjectCommand));

//Doc
document.AddCommand(addDocument);
document.AddCommand(docAspect);
document.AddCommand(docAspectGptTranslate);
document.AddCommand(docView);

//Doc - Add
addDocument.AddOption(AddDocCommand.SourceOption);
addDocument.Handler = container.ResolveNamed<ICommandHandler>(nameof(AddDocCommand));

//Doc - Aspect - OCR
docAspect.AddCommand(docAspectOcr);
docAspectOcr.AddOption(OcrAspectCommands.DocumentIdOption);
docAspectOcr.Handler = container.ResolveNamed<ICommandHandler>(nameof(OcrAspectCommands));


//Doc - Aspect - GPT
docAspect.AddCommand(docAspectGptTranslate);
docAspectGptTranslate.AddOption(GptFormatCommand.DocumentIdOption);
docAspectGptTranslate.AddOption(GptFormatCommand.SourceAspectOption);
docAspectGptTranslate.AddOption(GptFormatCommand.SourceAspectVariantOption);
docAspectGptTranslate.AddOption(GptFormatCommand.SourceAspectExtensionOption);
docAspectGptTranslate.Handler = container.ResolveNamed<ICommandHandler>(nameof(GptFormatCommand));

//Doc - View
docView.AddOption(ViewDocCommand.DocumentIdOption);
docView.Handler = container.ResolveNamed<ICommandHandler>(nameof(ViewDocCommand));

// Config
config.AddCommand(configGet);
config.AddOption(ConfigCommand.ConfigKeyOption);
config.AddOption(ConfigCommand.ConfigValueOption);

config.Handler = container.ResolveNamed<ICommandHandler>(nameof(ConfigCommand));




// Define the util command and its subcommand sguid
Command util = new("util", "Utility commands.");
Command sguid = new("sguid", "Generate a short GUID.");


// Implement the handler for sguid command
sguid.Handler = container.ResolveNamed<ICommandHandler>(nameof(SGuidCommandHandler));

install.AddOption(InstallCommand.LanguageOption);
install.Handler = container.ResolveNamed<ICommandHandler>(nameof(InstallCommand));


// Add the sguid command to util
util.AddCommand(sguid);

// Add the util command to the root command
rootCommand.AddCommand(util);

//Root
rootCommand.AddCommand(project);
rootCommand.AddCommand(document);
rootCommand.AddCommand(config);
rootCommand.AddCommand(install);
await rootCommand.InvokeAsync(args);