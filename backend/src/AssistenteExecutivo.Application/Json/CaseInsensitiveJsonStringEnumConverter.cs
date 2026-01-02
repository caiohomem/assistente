using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssistenteExecutivo.Application.Json;

/// <summary>
/// JSON converter that deserializes enums ignoring casing, allowing specs to send values like
/// "SendEmail" or "sendEmail" interchangeably.
/// </summary>
public sealed class CaseInsensitiveJsonStringEnumConverter : JsonConverterFactory
{
    private readonly bool _allowIntegerValues;

    public CaseInsensitiveJsonStringEnumConverter(bool allowIntegerValues = true)
    {
        _allowIntegerValues = allowIntegerValues;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsEnum)
        {
            return true;
        }

        var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
        return underlyingType?.IsEnum ?? false;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
        if (underlyingType != null)
        {
            var converterType = typeof(CaseInsensitiveNullableEnumConverter<>).MakeGenericType(underlyingType);
            return (JsonConverter)Activator.CreateInstance(converterType, _allowIntegerValues)!;
        }

        var converter = typeof(CaseInsensitiveEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converter, _allowIntegerValues)!;
    }

    private sealed class CaseInsensitiveEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        private readonly bool _allowIntegerValues;

        public CaseInsensitiveEnumConverter(bool allowIntegerValues)
        {
            _allowIntegerValues = allowIntegerValues;
        }

        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var enumText = reader.GetString();
                if (!string.IsNullOrWhiteSpace(enumText) &&
                    Enum.TryParse(enumText, ignoreCase: true, out TEnum value))
                {
                    return value;
                }

                throw new JsonException($"Unable to convert \"{enumText}\" to enum \"{typeof(TEnum).Name}\".");
            }

            if (reader.TokenType == JsonTokenType.Number && _allowIntegerValues && reader.TryGetInt32(out var intValue))
            {
                if (Enum.IsDefined(typeof(TEnum), intValue))
                {
                    return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
                }

                throw new JsonException($"Value {intValue} is not defined for enum \"{typeof(TEnum).Name}\".");
            }

            throw new JsonException($"Unexpected token {reader.TokenType} when parsing enum \"{typeof(TEnum).Name}\".");
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private sealed class CaseInsensitiveNullableEnumConverter<TEnum> : JsonConverter<TEnum?> where TEnum : struct, Enum
    {
        private readonly CaseInsensitiveEnumConverter<TEnum> _innerConverter;

        public CaseInsensitiveNullableEnumConverter(bool allowIntegerValues)
        {
            _innerConverter = new CaseInsensitiveEnumConverter<TEnum>(allowIntegerValues);
        }

        public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return _innerConverter.Read(ref reader, typeof(TEnum), options);
        }

        public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                _innerConverter.Write(writer, value.Value, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
