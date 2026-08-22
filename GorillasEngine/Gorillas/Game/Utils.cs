using Gorillas.Engine;

namespace Gorillas.Game;

internal static class Utils
{
	public static async Task<string> ReadLineInput(QBasic qBasic, int row, int column, string prompt, string defaultValue, int maxLength)
	{
		string result = string.Empty;
		qBasic.LOCATE(row, column);
		qBasic.PRINT(prompt);

		while (true)
		{
			char? key = await qBasic.WAITKEY();
			if (key == '\n')
			{
				return result.Length == 0 ? defaultValue : result;
			}

			if (key == '\b')
			{
				if (result.Length > 0)
				{
					result = result[..^1];
				}
			}
			else if (key >= ' ' && key <= '~' && result.Length < maxLength)
			{
				result += key.Value;
			}

			qBasic.LOCATE(row, column + prompt.Length);
			qBasic.PRINT(result.PadRight(maxLength));
		}
	}

	public static async Task<string> ReadNumericInput(QBasic qBasic, int row, int column, string prompt, int clearLength, bool allowDecimal)
	{
		string result = string.Empty;

		while (true)
		{
			qBasic.LOCATE(row, column);
			qBasic.PRINT(prompt);
			qBasic.LOCATE(row, column + prompt.Length);
			qBasic.PRINT(result.PadRight(clearLength));

			char? key = await qBasic.WAITKEY();
			if (key == '\n')
			{
				return result;
			}

			if (key == '\b')
			{
				if (result.Length > 0)
				{
					result = result[..^1];
				}
				continue;
			}

			if (key >= '0' && key <= '9' && result.Length < clearLength)
			{
				result += key.Value;
			}
			else if (allowDecimal && key == '.' && !result.Contains('.') && result.Length < clearLength)
			{
				result += key.Value;
			}
		}
	}
}
