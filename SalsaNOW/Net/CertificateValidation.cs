using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using SalsaNOW.Infrastructure;

namespace SalsaNOW.Net;

internal static class CertificateValidation
{
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    private static readonly Lazy<X509Certificate2[]> BundledRoots = new(LoadBundledRoots);

    private static readonly ConcurrentDictionary<string, bool> ReportedHosts = new(StringComparer.OrdinalIgnoreCase);

    public static bool Validate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None)
            return true;

        bool accepted = errors == SslPolicyErrors.RemoteCertificateChainErrors
            && certificate is not null
            && ChainsToBundledRoot(new X509Certificate2(certificate), chain);

        string host = (sender as HttpWebRequest)?.RequestUri.Host ?? "unknown host";
        if (ReportedHosts.TryAdd(host + "|" + accepted, true))
        {
            if (accepted)
                Log.Warn($"Windows does not trust the root certificate of {host}; it was verified against SalsaNOW's bundled roots.");
            else
                Log.Error($"Rejected the TLS certificate of {host} ({errors}).");
        }

        return accepted;
    }

    private static bool ChainsToBundledRoot(X509Certificate2 leaf, X509Chain? systemChain)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid(ServerAuthenticationOid));
        chain.ChainPolicy.ExtraStore.AddRange(BundledRoots.Value);

        if (systemChain is not null)
        {
            foreach (X509ChainElement element in systemChain.ChainElements)
                chain.ChainPolicy.ExtraStore.Add(element.Certificate);
        }

        if (!chain.Build(leaf))
            return false;

        if (chain.ChainStatus.Any(status => (status.Status & ~X509ChainStatusFlags.UntrustedRoot) != 0))
            return false;

        X509Certificate2 root = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
        return BundledRoots.Value.Any(bundled => string.Equals(bundled.Thumbprint, root.Thumbprint, StringComparison.OrdinalIgnoreCase));
    }

    private static X509Certificate2[] LoadBundledRoots()
    {
        using Stream stream = typeof(CertificateValidation).Assembly.GetManifestResourceStream("SalsaNOW.TrustedRoots.pem")
            ?? throw new InvalidOperationException("The bundled root certificates are missing from the build.");
        using var reader = new StreamReader(stream);

        return Regex.Matches(reader.ReadToEnd(), "-----BEGIN CERTIFICATE-----(.+?)-----END CERTIFICATE-----", RegexOptions.Singleline)
            .Cast<Match>()
            .Select(match => new X509Certificate2(Convert.FromBase64String(match.Groups[1].Value)))
            .ToArray();
    }
}
