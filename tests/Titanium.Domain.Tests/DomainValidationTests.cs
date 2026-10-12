using Titanium.Domain.Entities;
using Titanium.Domain.Enums;
using Titanium.Domain.Rules;
using Xunit;

namespace Titanium.Domain.Tests;

public class DomainValidationTests
{
    [Fact]
    public void IsValidEmail_ShouldValidateCorrectly()
    {
        Assert.True(DomainValidationRules.IsValidEmail("lawyer@firm.com"));
        Assert.False(DomainValidationRules.IsValidEmail("invalid-email"));
        Assert.False(DomainValidationRules.IsValidEmail(""));
    }

    [Fact]
    public void NormalizeSubject_ShouldStripPrefixes()
    {
        Assert.Equal("Meeting on Parenting Schedule", DomainValidationRules.NormalizeSubject("Re: Fwd: RE: Meeting on Parenting Schedule"));
        Assert.Equal("Contract dispute", DomainValidationRules.NormalizeSubject("FW: Contract dispute"));
    }

    [Fact]
    public void Matter_DefaultValues_ShouldBeValid()
    {
        var matter = new Matter
        {
            Name = "Family Dispute Case #1"
        };

        Assert.Equal(MatterStatus.Active, matter.Status);
        Assert.NotEqual(Guid.Empty, matter.Id);
    }
}
