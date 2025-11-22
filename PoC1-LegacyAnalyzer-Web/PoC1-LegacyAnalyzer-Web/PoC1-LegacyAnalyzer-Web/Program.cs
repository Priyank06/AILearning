using PoC1_LegacyAnalyzer_Web.Models;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using PoC1_LegacyAnalyzer_Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Azure Key Vault if enabled
        ConfigureKeyVault(builder);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        // Register your AI services
        builder.Services.AddCodeAnalysisServices();
        builder.Services.AddMultiAgentOrchestration(builder.Configuration);
        builder.Services.AddSemanticKernel(builder.Configuration);

        var promptConfig = builder.Configuration.GetSection("PromptConfiguration").Get<PromptConfiguration>();

        if (promptConfig == null)
        {
            throw new InvalidOperationException("PromptConfiguration section is missing in appsettings.json.");
        }
        if (promptConfig.SystemPrompts == null || promptConfig.SystemPrompts.Count == 0)
        {
            throw new InvalidOperationException("PromptConfiguration.SystemPrompts is missing or empty in appsettings.json.");
        }
        if (promptConfig.AnalysisPromptTemplates == null || promptConfig.AnalysisPromptTemplates.Templates == null)
        {
            throw new InvalidOperationException("PromptConfiguration.AnalysisPromptTemplates.Templates is missing in appsettings.json.");
        }

        // AgentConfiguration validation
        var agentConfig = builder.Configuration.GetSection("AgentConfiguration").Get<AgentConfiguration>();
        if (agentConfig == null)
        {
            throw new InvalidOperationException("AgentConfiguration section is missing in appsettings.json.");
        }
        if (agentConfig.AgentProfiles == null ||
            !agentConfig.AgentProfiles.ContainsKey("security") ||
            !agentConfig.AgentProfiles.ContainsKey("performance") ||
            !agentConfig.AgentProfiles.ContainsKey("architecture"))
        {
            throw new InvalidOperationException("AgentConfiguration.AgentProfiles must contain 'security', 'performance', and 'architecture' profiles.");
        }
        if (agentConfig.AgentPromptTemplates == null ||
            !agentConfig.AgentPromptTemplates.ContainsKey("security") ||
            !agentConfig.AgentPromptTemplates.ContainsKey("performance") ||
            !agentConfig.AgentPromptTemplates.ContainsKey("architecture"))
        {
            throw new InvalidOperationException("AgentConfiguration.AgentPromptTemplates must contain 'security', 'performance', and 'architecture' templates.");
        }
        if (agentConfig.OrchestrationPrompts == null)
        {
            throw new InvalidOperationException("AgentConfiguration.OrchestrationPrompts is missing in appsettings.json.");
        }

        // BusinessCalculationRules validation
        var businessRules = builder.Configuration.GetSection("BusinessCalculationRules").Get<BusinessCalculationRules>();
        if (businessRules == null)
        {
            throw new InvalidOperationException("BusinessCalculationRules section is missing in appsettings.json.");
        }
        if (businessRules.CostCalculation == null)
        {
            throw new InvalidOperationException("BusinessCalculationRules.CostCalculation is missing.");
        }
        if (businessRules.CostCalculation.BaseValuePerLine <= 0)
        {
            throw new InvalidOperationException("BusinessCalculationRules.CostCalculation.BaseValuePerLine must be greater than 0.");
        }
        if (businessRules.CostCalculation.MaxEstimatedValue <= 0)
        {
            throw new InvalidOperationException("BusinessCalculationRules.CostCalculation.MaxEstimatedValue must be greater than 0.");
        }
        if (businessRules.CostCalculation.DefaultDeveloperHourlyRate <= 0)
        {
            throw new InvalidOperationException("BusinessCalculationRules.CostCalculation.DefaultDeveloperHourlyRate must be greater than 0.");
        }
        if (businessRules.ComplexityThresholds == null)
        {
            throw new InvalidOperationException("BusinessCalculationRules.ComplexityThresholds is missing.");
        }
        if (!(businessRules.ComplexityThresholds.VeryLow < businessRules.ComplexityThresholds.Low &&
              businessRules.ComplexityThresholds.Low < businessRules.ComplexityThresholds.Medium &&
              businessRules.ComplexityThresholds.Medium < businessRules.ComplexityThresholds.High &&
              businessRules.ComplexityThresholds.High < businessRules.ComplexityThresholds.VeryHigh))
        {
            throw new InvalidOperationException("BusinessCalculationRules.ComplexityThresholds must be ordered: VeryLow < Low < Medium < High < VeryHigh.");
        }
        if (businessRules.RiskThresholds == null)
        {
            throw new InvalidOperationException("BusinessCalculationRules.RiskThresholds is missing.");
        }
        if (!(businessRules.RiskThresholds.LowRiskMax < businessRules.RiskThresholds.MediumRiskMax &&
              businessRules.RiskThresholds.MediumRiskMax <= businessRules.RiskThresholds.HighRiskMin))
        {
            throw new InvalidOperationException("BusinessCalculationRules.RiskThresholds must be logical: LowRiskMax < MediumRiskMax <= HighRiskMin.");
        }
        if (businessRules.ProcessingLimits == null)
        {
            throw new InvalidOperationException("BusinessCalculationRules.ProcessingLimits is missing.");
        }
        if (businessRules.ProcessingLimits.MetadataSampleFileCount <= 0 ||
            businessRules.ProcessingLimits.CodeContextSummaryMaxLength <= 0 ||
            businessRules.ProcessingLimits.TokenEstimationCharsPerToken <= 0)
        {
            throw new InvalidOperationException("BusinessCalculationRules.ProcessingLimits values must be positive.");
        }

        var app = builder.Build();
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }

    /// <summary>
    /// Configures Azure Key Vault as a configuration source if enabled.
    /// </summary>
    private static void ConfigureKeyVault(WebApplicationBuilder builder)
    {
        var keyVaultConfig = builder.Configuration.GetSection("KeyVault").Get<KeyVaultConfiguration>();
        
        if (keyVaultConfig?.Enabled == true && !string.IsNullOrWhiteSpace(keyVaultConfig.VaultUri))
        {
            try
            {
                var vaultUri = new Uri(keyVaultConfig.VaultUri);
                
                // Configure credential based on environment
                DefaultAzureCredential credential;
                
                if (!string.IsNullOrWhiteSpace(keyVaultConfig.ManagedIdentityClientId))
                {
                    // Use user-assigned managed identity
                    credential = new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions
                        {
                            ManagedIdentityClientId = keyVaultConfig.ManagedIdentityClientId
                        });
                }
                else if (!string.IsNullOrWhiteSpace(keyVaultConfig.TenantId))
                {
                    // Use specific tenant ID
                    credential = new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions
                        {
                            TenantId = keyVaultConfig.TenantId
                        });
                }
                else
                {
                    // Use default credential chain (Managed Identity, Azure CLI, Visual Studio, etc.)
                    credential = new DefaultAzureCredential();
                }

                // Add Key Vault as configuration source
                builder.Configuration.AddAzureKeyVault(
                    vaultUri,
                    credential,
                    new AzureKeyVaultConfigurationOptions
                    {
                        ReloadInterval = keyVaultConfig.ReloadIntervalSeconds > 0
                            ? TimeSpan.FromSeconds(keyVaultConfig.ReloadIntervalSeconds)
                            : null,
                        Manager = new PrefixKeyVaultSecretManager(keyVaultConfig.SecretPrefix ?? "App--")
                    });

                Console.WriteLine($"Azure Key Vault configured: {vaultUri}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to configure Azure Key Vault: {ex.Message}");
                Console.WriteLine("Application will continue without Key Vault. Ensure secrets are available via other configuration sources.");
            }
        }
        else
        {
            Console.WriteLine("Azure Key Vault is not enabled or not configured. Using standard configuration sources.");
        }
    }
}