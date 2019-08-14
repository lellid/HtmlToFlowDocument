using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HtmlToFlowDocument.Dom;

namespace HtmlToFlowDocument.Rendering
{
  /// <summary>
  /// Used to render the Dom tree into plain text.
  /// </summary>
  public class PlainTextRenderer
  {
    public void Render(TextElement e, TextWriter wr)
    {
      // Render TextElement properties

      // The following conventions apply for rendering elements

      // - a paragraph adds a linefeed at the end of the paragraph
      // - a section adds a linefeed at the end of the section
      // - listitems will not add additional characters
      // - a linebreak will add a linefeed


      // --------------  render pre-contents ------------------------------


      // --------------  render the contents ------------------------------

      if (e is Run r)
      {
        wr.Write(r.Text);
      }
      else if (e is Table tab)
      {
        foreach (var child in e.Childs)
        {
          Render(child, wr);
        }
      }
      else
      {
        // now, render all children
        foreach (var child in e.Childs)
        {
          Render(child, wr);
        }
      }

      // --------------  render post-contents ------------------------------

      switch (e)
      {
        case Paragraph p:
          wr.WriteLine();
          break;
        case Section s:
          wr.WriteLine();
          break;
        case LineBreak l:
          wr.WriteLine();
          break;
      }

    }



  }

}
