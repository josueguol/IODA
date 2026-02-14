using System.Text.Json;

namespace IODA.Core.Application.Validators.Schema;

internal static class SchemaValidationHelpers
{
    public static bool TryGetInt(object? obj, out int result)
    {
        result = 0;
        if (obj == null)
            return false;
        if (obj is int i)
        {
            result = i;
            return true;
        }

        if (obj is long l)
        {
            result = (int)l;
            return true;
        }

        if (obj is JsonElement je && je.TryGetInt32(out var j))
        {
            result = j;
            return true;
        }

        if (obj is string str && int.TryParse(str, out var p))
        {
            result = p;
            return true;
        }

        return false;
    }

    public static bool TryGetDecimal(object? obj, out decimal result)
    {
        result = 0;
        if (obj == null)
            return false;
        if (obj is decimal d)
        {
            result = d;
            return true;
        }

        if (obj is double db)
        {
            result = (decimal)db;
            return true;
        }

        if (obj is int i)
        {
            result = i;
            return true;
        }

        if (obj is long l)
        {
            result = l;
            return true;
        }

        if (obj is JsonElement je && je.TryGetDecimal(out var j))
        {
            result = j;
            return true;
        }

        if (obj is string s && decimal.TryParse(s, System.Globalization.NumberStyles.Any, null, out var p))
        {
            result = p;
            return true;
        }

        return false;
    }

    public static List<string> GetAllowedValuesList(object? allowedObj)
    {
        var allowed = new List<string>();
        if (allowedObj is JsonElement arrEl && arrEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in arrEl.EnumerateArray())
                allowed.Add(e.GetString() ?? "");
        }
        else if (allowedObj is IList<object> list)
        {
            foreach (var o in list)
                allowed.Add(o?.ToString() ?? "");
        }

        return allowed;
    }
}
