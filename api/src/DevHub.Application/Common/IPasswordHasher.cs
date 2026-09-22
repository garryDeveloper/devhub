using System;

namespace DevHub.Application.Common;

public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password for storage. The hash is salted and the salt is stored in the hash.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <returns>The hashed password.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    /// <param name="hash">The hash to verify against.</param>
    /// <param name="password">The password to verify.</param>
    /// <returns>True if the password matches the hash; otherwise, false.</returns>
    bool Verify(string hash, string password);
}
