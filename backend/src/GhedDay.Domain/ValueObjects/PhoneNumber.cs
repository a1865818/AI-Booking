using System.Text.RegularExpressions;

namespace GhedDay.Domain.ValueObjects;

/// <summary>
/// E.164-normalised phone number. Stored as the canonical <c>+&lt;country&gt;&lt;number&gt;</c> string.
/// </summary>
public sealed partial class PhoneNumber : IEquatable<PhoneNumber>
{
    public string Value { get; }

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber Parse(string raw)
    {
        if (!TryParse(raw, out var phone) || phone is null)
            throw new ArgumentException($"'{raw}' is not a valid E.164 phone number.", nameof(raw));
        return phone;
    }

    public static bool TryParse(string? raw, out PhoneNumber? phone)
    {
        phone = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        // Strip spaces, dashes, parentheses; keep a leading +.
        var cleaned = NonDigitOrPlus().Replace(raw.Trim(), string.Empty);
        if (!E164().IsMatch(cleaned))
            return false;

        phone = new PhoneNumber(cleaned);
        return true;
    }

    public bool Equals(PhoneNumber? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PhoneNumber other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    [GeneratedRegex(@"^\+[1-9]\d{6,14}$")]
    private static partial Regex E164();

    [GeneratedRegex(@"(?!^\+)[^\d]")]
    private static partial Regex NonDigitOrPlus();
}
