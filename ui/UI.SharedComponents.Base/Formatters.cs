using System.Globalization;

namespace UI.SharedComponents.Components
{
	public static class Formatters
	{
		public static string DateTimeShowOnlyTime(DateTime dt) =>
			dt.ToString("HH:mm", CultureInfo.InvariantCulture);
	}
}

