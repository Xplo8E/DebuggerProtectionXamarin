// DebuggerProtectionXamarin/IntegrityChecker.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Foundation; // Required for NSBundle

namespace DebuggerProtectionXamarin
{
    /// <summary>
    /// Defines the types of integrity checks available.
    /// </summary>
    public enum FileIntegrityCheckType
    {
        BundleId,
        MobileProvision
        // MachO // Mach-O check is complex to implement in C# due to P/Invoke requirements
    }

    /// <summary>
    /// Represents a specific integrity check to be performed.
    /// </summary>
    public class FileIntegrityCheck
    {
        public FileIntegrityCheckType Type { get; set; }
        public string ExpectedValue { get; set; }
        // public string ImageName { get; set; } // Optional: For MachO if implemented

        public override string ToString()
        {
            switch (Type)
            {
                case FileIntegrityCheckType.BundleId:
                    return $"Expected Bundle ID: {ExpectedValue}";
                case FileIntegrityCheckType.MobileProvision:
                    return $"Expected Mobile Provision SHA256 Hash: {ExpectedValue}";
                default:
                    return "Unknown Check";
            }
        }
    }

    /// <summary>
    /// Holds the result of the integrity checks.
    /// </summary>
    public class FileIntegrityCheckResult
    {
        /// <summary>
        /// True if any integrity check failed (tampering detected), false otherwise.
        /// </summary>
        public bool IsTampered { get; set; }

        /// <summary>
        /// A list of checks that failed.
        /// </summary>
        public List<FileIntegrityCheck> FailedChecks { get; set; } = new List<FileIntegrityCheck>();
    }

    /// <summary>
    /// Performs application integrity checks.
    /// </summary>
    public static class IntegrityChecker
    {
        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] IntegrityChecker: {message}");
        }

        /// <summary>
        /// Checks if the application has been tampered with based on the specified checks.
        /// </summary>
        /// <param name="checks">A list of integrity checks to perform.</param>
        /// <returns>A FileIntegrityCheckResult indicating if tampering was detected and which checks failed.</returns>
        public static FileIntegrityCheckResult AmITampered(List<FileIntegrityCheck> checks)
        {
            var result = new FileIntegrityCheckResult { IsTampered = false };

            foreach (var check in checks)
            {
                bool checkFailed = false;
                switch (check.Type)
                {
                    case FileIntegrityCheckType.BundleId:
                        Log($"Performing Bundle ID check. Expecting: {check.ExpectedValue}");
                        checkFailed = CheckBundleId(check.ExpectedValue);
                        Log($"Bundle ID check result: {(checkFailed ? "Failed" : "Passed")}");
                        break;

                    case FileIntegrityCheckType.MobileProvision:
                        Log($"Performing Mobile Provision hash check. Expecting: {check.ExpectedValue}");
                        checkFailed = CheckMobileProvision(check.ExpectedValue);
                        Log($"Mobile Provision hash check result: {(checkFailed ? "Failed" : "Passed")}");
                        break;

                        // case FileIntegrityCheckType.MachO:
                        //     Log("Mach-O check is not implemented in this version.");
                        //     // checkFailed = CheckMachO(check.ImageName, check.ExpectedValue);
                        //     break;
                }

                if (checkFailed)
                {
                    Log($"Integrity check failed: {check}");
                    result.IsTampered = true;
                    result.FailedChecks.Add(check);
                }
            }

            Log($"Overall Tampering Result: {result.IsTampered}");
            return result;
        }

        /// <summary>
        /// Checks if the current application's bundle identifier matches the expected one.
        /// </summary>
        /// <param name="expectedBundleId">The expected bundle identifier.</param>
        /// <returns>True if the bundle identifier does NOT match (tampering detected), false otherwise.</returns>
        private static bool CheckBundleId(string expectedBundleId)
        {
            string? currentBundleId = NSBundle.MainBundle.BundleIdentifier;
            Log($"Current Bundle ID: {currentBundleId ?? "null"}");

            if (string.IsNullOrEmpty(currentBundleId))
            {
                Log("Error: Could not retrieve current bundle identifier.");
                return true; // Treat inability to get ID as a potential issue/failure
            }

            // Comparison should be case-sensitive as bundle IDs usually are
            return !expectedBundleId.Equals(currentBundleId, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if the SHA256 hash of the embedded.mobileprovision file matches the expected hash.
        /// </summary>
        /// <param name="expectedSha256Value">The expected SHA256 hash (lowercase hex string).</param>
        /// <returns>True if the hash does NOT match or the file is inaccessible (tampering detected), false otherwise.</returns>
        private static bool CheckMobileProvision(string expectedSha256Value)
        {
            string? path = NSBundle.MainBundle.PathForResource("embedded", "mobileprovision");

            if (string.IsNullOrEmpty(path))
            {
                Log("embedded.mobileprovision file not found in the main bundle.");
                // If the file MUST exist in a production build, consider this a failure.
                // If it might be absent (e.g., simulator builds w/o signing), return false.
                // For security, let's treat its absence as suspicious in a signed build context.
                return true;
            }

            Log($"Found embedded.mobileprovision at: {path}");

            if (!File.Exists(path))
            {
                Log("Error: embedded.mobileprovision path exists in bundle info, but file not found on disk.");
                return true; // File expected but missing
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(path);
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(fileBytes);
                    string currentHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    Log($"Calculated Mobile Provision SHA256: {currentHash}");
                    Log($"Expected Mobile Provision SHA256:   {expectedSha256Value.ToLowerInvariant()}");

                    // Compare the calculated hash with the expected hash (case-insensitive)
                    return !currentHash.Equals(expectedSha256Value.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Log($"Error reading or hashing embedded.mobileprovision: {ex.Message}");
                return true; // Treat errors during processing as a potential tampering indicator
            }
        }

        // --- Mach-O Check (Placeholder - Complex Implementation) ---
        // private static bool CheckMachO(string imageName, string expectedSha256Value)
        // {
        //     Log($"Mach-O check for image '{imageName}' is not implemented.");
        //     // Requires complex P/Invoke calls to _dyld functions, struct marshalling,
        //     // and pointer arithmetic similar to the Swift code.
        //     return false; // Defaulting to 'not tampered' as it's not implemented
        // }
    }
}