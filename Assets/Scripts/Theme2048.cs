using System;
using UnityEngine;

public enum ThemeRole { Bg, Panel, Column, ScoreBox, TextMain, TextDim, ButtonDark, Overlay, MenuBox }

public static class Theme2048
{
    private const string Key = "2048_Dark";

    public static event Action Changed;

    public static bool IsDark { get; private set; } = PlayerPrefs.GetInt(Key, 1) == 1;

    public static void Toggle()
    {
        IsDark = !IsDark;
        PlayerPrefs.SetInt(Key, IsDark ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static Color Get(ThemeRole role)
    {
        bool d = IsDark;
        switch (role)
        {
            case ThemeRole.Bg: return d ? Hex(0x1C202A) : Hex(0xF6F4E7);
            case ThemeRole.Panel: return d ? Hex(0x272E3B) : Hex(0xFFFFFE);
            case ThemeRole.Column: return d ? Hex(0x2B3440) : Hex(0xF1EFE2);
            case ThemeRole.ScoreBox: return d ? Hex(0x1B202A) : Hex(0xF1EFE2);
            case ThemeRole.TextMain: return d ? Color.white : Hex(0x1C202A);
            case ThemeRole.TextDim: return d ? Hex(0x8A93A3) : Hex(0x8B8B85);
            case ThemeRole.ButtonDark: return Hex(0x414A5B);
            case ThemeRole.Overlay: return new Color(0f, 0f, 0f, d ? 0.75f : 0.6f);
            default: return d ? Hex(0x161A22) : Hex(0x515151);
        }
    }

    private static Color Hex(int h) =>
        new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f);
}
