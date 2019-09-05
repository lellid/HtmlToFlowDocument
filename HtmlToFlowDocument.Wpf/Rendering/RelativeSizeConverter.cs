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
  public class RelativeSizeConverter : IValueConverter
  {
    /// <summary>
    /// Gets an instance of <see cref="RelativeSizeConverter"/>.
    /// </summary>
    /// <value>
    /// The instance.
    /// </value>
    public static RelativeSizeConverter Instance { get; private set; } = new RelativeSizeConverter();

    /// <summary>
    /// Converts a reference length and a percentage value to an absolute length.
    /// </summary>
    /// <param name="value">The reference length. This value is produced by the binding source.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The percentage value. This value is from the converter parameter.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>
    /// The absolute length as referenceLength*100/percentageValue.
    /// If the reference length was not set, a value of (1920/2) is used for the reference length.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      double? w = null;

      if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out var asDouble))
        w = asDouble;
      else if (parameter is double d)
        w = d;
      else if (parameter is Int32 i32)
        w = i32;

      if (!w.HasValue)
        w = 100;

      if (value is double referenceSize)
      {
        return referenceSize * w / 100.0;
      }
      else // if the referenceSize is not set or the binding failed
      {
        // assume a typical page width of 1920/2;
        return (1920 / 2) * w / 100.0;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

}
