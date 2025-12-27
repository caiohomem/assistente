using AssistenteExecutivo.Domain.Exceptions;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string? Street { get; }
    public string? City { get; }
    public string? State { get; }
    public string? ZipCode { get; }
    public string? Country { get; }

    private Address(
        string? street,
        string? city,
        string? state,
        string? zipCode,
        string? country)
    {
        Street = street?.Trim();
        City = city?.Trim();
        State = state?.Trim();
        ZipCode = zipCode?.Trim();
        Country = country?.Trim();
    }

    public static Address Create(
        string? street = null,
        string? city = null,
        string? state = null,
        string? zipCode = null,
        string? country = null)
    {
        return new Address(street, city, state, zipCode, country);
    }

    public static Address Empty => new(null, null, null, null, null);

    public bool IsEmpty => string.IsNullOrWhiteSpace(Street) &&
                          string.IsNullOrWhiteSpace(City) &&
                          string.IsNullOrWhiteSpace(State) &&
                          string.IsNullOrWhiteSpace(ZipCode) &&
                          string.IsNullOrWhiteSpace(Country);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street ?? string.Empty;
        yield return City ?? string.Empty;
        yield return State ?? string.Empty;
        yield return ZipCode ?? string.Empty;
        yield return Country ?? string.Empty;
    }
}





