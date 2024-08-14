using Titanium.Domain.Config;

namespace Titanium.Commands;

public class ViewRenderer
{
    private readonly ConfigManager _config;
    public ViewRenderer(ConfigManager config)
    {
        _config = config;
    }
}