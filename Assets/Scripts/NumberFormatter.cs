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

    /// <summary>
    /// Format a large number to a shorter, more readable string
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

        // If within our predefined suffixes, use them
        if (magnitude < suffixes.Count)
        {
            // Get the suffix for this magnitude
            string suffix = suffixes[(int)magnitude];

            // Calculate the scaled value (e.g., 12345 becomes 12.345 for K)
            double scaled = number / System.Math.Pow(1000, magnitude);

            // Format with 1 decimal place if not a whole number, otherwise as integer
            string formatted = scaled.ToString("0.#");

            return formatted + suffix;
        }
        else
        {
            // Generate a double letter suffix for really huge numbers
            // For numbers beyond our predefined suffixes
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

            // Calculate the scaled value - use System.Math.Pow for double precision
            double scaled = number / System.Math.Pow(1000, magnitude);

            // Format with 1 decimal place if not a whole number
            string formatted = scaled.ToString("0.#");

            return formatted + suffix;
        }
    }
}
