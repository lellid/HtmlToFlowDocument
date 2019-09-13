using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace HtmlToFlowDocument.Rendering
{
  /// <summary>
  /// Helper class for inverting colors of an existing <see cref="System.Windows.Documents.FlowDocument"/>.
  /// </summary>
  public class FlowDocumentColorInverter
  {
    /// <summary>
    /// Inverts the colors of a FlowDocument recursively from leafes to root.
    /// </summary>
    /// <param name="parentElement">The parent element. Use a FlowDocument instance to invert the colors of the entire FlowDocument.</param>
    public void InvertColorsRecursively(FrameworkContentElement parentElement)
    {
      if (null == parentElement)
        return;

      foreach (var child in WpfHelper.GetImmediateChildsOf(parentElement))
        InvertColorsRecursively(child);

      switch (parentElement)
      {
        case TextElement block:
          if (block.ReadLocalValue(TextElement.ForegroundProperty) != DependencyProperty.UnsetValue)
          {
            block.Foreground = GetBrushWithInvertColors(block.Foreground);
          }
          if (block.ReadLocalValue(TextElement.BackgroundProperty) != DependencyProperty.UnsetValue)
          {
            block.Background = GetBrushWithInvertColors(block.Background);
          }
          break;

      }
    }

    /// <summary>
    /// Inverts the provided color.
    /// </summary>
    /// <param name="color">The color to invert.</param>
    /// <returns>The inverted color.</returns>
    public static Color InvertColor(Color color)
    {
      return Color.FromArgb(color.A, (byte)(255 - color.R), (byte)(255 - color.G), (byte)(255 - color.B));
    }

    /// <summary>
    /// Inverts the color(s) of a brush.
    /// </summary>
    /// <param name="brush">The brush.</param>
    /// <returns>A brush with inverted colors.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public Brush GetBrushWithInvertColors(Brush brush)
    {
      if (null == brush)
        return null;

      switch (brush)
      {
        case SolidColorBrush solidColorBrush:
          {
            return GetBrushFromColor(solidColorBrush.Color);
          }
        case LinearGradientBrush linearGradientBrush:
          {
            var result = (LinearGradientBrush)linearGradientBrush.Clone();
            for (int i = 0; i < result.GradientStops.Count; ++i)
              result.GradientStops[i].Color = InvertColor(result.GradientStops[i].Color);
            return result;
          }
        default:
          return brush; // don't know how to invert a TileBrush or an ImageBrush
      }


    }

    Dictionary<Color, Brush> _cachedSolidBrushes = new Dictionary<Color, System.Windows.Media.Brush>();

    /// <summary>
    /// Gets a solid color brush from the color specified.
    /// </summary>
    /// <param name="color">The color in RGBA format (A is the least significant byte).</param>
    /// <returns>A solid color brush.</returns>
    public System.Windows.Media.Brush GetBrushFromColor(Color color)
    {
      if (!_cachedSolidBrushes.TryGetValue(color, out var brush))
      {
        brush = new System.Windows.Media.SolidColorBrush(color);
        brush.Freeze();
        _cachedSolidBrushes.Add(color, brush);
      }
      return brush;
    }
  }
}
