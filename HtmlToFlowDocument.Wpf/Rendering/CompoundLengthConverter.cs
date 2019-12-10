// // Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HtmlToFlowDocument.Rendering
{
  /// <summary>
  /// Converts a relative size in percent to an absolute size. The relative size is given in the converter parameter,
  /// the reference size is the property bound.
  /// </summary>
  /// <seealso cref="System.Windows.Data.IValueConverter" />
  public class CompoundLengthConverter : IMultiValueConverter
  {
    /// <summary>
    /// Gets an instance of <see cref="CompoundLengthConverter"/>.
    /// </summary>
    /// <value>
    /// The instance.
    /// </value>
    public static CompoundLengthConverter Instance { get; private set; } = new CompoundLengthConverter();

    /// <summary>
    /// Converts a reference length and a percentage value to an absolute length.
    /// </summary>
    /// <param name="values">An array of two values: the column width and the height of the document. This value is produced by a multibinding.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">Enumerable, consisting of tuples. The first member of each tuple is the size type, the second is the value.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// The absolute length is pixels.
    /// </returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      // it is supposed that the values are two doubles, giving the column width and the height of the viewport
      double viewPortWidth = (1920 / 2); // assume a typical page width of 1920/2;
      if (values != null && values.Length > 0 && values[0] is double w && w > 0)
        viewPortWidth = w;
      double viewPortHeight = 1080 / 2;
      if (values != null && values.Length > 1 && values[1] is double h && h > 0)
        viewPortHeight = h;

      // parameter is assumed to be an enumerable of tuples consisting of an integer and a double
      if (parameter is IEnumerable<(int type, double value)> seq)
      {
        double result = 0;
        foreach (var entry in seq)
        {
          switch (entry.type)
          {
            case 0: // absolute value in px
              result += entry.value;
              break;
            case 1: // 100red of viewport width
              result += entry.value * viewPortWidth / 100;
              break;
            case 2: // 100red of viewport height
              result += entry.value * viewPortHeight / 100;
              break;
            case 3: // Vmin
              result += entry.value * Math.Min(viewPortWidth, viewPortHeight) / 100;
              break;
            case 4: // VMax
              result += entry.value * Math.Max(viewPortWidth, viewPortHeight) / 100;
              break;
          }
        }
        return result;
      }

      return 16; // fallback value
    }


    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

}
