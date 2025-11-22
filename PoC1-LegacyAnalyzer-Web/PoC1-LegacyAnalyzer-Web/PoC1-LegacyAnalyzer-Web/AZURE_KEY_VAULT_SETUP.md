# Azure Key Vault Integration Guide

This application uses Azure Key Vault for secure storage of sensitive configuration values such as API keys and connection strings. This guide explains how to set up and use Azure Key Vault with this application.

## Overview

Azure Key Vault integration provides:
- **Secure Storage**: Sensitive data is stored in Azure Key Vault instead of configuration files
- **Compliance**: Meets security best practices and compliance requirements
- **Centralized Management**: All secrets managed in one place
- **Automatic Rotation**: Secrets can be updated in Key Vault without code changes
- **Access Control**: Fine-grained access control using Azure RBAC

## Prerequisites

1. **Azure Subscription**: An active Azure subscription
2. **Azure Key Vault**: A Key Vault resource in Azure
3. **Authentication Method**: One of the following:
   - Azure Managed Identity (for Azure-hosted applications)
   - Azure CLI (for local development)
   - Service Principal (for CI/CD pipelines)
   - Visual Studio authentication (for local development)

## Setup Instructions

### 1. Create Azure Key Vault

1. Navigate to the [Azure Portal](https://portal.azure.com)
2. Create a new Key Vault resource:
   - **Name**: Choose a unique name (e.g., `your-app-keyvault`)
   - **Resource Group**: Select or create a resource group
   - **Region**: Choose your preferred region
   - **Pricing Tier**: Standard (recommended)
3. Note the **Vault URI** (e.g., `https://your-app-keyvault.vault.azure.net/`)

### 2. Add Secrets to Key Vault

Add the following secrets to your Key Vault. The application uses a prefix-based naming convention (default: `App--`).

#### Required Secrets

| Secret Name in Key Vault | Configuration Path | Description |
|-------------------------|-------------------|-------------|
| `App--AzureOpenAI--ApiKey` | `AzureOpenAI:ApiKey` | Azure OpenAI API key |
| `App--AzureOpenAI--Endpoint` | `AzureOpenAI:Endpoint` | Azure OpenAI endpoint URL |

#### Optional Secrets

You can also store other sensitive configuration values:

| Secret Name in Key Vault | Configuration Path | Description |
|-------------------------|-------------------|-------------|
| `App--ConnectionStrings--DefaultConnection` | `ConnectionStrings:DefaultConnection` | Database connection string |
| `App--CustomSecret` | `CustomSecret` | Any custom secret value |

**Note**: The double dash (`--`) in the secret name is converted to a colon (`:`) in the configuration path.

### 3. Configure Access Permissions

#### For Azure App Service / Managed Identity

1. In Azure Portal, go to your Key Vault
2. Navigate to **Access policies** or **Access control (IAM)**
3. For **Access policies**:
   - Click **Add Access Policy**
   - Select **Secret permissions**: Get, List
   - Under **Select principal**, choose your App Service's Managed Identity
   - Click **Add** and **Save**

4. For **Access control (IAM)** (recommended):
   - Click **Add** → **Add role assignment**
   - Role: **Key Vault Secrets User**
   - Assign access to: **Managed identity**
   - Select your App Service
   - Click **Review + assign**

#### For Local Development (Azure CLI)

1. Install [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
2. Login: `az login`
3. Grant yourself access to Key Vault:
   ```bash
   az keyvault set-policy --name <your-keyvault-name> --upn <your-email> --secret-permissions get list
   ```

### 4. Configure Application Settings

#### For Production (Azure App Service)

1. In Azure Portal, go to your App Service
2. Navigate to **Configuration** → **Application settings**
3. Add the following settings:

   | Name | Value |
   |------|-------|
   | `KeyVault:Enabled` | `true` |
   | `KeyVault:VaultUri` | `https://your-keyvault.vault.azure.net/` |
   | `KeyVault:SecretPrefix` | `App--` |
   | `KeyVault:ReloadIntervalSeconds` | `300` (optional, for secret refresh) |

4. **Enable Managed Identity**:
   - Go to **Identity** → **System assigned**
   - Set **Status** to **On**
   - Save and note the **Object (principal) ID**

#### For Local Development

Update `appsettings.Development.json`:

```json
{
  "KeyVault": {
    "Enabled": true,
    "VaultUri": "https://your-keyvault.vault.azure.net/",
    "SecretPrefix": "App--",
    "ReloadIntervalSeconds": 0
  }
}
```

**Important**: Ensure you're logged in via Azure CLI (`az login`) before running the application locally.

### 5. Alternative: User-Assigned Managed Identity

If using a user-assigned managed identity:

1. Create a user-assigned managed identity in Azure
2. Grant it access to Key Vault (same steps as above)
3. Add to application settings:
   - `KeyVault:ManagedIdentityClientId` = `<client-id-of-managed-identity>`

## Secret Naming Convention

The application uses a prefix-based naming convention:

- **Prefix**: `App--` (configurable via `KeyVault:SecretPrefix`)
- **Naming Rule**: `{Prefix}{ConfigurationPath}` where colons (`:`) are replaced with double dashes (`--`)

Examples:
- Configuration: `AzureOpenAI:ApiKey` → Key Vault Secret: `App--AzureOpenAI--ApiKey`
- Configuration: `ConnectionStrings:DefaultConnection` → Key Vault Secret: `App--ConnectionStrings--DefaultConnection`

## Configuration Priority

The application loads configuration in this order (later sources override earlier ones):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. Environment variables
4. **Azure Key Vault** (if enabled)
5. Command-line arguments

This means Key Vault secrets will override values from configuration files, which is the desired behavior for production.

## Testing the Integration

### Verify Key Vault Connection

1. Run the application
2. Check the console output for:
   - Success: `Azure Key Vault configured: https://your-keyvault.vault.azure.net/`
   - Error: `Warning: Failed to configure Azure Key Vault: ...`

### Verify Secret Access

The application will throw an exception at startup if required secrets are missing. Check the error message to identify which secret is not accessible.

## Troubleshooting

### Common Issues

#### 1. "Failed to configure Azure Key Vault"

**Cause**: Authentication failure or incorrect Vault URI

**Solutions**:
- Verify the Vault URI is correct
- Ensure you're authenticated (for local dev: `az login`)
- Check Managed Identity is enabled (for Azure-hosted apps)
- Verify access permissions in Key Vault

#### 2. "Azure OpenAI API key not configured"

**Cause**: Secret not found in Key Vault or incorrect naming

**Solutions**:
- Verify secret exists in Key Vault with correct name: `App--AzureOpenAI--ApiKey`
- Check the `SecretPrefix` matches your secret naming
- Ensure the secret has a value (not empty)

#### 3. "Access denied" or "Forbidden"

**Cause**: Insufficient permissions

**Solutions**:
- Verify Managed Identity has "Key Vault Secrets User" role
- Check Access Policies include "Get" and "List" permissions
- For local dev, ensure you're logged in: `az login`

#### 4. Secrets not updating

**Cause**: Reload interval not configured or too long

**Solutions**:
- Set `KeyVault:ReloadIntervalSeconds` to a reasonable value (e.g., 300 seconds)
- Restart the application to force immediate reload

## Security Best Practices

1. **Never commit secrets**: Keep `appsettings.json` free of sensitive data
2. **Use Managed Identity**: Prefer system-assigned managed identity for Azure-hosted apps
3. **Least Privilege**: Grant only "Get" and "List" permissions, not "Set" or "Delete"
4. **Enable Soft Delete**: Enable soft delete on Key Vault for recovery
5. **Enable Purge Protection**: Enable purge protection for production Key Vaults
6. **Monitor Access**: Enable Key Vault logging and monitor access patterns
7. **Rotate Secrets**: Regularly rotate secrets and update them in Key Vault

## Environment-Specific Configuration

### Development
- Use Azure CLI authentication OR user secrets
- Key Vault can be disabled for local testing (use user secrets or environment variables)
- Set `KeyVault:Enabled = false` and use `dotnet user-secrets` for local development

#### Using User Secrets for Local Development (Alternative)

Instead of Key Vault, you can use .NET user secrets for local development:

```bash
# Initialize user secrets (run once)
dotnet user-secrets init

# Set Azure OpenAI API Key
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-api-key>"

# Set Azure OpenAI Endpoint
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
```

User secrets are stored in your user profile and are not committed to source control. This is perfect for local development when you don't want to set up Key Vault access.

### Staging/Production
- Always use Key Vault with Managed Identity
- Enable secret reloading for automatic updates
- Use separate Key Vaults per environment

## Additional Resources

- [Azure Key Vault Documentation](https://docs.microsoft.com/azure/key-vault/)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
- [Azure Key Vault Configuration Provider](https://docs.microsoft.com/aspnet/core/security/key-vault-configuration)

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review Azure Key Vault logs in Azure Portal
3. Check application logs for detailed error messages

