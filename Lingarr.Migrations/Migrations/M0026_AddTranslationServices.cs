using System.Data;
using System.Text.Json;
using FluentMigrator;

namespace Lingarr.Migrations.Migrations;

[Migration(26)]
public class M0026_AddTranslationServices : Migration
{
    private const string DefaultProvider = "libretranslate";
    private const string ServiceTypeKey = "service_type";

    private static readonly Dictionary<string, string> DisplayNames = new()
    {
        ["libretranslate"] = "LibreTranslate",
        ["openai"] = "OpenAI",
        ["anthropic"] = "Anthropic",
        ["localai"] = "LocalAI / Ollama",
        ["deepl"] = "DeepL",
        ["gemini"] = "Google Gemini",
        ["deepseek"] = "DeepSeek",
        ["mistral"] = "Mistral",
        ["xai"] = "xAI",
        ["google"] = "Google Translate",
        ["bing"] = "Bing Translate",
        ["microsoft"] = "Microsoft Translate",
        ["yandex"] = "Yandex Translate"
    };

    private static readonly (string Provider, string Key, string SeedValue)[] ProviderSettings =
    [
        ("libretranslate", "libretranslate_url", ""),
        ("libretranslate", "libretranslate_api_key", ""),
        ("openai", "openai_model", ""),
        ("openai", "openai_api_key", ""),
        ("openai", "openai_request_template", ""),
        ("anthropic", "anthropic_model", ""),
        ("anthropic", "anthropic_api_key", ""),
        ("anthropic", "anthropic_version", ""),
        ("anthropic", "anthropic_request_template", ""),
        ("localai", "local_ai_model", ""),
        ("localai", "local_ai_endpoint", ""),
        ("localai", "local_ai_api_key", ""),
        ("localai", "local_ai_chat_request_template", ""),
        ("localai", "local_ai_generate_request_template", ""),
        ("localai", "local_ai_structured_output", "true"),
        ("deepl", "deepl_api_key", ""),
        ("gemini", "gemini_model", ""),
        ("gemini", "gemini_api_key", ""),
        ("gemini", "gemini_request_template", ""),
        ("deepseek", "deepseek_model", ""),
        ("deepseek", "deepseek_api_key", ""),
        ("deepseek", "deepseek_request_template", ""),
        ("mistral", "mistral_model", ""),
        ("mistral", "mistral_api_key", ""),
        ("mistral", "mistral_request_template", ""),
        ("xai", "xai_model", ""),
        ("xai", "xai_api_key", ""),
        ("xai", "xai_request_template", "")
    ];

    public override void Up()
    {
        if (!Schema.Table("translation_services").Exists())
        {
            Create.Table("translation_services")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("provider").AsCustom("TEXT").NotNullable()
                .WithColumn("name").AsCustom("TEXT").NotNullable()
                .WithColumn("position").AsInt32().NotNullable()
                .WithColumn("created_at").AsDateTime().NotNullable()
                .WithColumn("updated_at").AsDateTime().NotNullable();
        }

        if (!Schema.Table("translation_service_settings").Exists())
        {
            Create.Table("translation_service_settings")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("translation_service_id").AsInt32()
                    .NotNullable()
                    .ForeignKey("fk_translation_service_settings_service_id", "translation_services", "id")
                    .OnDelete(Rule.Cascade)
                .WithColumn("key").AsString(255).NotNullable()
                .WithColumn("value").AsCustom("TEXT").NotNullable();

            Create.Index("ix_translation_service_settings_translation_service_id_key")
                .OnTable("translation_service_settings")
                .OnColumn("translation_service_id").Ascending()
                .OnColumn("key").Ascending()
                .WithOptions().Unique();
        }

        IfDatabase("mysql").Execute.WithConnection((connection, transaction) =>
            MoveSettingsToTranslationServices(connection, transaction, "`"));
        IfDatabase("sqlite", "postgresql").Execute.WithConnection((connection, transaction) =>
            MoveSettingsToTranslationServices(connection, transaction, "\""));
    }

    public override void Down()
    {
        IfDatabase("mysql").Execute.WithConnection((connection, transaction) =>
            RestoreSettingsFromTranslationServices(connection, transaction, "`"));
        IfDatabase("sqlite", "postgresql").Execute.WithConnection((connection, transaction) =>
            RestoreSettingsFromTranslationServices(connection, transaction, "\""));

        Delete.Table("translation_service_settings");
        Delete.Table("translation_services");
    }

    private static void MoveSettingsToTranslationServices(
        IDbConnection connection,
        IDbTransaction transaction,
        string quote)
    {
        var settings = ReadSettings(connection, transaction, quote);
        var serviceTypeSetting = settings.FirstOrDefault(setting => setting.Key == ServiceTypeKey);
        var existingServices = Convert.ToInt64(ExecuteScalar(
            connection,
            transaction,
            "SELECT COUNT(*) FROM translation_services"));
        if (existingServices > 0)
        {
            if (serviceTypeSetting.Key is null)
            {
                DeleteMovedSettings(connection, transaction, quote);
                return;
            }

            ExecuteNonQuery(connection, transaction, "DELETE FROM translation_service_settings");
            ExecuteNonQuery(connection, transaction, "DELETE FROM translation_services");
        }

        var providers = ParseServiceType(serviceTypeSetting.Value);
        var createdAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        for (var position = 0; position < providers.Count; position++)
        {
            var provider = providers[position];
            var name = DisplayNames.GetValueOrDefault(provider, provider);

            ExecuteNonQuery(
                connection,
                transaction,
                "INSERT INTO translation_services (provider, name, " + Quote("position", quote) +
                ", created_at, updated_at) VALUES (@provider, @name, @position, @createdAt, @updatedAt)",
                ("@provider", provider),
                ("@name", name),
                ("@position", position),
                ("@createdAt", createdAt),
                ("@updatedAt", createdAt));

            var translationServiceId = Convert.ToInt32(ExecuteScalar(
                connection,
                transaction,
                "SELECT id FROM translation_services WHERE " + Quote("position", quote) + " = @position",
                ("@position", position)));

            var builtInKeys = ProviderSettings
                .Where(providerSetting => providerSetting.Provider == provider)
                .Select(providerSetting => providerSetting.Key)
                .ToHashSet();

            foreach (var setting in settings)
            {
                var isBuiltInSetting = builtInKeys.Contains(setting.Key);
                var isPluginSetting = setting.Provider != null &&
                    string.Equals(setting.Provider, provider, StringComparison.OrdinalIgnoreCase);
                if (!isBuiltInSetting && !isPluginSetting)
                {
                    continue;
                }

                ExecuteNonQuery(
                    connection,
                    transaction,
                    "INSERT INTO translation_service_settings (translation_service_id, " + Quote("key", quote) +
                    ", " + Quote("value", quote) + ") VALUES (@translationServiceId, @key, @value)",
                    ("@translationServiceId", translationServiceId),
                    ("@key", setting.Key),
                    ("@value", setting.Value));
            }
        }

        DeleteMovedSettings(connection, transaction, quote);
    }

    private static void DeleteMovedSettings(
        IDbConnection connection,
        IDbTransaction transaction,
        string quote)
    {
        DeleteSetting(connection, transaction, quote, ServiceTypeKey);

        foreach (var providerSetting in ProviderSettings)
        {
            DeleteSetting(connection, transaction, quote, providerSetting.Key);
        }

        ExecuteNonQuery(connection, transaction, "DELETE FROM settings WHERE provider IS NOT NULL");
    }

    private static void RestoreSettingsFromTranslationServices(
        IDbConnection connection,
        IDbTransaction transaction,
        string quote)
    {
        var services = new List<(int Id, string Provider)>();
        using (var command = CreateCommand(
                   connection,
                   transaction,
                   "SELECT id, provider FROM translation_services ORDER BY " + Quote("position", quote) + ", id"))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                services.Add((Convert.ToInt32(reader.GetValue(0)), reader.GetString(1)));
            }
        }

        var instanceSettings = new List<(int TranslationServiceId, string Key, string Value)>();
        using (var command = CreateCommand(
                   connection,
                   transaction,
                   "SELECT translation_service_id, " + Quote("key", quote) + ", " + Quote("value", quote) +
                   " FROM translation_service_settings"))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                instanceSettings.Add((
                    Convert.ToInt32(reader.GetValue(0)),
                    reader.GetString(1),
                    reader.GetString(2)));
            }
        }

        var cascade = services.Select(service => service.Provider).Distinct().ToList();
        if (cascade.Count == 0)
        {
            cascade.Add(DefaultProvider);
        }

        var firstInstanceIds = services
            .GroupBy(service => service.Provider)
            .ToDictionary(group => group.Key, group => group.Min(service => service.Id));
        var existingKeys = ReadSettings(connection, transaction, quote)
            .Select(setting => setting.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        InsertSetting(connection, transaction, quote, existingKeys, ServiceTypeKey,
            JsonSerializer.Serialize(cascade), null);

        foreach (var providerSetting in ProviderSettings)
        {
            var value = providerSetting.SeedValue;
            if (firstInstanceIds.TryGetValue(providerSetting.Provider, out var firstInstanceId))
            {
                var instanceSetting = instanceSettings.FirstOrDefault(setting =>
                    setting.TranslationServiceId == firstInstanceId && setting.Key == providerSetting.Key);
                if (instanceSetting.Key != null)
                {
                    value = instanceSetting.Value;
                }
            }

            InsertSetting(connection, transaction, quote, existingKeys, providerSetting.Key, value, null);
        }

        var builtInProviders = ProviderSettings
            .Select(providerSetting => providerSetting.Provider)
            .ToHashSet();
        foreach (var (provider, firstInstanceId) in firstInstanceIds)
        {
            if (builtInProviders.Contains(provider))
            {
                continue;
            }

            foreach (var instanceSetting in instanceSettings.Where(setting =>
                         setting.TranslationServiceId == firstInstanceId))
            {
                InsertSetting(connection, transaction, quote, existingKeys, instanceSetting.Key,
                    instanceSetting.Value, provider);
            }
        }
    }

    private static List<string> ParseServiceType(string? value)
    {
        var entries = new List<string>();
        var trimmed = value?.Trim() ?? "";

        if (trimmed.StartsWith('['))
        {
            try
            {
                entries = JsonSerializer.Deserialize<List<string?>>(trimmed)?
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry!)
                    .ToList() ?? [];
            }
            catch (JsonException)
            {
                entries = [];
            }
        }
        else if (trimmed.Length > 0)
        {
            entries = [trimmed];
        }

        var providers = entries
            .Select(entry => entry.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (providers.Count == 0)
        {
            providers.Add(DefaultProvider);
        }

        return providers;
    }

    private static List<(string Key, string Value, string? Provider)> ReadSettings(
        IDbConnection connection,
        IDbTransaction transaction,
        string quote)
    {
        var settings = new List<(string Key, string Value, string? Provider)>();
        using var command = CreateCommand(
            connection,
            transaction,
            "SELECT " + Quote("key", quote) + ", " + Quote("value", quote) + ", provider FROM settings");
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var provider = reader.IsDBNull(2) ? null : reader.GetString(2);
            settings.Add((reader.GetString(0), reader.GetString(1), provider));
        }

        return settings;
    }

    private static void InsertSetting(
        IDbConnection connection,
        IDbTransaction transaction,
        string quote,
        HashSet<string> existingKeys,
        string key,
        string value,
        string? provider)
    {
        if (!existingKeys.Add(key))
        {
            return;
        }

        ExecuteNonQuery(
            connection,
            transaction,
            "INSERT INTO settings (" + Quote("key", quote) + ", " + Quote("value", quote) +
            ", provider) VALUES (@key, @value, @provider)",
            ("@key", key),
            ("@value", value),
            ("@provider", provider));
    }

    private static void DeleteSetting(
        IDbConnection connection,
        IDbTransaction transaction,
        string quote,
        string key)
    {
        ExecuteNonQuery(
            connection,
            transaction,
            "DELETE FROM settings WHERE " + Quote("key", quote) + " = @key",
            ("@key", key));
    }

    private static string Quote(string identifier, string quote)
    {
        return quote + identifier + quote;
    }

    private static IDbCommand CreateCommand(
        IDbConnection connection,
        IDbTransaction transaction,
        string commandText,
        params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;

        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    private static void ExecuteNonQuery(
        IDbConnection connection,
        IDbTransaction transaction,
        string commandText,
        params (string Name, object? Value)[] parameters)
    {
        using var command = CreateCommand(connection, transaction, commandText, parameters);
        command.ExecuteNonQuery();
    }

    private static object? ExecuteScalar(
        IDbConnection connection,
        IDbTransaction transaction,
        string commandText,
        params (string Name, object? Value)[] parameters)
    {
        using var command = CreateCommand(connection, transaction, commandText, parameters);
        return command.ExecuteScalar();
    }
}
