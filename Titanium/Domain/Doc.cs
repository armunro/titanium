using CSharpVitamins;
using Titanium.Domain.Aspect;
using Titanium.Domain.Manifests;
using YamlDotNet.Serialization;

namespace Titanium.Domain;

public class Doc
{
    [YamlMember(Order = 0)] public string Id { get; set; }
    [YamlMember(Order = 1)] public string Project { get; set; }
    [YamlMember(Order = 2)] public string Name { get; set; }
    [YamlMember(Order = 3)] public string Author { get; set; }
    [YamlMember(Order = 4)] public DateTime Created { get; set; } = DateTime.Now;
    [YamlMember(Order = 5)] public DateTime Updated { get; set; } = DateTime.Now;
    [YamlMember(Order = 6)] public List<MasterManifest> Masters = new();
    [YamlMember(Order = 7)] public List<AspectManifest> Aspects = new();

    public void AddMaster(MasterManifest aspect)
    {
        Masters.Add(aspect);
    }

    public void AddAspect(DocAspect aspect)
    {
        if (Aspects.Any(a => a.Name == aspect.Name))
            Aspects.RemoveAll(a => a.Name == aspect.Name);
        Aspects.Add(new AspectManifest
        {
            Name = aspect.Name,
            Date = DateTime.Now
        });
    }

    public static Doc Create(string projectName, string docName, string author)
    {
        return new Doc()
        {
            Id = ShortGuid.NewGuid(),
            Name = docName,
            Author = author,
            Created = DateTime.Now,
            Updated = DateTime.Now,
            Project = projectName
        };
    }

    public void AddAspects(IEnumerable<DocAspect> aspects)
    {
        foreach (DocAspect aspect in aspects) AddAspect(aspect);
    }


}