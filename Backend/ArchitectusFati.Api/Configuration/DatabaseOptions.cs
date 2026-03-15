namespace ArchitectusFati.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool ApplySchemaOnStartup { get; set; } = true;

    public string SchemaScriptPath { get; set; } = "Database/001_init.sql";
}
