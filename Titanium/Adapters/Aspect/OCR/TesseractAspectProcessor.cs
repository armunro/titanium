using Titanium.Domain;
using Titanium.Domain;
using Titanium.Domain.Aspect;

namespace Titanium.Adapters.Aspect.OCR;

public abstract class TesseractAspectProcessor : IAspectProcessor
{
    public abstract Domain.Aspect.Aspect ProcessAspect(Doc doc, string masterFilePath);

}