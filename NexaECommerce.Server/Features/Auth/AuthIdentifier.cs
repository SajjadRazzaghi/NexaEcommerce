using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace NexaECommerce.Server.Features.Auth;

internal sealed record ParsedAuthIdentifier(
    string UserName,
    string? Email,
    string? PhoneNumber);

internal static class AuthIdentifier
{
    private static readonly Regex AllowedPhoneCharacters =
        new(
            @"^[+0-9().\s-]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex E164Phone =
        new(
            @"^\+[1-9]\d{7,14}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryParse(
        string? value,
        out ParsedAuthIdentifier parsed)
    {
        parsed = null!;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var input = value.Trim();

        if (input.Length > 256)
            return false;

        // Email account
        if (new EmailAddressAttribute().IsValid(input))
        {
            parsed = new ParsedAuthIdentifier(
                UserName: input,
                Email: input,
                PhoneNumber: null);

            return true;
        }

        // Phone account
        var ascii = NormalizeDigits(input);

        if (!AllowedPhoneCharacters.IsMatch(ascii))
            return false;

        var phone = new string(
            ascii.Where(c =>
                !char.IsWhiteSpace(c) &&
                c != '(' &&
                c != ')' &&
                c != '-' &&
                c != '.')
            .ToArray());

        // International dialing prefix: 0098... -> +98...
        if (phone.StartsWith("00", StringComparison.Ordinal))
        {
            phone = "+" + phone[2..];
        }
        else if (!phone.StartsWith('+'))
        {
            // Iranian mobile: 09123456789 -> +989123456789
            if (phone.Length == 11 &&
                phone.StartsWith("09", StringComparison.Ordinal))
            {
                phone = "+98" + phone[1..];
            }
            // Iranian mobile without initial zero: 9123456789
            else if (phone.Length == 10 &&
                     phone.StartsWith("9", StringComparison.Ordinal))
            {
                phone = "+98" + phone;
            }
            // International number entered without '+': 989123456789
            else if (phone.Length == 12 &&
                     phone.StartsWith("98", StringComparison.Ordinal))
            {
                phone = "+" + phone;
            }
        }

        if (!E164Phone.IsMatch(phone))
            return false;

        parsed = new ParsedAuthIdentifier(
            UserName: phone,
            Email: null,
            PhoneNumber: phone);

        return true;
    }

    private static string NormalizeDigits(string value)
    {
        return new string(value.Select(c =>
        {
            // Persian digits: ۰۱۲۳۴۵۶۷۸۹
            if (c >= '۰' && c <= '۹')
                return (char)('0' + c - '۰');

            // Arabic-Indic digits: ٠١٢٣٤٥٦٧٨٩
            if (c >= '٠' && c <= '٩')
                return (char)('0' + c - '٠');

            return c;
        }).ToArray());
    }
}