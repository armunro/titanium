namespace Titanium.Domain.Config;

public class ProjectConfig
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Description { get; set; }
    
  
    public ProjectConfig()
    {

    }

    public ProjectConfig(string name, string path, string description)
    {
        Name = name;
        Path = path;
        Description = description;
        
    }


}