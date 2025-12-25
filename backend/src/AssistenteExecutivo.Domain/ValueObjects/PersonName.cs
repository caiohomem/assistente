using AssistenteExecutivo.Domain.Exceptions;
using System;
using System.Linq;

namespace AssistenteExecutivo.Domain.ValueObjects;

public sealed class PersonName : IEquatable<PersonName>
{
    public string FirstName { get; }
    public string? LastName { get; }
    public string FullName { get; }

    private PersonName(string firstName, string? lastName = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("Domain:NomeObrigatorio");

        if (firstName.Length < 2)
            throw new DomainException("Domain:NomeMinimoCaracteres");

        FirstName = firstName.Trim();
        LastName = lastName?.Trim();
        FullName = string.IsNullOrWhiteSpace(LastName) 
            ? FirstName 
            : $"{FirstName} {LastName}";
    }

    public static PersonName Create(string firstName, string? lastName = null)
    {
        if (lastName == null)
        {
            var parts = firstName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                return new PersonName(parts[0], string.Join(" ", parts.Skip(1)));
            }
        }

        return new PersonName(firstName, lastName);
    }

    public bool Equals(PersonName? other)
    {
        if (other is null) return false;
        return FullName == other.FullName;
    }

    public override bool Equals(object? obj)
    {
        return obj is PersonName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return FullName.GetHashCode();
    }

    public override string ToString()
    {
        return FullName;
    }

    public static bool operator ==(PersonName? left, PersonName? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PersonName? left, PersonName? right)
    {
        return !Equals(left, right);
    }
}

