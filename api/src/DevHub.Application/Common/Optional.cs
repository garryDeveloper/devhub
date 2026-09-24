namespace DevHub.Application.Common;

/// <summary>
/// A PATCH field that may be absent. <c>default</c> is "absent — leave it alone"; a present
/// value, <b>including null</b>, is "set it to this" (DEVHUB-019).
/// </summary>
/// <remarks>
/// A plain nullable property cannot tell <c>{}</c> from <c>{"avatarAttachmentId": null}</c>:
/// both deserialize to null, but one means "don't touch the avatar" and the other "remove it".
/// This wrapper carries the missing bit. The JSON converter lives in DevHub.Api
/// (<c>OptionalJsonConverterFactory</c>): it only runs for properties that are present in the
/// body, so everything it reads is present, and everything it never sees stays <c>default</c>.
/// <para>
/// Every PATCH command uses this for its fields — solved once here, reused everywhere.
/// </para>
/// </remarks>
public readonly struct Optional<T>
{
    private readonly T _value;

    public Optional(T value)
    {
        _value = value;
        HasValue = true;
    }

    /// <summary>The field was in the request body — possibly as <c>null</c>.</summary>
    public bool HasValue { get; }

    public T Value => HasValue
        ? _value
        : throw new InvalidOperationException("An absent optional has no value. Check HasValue first.");

    public static implicit operator Optional<T>(T value) => new(value);

    public override string ToString() => HasValue ? $"{_value}" : "<absent>";
}
