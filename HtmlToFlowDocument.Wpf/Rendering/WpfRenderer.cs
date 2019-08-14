using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlToFlowDocument.Dom;
using swd = System.Windows.Documents;

namespace HtmlToFlowDocument.Rendering
{
  public class WpfRenderer
  {
    /// <summary>
    /// Gets or sets a value indicating whether the renderer shold invert the colors of the text and the background.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the renderer should invert colors; otherwise, <c>false</c>.
    /// </value>
    public bool InvertColors { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether during the conversion process the DOM elements are attached as tags to the UI elements.
    /// This will of course increase the memory footprint, because the DOM elements then could not be reclaimed by the garbage collector.
    /// </summary>
    /// <value>
    ///   <c>true</c> if DOM elements should be attached as tags to the UI elements; otherwise, <c>false</c>.
    /// </value>
    public bool AttachDomAsTags { get; set; }


    /// <summary>
    /// Renders the specified DOM flow document into a Wpf flow document.
    /// </summary>
    /// <param name="flowDocument">The DOM flow document to render.</param>
    /// <returns></returns>
    public swd.FlowDocument Render(FlowDocument flowDocument)
    {
      return (swd.FlowDocument)RenderRecursively(flowDocument);
    }

    /// <summary>
    /// Renders the specified DOM section into a Wpf section
    /// </summary>
    /// <param name="section">The DOM section to render.</param>
    /// <returns></returns>
    public swd.Section Render(Section section)
    {
      return (swd.Section)RenderRecursively(section);
    }

    /// <summary>
    /// Renders DOM text elements recursively, i.e. including their childs.
    /// </summary>
    /// <param name="e">The root DOM text element.</param>
    /// <returns>The corresponding Wpf element.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// </exception>
    /// <exception cref="System.NotImplementedException"></exception>
    public object RenderRecursively(TextElement e)
    {
      object wpf = null;

      switch (e)
      {
        case BlockUIContainer buc:
          {
            wpf = new swd.BlockUIContainer();
          }
          break;
        case FlowDocument flowDocument:
          {
            var flowDocumente = new swd.FlowDocument();
            if (InvertColors)
            {
              flowDocumente.Background = System.Windows.Media.Brushes.Black;
              flowDocumente.Foreground = System.Windows.Media.Brushes.White;
            }
            wpf = flowDocumente;
          }
          break;
        case Hyperlink hl:
          {
            var hle = new swd.Hyperlink();
            if (!string.IsNullOrEmpty(hl.NavigateUri))
            {
              if (System.Uri.TryCreate(hl.NavigateUri, UriKind.RelativeOrAbsolute, out var uri))
                hle.NavigateUri = uri;
            }
            if (!string.IsNullOrEmpty(hl.TargetName))
            {
              hle.TargetName = hl.TargetName;
            }
            wpf = hle;
          }
          break;
        case Image image:
          {
            var imagee = new System.Windows.Controls.Image();
            if (!string.IsNullOrEmpty(image.Source))
            {
              imagee.SetBinding(System.Windows.Controls.Image.SourceProperty, $"ImageProvider[{image.Source}]");
            }
            wpf = imagee;
          }
          break;
        case InlineUIContainer iuc:
          {
            wpf = new swd.InlineUIContainer();
          }
          break;
        case LineBreak lb:
          {
            wpf = new swd.LineBreak();
          }
          break;
        case List list:
          {
            var liste = new swd.List();
            if (list.MarkerStyle.HasValue)
            {
              liste.MarkerStyle = ToMarkerStyle(list.MarkerStyle.Value);
            }
            wpf = liste;
          }
          break;
        case ListItem li:
          {
            wpf = new swd.ListItem();
          }
          break;
        case Paragraph p:
          {
            var pe = new swd.Paragraph();
            if (p.TextDecorations.HasValue)
            {
              pe.TextDecorations = ToTextDecorations(p.TextDecorations.Value);
            }
            if (p.TextIndent.HasValue)
            {
              pe.TextIndent = p.TextIndent.Value;
            }
            wpf = pe;
          }
          break;
        case Run run:
          {
            var rune = new swd.Run { Text = run.Text };
            wpf = rune;
          }
          break;
        case Section s:
          {
            wpf = new swd.Section();
          }
          break;
        case Span span:
          {
            wpf = new swd.Span();
          }
          break;
        case Table tb:
          {
            var tbe = new swd.Table();
            foreach (var c in tb.Columns)
            {
              if (c.Width.HasValue)
                tbe.Columns.Add(new swd.TableColumn() { Width = new System.Windows.GridLength(c.Width.Value) });
              else
                tbe.Columns.Add(new swd.TableColumn());
            }
            wpf = tbe;
          }
          break;
        case TableCell tc:
          {
            var tce = new swd.TableCell();
            if (1 != tc.ColumnSpan)
            {
              tce.ColumnSpan = tc.ColumnSpan;
            }

            if (1 != tc.RowSpan)
            {
              tce.RowSpan = tc.RowSpan;
            }
            if (tc.BorderBrush.HasValue)
            {
              tce.BorderBrush = new System.Windows.Media.SolidColorBrush(ToColor(tc.BorderBrush.Value));
            }
            if (tc.BorderThickness.HasValue)
            {
              tce.BorderThickness = ToThickness(tc.BorderThickness.Value);
            }
            wpf = tce;
          }
          break;
        case TableRow trow:
          {
            wpf = new swd.TableRow();
          }
          break;
        case TableRowGroup trg:
          {
            wpf = new swd.TableRowGroup();
          }
          break;
        default:
          {
            wpf = null;
          }
          break;
      }

      // Render TextElement properties

      if (wpf is swd.TextElement te)
      {

        if (!string.IsNullOrEmpty(e.FontFamily))
        {
          te.FontFamily = new System.Windows.Media.FontFamily(e.FontFamily);
        }

        if (e.FontSize.HasValue)
        {
          te.FontSize = e.FontSize.Value;
        }

        if (e.FontStyle.HasValue)
        {
          te.FontStyle = ToFontStyle(e.FontStyle.Value);
        }

        if (e.FontWeight.HasValue)
        {
          te.FontWeight = ToFontWeight(e.FontWeight.Value);
        }

        if (e.Foreground.HasValue)
        {
          te.Foreground = new System.Windows.Media.SolidColorBrush(ToColor(e.Foreground.Value));
        }

        if (e.Background.HasValue)
        {
          te.Background = new System.Windows.Media.SolidColorBrush(ToColor(e.Background.Value));
        }
      }

      // now special properties

      if (e is Block b && wpf is swd.Block be)
      {
        if (b.Margin.HasValue)
        {
          be.Margin = ToThickness(b.Margin.Value);
        }

        if (b.Padding.HasValue)
        {
          be.Padding = ToThickness(b.Padding.Value);
        }

        if (b.BorderBrush.HasValue)
        {
          be.BorderBrush = new System.Windows.Media.SolidColorBrush(ToColor(b.BorderBrush.Value));
        }

        if (b.BorderThickness.HasValue)
        {
          be.BorderThickness = ToThickness(b.BorderThickness.Value);
        }

        if (b.TextAlignment.HasValue)
        {
          be.TextAlignment = ToTextAlignment(b.TextAlignment.Value);
        }
      }
      //  finished rendering the attributes


      // now, render all children
      foreach (var child in e.Childs)
      {
        var childe = RenderRecursively(child);

        switch (wpf)
        {
          case swd.Figure figure:
            figure.Blocks.Add((swd.Block)childe);
            break;
          case swd.Floater floater:
            floater.Blocks.Add((swd.Block)childe);
            break;
          case swd.FlowDocument flowDocument:
            flowDocument.Blocks.Add((swd.Block)childe);
            break;
          case swd.List list:
            list.ListItems.Add((swd.ListItem)childe);
            break;
          case swd.ListItem listItem:
            listItem.Blocks.Add((swd.Block)childe);
            break;
          case swd.Section section:
            section.Blocks.Add((swd.Block)childe);
            break;
          case swd.Table table:
            table.RowGroups.Add((swd.TableRowGroup)childe);
            break;
          case swd.TableCell tableCell:
            tableCell.Blocks.Add((swd.Block)childe);
            break;
          case swd.TableRow tableRow:
            tableRow.Cells.Add((swd.TableCell)childe);
            break;
          case swd.TableRowGroup tableRowGroup:
            tableRowGroup.Rows.Add((swd.TableRow)childe);
            break;

          // now elements that can contain inlines
          case swd.Paragraph paragraph:
            paragraph.Inlines.Add((swd.Inline)childe);
            break;
          case swd.Span span:
            span.Inlines.Add((swd.Inline)childe);
            break;

          // now some specialties
          case swd.InlineUIContainer inlineUIContainer:
            if (inlineUIContainer.Child != null)
              throw new InvalidOperationException($"{nameof(swd.InlineUIContainer)} can not contain more than one child");
            inlineUIContainer.Child = (System.Windows.UIElement)childe;
            break;
          case swd.BlockUIContainer blockUIContainer:
            if (blockUIContainer.Child != null)
              throw new InvalidOperationException($"{nameof(swd.BlockUIContainer)} can not contain more than one child");
            blockUIContainer.Child = (System.Windows.UIElement)childe;
            break;

          default:
            throw new NotImplementedException();
        }
      }

      if (AttachDomAsTags)
      {
        if (wpf is System.Windows.FrameworkContentElement conEle)
          conEle.Tag = e;
        else if (wpf is System.Windows.FrameworkElement uiEle)
          uiEle.Tag = e;
      }

      return wpf;
    }

    #region Conversion helper

    /// <summary>
    /// Converts internal FontStyle enum to WPF fontstyle.
    /// </summary>
    /// <param name="fs">The internal fontstyle value.</param>
    /// <returns>WPF fontstyle.</returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public System.Windows.FontStyle ToFontStyle(FontStyle fs)
    {
      switch (fs)
      {
        case FontStyle.Normal:
          return System.Windows.FontStyles.Normal;
        case FontStyle.Oblique:
          return System.Windows.FontStyles.Oblique;
        case FontStyle.Italic:
          return System.Windows.FontStyles.Italic;
        default:
          throw new NotImplementedException();
      }
    }

    public System.Windows.FontWeight ToFontWeight(FontWeight fw)
    {
      switch (fw)
      {
        case FontWeight.Thin:
          return System.Windows.FontWeights.Thin;
        case FontWeight.ExtraLight:
          return System.Windows.FontWeights.ExtraLight;
        case FontWeight.Normal:
          return System.Windows.FontWeights.Normal;
        case FontWeight.Medium:
          return System.Windows.FontWeights.Medium;
        case FontWeight.DemiBold:
          return System.Windows.FontWeights.DemiBold;

        case FontWeight.Bold:
          return System.Windows.FontWeights.Bold;
        case FontWeight.ExtraBold:
          return System.Windows.FontWeights.ExtraBold;

        case FontWeight.Black:
          return System.Windows.FontWeights.Black;

        case FontWeight.ExtraBlack:
          return System.Windows.FontWeights.ExtraBlack;
        default:
          throw new NotImplementedException();
      }
    }

    public System.Windows.Media.Color ToColor(int color)
    {
      uint c = (uint)color;

      byte a = (byte)(c & 0xFF);
      c >>= 8;
      byte b = (byte)(c & 0xFF);
      c >>= 8;
      byte g = (byte)(c & 0xFF);
      c >>= 8;
      byte r = (byte)(c & 0xFF);

      if (InvertColors)
        return System.Windows.Media.Color.FromArgb(a, (byte)(255 - r), (byte)(255 - g), (byte)(255 - b));
      else
        return System.Windows.Media.Color.FromArgb(a, r, g, b);
    }

    public System.Windows.Thickness ToThickness(Thickness t)
    {
      if (t.Left == t.Right && t.Left == t.Top && t.Left == t.Bottom)
        return new System.Windows.Thickness(t.Left);
      else
        return new System.Windows.Thickness(t.Left, t.Top, t.Right, t.Bottom);
    }

    private System.Windows.TextAlignment ToTextAlignment(TextAlignment value)
    {
      switch (value)
      {
        case TextAlignment.Left:
          return System.Windows.TextAlignment.Left;
        case TextAlignment.Right:
          return System.Windows.TextAlignment.Right;
        case TextAlignment.Center:
          return System.Windows.TextAlignment.Center;
        case TextAlignment.Justify:
          return System.Windows.TextAlignment.Justify;
        default:
          throw new NotImplementedException();
      }
    }

    private System.Windows.TextDecorationCollection ToTextDecorations(Dom.TextDecorations value)
    {
      switch (value)
      {
        case TextDecorations.None:
          return null;
        case TextDecorations.Underline:
          return System.Windows.TextDecorations.Underline;
        default:
          throw new NotImplementedException();
      }
    }

    private System.Windows.TextMarkerStyle ToMarkerStyle(ListMarkerStyle value)
    {
      switch (value)
      {
        case ListMarkerStyle.None:
          return System.Windows.TextMarkerStyle.None;
        case ListMarkerStyle.Square:
          return System.Windows.TextMarkerStyle.Square;
        case ListMarkerStyle.Box:
          return System.Windows.TextMarkerStyle.Box;
        case ListMarkerStyle.LowerLatin:
          return System.Windows.TextMarkerStyle.LowerLatin;
        case ListMarkerStyle.UpperLatin:
          return System.Windows.TextMarkerStyle.UpperLatin;
        case ListMarkerStyle.LowerRoman:
          return System.Windows.TextMarkerStyle.LowerRoman;
        case ListMarkerStyle.UpperRoman:
          return System.Windows.TextMarkerStyle.UpperRoman;
        case ListMarkerStyle.Decimal:
          return System.Windows.TextMarkerStyle.Decimal;
        case ListMarkerStyle.Disc:
          return System.Windows.TextMarkerStyle.Disc;
        case ListMarkerStyle.Circle:
          return System.Windows.TextMarkerStyle.Circle;
        default:
          throw new NotImplementedException();
      }
    }

    #endregion
  }
}
