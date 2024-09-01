using Cosmic.Aspects.Configuration;
using CSharpVitamins;
using Serilog;
using Titanium.Domain.Manifests;
using Titanium.Domain.Paths;
using YamlDotNet.Serialization;

namespace Titanium.Domain.Config;

public class ConfigManager : CosmicConfig<RootConfig>
{
    readonly PathFinder _pathFinder;
    
    private readonly ILogger _logger;
    RootConfig _rootConfig = new();
    public RootConfig RootConfig => _rootConfig;
    public string CurrentProject => _rootConfig.CurrentProject;
    public PathFinder Pathfinder => _pathFinder;


    public ConfigManager(PathFinder pathFinder, ILogger logger) : base(pathFinder.GetConfigPath(), logger)
    {
        _pathFinder = pathFinder;
        
        _logger = logger;
        LoadConfig();
    }


    public void CreateProject(string? projectName)
    {
        if (projectName == null) projectName = "default";

        string projectPath = _pathFinder.GetProjectPath(projectName);
        Directory.CreateDirectory(projectPath);
        
        ProjectConfig project = new(name: projectName, projectPath, description: "Default project configuration.");
       _rootConfig.Projects.Add(project);
        UseProject(projectName);
        SaveConfig();
    }

    public void SaveConfig()
    {
        string yaml = new SerializerBuilder().Build().Serialize(_rootConfig);
        File.WriteAllText(_pathFinder.GetConfigPath(), yaml);
    }

    public void LoadConfig()
    {
        string expectConfigPath = _pathFinder.GetConfigPath();
        _logger.Debug("Expecting config file at {ConfigPath}", expectConfigPath);
        if (!File.Exists(expectConfigPath))
        {
            _logger.Warning("Config file not found at {ConfigPath}. Initializing...", expectConfigPath);
            _rootConfig = new RootConfig();
            SaveConfig();
        }

        string yaml = File.ReadAllText(_pathFinder.GetConfigPath());
        _rootConfig = new DeserializerBuilder().Build().Deserialize<RootConfig>(yaml);
    }


    public List<ProjectConfig> GetProjects()
    {
        return _rootConfig.Projects;
    }

    public void UseProject(string name)
    {
        _rootConfig.CurrentProject = name;
    }

    public Doc AddDoc()
    {
        Doc doc = Doc.Create(_rootConfig.CurrentProject, ShortGuid.NewGuid(), Environment.UserName);
        Directory.CreateDirectory(_pathFinder.GetDocPath(doc.Project, doc.Id));
        

        SaveDoc(doc);
        return doc;
    }

    public void ImportMasters(Doc doc, string source)
    {
        string mastersPath = _pathFinder.GetMasterDirectory(doc.Project, doc.Id);
        _logger.Debug("Listing masters in {MastersPath}", mastersPath);
        Directory.CreateDirectory(mastersPath);
        if (Directory.Exists(source))
        {
            foreach (string file in Directory.GetFiles(source))
                ImportMaster(doc, file, mastersPath);
        }
        else
            ImportMaster(doc, source, mastersPath);
    }

    private void ImportMaster(Doc doc, string file, string mastersPath)
    {
        doc.AddMaster(new MasterManifest()
        {
            Name = Path.GetFileNameWithoutExtension(file),
            Extension = Path.GetExtension(file),
            Created = DateTime.Now
        });
        string importPath = Path.Join(mastersPath, Path.GetFileName(file));
        File.Copy(file, importPath);
        _logger.Information("Imported Master: {ImportPath}", importPath);
    }

    public void SaveDoc(Doc doc)
    {
        doc.Updated = DateTime.Now;
        string yaml = new SerializerBuilder().Build().Serialize(doc);
        File.WriteAllText(_pathFinder.GetDocManifestPath(doc.Project, doc.Id), yaml);
    }

    public string GetConfigYaml()
    {
        return new SerializerBuilder().Build().Serialize(_rootConfig);
    }

    public Doc GetDoc(string? docId)
    {
        string yaml = File.ReadAllText(_pathFinder.GetDocManifestPath(_rootConfig.CurrentProject, docId));
        Doc doc = new DeserializerBuilder().Build().Deserialize<Doc>(yaml);

        return doc;
    }

    public string[] GetDocNames()
    {
        return Directory.GetDirectories(_pathFinder.GetProjectPath(CurrentProject)).Select(x => Path.GetFileName(x))
            .ToArray();
    }
}