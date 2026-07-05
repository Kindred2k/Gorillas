namespace Gorillas.Engine;

public class QBasic
{
	// Internal state
	private int _color = 15;
	private int _locateX = 0;
	private int _locateY = 0;

	private int _charWidth = 8;
	private int _charHeight = 16;
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
	private Dictionary<int, int> CurrentPalettes = new Dictionary<int, int>
	{
		{0 , 0}, {1, 1}, {2, 2}, {3, 3}, {4, 4}, {5, 5}, {6, 6}, {7, 7},
		{8, 8}, {9, 9}, {10, 10}, {11, 11}, {12, 12}, {13, 13}, {14, 14}, {15, 15}
	};

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

	public void COLOR(int color)
	{
		_color = color;
	}

	public void LOCATE(int row, int col)
	{
		_locateX = col;
		_locateY = row;
	}

	public void PRINT(string text)
	{
		_fontRenderer.RenderText(text, _locateX, _locateY, 255, 255, 255);
	}
}