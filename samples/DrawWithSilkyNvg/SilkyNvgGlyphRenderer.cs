// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Silk.NET.Maths;
using SilkyNvg;
using SilkyNvg.Graphics;
using SilkyNvg.Paths;
using SixLabors.Fonts;

namespace DrawWithSilkyNvg;

[SuppressMessage("ReSharper", "ArrangeModifiersOrder", Justification = "StyleCop and ReSharper fight eachother")]
public class SilkyNvgGlyphRenderer : IColorGlyphRenderer
{
    required public Nvg Nvg { get; init; }

    public bool DrawTextBox { get; set; }

    public bool DrawGlyphBox { get; set; }

    /// <inheritdoc />
    public void BeginFigure()
    {
        // Span<byte> col = stackalloc byte[3];
        // Random.Shared.NextBytes(col);
        this.Nvg.BeginPath();
        //this.Nvg.FillColour(new Colour(col[0], col[1], col[2], 255));
    }

    // DEVELOPER NOTE: I could've sworn we added an implicit cast in Silk.NET between System.Numerics vectors and
    // Silk.NET.Maths vectors, perhaps look at adding this in Silk.NET. Also SilkyNvg disregarded the advice of "use the
    // speedy System.Numerics APIs for floats, and only use Silk.NET.Maths for everything else"

    /// <inheritdoc />
    public void MoveTo(Vector2 point)
    {
        // Span<byte> col = stackalloc byte[3];
        // Random.Shared.NextBytes(col);
        // this.Nvg.FillColour(new Colour(col[0], col[1], col[2], 255));
        this.Nvg.MoveTo(new Vector2D<float>(point.X, point.Y));
    }

    /// <inheritdoc />
    public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
        => this.Nvg.QuadTo(
            new Vector2D<float>(secondControlPoint.X, secondControlPoint.Y),
            new Vector2D<float>(point.X, point.Y));

    public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
        => this.Nvg.BezierTo(
            new Vector2D<float>(secondControlPoint.X, secondControlPoint.Y),
            new Vector2D<float>(thirdControlPoint.X, thirdControlPoint.Y),
            new Vector2D<float>(point.X, point.Y));

    /// <inheritdoc />
    public void LineTo(Vector2 point) => this.Nvg.LineTo(new Vector2D<float>(point.X, point.Y));

    /// <inheritdoc />
    public void EndFigure() => this.Nvg.Fill();

    /// <inheritdoc />
    public void EndGlyph() => this.Nvg.Restore();

    /// <inheritdoc />
    public bool BeginGlyph(in FontRectangle bounds, in GlyphRendererParameters parameters)
    {
        this.Nvg.Save();
        return true;
    }

    /// <inheritdoc />
    public void EndText()
    {
        // Everything Is Ticketty-Boo! Which is code for "this function does sweet F.A. in this impl".
    }

    /// <inheritdoc />
    public void BeginText(in FontRectangle bounds)
    {
        // Everything Is Ticketty-Boo! Which is code for "this function does sweet F.A. in this impl".
    }

    /// <inheritdoc />
    public TextDecorations EnabledDecorations() => TextDecorations.None;

    /// <inheritdoc />
    public void SetDecoration(TextDecorations textDecorations, Vector2 start, Vector2 end, float thickness)
        => Debug.WriteLine($"attempted to decorate text: {textDecorations} / {start} / {end} / {thickness}");

    /// <inheritdoc />
    public void SetColor(GlyphColor color) => this.Nvg.FillColour(new Colour(color.Red, color.Green, color.Blue, color.Alpha));
}
