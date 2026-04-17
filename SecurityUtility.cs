using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NetworkMonitor
{
    internal static class SecurityUtility
    {
        private const string ProtectedValuePrefix = "dpapi:";
        private static readonly byte[] AdditionalEntropy = Encoding.UTF8.GetBytes("NetworkMonitor.Password.v1");

        private static readonly Regex QueryValueRegex = new Regex(
            @"(?i)(?<key>\b(?:userid|userId|passwd|password|wlanuserip|mac|hostname)\b=)(?<value>[^&\s]+)",
            RegexOptions.Compiled);

        private static readonly Regex LabeledValueRegex = new Regex(
            @"(?im)(?<prefix>(?:用户名|→\s*wlanuserip|→\s*mac|→\s*hostname):\s*)(?<value>.+)$",
            RegexOptions.Compiled);

        public static string ProtectForCurrentUser(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var protectedBytes = ProtectedData.Protect(plainBytes, AdditionalEntropy, DataProtectionScope.CurrentUser);
            return ProtectedValuePrefix + Convert.ToBase64String(protectedBytes);
        }

        public static bool TryUnprotectForCurrentUser(string protectedText, out string plainText)
        {
            plainText = string.Empty;
            if (!IsProtectedValue(protectedText))
            {
                return false;
            }

            try
            {
                var cipherBytes = Convert.FromBase64String(protectedText.Substring(ProtectedValuePrefix.Length));
                var plainBytes = ProtectedData.Unprotect(cipherBytes, AdditionalEntropy, DataProtectionScope.CurrentUser);
                plainText = Encoding.UTF8.GetString(plainBytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsProtectedValue(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.StartsWith(ProtectedValuePrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string SanitizeLogMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            var sanitized = QueryValueRegex.Replace(message, match =>
            {
                var key = match.Groups["key"].Value;
                var value = WebUtility.UrlDecode(match.Groups["value"].Value);
                return key + MaskByKey(key.TrimEnd('='), value);
            });

            sanitized = LabeledValueRegex.Replace(sanitized, match =>
            {
                var prefix = match.Groups["prefix"].Value;
                var rawLabel = prefix.Split(':')[0].Replace("→", string.Empty).Trim();
                var value = match.Groups["value"].Value.Trim();
                return prefix + MaskByKey(rawLabel, value);
            });

            return sanitized;
        }

        private static string MaskByKey(string key, string value)
        {
            switch (key.Trim().ToLowerInvariant())
            {
                case "userid":
                case "用户名":
                    return MaskAccount(value);
                case "passwd":
                case "password":
                    return "******";
                case "wlanuserip":
                    return MaskIpAddress(value);
                case "mac":
                    return MaskMacAddress(value);
                case "hostname":
                    return MaskHostname(value);
                default:
                    return value;
            }
        }

        private static string MaskAccount(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Length <= 2)
            {
                return new string('*', value.Length);
            }

            if (value.Length <= 6)
            {
                return $"{value[0]}{new string('*', value.Length - 2)}{value[^1]}";
            }

            return $"{value[..3]}****{value[^2..]}";
        }

        private static string MaskIpAddress(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (IPAddress.TryParse(value, out var address))
            {
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    var parts = value.Split('.');
                    if (parts.Length == 4)
                    {
                        return $"{parts[0]}.{parts[1]}.{parts[2]}.*";
                    }
                }

                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    var parts = value.Split(':');
                    if (parts.Length >= 3)
                    {
                        return $"{parts[0]}:{parts[1]}:****";
                    }
                }
            }

            return MaskGeneric(value);
        }

        private static string MaskMacAddress(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var separator = value.Contains('-') ? '-' : ':';
            var parts = value.Split(separator);
            if (parts.Length >= 6)
            {
                return $"{parts[0]}{separator}{parts[1]}{separator}**{separator}**{separator}**{separator}{parts[^1]}";
            }

            return MaskGeneric(value);
        }

        private static string MaskHostname(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.Length <= 2)
            {
                return value[0] + "*";
            }

            return $"{value[0]}***{value[^1]}";
        }

        private static string MaskGeneric(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Length <= 4)
            {
                return "****";
            }

            return $"{value[..2]}****{value[^2..]}";
        }
    }
}
