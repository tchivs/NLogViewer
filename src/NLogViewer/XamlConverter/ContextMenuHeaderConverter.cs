using System;
using System.Globalization;
using System.Windows.Data;

namespace DJ.XamlConverter
{
	public class ContextMenuHeaderConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string s)
			{
				return $"add '{s}' to filter";
			}

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
