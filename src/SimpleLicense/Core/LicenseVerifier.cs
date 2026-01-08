// signs and saves to files
//
using System.Security.Cryptography;
using System.Text.Json;
using SimpleLicense.LicenseValidation;

namespace SimpleLicense.Core
{
    public class LicenseVerifier
    {
        /// <summary>
        /// The RSA public key in PEM format used for verifying licenses
        /// </summary>
        public string PublicPemText {get; }
        public PaddingChoice Padding { get; }
        /// <summary>
        /// The serializer used to convert License objects to/from JSON
        /// </summary>
        public CanonicalLicenseSerializer CanonicalSerializer {get; }

        public LicenseVerifier(
            string publicPemText,
            PaddingChoice padding = PaddingChoice.Pss,
            CanonicalLicenseSerializer? serializer = null)
        {
            PublicPemText = publicPemText ?? throw new ArgumentNullException(nameof(publicPemText));
            Padding = padding;
            CanonicalSerializer = serializer ?? new CanonicalLicenseSerializer()
            {
                UnSerializedFields = new List<string> { "licenseInfo", "licenseId" }
            };
        }

        /// <summary>
        /// Verifies a flexible LicenseDocument whose Signature field contains base64 signature.
        /// Returns true if valid; false otherwise and sets failureReason.
        /// </summary>
        public bool VerifyLicenseDocument(LicenseDocument license, out string? failureReason)
        {
            failureReason = null;
            if (license is null) { failureReason = "License is null"; return false; }
            
            // Extract signature and ensure present
            var sigValue = license["Signature"];
            if (sigValue is not string sigBase64 || string.IsNullOrEmpty(sigBase64))
            {
                failureReason = "Signature missing or empty";
                return false;
            }
            
            byte[] signatureBytes;
            try { signatureBytes = Convert.FromBase64String(sigBase64); }
            catch (FormatException) { failureReason = "Signature is not valid Base64"; return false; }
            
            // Temporarily clear signature for verification
            var originalSignature = license["Signature"];
            license["Signature"] = null;
            
            try
            {
                // Create canonical bytes from the document with Signature removed
                byte[] canonicalBytes;
                try
                {
                    canonicalBytes = CanonicalSerializer.SerializeLicenseDocument(license);
                }
                catch (Exception ex)
                {
                    failureReason = $"Canonicalization failed: {ex.Message}";
                    return false;
                }

                // Verify signature with RSA public key
                using var rsa = RSA.Create();
                rsa.ImportFromPem(PublicPemText.ToCharArray());

                var hash = HashAlgorithmName.SHA256;
                var pad = (Padding == PaddingChoice.Pss) ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1;

                bool ok = rsa.VerifyData(canonicalBytes, signatureBytes, hash, pad);
                if (!ok) failureReason = "Signature verification failed";
                return ok;
            }
            catch (Exception ex)
            {
                failureReason = $"RSA verification error: {ex.Message}";
                return false;
            }
            finally
            {
                // Restore original signature
                license["Signature"] = originalSignature;
            }
        }

        /// <summary>
        /// Verifies a flexible LicenseDocument given as JSON string.
        /// Parses JSON to LicenseDocument, then calls VerifyLicenseDocument.
        /// </summary>
        public bool VerifyLicenseDocumentJson(string licenseJson, out string? failureReason)
        {
            if (string.IsNullOrWhiteSpace(licenseJson)) { failureReason = "Empty JSON"; return false; }

            try
            {
                // Deserialize to LicenseDocument
                var license = CanonicalSerializer.DeserializeLicenseDocument(licenseJson);
                if (license == null) { failureReason = "Deserialization produced null license"; return false; }
                return VerifyLicenseDocument(license, out failureReason);
            }
            catch (JsonException ex)
            {
                failureReason = "Invalid JSON: " + ex.Message;
                return false;
            }
            catch (LicenseValidationException ex)
            {
                failureReason = "License validation failed: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Legacy method: Verify a License record whose Signature property contains base64 signature.
        /// Returns true if valid; false otherwise and sets failureReason.
        /// For new code, use VerifyLicenseDocument() instead.
        /// </summary>
        public bool VerifyLicense(License license, out string? failureReason)
        {
            failureReason = null;
            if (license is null) { failureReason = "License is null"; return false; }
            // Encoding encoding = Encoding.UTF8;
            // Extract signature and ensure present
            var sigBase64 = license.Signature;
            if (string.IsNullOrEmpty(sigBase64)) { failureReason = "Signature missing or empty"; return false; }
            byte[] signatureBytes;
            try { signatureBytes = Convert.FromBase64String(sigBase64); }
            catch (FormatException) { failureReason = "Signature is not valid Base64"; return false; }
            // Create canonical bytes from the object with Signature removed (same routine as signer)
            var licenseToVerify = license with { Signature = null };
            byte[] canonicalBytes;
            try
            {
                canonicalBytes = CanonicalSerializer.Serialize(licenseToVerify);
            }
            catch (Exception ex)
            {
                failureReason = $"Canonicalization failed: {ex.Message}";
                return false;
            }

            // Verify signature with RSA public key
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(PublicPemText.ToCharArray());

                var hash = HashAlgorithmName.SHA256;
                var pad = (Padding == PaddingChoice.Pss) ? RSASignaturePadding.Pss : RSASignaturePadding.Pkcs1;

                bool ok = rsa.VerifyData(canonicalBytes, signatureBytes, hash, pad);
                if (!ok) failureReason = "Signature verification failed";
                return ok;
            }
            catch (Exception ex)
            {
                failureReason = $"RSA verification error: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Legacy method: Verify a license given as JSON string (pretty or compact). 
        /// Parses JSON, binds to License record, then calls VerifyLicense(License,...). 
        /// For new code, use VerifyLicenseDocumentJson() instead.
        /// </summary>
        public bool VerifyLicenseJson(string licenseJson, out string? failureReason)
        {
            //failureReason = null;
            if (string.IsNullOrWhiteSpace(licenseJson)) { failureReason = "Empty JSON"; return false; }

            try
            {
                // Deserialize to License using the same JsonOptions (camelCase mapping).
                var license = CanonicalSerializer.Deserialize(licenseJson);
                if (license == null) { failureReason = "Deserialization produced null license"; return false; }
                return VerifyLicense(license, out failureReason);
            }
            catch (JsonException ex)
            {
                failureReason = "Invalid JSON: " + ex.Message;
                return false;
            }
        }


    }
}