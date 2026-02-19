using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web.Models.Marketplace;

namespace PoC1_LegacyAnalyzer_Web.Services.Validation
{
    /// <summary>
    /// Validates marketplace CSV submissions for the Microsoft 365 Certification programme.
    ///
    /// The schema is derived directly from the official marketplace CSV template and covers:
    ///   - General (GEN) fields
    ///   - Data Handling &amp; Privacy (DHP / PRV) fields
    ///   - Security (SEC) fields
    ///   - Compliance (CMP) fields
    ///   - Identity (IDD / ZTR) fields
    /// </summary>
    public class MarketplaceCsvValidationService : IMarketplaceCsvValidationService
    {
        private readonly ILogger<MarketplaceCsvValidationService> _logger;

        // Pre-compiled patterns used during validation.
        private static readonly Regex DatePattern =
            new(@"^\d{4}/(?:0[1-9]|1[0-2])/(?:0[1-9]|[12]\d|3[01])$", RegexOptions.Compiled);

        private static readonly Regex EmailPattern =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Accepted boolean literals (case-insensitive).
        private static readonly HashSet<string> TrueFalseValues =
            new(StringComparer.OrdinalIgnoreCase) { "True", "False" };

        private static readonly HashSet<string> TrueFalseNAValues =
            new(StringComparer.OrdinalIgnoreCase) { "True", "False", "NA" };

        // ------------------------------------------------------------------
        // Field schema – one entry per marketable CSV field.
        // ------------------------------------------------------------------
        private static readonly IReadOnlyList<MarketplaceFieldDefinition> FieldSchema = BuildSchema();

        // Lookup by field ID for O(1) access during validation.
        private static readonly IReadOnlyDictionary<string, MarketplaceFieldDefinition> SchemaById =
            FieldSchema.ToDictionary(f => f.FieldId, StringComparer.OrdinalIgnoreCase);

        public MarketplaceCsvValidationService(ILogger<MarketplaceCsvValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        public IReadOnlyList<MarketplaceFieldDefinition> GetFieldDefinitions() => FieldSchema;

        public MarketplaceCsvValidationResult ValidateCsvData(IReadOnlyDictionary<string, string?> csvData)
        {
            ArgumentNullException.ThrowIfNull(csvData);

            _logger.LogDebug("Starting marketplace CSV validation for {Count} supplied fields.", csvData.Count);

            var issues = new List<MarketplaceCsvFieldError>();
            var validatedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Validate every field that appears in the schema.
            foreach (var definition in FieldSchema)
            {
                if (definition.FieldType == MarketplaceFieldType.Ignored)
                    continue;

                validatedFields.Add(definition.FieldId);

                csvData.TryGetValue(definition.FieldId, out var rawValue);
                var value = rawValue?.Trim();

                // Required field check.
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (definition.Required)
                    {
                        issues.Add(MakeError(definition, value,
                            "Field is required but no value was provided.", ValidationSeverity.Error));
                    }
                    // Optional fields with no value need no further validation.
                    continue;
                }

                // Type-specific validation.
                IEnumerable<MarketplaceCsvFieldError> fieldIssues = definition.FieldType switch
                {
                    MarketplaceFieldType.TrueFalse => ValidateTrueFalse(definition, value),
                    MarketplaceFieldType.TrueFalseNA => ValidateTrueFalseNA(definition, value),
                    MarketplaceFieldType.Url => ValidateUrl(definition, value),
                    MarketplaceFieldType.Email => ValidateEmail(definition, value),
                    MarketplaceFieldType.Text => ValidateText(definition, value),
                    MarketplaceFieldType.Date => ValidateDate(definition, value),
                    MarketplaceFieldType.SelectOne => ValidateSelectOne(definition, value),
                    MarketplaceFieldType.SelectOneOrMore => ValidateSelectOneOrMore(definition, value),
                    _ => Array.Empty<MarketplaceCsvFieldError>()
                };

                issues.AddRange(fieldIssues);
            }

            // Warn about any supplied field IDs that are not in the schema.
            foreach (var key in csvData.Keys)
            {
                if (!SchemaById.ContainsKey(key))
                {
                    _logger.LogWarning("Unknown marketplace field ID '{FieldId}' was supplied but is not in the schema.", key);
                }
            }

            var errorFieldIds = issues
                .Where(i => i.Severity == ValidationSeverity.Error)
                .Select(i => i.FieldId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var warningOnlyFieldIds = issues
                .Where(i => i.Severity == ValidationSeverity.Warning)
                .Select(i => i.FieldId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(id => !errorFieldIds.Contains(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var result = new MarketplaceCsvValidationResult
            {
                IsValid = errorFieldIds.Count == 0,
                Issues = issues.AsReadOnly(),
                TotalFieldsValidated = validatedFields.Count,
                FieldsWithErrors = errorFieldIds.Count,
                FieldsWithWarnings = warningOnlyFieldIds.Count
            };

            _logger.LogInformation(
                "Marketplace CSV validation complete. Valid={IsValid}, Errors={Errors}, Warnings={Warnings}.",
                result.IsValid, result.FieldsWithErrors, result.FieldsWithWarnings);

            return result;
        }

        // ------------------------------------------------------------------
        // Per-type validators
        // ------------------------------------------------------------------

        private static IEnumerable<MarketplaceCsvFieldError> ValidateTrueFalse(
            MarketplaceFieldDefinition def, string value)
        {
            if (!TrueFalseValues.Contains(value))
            {
                yield return MakeError(def, value,
                    $"Expected 'True' or 'False', but received '{value}'.", ValidationSeverity.Error);
            }
        }

        private static IEnumerable<MarketplaceCsvFieldError> ValidateTrueFalseNA(
            MarketplaceFieldDefinition def, string value)
        {
            if (!TrueFalseNAValues.Contains(value))
            {
                yield return MakeError(def, value,
                    $"Expected 'True', 'False', or 'NA', but received '{value}'.", ValidationSeverity.Error);
            }
        }

        private static IEnumerable<MarketplaceCsvFieldError> ValidateUrl(
            MarketplaceFieldDefinition def, string value)
        {
            if (def.MaxLength.HasValue && value.Length > def.MaxLength.Value)
            {
                yield return MakeError(def, value,
                    $"URL exceeds the maximum of {def.MaxLength} characters (actual: {value.Length}).",
                    ValidationSeverity.Error);
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                yield return MakeError(def, value,
                    $"'{value}' is not a valid absolute HTTP/HTTPS URL.", ValidationSeverity.Error);
            }
        }

        private static IEnumerable<MarketplaceCsvFieldError> ValidateEmail(
            MarketplaceFieldDefinition def, string value)
        {
            if (!EmailPattern.IsMatch(value))
            {
                yield return MakeError(def, value,
                    $"'{value}' does not appear to be a valid email address.", ValidationSeverity.Error);
            }
        }

        private static IEnumerable<MarketplaceCsvFieldError> ValidateText(
            MarketplaceFieldDefinition def, string value)
        {
            if (def.MaxLength.HasValue && value.Length > def.MaxLength.Value)
            {
                yield return MakeError(def, value,
                    $"Text exceeds the maximum of {def.MaxLength} characters (actual: {value.Length}).",
                    ValidationSeverity.Error);
            }
        }

        private static IEnumerable<MarketplaceCsvFieldError> ValidateDate(
            MarketplaceFieldDefinition def, string value)
        {
            if (!DatePattern.IsMatch(value))
            {
                yield return MakeError(def, value,
                    $"Date '{value}' does not match the required format yyyy/mm/dd.", ValidationSeverity.Error);
            }
        }

        private static IEnumerable<MarketplaceCsvFieldError> ValidateSelectOne(
            MarketplaceFieldDefinition def, string value)
        {
            if (def.AllowedValues == null || def.AllowedValues.Count == 0)
                yield break; // No known values to validate against.

            if (!def.AllowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                yield return MakeError(def, value,
                    $"'{value}' is not one of the allowed values: {string.Join(", ", def.AllowedValues)}.",
                    ValidationSeverity.Error);
            }
        }

        private static IEnumerable<MarketplaceCsvFieldError> ValidateSelectOneOrMore(
            MarketplaceFieldDefinition def, string value)
        {
            if (def.AllowedValues == null || def.AllowedValues.Count == 0)
                yield break; // No known values to validate against.

            var selected = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var item in selected)
            {
                if (!def.AllowedValues.Contains(item, StringComparer.OrdinalIgnoreCase))
                {
                    yield return MakeError(def, value,
                        $"'{item}' is not one of the allowed values: {string.Join(", ", def.AllowedValues)}.",
                        ValidationSeverity.Error);
                }
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static MarketplaceCsvFieldError MakeError(
            MarketplaceFieldDefinition def,
            string? value,
            string message,
            ValidationSeverity severity) =>
            new()
            {
                FieldId = def.FieldId,
                Question = def.Question,
                ProvidedValue = value,
                ErrorMessage = message,
                Severity = severity
            };

        // ------------------------------------------------------------------
        // Schema definition
        // ------------------------------------------------------------------

        private static IReadOnlyList<MarketplaceFieldDefinition> BuildSchema()
        {
            // Helper shortcuts to keep the list readable.
            static MarketplaceFieldDefinition TF(string id, string q, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.TrueFalse, Required = required };

            static MarketplaceFieldDefinition TFNA(string id, string q, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.TrueFalseNA, Required = required };

            static MarketplaceFieldDefinition Url(string id, string q, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.Url, MaxLength = 500, Required = required };

            static MarketplaceFieldDefinition Email(string id, string q, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.Email, Required = required };

            static MarketplaceFieldDefinition Text(string id, string q, int maxLen = 500, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.Text, MaxLength = maxLen, Required = required };

            static MarketplaceFieldDefinition Date(string id, string q, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.Date, Required = required };

            static MarketplaceFieldDefinition One(string id, string q, IReadOnlyList<string>? values = null, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.SelectOne, AllowedValues = values, Required = required };

            static MarketplaceFieldDefinition Many(string id, string q, IReadOnlyList<string>? values = null, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.SelectOneOrMore, AllowedValues = values, Required = required };

            static MarketplaceFieldDefinition Free(string id, string q, bool required = false) =>
                new() { FieldId = id, Question = q, FieldType = MarketplaceFieldType.FreeText, Required = required };

            // ----------------------------------------------------------------
            // GEN – General fields
            // ----------------------------------------------------------------
            return
            [
                Free("GEN01_appNameTranslated",
                    "App or Add-in Names (Translated in English)"),

                Url("GEN37_companyWebsiteUrl",
                    "Link to your company's website"),

                Url("GEN39_termsOfUseUrl",
                    "Link to the app's Terms of Use"),

                Text("GEN26_shortDescription",
                    "Core functionality description (500 characters or fewer)", maxLen: 500),

                TF("GEN17_aiFunctionality",
                    "Is AI functionality part of your app's core features or workflows?",
                    required: true),

                One("GEN13_mcasLocationHQ",
                    "Country or region where your company is headquartered",
                    required: true),

                TF("GEN30_hasAppInfoUrl",
                    "Does the app have an info page?",
                    required: true),

                Url("GEN30_appInfoUrl",
                    "Link to the app's info page"),

                Free("GEN20_hostingEnvironment",
                    "Hosting environment or service model used to run your app"),

                Many("GEN16_mcasHostingCompanyName",
                    "Which hosting cloud providers does the app use?",
                    values: ["Azure", "Google Cloud Platform", "VMware Cloud", "IBM Cloud",
                             "Oracle Cloud", "Alibaba Cloud"]),

                Email("GEN31_customerContact",
                    "Customer support contact email",
                    required: true),

                Email("GEN32_additionalContact",
                    "Additional contact email"),

                // ----------------------------------------------------------------
                // DHP – Data Handling & Privacy
                // ----------------------------------------------------------------
                TF("DHP_G07_customerDataProcess",
                    "Does the app or underlying infrastructure process any Microsoft customer data?",
                    required: true),

                Free("DHP_G04_graphPermissionData",
                    "What data is collected or processed by your app?"),

                TF("DHP_G16_tlsSupport",
                    "Does the app support TLS 1.2 or higher?",
                    required: true),

                TF("DHP_G06_customerDataStorage",
                    "Does the app or underlying infrastructure store any Microsoft customer data?",
                    required: true),

                Text("DHP_G05_graphPermissionInfo",
                    "What data is stored in your databases?"),

                Many("DHP_G08_storageLocation",
                    "Where is Microsoft customer data geographically stored?"),

                TF("DHP_G09_dataRetention",
                    "Do you have an established data retention and disposal process?",
                    required: true),

                One("LEG03_complianceDataTermination",
                    "How long do you maintain user data after account termination?",
                    values: ["Not retained", "Less than 30 days", "Less than 60 days",
                             "Less than 90 days", "More than 90 days"]),

                TF("DHP_G10_customerDataManagement",
                    "Do you have an established process to manage all access to customer data, encryption keys/secrets?",
                    required: true),

                TF("DHP_G11_dataTransfer",
                    "Does the app transfer any Microsoft customer data to third parties or sub-processors?",
                    required: true),

                TF("DHP_G12_dataAgreement",
                    "Do you have data sharing agreements with any third-party service you share Microsoft customer data with?"),

                // ----------------------------------------------------------------
                // SEC – Security
                // ----------------------------------------------------------------
                TF("SEC28_securityPenTest",
                    "Do you perform annual penetration testing on the app?",
                    required: true),

                TF("SEC26_mcasDisasterRecoveryPlan",
                    "Does the service have a documented disaster recovery plan?",
                    required: true),

                Many("SEC27_antiMalware",
                    "Does your environment use traditional anti-malware protection or application controls?",
                    values: ["Traditional Anti-Malware", "Application Controls"],
                    required: true),

                TF("SEC29_complianceSecurityRisk",
                    "Do you have an established process for identifying and risk-ranking security vulnerabilities?",
                    required: true),

                TF("SEC30_complianceSlaAgreement",
                    "Do you have a policy that governs your SLA for applying patches?",
                    required: true),

                TF("SEC31_compliancePatchManagement",
                    "Do you carry out patch management activities according to your patching policy SLAs?",
                    required: true),

                TF("SEC32_complianceUnsupportedSoftware",
                    "Does your environment have any unsupported operating systems or software?",
                    required: true),

                TF("SEC33_complianceVulnerabilityScanning",
                    "Do you conduct quarterly vulnerability scanning on your app and supporting infrastructure?",
                    required: true),

                TF("SEC34_complianceFirewallInstallation",
                    "Do you have a firewall installed on your external network boundary?",
                    required: true),

                TF("SEC35_complianceChangeManagementProcess",
                    "Do you have an established change management process for production deployments?",
                    required: true),

                TF("SEC36_complianceAdditionalReviewer",
                    "Is an additional person reviewing and approving all code changes submitted to production?",
                    required: true),

                TF("SEC37_complianceSecureCoding",
                    "Do secure coding practices take into account common vulnerability classes such as OWASP Top 10?",
                    required: true),

                Many("SEC38_complianceMfa",
                    "Which of the following are secured by multifactor authentication (MFA)?",
                    values: ["Code Repositories", "DNS Management", "Credential/Key Stores", "None of the above"],
                    required: true),

                TF("SEC39_complianceAccountsMonitoring",
                    "Do you have an established process for provisioning, modification, and deletion of employee accounts?",
                    required: true),

                TFNA("SEC40_complianceSecureAppSupport",
                    "Do you have Intrusion Detection and Prevention (IDPS) software deployed at the network perimeter?",
                    required: true),

                TF("SEC41_complianceEventLogging",
                    "Do you have event logging set up on all system components supporting your app?",
                    required: true),

                TF("SEC42_complianceLogsReview",
                    "Are all logs reviewed on a regular cadence by human or automated tooling?",
                    required: true),

                TF("SEC43_complianceSecurityEvent",
                    "When a security event is detected are alerts automatically sent to an employee for triage?",
                    required: true),

                TF("SEC44_complianceRiskManagement",
                    "Do you have a formal information security risk management process established?",
                    required: true),

                TF("SEC45_complianceIncidentResponse",
                    "Do you have a formal security incident response process documented and established?",
                    required: true),

                TF("SEC46_complianceDataBreachReporting",
                    "Do you report data breaches to supervisory authorities and affected individuals within 72 hours?",
                    required: true),

                // ----------------------------------------------------------------
                // CMP – Compliance
                // ----------------------------------------------------------------
                TFNA("CMP04_complianceHIPAA",
                    "Does the app comply with HIPAA?"),

                TFNA("CMP28_complianceHITRUST",
                    "Does the app comply with HITRUST CSF?"),

                TFNA("CMP08_complianceSOC_1",
                    "Has your organization achieved a SOC 1 Certification?"),

                Date("CMP08_complianceSOC_1_DATE",
                    "SOC 1 most recent certification date (yyyy/mm/dd)"),

                TF("CMP09_complianceSOC_2",
                    "Has your organization achieved a SOC 2 Certification?"),

                Date("CMP09_complianceSOC_2_DATE",
                    "SOC 2 most recent certification date (yyyy/mm/dd)"),

                One("CMP09_complianceSOC2Type",
                    "Which SOC 2 certification type did you achieve?",
                    values: ["Type 1", "Type 2"]),

                TF("CMP11_complianceSOC_3",
                    "Has your organization achieved a SOC 3 Certification?"),

                Date("CMP11_complianceSOC_3_DATE",
                    "SOC 3 most recent certification date (yyyy/mm/dd)"),

                TFNA("CMP15_compliancePCIAnnualCheck",
                    "Do you carry out annual PCI DSS assessments against this app?"),

                TF("CMP06_complianceISO_27001",
                    "Is the app ISO 27001 certified?"),

                Date("CMP06_complianceISO_27001_DATE",
                    "ISO 27001 most recent certification date (yyyy/mm/dd)"),

                TFNA("CMP16_complianceISO_27018",
                    "Does the app comply with ISO 27018?"),

                Date("CMP16_complianceISO_27018_DATE",
                    "ISO 27018 most recent certification date (yyyy/mm/dd)"),

                TFNA("CMP22_complianceISO_27017",
                    "Does the app comply with ISO 27017?"),

                Date("CMP22_complianceISO_27017_DATE",
                    "ISO 27017 most recent certification date (yyyy/mm/dd)"),

                TFNA("CMP30_complianceISO_27002",
                    "Does the app comply with ISO 27002?"),

                Date("CMP30_complianceISO_27002_DATE",
                    "ISO 27002 most recent certification date (yyyy/mm/dd)"),

                TF("CMP18_complianceFedRAMP",
                    "Is the app FedRAMP compliant?"),

                One("CMP18_complianceFedRAMPLevel",
                    "FedRAMP compliance level",
                    values: ["Low", "LI SAAS", "Moderate", "High"]),

                TFNA("CMP26_complianceFERPA",
                    "Does the app comply with FERPA?"),

                TFNA("CMP25_complianceCOPPA",
                    "Does the app comply with COPPA?"),

                TFNA("CMP12_complianceSOX",
                    "Does the app comply with SOX?"),

                TFNA("CMP13_nist800171",
                    "Does the app comply with NIST 800-171?"),

                TF("CMP19_complianceCSAStarCert",
                    "Has the app been Cloud Security Alliance (CSA STAR) certified?"),

                One("CMP19_complianceCSAStar",
                    "CSA STAR certification level",
                    values: ["Continuous Monitoring", "Assessment", "Attestation",
                             "Self Assessment", "Certification"]),

                // ----------------------------------------------------------------
                // PRV – Privacy
                // ----------------------------------------------------------------
                TF("PRV01_dataProtection",
                    "Do you have GDPR or other privacy/data protection obligations?",
                    required: true),

                TF("PRV02_privacyNotice",
                    "Does the app have an external-facing privacy notice?"),

                Url("PRV03_url",
                    "URL of the privacy notice"),

                TF("PRV04_automatedDecisionMaking",
                    "Does the app perform automated decision making or profiling with legal effect?"),

                TF("PRV05_objectProcessing",
                    "Are individuals provided an option to object to the processing?"),

                TF("PRV06_personalDataProcessing",
                    "Does the app process personal data for a secondary purpose not described in the privacy notice?"),

                TF("PRV07_sensitiveData",
                    "Do you process special categories of sensitive data?"),

                TF("PRV08_minorDataProcessing",
                    "Does the app collect or process data from minors (under 16)?"),

                TF("PRV09_consent",
                    "Is consent obtained from a parent or legal guardian for minor data processing?"),

                TFNA("PRV10_deletePersonalData",
                    "Does the app have capabilities to delete an individual's personal data upon request?"),

                TFNA("PRV11_restrictDataProcessing",
                    "Does the app have capabilities to restrict the processing of personal data upon request?"),

                TFNA("PRV12_updatePersonalData",
                    "Does the app provide individuals the ability to correct or update their personal data?"),

                TFNA("PRV13_dataSecurity",
                    "Are regular data security and privacy reviews performed (e.g. DPIAs)?"),

                // ----------------------------------------------------------------
                // IDD / ZTR – Identity
                // ----------------------------------------------------------------
                TF("IDD01_iddIntegrationPlatform",
                    "Does your application integrate with Microsoft Identity Platform (Azure AD)?",
                    required: true),

                TF("DHP_G01_azureAppIdQuestion",
                    "Does your app use Azure Application appId(s)?"),

                Text("DHP_G01_azureAppId",
                    "Azure Application appId"),

                Text("DHP_G08_azureAppIdSingleTenantId",
                    "ID of the tenant where the Azure Application appId is registered"),

                TF("ZTR01_azureAppIdMultiuse",
                    "Is this Azure Application appId used by multiple applications?"),

                TF("DHP_G02_graphPermission",
                    "Does the app use Microsoft Graph permissions?"),

                One("DHP_G06_MSGraphPerm",
                    "Microsoft Graph permission",
                    // Values are too numerous to enumerate – any non-empty value is accepted.
                    values: null),

                One("DHP_G03_graphPermissionType",
                    "Graph permission type",
                    values: ["Delegated", "Application", "Both", "Resource-specific consent (RSC)"]),

                Text("ZTR04_graphPermJustify",
                    "Justification for using this Graph permission"),

                TF("IDD04_iddPrivPerm",
                    "Does your app request least privilege permissions for your scenario?"),

                TF("IDD18_iddIntegrationChecklist",
                    "Have you reviewed and complied with all applicable best practices in the Microsoft identity platform integration checklist?",
                    required: true),

                TFNA("IDD19_iddMsal",
                    "Does your app use the latest version of MSAL or Microsoft Identity Web for authentication?"),

                Text("ZTR03_libraries",
                    "Authentication library or libraries used (if not MSAL)"),

                TF("IDD02_iddAccessPolicy",
                    "Does your app support Conditional Access policies?"),

                Text("IDD03_iddTypesofPolicies",
                    "Types of Conditional Access policies supported"),

                TFNA("ZTR05_cae",
                    "Does your app support Continuous Access Evaluation (CAE)?"),

                TF("ZTR06_credStore",
                    "Does your app store any credentials in code?",
                    required: true),

                TF("DHP21_additionalMSAPIs",
                    "Does your app or add-in use additional Microsoft APIs outside of Graph?"),

                Text("DHP20_serviceName",
                    "Service name of the additional API"),

                Text("ZTR07_apiJustify",
                    "Justification for using the additional API")
            ];
        }
    }
}
