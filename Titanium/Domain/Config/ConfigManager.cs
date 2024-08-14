using CSharpVitamins;
using Titanium.Domain.Document;
using YamlDotNet.Serialization;

namespace Titanium.Domain.Config;

public class ConfigManager
{
    readonly PathFinder _pathFinder;
    RootConfig _rootConfig = new();

    public string CurrentProject => _rootConfig.CurrentProject;
    public PathFinder Pathfinder => _pathFinder;


    public ConfigManager(PathFinder pathFinder)
    {
        _pathFinder = pathFinder;
        LoadConfig();
    }
 

    public void CreateProject(string? projectName)
    {
        if (projectName == null) projectName = "default";

        ProjectConfig project = new(name: projectName,
            path: Path.Join(_pathFinder.GetTitaniumHomePath(), projectName),
            description: "Default project configuration.");
        CreateProjectDirectory(projectName);
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
        if (!File.Exists(_pathFinder.GetConfigPath()))
        {
            return;
        }

        string yaml = File.ReadAllText(_pathFinder.GetConfigPath());
        _rootConfig = new DeserializerBuilder().Build().Deserialize<RootConfig>(yaml);
    }

    void CreateProjectDirectory(string projectName)
    {
        Directory.CreateDirectory(Path.Join(_pathFinder.GetTitaniumHomePath(), projectName));
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
        Doc doc = Doc.Create(_rootConfig.CurrentProject, ShortGuid.NewGuid(),  Environment.UserName);
        string docBasePath = _pathFinder.GetDocPath(doc.Project, doc.Id);
        string docMastersPath = _pathFinder.GetMasterDirectory(doc.Project, doc.Id);
        Directory.CreateDirectory(docBasePath);
        Directory.CreateDirectory(docMastersPath);
        SaveDoc(doc);
        return doc;
    }

    public void ImportMasters(Doc doc, string source)
    {
        string mastersPath = _pathFinder.GetMasterDirectory(doc.Project, doc.Id);
        if (Directory.Exists(source))
        {
            foreach (string file in Directory.GetFiles(source))
            {
                doc.AddMaster(new MasterManifest()
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Extension = Path.GetExtension(file),
                    Created = DateTime.Now
                });
                File.Copy(file, Path.Join(mastersPath, Path.GetFileName(file)));
            }
        }
        else
        {
            
            doc.AddMaster(new MasterManifest()
            {
                Name = Path.GetFileNameWithoutExtension(source),
                Extension = Path.GetExtension(source),
                Created = DateTime.Now
            });
            File.Copy(source, Path.Join(mastersPath, Path.GetFileName(source)));
        }
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
}