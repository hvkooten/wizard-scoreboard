using SkiaSharp;
using System.Globalization;
using System.Text;

// Args: <fontPath> <iconFgOutSvg> <splashOutSvg> <iconBgOutSvg> <logoOutSvg>
var fontPath = args.Length > 0 ? args[0] : "../../src/scoreboard/Resources/Fonts/MedievalSharp.ttf";
var iconOut = args.Length > 1 ? args[1] : "../../src/scoreboard/Resources/AppIcon/appiconfg.svg";
var splashOut = args.Length > 2 ? args[2] : "../../src/scoreboard/Resources/Splash/splash.svg";
var iconBgOut = args.Length > 3 ? args[3] : "../../src/scoreboard/Resources/AppIcon/appicon.svg";
var logoOut = args.Length > 4 ? args[4] : "../../src/scoreboard/Resources/Images/logo.svg";

const float textSize = 256f;

using var typeface = SKTypeface.FromFile(fontPath)
    ?? throw new FileNotFoundException($"Font not found: {fontPath}");
using var font = new SKFont(typeface, textSize);

string Num(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

// Builds a normalised (origin at 0,0) SVG path plus its bounds for the given text.
(string PathData, float Width, float Height) BuildGlyphPath(string text)
{
    var glyphs = font.GetGlyphs(text);
    var widths = font.GetGlyphWidths(glyphs);

    using var full = new SKPath();
    float x = 0;
    for (var i = 0; i < glyphs.Length; i++)
    {
        using var gp = font.GetGlyphPath(glyphs[i]);
        if (gp is not null && !gp.IsEmpty)
        {
            using var moved = new SKPath(gp);
            moved.Transform(SKMatrix.CreateTranslation(x, 0));
            full.AddPath(moved);
        }
        x += widths[i];
    }

    var b = full.TightBounds;
    using var normalised = new SKPath(full);
    normalised.Transform(SKMatrix.CreateTranslation(-b.Left, -b.Top));
    return (normalised.ToSvgPathData(), b.Width, b.Height);
}

var (pathData, glyphW, glyphH) = BuildGlyphPath("Wizard");

// Builds an SVG where the given glyph path is filled with a lava gradient and given an
// amber glow (a wide soft stroke behind a thinner bright stroke), evoking the box-art vibe.
string BuildSvg(string glyphPath, float pathW, float pathH, float padX, float padY, bool withStoneBackdrop)
{
    var w = pathW + padX * 2;
    var h = pathH + padY * 2;
    var sb = new StringBuilder();
    sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Num(w)}\" height=\"{Num(h)}\" viewBox=\"0 0 {Num(w)} {Num(h)}\">\n");
    sb.Append("  <defs>\n");
    sb.Append("    <linearGradient id=\"lava\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">\n");
    sb.Append("      <stop offset=\"0\" stop-color=\"#FFD27A\"/>\n");
    sb.Append("      <stop offset=\"0.35\" stop-color=\"#FF7A18\"/>\n");
    sb.Append("      <stop offset=\"0.7\" stop-color=\"#C42A0C\"/>\n");
    sb.Append("      <stop offset=\"1\" stop-color=\"#7A1608\"/>\n");
    sb.Append("    </linearGradient>\n");
    sb.Append("  </defs>\n");

    if (withStoneBackdrop)
    {
        sb.Append(CultureInfo.InvariantCulture, $"  <rect x=\"0\" y=\"0\" width=\"{Num(w)}\" height=\"{Num(h)}\" fill=\"#10161d\"/>\n");
    }

    sb.Append(CultureInfo.InvariantCulture, $"  <g transform=\"translate({Num(padX)},{Num(padY)})\">\n");
    // Outer amber glow.
    sb.Append(CultureInfo.InvariantCulture, $"    <path d=\"{glyphPath}\" fill=\"none\" stroke=\"#FF7A18\" stroke-width=\"14\" stroke-linejoin=\"round\" stroke-opacity=\"0.55\"/>\n");
    // Dark outline for definition.
    sb.Append(CultureInfo.InvariantCulture, $"    <path d=\"{glyphPath}\" fill=\"none\" stroke=\"#2A0A03\" stroke-width=\"6\" stroke-linejoin=\"round\"/>\n");
    // Lava-filled glyphs.
    sb.Append(CultureInfo.InvariantCulture, $"    <path d=\"{glyphPath}\" fill=\"url(#lava)\"/>\n");
    sb.Append("  </g>\n");
    sb.Append("</svg>\n");
    return sb.ToString();
}

// Single-letter "W" for the app icon foreground so it stays legible inside the square,
// safe-zone-cropped icon.
var (wPath, wW, wH) = BuildGlyphPath("W");

// App icon foreground: the "W" on a transparent layer (the stone backdrop is the app icon
// background SVG). Square-ish padding keeps the letter centered within the safe zone.
var iconPad = MathF.Max(wW, wH) * 0.35f;
File.WriteAllText(iconOut, BuildSvg(wPath, wW, wH, padX: iconPad + MathF.Max(0, (wH - wW) / 2f), padY: iconPad + MathF.Max(0, (wW - wH) / 2f), withStoneBackdrop: false));

// App icon background: solid dark stone so the fiery "W" pops (replaces the old blue art).
File.WriteAllText(iconBgOut,
    "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 512 512\">\n" +
    "  <rect x=\"16\" y=\"16\" width=\"480\" height=\"480\" rx=\"96\" fill=\"#10161d\"/>\n" +
    "</svg>\n");

// Splash: dark stone backdrop with the full fiery word centered.
File.WriteAllText(splashOut, BuildSvg(pathData, glyphW, glyphH, padX: glyphW * 0.12f, padY: glyphH * 0.6f, withStoneBackdrop: true));

// Standalone logo (transparent) for in-app use such as the About page.
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logoOut))!);
File.WriteAllText(logoOut, BuildSvg(pathData, glyphW, glyphH, padX: glyphW * 0.06f, padY: glyphH * 0.2f, withStoneBackdrop: false));

Console.WriteLine($"Glyph bounds (Wizard): {Num(glyphW)} x {Num(glyphH)}; (W): {Num(wW)} x {Num(wH)}");
Console.WriteLine($"Wrote: {Path.GetFullPath(iconOut)}");
Console.WriteLine($"Wrote: {Path.GetFullPath(iconBgOut)}");
Console.WriteLine($"Wrote: {Path.GetFullPath(splashOut)}");
Console.WriteLine($"Wrote: {Path.GetFullPath(logoOut)}");
