using Lingarr.Contracts.Plugins;
using Lingarr.Core.Configuration;
using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Interfaces.Services.Translation;
using Lingarr.Server.Services.Plugins;
using Microsoft.EntityFrameworkCore;

namespace Lingarr.Server.Services.Translation;

public class TranslationServiceInstanceService : ITranslationServiceInstanceService
{
    private static readonly Dictionary<string, string[]> RequestTemplateKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        { "openai", [SettingKeys.Translation.OpenAi.RequestTemplate] },
        { "anthropic", [SettingKeys.Translation.Anthropic.RequestTemplate] },
        {
            "localai", [
                SettingKeys.Translation.LocalAi.ChatRequestTemplate,
                SettingKeys.Translation.LocalAi.GenerateRequestTemplate
            ]
        },
        { "gemini", [SettingKeys.Translation.Gemini.RequestTemplate] },
        { "deepseek", [SettingKeys.Translation.DeepSeek.RequestTemplate] },
        { "mistral", [SettingKeys.Translation.Mistral.RequestTemplate] },
        { "xai", [SettingKeys.Translation.XAi.RequestTemplate] }
    };

    private static readonly HashSet<string> PromptKeys = new(StringComparer.Ordinal)
    {
        SettingKeys.Translation.AiPrompt,
        SettingKeys.Translation.AiUserPrompt,
        SettingKeys.Translation.ProofreadPrompt,
        SettingKeys.Translation.ProofreadUserPrompt
    };

    private readonly LingarrDbContext _dbContext;
    private readonly IPluginRegistry _pluginRegistry;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<TranslationServiceInstanceService> _logger;

    public TranslationServiceInstanceService(
        LingarrDbContext dbContext,
        IPluginRegistry pluginRegistry,
        IEncryptionService encryptionService,
        ILogger<TranslationServiceInstanceService> logger)
    {
        _dbContext = dbContext;
        _pluginRegistry = pluginRegistry;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<TranslationServiceInstance>> GetInstances()
    {
        return await _dbContext.TranslationServiceInstances
            .OrderBy(instance => instance.Position)
            .ThenBy(instance => instance.Id)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TranslationServiceInstance?> GetInstance(int id)
    {
        return await _dbContext.TranslationServiceInstances.FirstOrDefaultAsync(instance => instance.Id == id);
    }

    /// <inheritdoc />
    public async Task<TranslationServiceInstance> CreateInstance(string provider, string? name)
    {
        var plugin = _pluginRegistry.Find(provider);
        if (plugin is null)
        {
            throw new ArgumentException($"Translation service provider '{provider}' is not registered.", nameof(provider));
        }

        var providerId = plugin.Manifest.Provider.ToLowerInvariant();
        var instances = await GetInstances();

        if (plugin.Manifest.Settings.Count == 0 && instances.Any(instance => instance.Provider == providerId))
        {
            throw new InvalidOperationException(
                $"Translation service provider '{providerId}' has no settings and can only be added once.");
        }

        var baseName = string.IsNullOrWhiteSpace(name)
            ? plugin.Manifest.DisplayName
            : name.Trim();

        var instance = new TranslationServiceInstance
        {
            Provider = providerId,
            Name = GetUniqueName(baseName, instances.Select(existing => existing.Name)),
            Position = instances.Count == 0 ? 0 : instances.Max(existing => existing.Position) + 1
        };

        foreach (var field in plugin.Manifest.Settings)
        {
            if (field.Type == PluginSettingType.Secret)
            {
                continue;
            }

            instance.Settings.Add(new TranslationServiceInstanceSetting
            {
                Key = field.Key,
                Value = field.Default ?? string.Empty
            });
        }

        _dbContext.TranslationServiceInstances.Add(instance);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Added translation service {Name} for provider {Provider}.",
            instance.Name,
            instance.Provider);

        return instance;
    }

    /// <inheritdoc />
    public async Task<TranslationServiceInstance?> RenameInstance(int id, string name)
    {
        var instance = await GetInstance(id);
        if (instance is null)
        {
            return null;
        }

        instance.Name = name.Trim();
        await _dbContext.SaveChangesAsync();
        return instance;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteInstance(int id)
    {
        var instances = await GetInstances();
        var instance = instances.FirstOrDefault(existing => existing.Id == id);
        if (instance is null)
        {
            return false;
        }

        if (instances.Count == 1)
        {
            throw new InvalidOperationException("The last translation service cannot be deleted.");
        }

        var settings = await _dbContext.TranslationServiceInstanceSettings
            .Where(setting => setting.TranslationServiceInstanceId == id)
            .ToListAsync();
        _dbContext.TranslationServiceInstanceSettings.RemoveRange(settings);
        _dbContext.TranslationServiceInstances.Remove(instance);

        var position = 0;
        foreach (var remaining in instances.Where(existing => existing.Id != id))
        {
            remaining.Position = position;
            position++;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted translation service {Name} for provider {Provider}.",
            instance.Name,
            instance.Provider);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<TranslationServiceInstance>?> ReorderInstances(IReadOnlyList<int> ids)
    {
        var instances = await GetInstances();
        var instancesById = instances.ToDictionary(instance => instance.Id);

        if (ids.Count != instances.Count
            || ids.Distinct().Count() != ids.Count
            || ids.Any(id => !instancesById.ContainsKey(id)))
        {
            return null;
        }

        for (var position = 0; position < ids.Count; position++)
        {
            instancesById[ids[position]].Position = position;
        }

        await _dbContext.SaveChangesAsync();
        return await GetInstances();
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>?> GetInstanceSettings(int id)
    {
        var instance = await GetInstance(id);
        if (instance is null)
        {
            return null;
        }

        var secretKeys = GetSecretKeys(instance.Provider);
        var settings = await _dbContext.TranslationServiceInstanceSettings
            .Where(setting => setting.TranslationServiceInstanceId == id)
            .ToListAsync();

        var result = new Dictionary<string, string>();
        foreach (var setting in settings)
        {
            if (secretKeys.Contains(setting.Key))
            {
                result[setting.Key] = _encryptionService.Decrypt(setting.Value);
            }
            else
            {
                result[setting.Key] = setting.Value;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> SetInstanceSetting(int id, string key, string value)
    {
        var instance = await GetInstance(id);
        if (instance is null)
        {
            return false;
        }

        if (!GetInstanceSettingKeys(instance.Provider).Contains(key) && !IsPromptKey(instance.Provider, key))
        {
            return false;
        }

        var storedValue = GetSecretKeys(instance.Provider).Contains(key)
            ? _encryptionService.Encrypt(value)
            : value;

        var setting = await _dbContext.TranslationServiceInstanceSettings
            .FirstOrDefaultAsync(existing => existing.TranslationServiceInstanceId == id && existing.Key == key);
        if (setting is null)
        {
            _dbContext.TranslationServiceInstanceSettings.Add(new TranslationServiceInstanceSetting
            {
                TranslationServiceInstanceId = id,
                Key = key,
                Value = storedValue
            });
        }
        else
        {
            setting.Value = storedValue;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteInstancePrompt(int id, string key)
    {
        var instance = await GetInstance(id);
        if (instance is null || !IsPromptKey(instance.Provider, key))
        {
            return false;
        }

        var setting = await _dbContext.TranslationServiceInstanceSettings
            .FirstOrDefaultAsync(existing => existing.TranslationServiceInstanceId == id && existing.Key == key);
        if (setting is not null)
        {
            _dbContext.TranslationServiceInstanceSettings.Remove(setting);
            await _dbContext.SaveChangesAsync();
        }

        return true;
    }

    /// <inheritdoc />
    public async Task EncryptPlaintextSecrets()
    {
        var instances = await _dbContext.TranslationServiceInstances
            .Include(instance => instance.Settings)
            .ToListAsync();

        var encryptedCount = 0;
        foreach (var instance in instances)
        {
            var secretKeys = GetSecretKeys(instance.Provider);
            foreach (var setting in instance.Settings)
            {
                if (!secretKeys.Contains(setting.Key)
                    || string.IsNullOrEmpty(setting.Value)
                    || _encryptionService.IsEncrypted(setting.Value))
                {
                    continue;
                }

                setting.Value = _encryptionService.Encrypt(setting.Value);
                encryptedCount++;
                _logger.LogInformation(
                    "Encrypted plaintext setting {SettingKey} of translation service {Name}.",
                    setting.Key,
                    instance.Name);
            }
        }

        if (encryptedCount > 0)
        {
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetInstanceSettingKeys(string provider)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        var plugin = _pluginRegistry.Find(provider);
        if (plugin is not null)
        {
            foreach (var field in plugin.Manifest.Settings)
            {
                keys.Add(field.Key);
            }
        }

        if (RequestTemplateKeys.TryGetValue(provider, out var templateKeys))
        {
            foreach (var templateKey in templateKeys)
            {
                keys.Add(templateKey);
            }
        }

        return keys;
    }

    private bool IsPromptKey(string provider, string key)
    {
        var plugin = _pluginRegistry.Find(provider);
        return plugin is not null && plugin.Manifest.HasRequestTemplate && PromptKeys.Contains(key);
    }

    private HashSet<string> GetSecretKeys(string provider)
    {
        var plugin = _pluginRegistry.Find(provider);
        if (plugin is null)
        {
            return [];
        }

        return plugin.Manifest.Settings
            .Where(field => field.Type == PluginSettingType.Secret)
            .Select(field => field.Key)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string GetUniqueName(string baseName, IEnumerable<string> usedNames)
    {
        var used = usedNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(baseName))
        {
            return baseName;
        }

        var suffix = 2;
        while (used.Contains($"{baseName} {suffix}"))
        {
            suffix++;
        }

        return $"{baseName} {suffix}";
    }
}
