namespace Titanium.Domain.Rules;

public static class DomainValidationRules
{
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return email.Contains('@') && email.Contains('.');
    }

    public static string NormalizeSubject(string subject)
    {
        if (string.IsNullOrWhiteSpace(subject)) return string.Empty;
        var s = subject.Trim();
        while (s.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ||
               s.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase) ||
               s.StartsWith("FW:", StringComparison.OrdinalIgnoreCase))
        {
            s = s[(s.IndexOf(':') + 1)..].Trim();
        }
        return s;
    }
}
