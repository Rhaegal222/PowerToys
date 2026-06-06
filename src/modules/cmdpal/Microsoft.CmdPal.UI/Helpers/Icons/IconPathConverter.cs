// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file is a C# port of the C++ IconPathConverter from Microsoft.Terminal.UI.
// It replaces the dependency on the native Microsoft.Terminal.UI.winmd/dll.
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

#pragma warning disable SA1310 // Field names should not contain underscore

namespace Microsoft.CmdPal.UI.Helpers;

/// <summary>
/// Pure C# implementation of IconPathConverter.IconSourceMUX from Microsoft.Terminal.UI.
/// Converts an icon string (URI, glyph, or emoji) into a WinUI <see cref="IconSource"/>.
/// </summary>
internal static class IconPathConverter
{
    // Fluent UI private-use-area glyph range (Segoe MDL2 Assets / Segoe Fluent Icons)
    private const char FluentIconPuaStart = '\uE700';
    private const char FluentIconPuaEnd = '\uF8FF';

    // Variation selectors
    private const char Vs15 = '\uFE0E'; // text variation selector
    private const char Vs16 = '\uFE0F'; // emoji variation selector

    /// <summary>
    /// Creates a WinUI <see cref="IconSource"/> from a path/glyph/emoji string.
    /// Mirrors the C++ IconPathConverter::IconSourceMUX method.
    /// </summary>
    /// <param name="iconPath">The icon path, glyph character, or emoji.</param>
    /// <param name="fontFamily">Optional explicit font family for font icons.</param>
    /// <param name="targetSize">Target pixel size for decoding/rasterizing.</param>
    public static IconSource? IconSourceMUX(string iconPath, string? fontFamily, int targetSize)
    {
        if (string.IsNullOrEmpty(iconPath))
        {
            return MakeNullBitmapIconSource();
        }

        // Check for .exe / .dll icon extraction (path,index syntax)
        var iconIndex = GetIconIndex(iconPath, out var pathWithoutIndex);
        if (iconIndex.HasValue)
        {
            // For binary icon extraction we return a null-source BitmapIconSource.
            // The WIC/HICON→SoftwareBitmap pipeline is not safely accessible from managed
            // WinUI3 code without additional P/Invoke that would be fragile; the C++
            // implementation itself falls back to a null-source on any failure.
            return MakeNullBitmapIconSource();
        }

        return GetIconSource(iconPath, fontFamily, targetSize);
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------
    private static IconSource GetIconSource(string iconPath, string? fontFamily, int targetSize)
    {
        // Try treating iconPath as a URI (image file, SVG, …)
        var bitmapIcon = TryGetBitmapIconSource(iconPath, targetSize);
        if (bitmapIcon != null)
        {
            return bitmapIcon;
        }

        // Fall back to font icon (glyph / emoji)
        var fontIcon = TryGetFontIconSource(iconPath, fontFamily, targetSize);
        if (fontIcon != null)
        {
            return fontIcon;
        }

        return MakeNullBitmapIconSource();
    }

    private static IconSource? TryGetBitmapIconSource(string path, int targetSize)
    {
        // FontIcon glyphs live in PUA and cannot form a valid URI.
        // ASCII-only first character is a fast-reject for glyphs.
        if (string.IsNullOrEmpty(path) || path[0] >= 128)
        {
            return null;
        }

        if (!Uri.TryCreate(path, UriKind.Absolute, out var iconUri))
        {
            return null;
        }

        try
        {
            var ext = System.IO.Path.GetExtension(iconUri.LocalPath);
            if (string.Equals(ext, ".svg", StringComparison.OrdinalIgnoreCase))
            {
                var svgSource = new SvgImageSource(iconUri);
                if (targetSize > 0)
                {
                    svgSource.RasterizePixelWidth = targetSize;
                }

                var imageIconSource = new ImageIconSource();
                imageIconSource.ImageSource = svgSource;
                return imageIconSource;
            }
            else
            {
                var bitmap = new BitmapImage(iconUri);
                if (targetSize > 0)
                {
                    bitmap.DecodePixelWidth = targetSize;
                }

                var imageIconSource = new ImageIconSource();
                imageIconSource.ImageSource = bitmap;
                return imageIconSource;
            }
        }
        catch
        {
            return null;
        }
    }

    private static IconSource? TryGetFontIconSource(string text, string? explicitFontFamily, int targetSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var kind = ClassifyGlyph(text);

        string family;
        if (kind == GlyphKind.Invalid)
        {
            // Multiple graphemes – treat as plain text with Segoe UI so at least
            // something is rendered rather than crashing.
            family = "Segoe UI";
        }
        else if (!string.IsNullOrEmpty(explicitFontFamily))
        {
            family = explicitFontFamily;
        }
        else if (kind == GlyphKind.FluentSymbol)
        {
            family = "Segoe Fluent Icons, Segoe MDL2 Assets";
        }
        else if (kind == GlyphKind.Emoji)
        {
            family = "Segoe UI Emoji, Segoe UI";
        }
        else
        {
            family = "Segoe UI";
        }

        var glyph = kind == GlyphKind.Invalid ? "\u25CC" : text;

        try
        {
            var fontIconSource = new FontIconSource();
            fontIconSource.FontFamily = new FontFamily(family);
            fontIconSource.FontSize = targetSize > 0 ? targetSize : 8;
            fontIconSource.Glyph = glyph;
            return fontIconSource;
        }
        catch
        {
            return null;
        }
    }

    private static BitmapIconSource MakeNullBitmapIconSource()
    {
        var icon = new BitmapIconSource();
        icon.UriSource = null;
        return icon;
    }

    // -------------------------------------------------------------------------
    // Glyph classification (mirrors FontIconGlyphClassifier::Classify)
    // -------------------------------------------------------------------------
    private enum GlyphKind
    {
        None,
        FluentSymbol,
        Emoji,
        Other,
        Invalid,
    }

    private static GlyphKind ClassifyGlyph(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return GlyphKind.None;
        }

        // Fast path: two ASCII chars → definitely not a single glyph (file path, etc.)
        if (text.Length >= 2 && text[0] <= 0x7F && text[1] <= 0x7F)
        {
            return GlyphKind.Invalid;
        }

        // Single UTF-16 code unit
        if (text.Length == 1)
        {
            var ch = text[0];
            if (char.IsHighSurrogate(ch))
            {
                return GlyphKind.Invalid;
            }

            if (ch >= FluentIconPuaStart && ch <= FluentIconPuaEnd)
            {
                return GlyphKind.FluentSymbol;
            }

            if (IsEmojiPresentation(ch))
            {
                return GlyphKind.Emoji;
            }

            return GlyphKind.Other;
        }

        // Surrogate pair (single emoji SMP codepoint)
        if (text.Length == 2 && char.IsHighSurrogate(text[0]) && char.IsLowSurrogate(text[1]))
        {
            return GlyphKind.Emoji;
        }

        // Check for variation-selector sequences (e.g. "2️⃣" = digit + VS16)
        if (text.Length == 2)
        {
            if (text[1] == Vs16)
            {
                return GlyphKind.Emoji;
            }

            if (text[1] == Vs15)
            {
                return GlyphKind.Other;
            }
        }

        // Anything else with mixed ASCII is invalid (multi-word string etc.)
        foreach (var c in text)
        {
            if (c <= 0x7F)
            {
                return GlyphKind.Invalid;
            }
        }

        // Assume a single multi-byte emoji grapheme
        return GlyphKind.Emoji;
    }

    private static bool IsEmojiPresentation(char ch)
    {
        // Quick subset of characters with default emoji presentation per Unicode 15.
        return ch == 0x00A9 // ©
            || ch == 0x00AE // ®
            || ch == 0x203C // ‼
            || ch == 0x2049 // ⁉
            || ch == 0x2122 // ™
            || (ch >= 0x231A && ch <= 0x231B) // ⌚⌛
            || ch == 0x2328 // ⌨
            || ch == 0x23CF // ⏏
            || (ch >= 0x23E9 && ch <= 0x23F3) // ⏩…⏳
            || (ch >= 0x23F8 && ch <= 0x23FA) // ⏸⏹⏺
            || ch == 0x24C2 // Ⓜ
            || (ch >= 0x25AA && ch <= 0x25AB) // ▪▫
            || ch == 0x25B6 // ▶
            || ch == 0x25C0 // ◀
            || (ch >= 0x25FB && ch <= 0x25FE) // ◻◼◽◾
            || (ch >= 0x2600 && ch <= 0x27FF) // misc symbols, dingbats
            || (ch >= 0x2B05 && ch <= 0x2B07) // ⬅⬆⬇
            || (ch >= 0x2B1B && ch <= 0x2B1C) // ⬛⬜
            || ch == 0x2B50 // ⭐
            || ch == 0x2B55 // ⭕
            || ch == 0x3030 // 〰
            || ch == 0x303D // 〽
            || ch == 0x3297 // ㊗
            || ch == 0x3299; // ㊙
    }

    // -------------------------------------------------------------------------
    // .exe / .dll path detection (mirrors _getIconIndex)
    // -------------------------------------------------------------------------
    private static int? GetIconIndex(string iconPath, out string pathWithoutIndex)
    {
        pathWithoutIndex = iconPath;

        var commaPos = iconPath.IndexOf(',', StringComparison.Ordinal);
        var pathPart = commaPos >= 0 ? iconPath[..commaPos] : iconPath;
        pathWithoutIndex = pathPart;

        var ext = System.IO.Path.GetExtension(pathPart);
        if (!string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(ext, ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (commaPos >= 0)
        {
            var indexStr = iconPath[(commaPos + 1)..];
            if (int.TryParse(indexStr, out var idx))
            {
                return idx;
            }

            return null;
        }

        return 0;
    }
}
