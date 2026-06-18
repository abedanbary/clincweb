namespace ClinicApp.Web.Services
{
    /// <summary>
    /// Formats phone numbers to E.164 (digits only, no +) for WhatsApp Cloud API.
    /// Currently supports Israeli numbers. Extend CountryRules to add other countries.
    /// </summary>
    public static class PhoneNumberHelper
    {
        // Maps local prefix → country code to strip + re-add
        private static readonly (string LocalPrefix, string CountryCode)[] CountryRules =
        [
            ("0", "972"),   // Israel: 05X → 97205X
        ];

        public static (bool IsValid, string Formatted, string? Error) Format(string? rawPhone)
        {
            if (string.IsNullOrWhiteSpace(rawPhone))
                return (false, string.Empty, "Phone number is missing.");

            // Strip spaces, dashes, brackets, and the leading "+"
            var digits = new string(rawPhone.Where(char.IsDigit).ToArray());

            if (digits.Length == 0)
                return (false, string.Empty, $"Phone number '{rawPhone}' contains no digits.");

            // Already in international format (e.g. 972501234567)
            foreach (var (localPrefix, countryCode) in CountryRules)
            {
                if (digits.StartsWith(countryCode))
                {
                    return ValidateLength(digits, rawPhone);
                }

                if (digits.StartsWith(localPrefix))
                {
                    var formatted = countryCode + digits[localPrefix.Length..];
                    return ValidateLength(formatted, rawPhone);
                }
            }

            return (false, string.Empty,
                $"Unrecognized phone number format: '{rawPhone}'. " +
                "Expected Israeli format: 05X..., +972X..., or 972X...");
        }

        private static (bool IsValid, string Formatted, string? Error) ValidateLength(string digits, string rawPhone)
        {
            // E.164 allows 7–15 digits total; Israeli mobile = 12 (972 + 9)
            if (digits.Length < 7 || digits.Length > 15)
                return (false, string.Empty,
                    $"Phone number '{rawPhone}' has invalid length ({digits.Length} digits) after formatting.");

            return (true, digits, null);
        }
    }
}
