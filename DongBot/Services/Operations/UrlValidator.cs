using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DongBot
{
    /// <summary>
    /// Validates GIF URLs for format, domain whitelist, and optional accessibility
    /// </summary>
    public class UrlValidator
    {
        private static readonly string[] ValidGifDomains = new[]
        {
            "giphy.com",
            "tenor.com",
            "gfycat.com",
            "imgur.com",
            "i.imgur.com",
            "media.giphy.com",
            "media1.giphy.com",
            "media2.giphy.com",
            "media3.giphy.com",
            "media.tenor.com",
            "media1.tenor.com",
            "media2.tenor.com",
            "c.tenor.com",
            "i.gifer.com",
            "media.tumblr.com",
            "cdn.discordapp.com",
            "media.discordapp.net"
        };

        private static readonly HttpClient SharedHttpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            return client;
        }

        /// <summary>
        /// Basic URL format validation
        /// </summary>
        public static bool IsValidUrl(string url)
        {
            return  string.IsNullOrWhiteSpace(url) 
                ?  false 
                :  Uri.TryCreate(url, UriKind.Absolute, out Uri? result)
                && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Check if URL points to common image/GIF hosting domains
        /// </summary>
        public static bool IsValidGifDomain(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri == null)
            {
                return false;
            }

            string host = uri.Host.ToLower();
            return ValidGifDomains.Any(domain => host.Contains(domain));
        }

        /// <summary>
        /// Check if URL is accessible via HTTP HEAD request
        /// </summary>
        public static async Task<bool> IsUrlAccessibleAsync(string url)
        {
            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url))
                using (HttpResponseMessage response = await SharedHttpClient.SendAsync(request))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"URL accessibility check failed for {url}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validate a GIF URL with detailed result
        /// </summary>
        public static UrlValidationResult ValidateGifUrl(string url, bool strictMode = false)
        {
            UrlValidationResult result = new UrlValidationResult
            {
                Url = url,
                IsValid = false,
                ErrorMessage = null,
                WarningOnly = false
            };

            // Check if empty
            if (string.IsNullOrWhiteSpace(url))
            {
                result.ErrorMessage = "URL cannot be empty";
                return result;
            }

            // Check basic URL format
            if (!IsValidUrl(url))
            {
                result.ErrorMessage = "Invalid URL format (must be http:// or https://)";
                return result;
            }

            // Check domain whitelist
            if (!IsValidGifDomain(url))
            {
                if (strictMode)
                {
                    result.ErrorMessage = "URL is not from a recognized GIF hosting service. Use strict mode override to add anyway.";
                    return result;
                }
                else
                {
                    result.IsValid = true;
                    result.WarningOnly = true;
                    result.ErrorMessage = "Warning: URL is not from a recognized GIF hosting service";
                    return result;
                }
            }

            // All checks passed
            result.IsValid = true;
            return result;
        }

        /// <summary>
        /// Validate a GIF URL including accessibility check (async)
        /// </summary>
        public static async Task<UrlValidationResult> ValidateGifUrlAsync(string url, bool checkAccessibility = true, bool strictMode = false)
        {
            // First do synchronous validation
            UrlValidationResult result = ValidateGifUrl(url, strictMode);

            if (!result.IsValid)
            {
                return result;
            }

            // Optionally check if URL is accessible
            if (checkAccessibility)
            {
                bool isAccessible = await IsUrlAccessibleAsync(url);
                if (!isAccessible)
                {
                    result.IsValid = true; // Don't block, just warn
                    result.WarningOnly = true;
                    result.ErrorMessage = result.WarningOnly 
                        ? result.ErrorMessage + " (Also: URL may not be accessible)"
                        : "Warning: URL may not be accessible or is taking too long to respond";
                }
            }

            return result;
        }

        /// <summary>
        /// Get list of valid GIF domains
        /// </summary>
        public static string[] GetValidDomains()
        {
            return ValidGifDomains;
        }
    }

    /// <summary>
    /// Result of URL validation
    /// </summary>
    public class UrlValidationResult
    {
        public string Url { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public bool WarningOnly { get; set; }
    }
}
