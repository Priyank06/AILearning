# Key Vault Secrets Reference

This document provides a quick reference for all secrets that should be stored in Azure Key Vault.

## Secret Naming Convention

All secrets use the prefix `App--` (configurable) and follow this pattern:
- Configuration path: `Section:Subsection:Key`
- Key Vault secret name: `App--Section--Subsection--Key`

## Required Secrets

### Azure OpenAI Configuration

| Secret Name in Key Vault | Configuration Path | Type | Description |
|-------------------------|-------------------|------|-------------|
| `App--AzureOpenAI--ApiKey` | `AzureOpenAI:ApiKey` | String | Azure OpenAI API key |
| `App--AzureOpenAI--Endpoint` | `AzureOpenAI:Endpoint` | String | Azure OpenAI endpoint URL (e.g., `https://your-resource.openai.azure.com/`) |

## Optional Secrets

You can store any configuration value in Key Vault. Common examples:

| Secret Name in Key Vault | Configuration Path | Type | Description |
|-------------------------|-------------------|------|-------------|
| `App--ConnectionStrings--DefaultConnection` | `ConnectionStrings:DefaultConnection` | String | Database connection string |
| `App--ExternalApi--ApiKey` | `ExternalApi:ApiKey` | String | External API key |
| `App--Jwt--SecretKey` | `Jwt:SecretKey` | String | JWT signing key |

## Adding Secrets via Azure Portal

1. Navigate to your Key Vault in Azure Portal
2. Go to **Secrets** → **Generate/Import**
3. Enter the secret name (e.g., `App--AzureOpenAI--ApiKey`)
4. Enter the secret value
5. Click **Create**

## Adding Secrets via Azure CLI

```bash
# Set Azure OpenAI API Key
az keyvault secret set \
  --vault-name <your-keyvault-name> \
  --name "App--AzureOpenAI--ApiKey" \
  --value "<your-api-key>"

# Set Azure OpenAI Endpoint
az keyvault secret set \
  --vault-name <your-keyvault-name> \
  --name "App--AzureOpenAI--Endpoint" \
  --value "https://your-resource.openai.azure.com/"
```

## Adding Secrets via PowerShell

```powershell
# Set Azure OpenAI API Key
$secret = ConvertTo-SecureString -String "<your-api-key>" -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName "<your-keyvault-name>" -Name "App--AzureOpenAI--ApiKey" -SecretValue $secret

# Set Azure OpenAI Endpoint
$secret = ConvertTo-SecureString -String "https://your-resource.openai.azure.com/" -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName "<your-keyvault-name>" -Name "App--AzureOpenAI--Endpoint" -SecretValue $secret
```

## Verifying Secrets

### Via Azure Portal
1. Navigate to **Secrets** in your Key Vault
2. Click on a secret to view its details
3. Click **Show Secret Value** to verify the value

### Via Azure CLI
```bash
az keyvault secret show \
  --vault-name <your-keyvault-name> \
  --name "App--AzureOpenAI--ApiKey" \
  --query value -o tsv
```

## Secret Rotation

To rotate a secret:
1. Update the secret value in Key Vault
2. If `ReloadIntervalSeconds` is configured, the application will automatically pick up the new value
3. Otherwise, restart the application

## Notes

- Secret names are case-insensitive in Key Vault
- Secret values can be up to 25KB in size
- Use versioning to track secret changes
- Enable soft delete and purge protection for production Key Vaults

