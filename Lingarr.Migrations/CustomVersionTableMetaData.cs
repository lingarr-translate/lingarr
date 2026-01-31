using FluentMigrator.Runner.VersionTableInfo;

namespace Lingarr.Migrations;

/// <summary>
/// Custom version table metadata to use snake_case naming convention.
/// </summary>
public class CustomVersionTableMetaData : IVersionTableMetaData
{
    public object? ApplicationContext { get; set; }
    public bool OwnsSchema => true;
    public string SchemaName => string.Empty;
    public string TableName => "version_info";
    public string ColumnName => "version";
    public string UniqueIndexName => "uc_version";
    public string AppliedOnColumnName => "applied_on";
    public string DescriptionColumnName => "description";
    public bool CreateWithPrimaryKey => true;
}