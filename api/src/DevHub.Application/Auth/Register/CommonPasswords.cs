namespace DevHub.Application.Auth.Register;

/// <summary>
/// The "small common-password list" of auth-spec.md §2: passwords that pass the length rule
/// but are among the first a credential-stuffing list tries.
/// </summary>
/// <remarks>
/// Deliberately small and in code. A full breached-password check (e.g. the k-anonymity
/// Pwned Passwords API) is a better control, but it is an outbound dependency no ticket has
/// decided on. Only entries of 10+ characters are listed; shorter ones already fail on length.
/// </remarks>
internal static class CommonPasswords
{
    private static readonly HashSet<string> Passwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "1234567890",
        "0123456789",
        "0987654321",
        "1111111111",
        "12345678910",
        "123456789a",
        "1q2w3e4r5t",
        "qwertyuiop",
        "1qaz2wsx3edc",
        "asdfghjkl1",
        "password12",
        "password123",
        "password1234",
        "passw0rd123",
        "iloveyou12",
        "iloveyou123",
        "qwerty1234",
        "qwerty12345",
        "abcdefghij",
        "abc1234567",
        "letmein123",
        "welcome123",
        "football123",
        "baseball123",
        "sunshine123",
        "princess123",
        "starwars123",
        "trustno1234",
        "administrator",
        "changeme123",
    };

    public static bool Contains(string password) => Passwords.Contains(password);
}
