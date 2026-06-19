using System.Security.Cryptography;
using QuantInfra.Common.Infrastructure.Abstractions;

namespace QuantInfra.Services.MonolithService;

public class FileSecretProviderConfig
{
    public string FilePath { get; init; }
}

public sealed class FileSecretProvider(FileSecretProviderConfig config) : ISecretProvider
{
    public byte[] GetOrCreateMasterSecret()
    {
        if (File.Exists(config.FilePath))
        {
            var base64 = File.ReadAllText(config.FilePath).Trim();

            if (string.IsNullOrWhiteSpace(base64))
            {
                throw new InvalidOperationException(
                    $"Master key file '{config.FilePath}' is empty.");
            }

            return Convert.FromBase64String(base64);
        }

        Directory.CreateDirectory(
            Path.GetDirectoryName(config.FilePath)
            ?? throw new InvalidOperationException("Invalid key path."));

        var key = RandomNumberGenerator.GetBytes(32); // AES-256

        var base64Key = Convert.ToBase64String(key);

        File.WriteAllText(config.FilePath, base64Key);

        TryRestrictPermissions(config.FilePath);

        return key;
    }

    private static void TryRestrictPermissions(string path)
    {
        try
        {
            if (!OperatingSystem.IsLinux() &&
                !OperatingSystem.IsMacOS())
            {
                return;
            }

            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite);
        }
        catch
        {
            // Best effort only.
        }
    }
}