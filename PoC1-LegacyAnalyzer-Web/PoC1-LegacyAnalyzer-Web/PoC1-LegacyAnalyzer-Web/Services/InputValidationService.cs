using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PoC1_LegacyAnalyzer_Web.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    /// <summary>
    /// Service for validating and sanitizing user inputs.
    /// </summary>
    public interface IInputValidationService
    {
        /// <summary>
        /// Validates and sanitizes a business objective string.
        /// </summary>
        ValidationResult ValidateBusinessObjective(string businessObjective);

        /// <summary>
        /// Validates a file before processing.
        /// </summary>
        Task<ValidationResult> ValidateFileAsync(IBrowserFile file);

        /// <summary>
        /// Validates multiple files.
        /// </summary>
        Task<List<ValidationResult>> ValidateFilesAsync(IEnumerable<IBrowserFile> files);

        /// <summary>
        /// Sanitizes a business objective to prevent prompt injection.
        /// </summary>
        string SanitizeBusinessObjective(string businessObjective);

        /// <summary>
        /// Checks if a string contains prompt injection patterns.
        /// </summary>
        bool ContainsPromptInjection(string text);
    }

    /// <summary>
    /// Result of input validation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
        public string? SanitizedValue { get; set; }
    }

    /// <summary>
    /// Implementation of input validation service.
    /// </summary>
    public class InputValidationService : IInputValidationService
    {
        private readonly InputValidationConfiguration _config;
        private readonly ILogger<InputValidationService> _logger;
        private readonly List<Regex> _promptInjectionRegexes;
        private readonly List<Regex> _suspiciousPatternRegexes;

        public InputValidationService(
            IOptions<InputValidationConfiguration> config,
            ILogger<InputValidationService> logger)
        {
            _config = config?.Value ?? new InputValidationConfiguration();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Compile regex patterns for performance
            _promptInjectionRegexes = _config.PromptInjectionPatterns
                .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();

            _suspiciousPatternRegexes = _config.SuspiciousPatterns
                .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
                .ToList();
        }

        public ValidationResult ValidateBusinessObjective(string businessObjective)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(businessObjective))
            {
                result.IsValid = false;
                result.ErrorMessage = "Business objective cannot be empty.";
                return result;
            }

            // Check length
            if (businessObjective.Length > _config.MaxBusinessObjectiveLength)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Business objective exceeds maximum length of {_config.MaxBusinessObjectiveLength} characters.";
                return result;
            }

            // Check for forbidden characters
            foreach (var forbidden in _config.ForbiddenCharacters)
            {
                if (businessObjective.Contains(forbidden))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Business objective contains invalid characters.";
                    return result;
                }
            }

            // Check for prompt injection patterns
            if (ContainsPromptInjection(businessObjective))
            {
                result.IsValid = false;
                result.ErrorMessage = "Business objective contains potentially malicious content. Please rephrase.";
                _logger.LogWarning("Prompt injection attempt detected in business objective");
                return result;
            }

            // Sanitize and return
            result.SanitizedValue = SanitizeBusinessObjective(businessObjective);
            return result;
        }

        public async Task<ValidationResult> ValidateFileAsync(IBrowserFile file)
        {
            var result = new ValidationResult { IsValid = true };

            if (file == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "File is null.";
                return result;
            }

            // Validate file name
            if (string.IsNullOrWhiteSpace(file.Name))
            {
                result.IsValid = false;
                result.ErrorMessage = "File name is empty.";
                return result;
            }

            if (file.Name.Length > _config.MaxFileNameLength)
            {
                result.IsValid = false;
                result.ErrorMessage = $"File name exceeds maximum length of {_config.MaxFileNameLength} characters.";
                return result;
            }

            // Check for forbidden characters in file name
            foreach (var forbiddenChar in _config.ForbiddenFileNameCharacters)
            {
                if (file.Name.Contains(forbiddenChar))
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"File name contains invalid character: {forbiddenChar}";
                    return result;
                }
            }

            // Validate file extension
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();
            if (!_config.AllowedFileExtensions.Contains(extension))
            {
                result.IsValid = false;
                result.ErrorMessage = $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", _config.AllowedFileExtensions)}";
                return result;
            }

            // Validate file size
            if (file.Size > _config.MaxFileSizeBytes)
            {
                result.IsValid = false;
                result.ErrorMessage = $"File size ({file.Size} bytes) exceeds maximum allowed size ({_config.MaxFileSizeBytes} bytes).";
                return result;
            }

            // Validate file signature (magic numbers) if enabled
            if (_config.ValidateFileSignatures && _config.FileSignatures.ContainsKey(extension))
            {
                try
                {
                    var signatureValid = await ValidateFileSignatureAsync(file, extension);
                    if (!signatureValid)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"File signature validation failed for {extension} file.";
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to validate file signature for {FileName}", file.Name);
                    result.Warnings.Add("Could not validate file signature.");
                }
            }

            // Scan for suspicious patterns if enabled
            if (_config.ScanForSuspiciousPatterns)
            {
                try
                {
                    var suspiciousPatterns = await ScanForSuspiciousPatternsAsync(file);
                    if (suspiciousPatterns.Any())
                    {
                        result.Warnings.Add($"File contains potentially suspicious patterns: {string.Join(", ", suspiciousPatterns)}");
                        _logger.LogWarning("Suspicious patterns detected in file {FileName}: {Patterns}", file.Name, string.Join(", ", suspiciousPatterns));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan file for suspicious patterns: {FileName}", file.Name);
                    // Don't fail validation if scanning fails, just log warning
                }
            }

            return result;
        }

        public async Task<List<ValidationResult>> ValidateFilesAsync(IEnumerable<IBrowserFile> files)
        {
            var results = new List<ValidationResult>();
            var fileList = files.ToList();

            // Check total file count
            if (fileList.Count > _config.MaxFilesPerAnalysis)
            {
                var errorResult = new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Number of files ({fileList.Count}) exceeds maximum allowed ({_config.MaxFilesPerAnalysis})."
                };
                results.Add(errorResult);
                return results;
            }

            // Validate each file
            foreach (var file in fileList)
            {
                var result = await ValidateFileAsync(file);
                results.Add(result);
            }

            return results;
        }

        public string SanitizeBusinessObjective(string businessObjective)
        {
            if (string.IsNullOrWhiteSpace(businessObjective))
                return businessObjective;

            var sanitized = businessObjective;

            // Remove null characters
            sanitized = sanitized.Replace("\0", string.Empty);

            // Normalize line endings
            sanitized = sanitized.Replace("\r\n", "\n").Replace("\r", "\n");

            // Remove excessive whitespace
            sanitized = Regex.Replace(sanitized, @"\s+", " ");

            // Trim
            sanitized = sanitized.Trim();

            // Truncate if still too long (shouldn't happen after validation, but safety check)
            if (sanitized.Length > _config.MaxBusinessObjectiveLength)
            {
                sanitized = sanitized.Substring(0, _config.MaxBusinessObjectiveLength);
            }

            return sanitized;
        }

        public bool ContainsPromptInjection(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return _promptInjectionRegexes.Any(regex => regex.IsMatch(text));
        }

        /// <summary>
        /// Validates file signature (magic numbers) against expected patterns.
        /// </summary>
        private async Task<bool> ValidateFileSignatureAsync(IBrowserFile file, string extension)
        {
            if (!_config.FileSignatures.ContainsKey(extension))
                return true; // No signature defined, allow

            var expectedSignatures = _config.FileSignatures[extension];
            if (!expectedSignatures.Any())
                return true; // No signatures defined, allow

            // Read first few bytes
            using var stream = file.OpenReadStream(maxAllowedSize: 1024);
            var buffer = new byte[Math.Min(32, (int)stream.Length)];
            await stream.ReadAsync(buffer, 0, buffer.Length);

            var fileSignature = BitConverter.ToString(buffer).Replace("-", "");

            // Check if file signature matches any expected pattern
            foreach (var expectedSignature in expectedSignatures)
            {
                var expectedBytes = expectedSignature.Replace(" ", "").Replace("-", "");
                if (fileSignature.StartsWith(expectedBytes, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Scans file content for suspicious patterns.
        /// </summary>
        private async Task<List<string>> ScanForSuspiciousPatternsAsync(IBrowserFile file)
        {
            var foundPatterns = new List<string>();

            try
            {
                // Read file content (limit to first 100KB for performance)
                using var stream = file.OpenReadStream(maxAllowedSize: 100 * 1024);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var content = await reader.ReadToEndAsync();

                // Check for suspicious patterns
                foreach (var regex in _suspiciousPatternRegexes)
                {
                    if (regex.IsMatch(content))
                    {
                        foundPatterns.Add(regex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning file {FileName} for suspicious patterns", file.Name);
            }

            return foundPatterns;
        }
    }
}

