using System.Text;

namespace Shelfora.Services;

public static class CsvExportFormatter
{
    public static string EscapeField(string? value)
    {
        value ??= "";
        var trimmed = value.TrimStart();
        var formulaRisk = trimmed.Length > 0 && (trimmed[0] is '=' or '+' or '-' or '@' or '\t' or '\r');
        var safe = formulaRisk ? "'" + value : value;
        return "\"" + safe.Replace("\"", "\"\"") + "\"";
    }

    public static byte[] Encode(string csv) => new UTF8Encoding(true).GetBytes(csv);
}
