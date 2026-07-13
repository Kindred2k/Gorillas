namespace Gorillas.Engine;

public class QBasic
{
	// Internal state
	private int _color = 15;
	private int _locateX = 0;
	private int _locateY = 0;

	private int _charWidth = 8;
	private int _charHeight = 14;
	private byte[] _pixelBuffer;
	private int _bufferWidth;
	private int _bufferHeight;
	private FontRenderer _fontRenderer;

	/// <summary>
	/// A dictionary that maps QBasic color indices to their corresponding RGB values. This allows for easy translation of QBasic color codes into actual colors that can be rendered on the screen.
	/// </summary>
	private static Dictionary<int, (byte r, byte g, byte b)> ColorMap = new Dictionary<int, (byte r, byte g, byte b)>
	{
		{ 0, (0, 0, 0) },       // Black
		{ 1, (0, 0, 168) },     // Blue
		{ 2, (0, 168, 0) },     // Green
		{ 3, (0, 168, 168) },   // Cyan
		{ 4, (168, 0, 0) },     // Red
		{ 5, (168, 0, 168) },   // Magenta
		{ 6, (168, 84, 0) },	// Brown
		{ 7, (168, 168, 168) }, // Light Gray
		{ 8, (84, 84, 84) },    // Dark Gray
		{ 9, (84, 84, 252) },   // Light Blue
		{ 10, (84, 252, 84) },  // Light Green
		{ 11, (84, 252, 252) }, // Light Cyan
		{ 12, (252, 84, 84) },  // Light Red
		{ 13, (252, 84, 252) }, // Light Magenta
		{ 14, (252, 252, 84) }, // Yellow
		{ 15, (252, 252, 252) } // White
	};

	/// <summary>
	/// A dictionary that maps the current palette indices to their corresponding color values. This allows for dynamic color changes in the QBasic environment, enabling the use of different color schemes during gameplay or other visual effects.
	/// </summary>
	/// <remarks>It would be beneficial to convert all colors to a uint32 so that less lookups are being performed.</remarks>
	private Dictionary<int, int> CurrentPalette = new Dictionary<int, int>
	{
		{0 , 0}, {1, 1}, {2, 2}, {3, 3}, {4, 4}, {5, 5}, {6, 6}, {7, 7},
		{8, 8}, {9, 9}, {10, 10}, {11, 11}, {12, 12}, {13, 13}, {14, 14}, {15, 15}
	};

	public enum LineBoxStyle
	{
		// Draw a line
		None = 0,

		// Draw a box
		B = 1,

		// Draw a filled box
		BF = 2
	}

	public QBasic(byte[] pixelBuffer, int bufferWidth, int bufferHeight, FontRenderer fontRenderer)
	{
		_pixelBuffer = pixelBuffer;
		_bufferWidth = bufferWidth;
		_bufferHeight = bufferHeight;
		_fontRenderer = fontRenderer;
	}

	public Tuple<int, int> TranslateRowColToPixel(int row, int col)
	{
		int pixelX = (col - 1) * _charWidth;
		int pixelY = (row - 1) * _charHeight;
		return new Tuple<int, int>(pixelX, pixelY);
	}

	public void CIRCLE(bool step, int x, int y, int radius, int? color = null, float? start = null, float? end = null, float? aspect = null)
	{
		int drawColor = color ?? _color;

		if (start != null && end != null && aspect != null)
		{
			Draw.DrawQBasicCircle(_pixelBuffer, _bufferWidth, _bufferHeight, step, x, y, radius, drawColor, start.Value, end.Value, aspect.Value);
		}
		else
		{
			Draw.DrawCircleOutline(_pixelBuffer, _bufferWidth, _bufferHeight, x, y, radius, ColorMap[CurrentPalette[drawColor]].r, ColorMap[CurrentPalette[drawColor]].g, ColorMap[CurrentPalette[drawColor]].b, 255);
		}
	}

	public void COLOR(int color)
	{
		_color = color;
	}

	// <summary>
	/// /// Draws a line or box on the pixel buffer based on the specified coordinates, color, and style. The method supports three styles: None (draws a simple line), B (draws a box outline), and BF (draws a filled box). The method uses the current palette to determine the color to be used for drawing.
	/// </summary>
	/// <param name="x1"></param>
	/// <param name="y1"></param>
	/// <param name="x2"></param>
	/// <param name="y2"></param>
	/// <param name="color"></param>
	/// <param name="style"></param>
	/// <param name="pixelBuffer">Optional destination pixel buffer</param>
	/// <param name="bufferWidth"></param>
	/// <param name="bufferHeight"></param>
	public void LINE(int x1, int y1, int x2, int y2, int color, LineBoxStyle style = LineBoxStyle.None, byte[]? pixelBuffer = null, int? bufferWidth = null, int? bufferHeight = null)
	{
		byte[] buffer = pixelBuffer ?? _pixelBuffer;
		int width = bufferWidth ?? _bufferWidth;
		int height = bufferHeight ?? _bufferHeight;

		byte r = ColorMap[CurrentPalette[_color]].r;
		byte g = ColorMap[CurrentPalette[_color]].g;
		byte b = ColorMap[CurrentPalette[_color]].b;
		byte a = 255;

		switch (style)
		{
			case LineBoxStyle.None:
				Draw.DrawLine(buffer, width, height, x1, y1, x2, y2, r, g, b, a);
				break;
			case LineBoxStyle.B:
				// Draw the box outline
				Draw.DrawLine(buffer, width, height, x1, y1, x2, y1, r, g, b, a); // Top
				Draw.DrawLine(buffer, width, height, x1, y2, x2, y2, r, g, b, a); // Bottom
				Draw.DrawLine(buffer, width, height, x1, y1, x1, y2, r, g, b, a); // Left
				Draw.DrawLine(buffer, width, height, x2, y1, x2, y2, r, g, b, a); // Right
				break;
			case LineBoxStyle.BF:
				// Draw filled box
				for (int y = y1; y <= y2; y++)
				{
					Draw.DrawLine(buffer, width, height, x1, y, x2, y, r, g, b, a);
				}
				break;
		}
	}

	public void LOCATE(int row, int col)
	{
		_locateX = col;
		_locateY = row;
	}

	public void PRINT(string text)
	{
		byte r = ColorMap[CurrentPalette[_color]].r;
		byte g = ColorMap[CurrentPalette[_color]].g;
		byte b = ColorMap[CurrentPalette[_color]].b;

		Tuple<int, int> pixelCoords = TranslateRowColToPixel(_locateX, _locateY);
		_fontRenderer.RenderText(text, pixelCoords.Item1, pixelCoords.Item2, r, g, b);
	}

	public void PSET(int x, int y, int color)
	{
		byte r = ColorMap[CurrentPalette[color]].r;
		byte g = ColorMap[CurrentPalette[color]].g;
		byte b = ColorMap[CurrentPalette[color]].b;
		byte a = 255;

		Draw.DrawPixel(_pixelBuffer, x, y, _bufferWidth, _bufferHeight, r, g, b, a);
	}
}