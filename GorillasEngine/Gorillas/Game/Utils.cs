using Gorillas.Engine;

namespace Gorillas.Game;

internal static class Utils
{
	public static byte[] CaptureRegion(byte[] buffer, int bufferWidth, int bufferHeight, int x, int y, int width, int height)
	{
		byte[] region = new byte[width * height * 4];
		for (int row = 0; row < height; row++)
		{
			int sourceY = y + row;
			if (sourceY < 0 || sourceY >= bufferHeight)
			{
				continue;
			}

			int sourceX = Math.Max(0, x);
			int copyWidth = Math.Min(width - Math.Max(0, -x), bufferWidth - sourceX);
			if (copyWidth > 0)
			{
				Buffer.BlockCopy(buffer, (sourceY * bufferWidth + sourceX) * 4, region, (row * width + (sourceX - x)) * 4, copyWidth * 4);
			}
		}
		return region;
	}

	public static void RestoreRegion(byte[] buffer, int bufferWidth, int bufferHeight, byte[] region, int x, int y)
	{
		const int height = 7;
		int width = region.Length / (height * 4);
		for (int row = 0; row < height; row++)
		{
			int targetY = y + row;
			if (targetY >= 0 && targetY < bufferHeight)
			{
				Buffer.BlockCopy(region, row * width * 4, buffer, (targetY * bufferWidth + x) * 4, width * 4);
			}
		}
	}

	public static void RestoreRegion(byte[] buffer, int bufferWidth, int bufferHeight, byte[] region, int x, int y, int width, int height)
	{
		for (int row = 0; row < height; row++)
		{
			int targetY = y + row;
			int targetX = Math.Max(0, x);
			int sourceX = targetX - x;
			int copyWidth = Math.Min(width - sourceX, bufferWidth - targetX);
			if (targetY >= 0 && targetY < bufferHeight && copyWidth > 0)
			{
				Buffer.BlockCopy(region, (row * width + sourceX) * 4, buffer, (targetY * bufferWidth + targetX) * 4, copyWidth * 4);
			}
		}
	}

	public static async Task<string> ReadLineInput(QBasic qBasic, int row, int column, string prompt, string defaultValue, int maxLength, CancellationToken cancellationToken = default)
	{
		string result = string.Empty;
		int x = (column - 1) * qBasic.CharWidth;
		int y = (row - 1) * qBasic.CharHeight;
		int width = (prompt.Length + maxLength) * qBasic.CharWidth;
		byte[] background = CaptureRegion(qBasic.PixelBuffer, qBasic.BufferWidth, qBasic.BufferHeight, x, y, width, qBasic.CharHeight);

		while (true)
		{
			qBasic.LOCATE(row, column);
			RestoreRegion(qBasic.PixelBuffer, qBasic.BufferWidth, qBasic.BufferHeight, background, x, y, width, qBasic.CharHeight);
			qBasic.PRINT(prompt + result.PadRight(maxLength));
			char? key = await qBasic.WAITKEY(cancellationToken: cancellationToken);
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

		}
	}

	public static async Task<string> ReadNumericInput(QBasic qBasic, int row, int column, string prompt, int clearLength, bool allowDecimal, CancellationToken cancellationToken = default)
	{
		string result = string.Empty;
		int x = (column - 1) * qBasic.CharWidth;
		int y = (row - 1) * qBasic.CharHeight;
		int width = (prompt.Length + clearLength) * qBasic.CharWidth;
		byte[] background = CaptureRegion(qBasic.PixelBuffer, qBasic.BufferWidth, qBasic.BufferHeight, x, y, width, qBasic.CharHeight);

		while (true)
		{
			qBasic.LOCATE(row, column);
			RestoreRegion(qBasic.PixelBuffer, qBasic.BufferWidth, qBasic.BufferHeight, background, x, y, width, qBasic.CharHeight);
			qBasic.PRINT(prompt + result.PadRight(clearLength));

			char? key = await qBasic.WAITKEY(cancellationToken: cancellationToken);
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
