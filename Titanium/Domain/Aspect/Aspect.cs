using Tesseract;
using YamlDotNet.Serialization;

namespace Titanium.Domain.Aspect;

public abstract class Aspect
{
    public string MasterName { get; set; }
    public string Name { get; set; }
    public string Extension { get; set; }
    public string Variant { get; set; } 
    protected Aspect(string name, string extension, string variant, string masterName)
    {
        Name = name;
        Extension = extension;
        Variant = variant;
        MasterName = masterName;
    }

    public abstract void Save(string filePath);
    
    public static TextAspect NewText(string masterName, string contents, string aspectName, string variant = "text") => new(masterName, aspectName, contents, variant, "txt");
    public static TextAspect NewJson(string masterName,  string contents, string aspectName, string variant = "object") => new(masterName, aspectName, contents, variant, "json");
    public static TextAspect NewHtml(string masterName,  string contents, string variant ) => new( masterName, "ocr", contents, variant, "html");
    public static TextAspect NewXml(string masterName, string contents, string variant) => new(masterName, "ocr", contents, variant, "xml");
    public static TextAspect NewOcrErrorText(string masterName, Exception exception) =>
        new(masterName, "ocr", new SerializerBuilder().Build().Serialize(exception) , "error", "txt");

    public static Aspect NewPix(string masterName, Pix pix, string variant) => new PixAspect( masterName, pix, "ocr", "png", variant);
    
}

public abstract class Aspect<T> : Aspect
{
    public T Content { get; set; }

    public Aspect(string masterName, T content, string name, string extension, string variant) : base(name, extension,
        variant, masterName)
    {
        Content = content;
    }
}