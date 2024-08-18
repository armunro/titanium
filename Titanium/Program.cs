using System.ClientModel;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Autofac;
using OpenAI;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Titanium;
using Titanium.Adapters.Generators;
using Titanium.Commands;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Extensions;

ILogger Logger(IComponentContext componentContext) =>
    new LoggerConfiguration().MinimumLevel.Debug()
        .WriteTo.File(Path.Combine(componentContext.Resolve<PathFinder>().GetLogPath(), "log-.txt"),
            rollingInterval: RollingInterval.Day)
        .WriteTo.Console(theme: AnsiConsoleTheme.Code).CreateLogger();

ContainerBuilder builder = new();
builder.RegisterType<ConfigManager>().AsSelf().SingleInstance();
builder.RegisterType<Project>().SingleInstance();
builder.RegisterType<ProjectAdd>().SingleInstance();
builder.RegisterType<ProjectList>().SingleInstance();
builder.RegisterType<ProjectUse>().SingleInstance();
builder.RegisterType<GptFormat>().SingleInstance();
builder.RegisterType<DocCommand>().SingleInstance();
builder.RegisterType<DocAdd>().SingleInstance();
builder.RegisterType<AspectCommand>().SingleInstance();
builder.RegisterType<OcrAspect>().SingleInstance();
builder.RegisterType<Config>().SingleInstance();
builder.RegisterType<Install>().SingleInstance();
builder.RegisterType<OcrAspectGenerator>().AsSelf();
builder.RegisterType<DocumentProcessor>().AsSelf();
builder.RegisterType<Installer>().AsSelf();
builder.RegisterType<PathFinder>().AsSelf().SingleInstance();
builder.RegisterType<ApiKeyCredential>().AsSelf();
builder.Register<OpenAIClient>((c, p) =>
    new OpenAIClient(new ApiKeyCredential(c.Resolve<ConfigManager>().RootConfig.OpenAIApiKey)));
builder.Register<ILogger>((c, p) => { return Logger(c); }).SingleInstance();

IContainer container = builder.Build();

RootCommand rootCommand = new();


Command docCommand = container.Resolve<DocCommand>();
Command projectCommand = container.Resolve<Project>();
Command addProjectCommand = container.Resolve<ProjectAdd>();
Command listProjectCommand = container.Resolve<ProjectList>();
Command useProjectCommand = container.Resolve<ProjectUse>();
Command docAspectCommand = container.Resolve<AspectCommand>();
Command docAspectOcrCommand = container.Resolve<OcrAspect>();
Command configCommand = container.Resolve<Config>();
Command addDocument = container.Resolve<DocAdd>();
Command installCommand = container.Resolve<Install>();

projectCommand.AddSubcommands(addProjectCommand, listProjectCommand, useProjectCommand);
docCommand.AddSubcommands(addDocument, docAspectCommand);

docAspectCommand.AddCommand(docAspectOcrCommand);
rootCommand.AddSubcommands(projectCommand, docCommand, configCommand, installCommand);

addProjectCommand.AddOption(ProjectAdd.ProjectNameOption);
useProjectCommand.AddOption(ProjectUse.ProjectNameOption);
addDocument.AddOption(DocAdd.SourceOption);
docAspectOcrCommand.AddOption(OcrAspect.DocumentIdOption);


configCommand.AddOptions(Config.ConfigKeyOption, Config.ConfigValueOption);
installCommand.AddOption(Install.LanguageOption);

Parser parser = new CommandLineBuilder(rootCommand).UseDefaults().Build();

await parser.InvokeAsync(args);