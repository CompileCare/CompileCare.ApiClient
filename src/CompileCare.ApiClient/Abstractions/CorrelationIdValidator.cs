namespace CompileCare.ApiClient.Abstractions;

internal static class CorrelationIdValidator
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64)
            return false;

        foreach (var character in value)
        {
            if ((character >= 'A' && character <= 'Z') ||
                (character >= 'a' && character <= 'z') ||
                (character >= '0' && character <= '9') ||
                character is '.' or '_' or '-')
                continue;

            return false;
        }

        return true;
    }
}