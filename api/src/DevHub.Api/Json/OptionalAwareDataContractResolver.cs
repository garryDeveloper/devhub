using DevHub.Application.Common;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DevHub.Api.Json;

/// <summary>
/// Makes Swagger describe an <see cref="Optional{T}"/> field as plain <c>T</c> — what actually
/// travels on the wire — instead of an object with <c>hasValue</c>/<c>value</c>.
/// </summary>
/// <remarks>
/// Wraps Swashbuckle's own resolver rather than adding a schema filter: it answers the question
/// "what JSON shape is this type?" at the one place Swashbuckle asks it, for every closed
/// <c>Optional&lt;T&gt;</c> at once.
/// </remarks>
internal sealed class OptionalAwareDataContractResolver(ISerializerDataContractResolver inner)
    : ISerializerDataContractResolver
{
    public DataContract GetDataContractForType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Optional<>)
            ? inner.GetDataContractForType(type.GetGenericArguments()[0])
            : inner.GetDataContractForType(type);
}
