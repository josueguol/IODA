namespace IODA.Core.Application.Schemas;

public static class FieldTypeCanonicalizer
{
    public static string Canonicalize(string? fieldType)
    {
        var normalized = (fieldType ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "blocknote_markdown" => "richtexteditor",
            "markdown" => "richtexteditor",
            "richtext" => "richtexteditor",
            _ => normalized
        };
    }
}
