using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace SalsaNOW
{
    internal static class CertificateValidation
    {
        private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

        private static readonly Lazy<X509Certificate2[]> BundledRoots =
            new Lazy<X509Certificate2[]>(LoadBundledRoots);

        private static readonly ConcurrentDictionary<string, bool> ReportedHosts =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public static bool Validate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;

            string host = (sender as HttpWebRequest)?.RequestUri.Host ?? "unknown host";

            bool accepted = errors == SslPolicyErrors.RemoteCertificateChainErrors
                && certificate != null
                && ChainsToBundledRoot(new X509Certificate2(certificate), chain);

            if (ReportedHosts.TryAdd(host + "|" + accepted, true))
            {
                if (accepted)
                    SalsaLogger.Warn($"Windows does not trust the root certificate of {host}; it was verified against SalsaNOW's bundled roots.");
                else
                    SalsaLogger.Error($"Rejected the TLS certificate of {host} ({errors}).");
            }

            return accepted;
        }

        private static bool ChainsToBundledRoot(X509Certificate2 leaf, X509Chain systemChain)
        {
            using (var bundled = new X509Chain())
            {
                bundled.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                bundled.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                bundled.ChainPolicy.ApplicationPolicy.Add(new Oid(ServerAuthenticationOid));
                bundled.ChainPolicy.ExtraStore.AddRange(BundledRoots.Value);

                if (systemChain != null)
                {
                    foreach (X509ChainElement element in systemChain.ChainElements)
                        bundled.ChainPolicy.ExtraStore.Add(element.Certificate);
                }

                if (!bundled.Build(leaf))
                    return false;

                if (bundled.ChainStatus.Any(s => (s.Status & ~X509ChainStatusFlags.UntrustedRoot) != 0))
                    return false;

                X509Certificate2 root = bundled.ChainElements[bundled.ChainElements.Count - 1].Certificate;
                return BundledRoots.Value.Any(r => string.Equals(r.Thumbprint, root.Thumbprint, StringComparison.OrdinalIgnoreCase));
            }
        }

        private static X509Certificate2[] LoadBundledRoots()
        {
            using (Stream stream = typeof(CertificateValidation).Assembly.GetManifestResourceStream("SalsaNOW.TrustedRoots.pem"))
            using (var reader = new StreamReader(stream))
            {
                string pem = reader.ReadToEnd();

                return Regex.Matches(pem, "-----BEGIN CERTIFICATE-----(.+?)-----END CERTIFICATE-----", RegexOptions.Singleline)
                    .Cast<Match>()
                    .Select(m => new X509Certificate2(Convert.FromBase64String(m.Groups[1].Value)))
                    .ToArray();
            }
        }
    }
}
