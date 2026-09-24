using System.Text.Json;
using DevHub.Api.Json;
using DevHub.Application.Common;

namespace DevHub.Api.IntegrationTests.Unit;

/// <summary>
/// DEVHUB-019's PATCH pattern: absent, null and a value are three different things.
/// </summary>
public sealed class OptionalJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new OptionalJsonConverterFactory() },
    };

    private sealed record Patch(Optional<string?> Name, Optional<Guid?> AvatarId);

    [Fact]
    public void An_absent_field_is_not_present()
    {
        var patch = Deserialize("{}");

        Assert.False(patch.Name.HasValue);
        Assert.False(patch.AvatarId.HasValue);
    }

    [Fact]
    public void An_explicit_null_is_present_and_null()
    {
        var patch = Deserialize("""{ "avatarId": null }""");

        Assert.True(patch.AvatarId.HasValue);
        Assert.Null(patch.AvatarId.Value);
        Assert.False(patch.Name.HasValue);
    }

    [Fact]
    public void A_value_is_present_with_that_value()
    {
        var id = Guid.NewGuid();

        var patch = Deserialize($$"""{ "name": "Ada", "avatarId": "{{id}}" }""");

        Assert.Equal("Ada", patch.Name.Value);
        Assert.Equal(id, patch.AvatarId.Value);
    }

    [Fact]
    public void A_value_of_the_wrong_type_is_a_json_error()
    {
        Assert.Throws<JsonException>(() => Deserialize("""{ "avatarId": 42 }"""));
    }

    [Fact]
    public void Reading_the_value_of_an_absent_optional_throws()
    {
        Assert.Throws<InvalidOperationException>(() => default(Optional<string>).Value);
    }

    private static Patch Deserialize(string json) => JsonSerializer.Deserialize<Patch>(json, Options)!;
}
