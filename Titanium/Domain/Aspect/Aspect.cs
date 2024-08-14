namespace Titanium.Domain.Aspect;

public abstract class Aspect<T> : BaseAspect
{
    public T Content { get; set; }
  
    public Aspect(string masterName, T content, string name, string extension, string variant) : base(name, extension, variant,  masterName)
    {
        Content = content;
        
    }
}