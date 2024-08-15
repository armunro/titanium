namespace Titanium.Domain.Aspect;

public interface IAspectGenerator
{
    List<BaseAspect> GenerateAspects(Doc doc, string masterFilePath);

}