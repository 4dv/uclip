namespace uclip
{
    public static class StringHelper
    {
        public static string TrimStart(string stringToRemove, string text)
        {
            if (text.StartsWith(stringToRemove))
                return text.Substring(stringToRemove.Length);
            return text;
        }
    }
}