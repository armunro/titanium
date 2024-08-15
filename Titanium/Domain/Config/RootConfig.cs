namespace Titanium.Domain.Config;

public class RootConfig
{
    public string CurrentProject { get; set; }= "default";
    public string OpenAIApiKey { get; set; } = "SET_ME";
    public List<ProjectConfig> Projects { get; set; } = new();

    public RootConfig()
    {
        
    }

}
