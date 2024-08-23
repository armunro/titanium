namespace Titanium.Domain.Aspect;

public interface IAspectProcessor
{
    Aspect ProcessAspect(Doc doc, string masterFilePath);
}