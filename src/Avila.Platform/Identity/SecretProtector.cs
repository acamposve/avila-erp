using System.Security.Cryptography;

namespace Avila.Platform.Identity;

public static class SecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public static byte[] ParseKey(string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        if (key.Length != 32)
        {
            throw new InvalidOperationException("Identity:MfaSecretKey must be a base64-encoded 32-byte key.");
        }

        return key;
    }

    public static string Protect(string plaintext, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        ciphertext.CopyTo(payload, NonceSize + TagSize);
        return Convert.ToBase64String(payload);
    }

    public static string Unprotect(string protectedValue, byte[] key)
    {
        var payload = Convert.FromBase64String(protectedValue);
        if (payload.Length < NonceSize + TagSize)
        {
            throw new InvalidOperationException("Protected MFA secret is invalid.");
        }

        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var ciphertext = payload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return System.Text.Encoding.UTF8.GetString(plaintext);
    }
}
