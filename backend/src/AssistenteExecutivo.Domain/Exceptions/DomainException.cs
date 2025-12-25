namespace AssistenteExecutivo.Domain.Exceptions;

public class DomainException : Exception
{
    public string? LocalizationCode { get; }
    public object[]? LocalizationParameters { get; }

    public DomainException(string message) : base(message)
    {
        // Se a mensagem começa com "Domain:", trata como código de localização
        if (message.StartsWith("Domain:"))
        {
            LocalizationCode = message;
        }
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DomainException(string localizationCode, params object[] parameters) 
        : base(localizationCode)
    {
        LocalizationCode = localizationCode;
        LocalizationParameters = parameters;
    }

    public DomainException(string localizationCode, Exception innerException, params object[] parameters) 
        : base(localizationCode, innerException)
    {
        LocalizationCode = localizationCode;
        LocalizationParameters = parameters;
    }
}

