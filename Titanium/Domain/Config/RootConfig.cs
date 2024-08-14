namespace Titanium.Domain.Config;

public class RootConfig
{
    public string CurrentProject = "default";
    public List<ProjectConfig> Projects { get; set; } = new();
}
