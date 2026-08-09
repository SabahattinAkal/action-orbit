using System.Security.Cryptography;

namespace ActionOrbit.App.Services.MiniTools;

internal static class PasswordGenerator
{
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%&*+-_=?:";

    public static string Generate(
        int length,
        bool includeLowercase,
        bool includeUppercase,
        bool includeDigits,
        bool includeSymbols)
    {
        if (length is < 8 or > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Parola uzunluğu 8 ile 128 arasında olmalı.");
        }

        var selectedSets = new List<string>(4);
        if (includeLowercase)
        {
            selectedSets.Add(Lowercase);
        }

        if (includeUppercase)
        {
            selectedSets.Add(Uppercase);
        }

        if (includeDigits)
        {
            selectedSets.Add(Digits);
        }

        if (includeSymbols)
        {
            selectedSets.Add(Symbols);
        }

        if (selectedSets.Count == 0)
        {
            throw new ArgumentException("En az bir karakter grubu seçilmeli.");
        }

        if (length < selectedSets.Count)
        {
            throw new ArgumentException("Parola uzunluğu seçilen karakter grubu sayısından kısa olamaz.");
        }

        var allCharacters = string.Concat(selectedSets);
        var result = new char[length];
        var index = 0;
        foreach (var set in selectedSets)
        {
            result[index++] = RandomCharacter(set);
        }

        while (index < result.Length)
        {
            result[index++] = RandomCharacter(allCharacters);
        }

        for (var current = result.Length - 1; current > 0; current--)
        {
            var swapWith = RandomNumberGenerator.GetInt32(current + 1);
            (result[current], result[swapWith]) = (result[swapWith], result[current]);
        }

        return new string(result);
    }

    private static char RandomCharacter(string characters) =>
        characters[RandomNumberGenerator.GetInt32(characters.Length)];
}
