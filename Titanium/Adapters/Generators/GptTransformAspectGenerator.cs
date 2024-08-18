using OpenAI;
using Titanium.Domain.Config;

namespace Titanium.Adapters.Generators;

public class GptTransformAspectGenerator : GptAspectGenerator
{
    public GptTransformAspectGenerator(ConfigManager config, OpenAIClient client) : base(config, client)
    {
    }
}