namespace RTS.Core.Rules
{
    /// <summary>Turns data keys into names for the screen: "raider_boat" -> "Raider Boat". Keys stay the ids in the JSON and the AI schema.</summary>
    public static class DisplayNames
    {
        public static string Of(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var parts = key.Split('_');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0) parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            return string.Join(" ", parts);
        }
    }
}
