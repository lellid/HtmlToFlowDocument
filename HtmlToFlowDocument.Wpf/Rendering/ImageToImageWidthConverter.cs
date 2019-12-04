// Copyright (c) Dr. Dirk Lellinger. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace HtmlToFlowDocument.Rendering
{
  /// <summary>
  /// Converts a relative size in percent to an absolute size. The relative size is given in the converter parameter,
  /// the reference size is the property bound.
  /// </summary>
  /// <seealso cref="System.Windows.Data.IValueConverter" />
  public class ImageToImageWidthConverter : IValueConverter
  {
    /// <summary>
    /// Gets an instance of <see cref="ImageToImageWidthConverter"/>.
    /// </summary>
    /// <value>
    /// The instance.
    /// </value>
    public static ImageToImageWidthConverter Instance { get; private set; } = new ImageToImageWidthConverter();

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
      if (value is System.Windows.Media.Imaging.BitmapImage bmpImage)
      {
        if (bmpImage.DpiX <= 16)
        {
          return bmpImage.PixelWidth;
        }
        else
        {
          return bmpImage.Width;
        }
      }
      else if (value is ImageSource imgSource)
      {
        return imgSource.Width;
      }
      else
      {
        return 16;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

}
