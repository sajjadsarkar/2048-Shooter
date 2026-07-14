using UnityEngine;
using System.Collections.Generic;

public static class NumberFormatter
{
    // Dictionary to store suffix letters for different magnitudes
    private static readonly Dictionary<int, string> suffixes = new Dictionary<int, string>
    {
        { 0, "" },       // 1-9,999
        { 1, "K" },      // 10,000-999,999 
        { 2, "M" },      // Million
        { 3, "B" },      // Billion
        { 4, "T" },      // Trillion
        { 5, "q" },      // Quadrillion
        { 6, "Q" },      // Quintillion
        { 7, "s" },      // Sextillion
        { 8, "S" },      // Septillion
        { 9, "o" },      // Octillion
        { 10, "N" },     // Nonillion
        { 11, "d" },     // Decillion
        { 12, "U" },     // Undecillion
        { 13, "D" },     // Duodecillion
    };

    // Single character suffixes for higher numbers (after reaching duodecillion)
    private static readonly char[] letterSuffixes = "abcdefghijklmnopqrstuvwxyz".ToCharArray();

    // Maximum characters allowed so the value fits the UI size (e.g. "12.3K", "1.2M")
    private const int MaxLength = 5;

    /// <summary>
    /// Format a large number to a shorter, more readable string that fits the UI size.
    /// Uses K/M/B/T/... suffixes for big numbers and trims decimals to stay compact.
    /// </summary>
    /// <param name="number">The number to format</param>
    /// <returns>Formatted string representation of the number</returns>
    public static string FormatNumber(int number)
    {
        // Handle special case for zero
        if (number == 0)
            return "0";

        // For small numbers (up to 9,999), return as is
        if (number < 10000)
            return number.ToString();

        // Calculate the magnitude (how many groups of 3 zeros)
        // For 10K+, we want to start at magnitude 1
        double magnitude = Mathf.Floor(Mathf.Log10(number) / 3);

        // Resolve the suffix for this magnitude (predefined list or generated letter suffix)
        string suffix = ResolveSuffix((int)magnitude);

        // Calculate the scaled value - use System.Math.Pow for double precision
        double scaled = number / System.Math.Pow(1000, magnitude);

        // Try progressively shorter formats so the result fits MaxLength characters.
        // Examples: "12.3K" -> "12K" -> "12K" (when integer)
        string[] formats = { "0.##", "0.#", "0" };
        foreach (string fmt in formats)
        {
            string candidate = scaled.ToString(fmt) + suffix;
            if (candidate.Length <= MaxLength)
                return candidate;
        }

        // Fallback: most compact form
        return scaled.ToString("0") + suffix;
    }

    private static string ResolveSuffix(int magnitude)
    {
        if (magnitude < suffixes.Count)
            return suffixes[magnitude];

        // Generate a double letter suffix for really huge numbers
        int firstLetterIndex = (int)((magnitude - suffixes.Count) / letterSuffixes.Length);
        int secondLetterIndex = (int)((magnitude - suffixes.Count) % letterSuffixes.Length);

        string suffix = "";

        // Handle extremely large numbers by adding more letters
        if (firstLetterIndex >= letterSuffixes.Length)
        {
            int thirdLetterIndex = firstLetterIndex / letterSuffixes.Length - 1;
            firstLetterIndex = firstLetterIndex % letterSuffixes.Length;

            if (thirdLetterIndex < letterSuffixes.Length)
            {
                suffix = letterSuffixes[thirdLetterIndex].ToString();
            }
        }

        suffix += letterSuffixes[firstLetterIndex].ToString() + letterSuffixes[secondLetterIndex].ToString();
        return suffix;
    }
}
