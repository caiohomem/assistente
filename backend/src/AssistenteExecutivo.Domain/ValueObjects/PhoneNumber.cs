using AssistenteExecutivo.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class PhoneNumber : IEquatable<PhoneNumber>
{
    public string Number { get; }
    public string FormattedNumber { get; }

    private PhoneNumber(string number)
    {
        var cleanNumber = RemoveFormatting(number);

        if (!Validate(cleanNumber))
            throw new DomainException("Domain:TelefoneInvalido", number);

        Number = cleanNumber;
        FormattedNumber = Format(cleanNumber);
    }

    public static PhoneNumber Create(string number)
    {
        return new PhoneNumber(number);
    }

    private static string RemoveFormatting(string phone)
    {
        return Regex.Replace(phone ?? string.Empty, @"[^\d]", "");
    }

    private static bool Validate(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        // Aceita telefone fixo (10 dígitos) ou celular (11 dígitos)
        return phone.Length == 10 || phone.Length == 11;
    }

    private static string Format(string phone)
    {
        if (phone.Length == 10)
        {
            // Telefone fixo: (00) 0000-0000
            return $"({phone.Substring(0, 2)}) {phone.Substring(2, 4)}-{phone.Substring(6, 4)}";
        }
        else if (phone.Length == 11)
        {
            // Celular: (00) 00000-0000
            return $"({phone.Substring(0, 2)}) {phone.Substring(2, 5)}-{phone.Substring(7, 4)}";
        }

        return phone;
    }

    public bool Equals(PhoneNumber? other)
    {
        if (other is null) return false;
        return Number == other.Number;
    }

    public override bool Equals(object? obj)
    {
        return obj is PhoneNumber other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Number.GetHashCode();
    }

    public override string ToString()
    {
        return FormattedNumber;
    }

    public static bool operator ==(PhoneNumber? left, PhoneNumber? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PhoneNumber? left, PhoneNumber? right)
    {
        return !Equals(left, right);
    }
}

