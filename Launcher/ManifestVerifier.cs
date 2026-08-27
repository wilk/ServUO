using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Launcher;

internal static class ManifestVerifier
{
    private const string PublicKeyResourceName = "Launcher.publickey.json";

    /// <summary>
    /// Verifies manifestBytes (the raw manifest.json file content) against
    /// signature using the public key embedded as a resource inside the
    /// launcher assembly (see Launcher.csproj), so a single ShardLauncher.exe
    /// is enough to verify updates without any loose file alongside it.
    /// </summary>
    public static bool Verify(byte[] manifestBytes, byte[] signature)
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(PublicKeyResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{PublicKeyResourceName}' was not found in the launcher assembly.");
        using var reader = new StreamReader(stream);
        string json = reader.ReadToEnd();

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        EmbeddedPublicKey key = JsonSerializer.Deserialize<EmbeddedPublicKey>(json, options)
            ?? throw new InvalidOperationException("publickey.json could not be parsed.");

        var parameters = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint
            {
                X = Convert.FromBase64String(key.X),
                Y = Convert.FromBase64String(key.Y)
            }
        };

        using ECDsa ecdsa = ECDsa.Create(parameters);
        return ecdsa.VerifyData(manifestBytes, signature, HashAlgorithmName.SHA256);
    }
}
