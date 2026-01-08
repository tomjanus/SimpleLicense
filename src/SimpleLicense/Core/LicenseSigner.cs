// Signs the license and 
using System.Security.Cryptography;
using SimpleLicense.LicenseValidation;

namespace SimpleLicense.Core
{
    public enum PaddingChoice
    {
        Pkcs1,
        Pss
    }

    public class LicenseSigner
    {
        /// <summary>
        /// The RSA private key in PEM format used for signing licenses
        /// </summary>
        public string PrivatePemText { get; }
        public PaddingChoice Padding { get; }
        public CanonicalLicenseSerializer CanonicalSerializer {get; }

        public LicenseSigner(
            string privatePemText, 
            PaddingChoice padding = PaddingChoice.Pss,
            CanonicalLicenseSerializer? serializer = null)
        {
            PrivatePemText = privatePemText ?? throw new ArgumentNullException(nameof(privatePemText));
            Padding = padding;
            CanonicalSerializer = serializer ?? new CanonicalLicenseSerializer()
                {
                    UnSerializedFields = new List<string> { "licenseInfo", "licenseId" }
                };
        }

        /// <summary>
        /// Signs a flexible LicenseDocument. 
        /// Sets the Signature field with the base64-encoded signature.
        /// </summary>
        /// <param name="license">The LicenseDocument to sign</param>
        /// <returns>The same LicenseDocument instance with Signature field populated</returns>
        public LicenseDocument SignLicenseDocument(LicenseDocument license)
        {
            ArgumentNullException.ThrowIfNull(license);
            
            // Temporarily clear signature field for signing
            var originalSignature = license["Signature"];
            license["Signature"] = null;
            
            try
            {
                // Canonical bytes of the document (serialize with sorted keys, excluding signature)
                var canonicalBytes = CanonicalSerializer.SerializeLicenseDocument(license);
                
                // Sign
                using var rsa = RSA.Create();
                rsa.ImportFromPem(PrivatePemText.ToCharArray());
                var hash = HashAlgorithmName.SHA256;
                var pad = (Padding == PaddingChoice.Pss) ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1;
                var signatureBytes = rsa.SignData(canonicalBytes, hash, pad);
                var sigBase64 = Convert.ToBase64String(signatureBytes);
                
                // Set signature on the document
                license["Signature"] = sigBase64;
                return license;
            }
            catch
            {
                // Restore original signature on error
                license["Signature"] = originalSignature;
                throw;
            }
        }

        /// <summary>
        /// Legacy method: Sign a License record. Returns a new License with Signature filled (base64).
        /// For new code, use SignLicenseDocument() instead.
        /// </summary>
        public License SignLicense(License license)
        {
            ArgumentNullException.ThrowIfNull(license);
            // Create a copy with Signature = null to ensure it's not part of signed bytes.
            var licenseToSign = license with { Signature = null };
            // Canonical bytes of the object (serialize with JsonOptions, parse, then canonicalize sorted keys).
            var canonicalBytes = CanonicalSerializer.Serialize(licenseToSign);
            // Sign
            using var rsa = RSA.Create();
            rsa.ImportFromPem(PrivatePemText.ToCharArray());
            var hash = HashAlgorithmName.SHA256;
            var pad = (Padding == PaddingChoice.Pss) ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1;
            var signatureBytes = rsa.SignData(canonicalBytes, hash, pad);
            var sigBase64 = Convert.ToBase64String(signatureBytes);
            // Return new license with signature populated
            return licenseToSign with { Signature = sigBase64 };
        }

    }
}

