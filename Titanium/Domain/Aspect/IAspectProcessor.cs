namespace Titanium.Domain;

public interface IAspectProcessor
{
    DocAspect ProcessAspect(Doc doc, string masterFilePath);
}