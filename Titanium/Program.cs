using System.ClientModel;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Reflection;
using Autofac;
using Autofac.Extras.AttributeMetadata;
using OpenAI;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Titanium;
using Titanium.Commands;
using Titanium.Domain;
using Titanium.Domain.Aspect;
using Titanium.Domain.Config;
using Titanium.Domain.Extensions;
using Titanium.Domain.Paths;

ILogger Logger(IComponentContext componentContext) =>
    new LoggerConfiguration().MinimumLevel.Debug()
        .WriteTo.File(Path.Combine(componentContext.Resolve<PathFinder>().GetLogPath(), "log-.txt"),
            rollingInterval: RollingInterval.Day)
        .WriteTo.Console(theme: AnsiConsoleTheme.Code).CreateLogger();

ContainerBuilder builder = new();
builder.RegisterModule<AttributedMetadataModule>();
builder.RegisterType<ConfigManager>().AsSelf().SingleInstance();

builder.RegisterCosmicCommands();
builder.RegisterAssemblyTypes(Assembly.GetEntryAssembly() ).Where(x=>x.IsAssignableTo<IAspectProcessor>()).As<IAspectProcessor>().WithAttributedMetadata<AspectMetadataAttribute>();

builder.RegisterType<DocumentProcessor>().AsSelf();
builder.RegisterType<Installer>().AsSelf();
builder.RegisterType<PathFinder>().AsSelf().SingleInstance();
builder.RegisterType<PathScaffolder>().AsSelf().SingleInstance();
builder.RegisterType<ApiKeyCredential>().AsSelf();
builder.Register<OpenAIClient>((c, p) =>
    new OpenAIClient(new ApiKeyCredential(c.Resolve<ConfigManager>().RootConfig.OpenAIApiKey)));
builder.Register<ILogger>((c, p) => { return Logger(c); }).SingleInstance();

IContainer container = builder.Build();

RootCommand rootCommand = new("[Ti]tanium - Lightweight document management & processing");

Command docCommand = container.Resolve<ListCommand>();
Command projectCommand = container.Resolve<Project>();
Command addProjectCommand = container.Resolve<ProjectAdd>();
Command useProjectCommand = container.Resolve<ProjectUse>();

Command aspectCommand = container.Resolve<AspectCommand>();
Command configCommand = container.Resolve<Config>();
Command docImportCommand = container.Resolve<DocImport>();
Command installCommand = container.Resolve<InstallCommand>();

projectCommand.AddCommand(addProjectCommand);
projectCommand.AddCommand(useProjectCommand);


rootCommand.AddCommand(projectCommand);
rootCommand.AddCommand(docCommand);
rootCommand.Add(docImportCommand);
rootCommand.AddCommand(configCommand);
rootCommand.AddCommand(installCommand);
rootCommand.AddCommand(aspectCommand);

Parser parser = new CommandLineBuilder(rootCommand).UseDefaults().Build();

await parser.InvokeAsync(args);