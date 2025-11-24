using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PoC1_LegacyAnalyzer_Web;

namespace PoC1_LegacyAnalyzer_Web.Services
{
    public class PaymentService
    {
        private readonly IKeyVaultService _keyVaultService;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IKeyVaultService keyVaultService, ILogger<PaymentService> logger)
        {
            _keyVaultService = keyVaultService;
            _logger = logger;
        }

        public async Task<string?> GetPaymentApiKeyAsync()
        {
            // Example: Fetch payment API key securely from Key Vault
            var apiKey = await _keyVaultService.GetSecretAsync("Payment--ApiKey");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Payment API key not found in Key Vault.");
                return null;
            }
            return apiKey;
        }
    }
}
