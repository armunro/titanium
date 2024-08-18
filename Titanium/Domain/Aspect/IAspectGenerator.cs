namespace Titanium.Domain.Aspect;

public interface IAspectGenerator
{
    List<Aspect> GenerateAspects(Doc doc, string masterFilePath);

}