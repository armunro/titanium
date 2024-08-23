namespace Titanium.Domain.Aspect;

public class TextAspect : Aspect<string>
{
    public TextAspect(string masterName, string name, string content, string variant, string extension) : base(masterName,content, name, extension, variant)
    {
        Content = content;
    }

    

    public override void Save(string filePath)
    {
        File.WriteAllText(filePath, Content);
    }
}