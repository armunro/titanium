using Tesseract;
using Titanium.Domain.Aspect;
using YamlDotNet.Serialization;

namespace Titanium.Domain;

public abstract class DocAspect
{
    public string MasterName { get; set; }
    public string Name { get; set; }
    public string Extension { get; set; }
    public string Variant { get; set; } 
    protected DocAspect(string name, string extension, string variant, string masterName)
    {
        Name = name;
        Extension = extension;
        Variant = variant;
        MasterName = masterName;
    }

    public abstract void Save(string filePath);
    
    public static TextDocAspect NewText(string masterName, string contents, string aspectName, string variant = "text") => new(masterName, aspectName, contents, variant, "txt");
    public static TextDocAspect NewJson(string masterName,  string contents, string aspectName, string variant = "object") => new(masterName, aspectName, contents, variant, "json");
    public static TextDocAspect NewHtml(string masterName,  string contents, string variant ) => new( masterName, "ocr", contents, variant, "html");
    public static TextDocAspect NewXml(string masterName, string contents, string variant) => new(masterName, "ocr", contents, variant, "xml");
    public static TextDocAspect NewOcrErrorText(string masterName, Exception exception) =>
        new(masterName, "ocr", new SerializerBuilder().Build().Serialize(exception) , "error", "txt");

    public static PixDocAspect NewPix(string masterName, Pix pix, string variant) => new PixDocAspect( masterName, pix, "ocr", "png", variant);
    
}

public abstract class DocAspect<T> : DocAspect
{
    public T Content { get; set; }

    public DocAspect(string masterName, T content, string name, string extension, string variant) : base(name, extension,
        variant, masterName)
    {
        Content = content;
    }
}