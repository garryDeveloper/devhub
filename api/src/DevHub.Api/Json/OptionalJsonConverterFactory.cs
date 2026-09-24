using System.Text.Json;
using System.Text.Json.Serialization;
using DevHub.Application.Common;

namespace DevHub.Api.Json;

/// <summary>
/// Reads <see cref="Optional{T}"/> PATCH fields (DEVHUB-019). Registered once on the HTTP JSON
/// options, so every command gets absent-vs-null for free.
/// </summary>
/// <remarks>
/// The trick is what System.Text.Json does <i>not</i> do: it never calls a converter for a
/// property missing from the JSON. So a field that is absent keeps its <c>default</c>
/// (<see cref="Optional{T}.HasValue"/> false), and any field that reaches <c>Read</c> — even a
/// literal <c>null</c> — becomes present.
/// </remarks>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(OptionalJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    private sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
    {
        // Explicit: without it, a JSON null would skip Read and leave the field "absent" — the
        // exact case this type exists to tell apart.
        public override bool HandleNull => true;

        public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(JsonSerializer.Deserialize<T>(ref reader, options)!);

        // Commands are read, not written; this exists so a round trip does not throw. An absent
        // value has no way to vanish from here, so it is written as null.
        public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.HasValue ? value.Value : default, options);
    }
}
