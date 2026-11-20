using Lingarr.Core.Entities;

namespace Lingarr.Server.Interfaces.Services.Translation;

/// <summary>
/// Manages the configured translation service instances that make up the translation cascade.
/// </summary>
public interface ITranslationServiceInstanceService
{
    /// <summary>
    /// Retrieves all translation service instances ordered by position, without their settings.
    /// </summary>
    Task<List<TranslationServiceInstance>> GetInstances();

    /// <summary>
    /// Retrieves one translation service instance, or null when it does not exist.
    /// </summary>
    Task<TranslationServiceInstance?> GetInstance(int id);

    /// <summary>
    /// Adds an instance of a provider at the end of the cascade with the defaults of its manifest. Throws an
    /// ArgumentException for an unknown provider and an InvalidOperationException when a provider without settings
    /// is already present.
    /// </summary>
    Task<TranslationServiceInstance> CreateInstance(string provider, string? name);

    /// <summary>
    /// Renames an instance, returns null when it does not exist.
    /// </summary>
    Task<TranslationServiceInstance?> RenameInstance(int id, string name);

    /// <summary>
    /// Deletes an instance with its settings, returns false when it does not exist. Throws an
    /// InvalidOperationException when it is the last instance.
    /// </summary>
    Task<bool> DeleteInstance(int id);

    /// <summary>
    /// Sets the cascade order, returns null when the ids are not exactly the ids of all instances.
    /// </summary>
    Task<List<TranslationServiceInstance>?> ReorderInstances(IReadOnlyList<int> ids);

    /// <summary>
    /// Retrieves the settings of an instance with secret fields decrypted, returns null when it does not exist.
    /// </summary>
    Task<Dictionary<string, string>?> GetInstanceSettings(int id);

    /// <summary>
    /// Stores a setting of an instance and encrypts secret fields, returns false when the instance does not exist
    /// or the key is not allowed for its provider. An AI service instance may also store its own prompts.
    /// </summary>
    Task<bool> SetInstanceSetting(int id, string key, string value);

    /// <summary>
    /// Removes the own prompt of an instance so it uses the global prompt again, returns false when the instance
    /// does not exist or the key is not a prompt of an AI service.
    /// </summary>
    Task<bool> DeleteInstancePrompt(int id, string key);

    /// <summary>
    /// Encrypts secret instance settings that are still stored as plaintext.
    /// </summary>
    Task EncryptPlaintextSecrets();

    /// <summary>
    /// Returns the setting keys an instance of the provider may hold.
    /// </summary>
    IReadOnlySet<string> GetInstanceSettingKeys(string provider);
}
