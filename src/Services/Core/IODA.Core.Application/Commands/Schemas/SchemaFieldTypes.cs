namespace IODA.Core.Application.Commands.Schemas;

public static class SchemaFieldTypes
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "string",
        "formattedtext",
        "richtexteditor",
        "number",
        "boolean",
        "date",
        "datetime",
        "enum",
        "json",
        "list",
        "reference",
        "media",
    };
}
