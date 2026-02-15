namespace termix.Helpers;

public static class FuzzyMatcher
{
    public static bool IsMatch(string text, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return true;
        if (pattern.Length > text.Length) return false;

        var textSpan = text.AsSpan();
        var patternSpan = pattern.AsSpan();

        int textIdx = 0;
        int patternIdx = 0;

        while (textIdx < textSpan.Length && patternIdx < patternSpan.Length)
        {
            if (char.ToLowerInvariant(textSpan[textIdx]) == char.ToLowerInvariant(patternSpan[patternIdx]))
            {
                patternIdx++;
            }

            textIdx++;
        }

        return patternIdx == patternSpan.Length;
    }
}