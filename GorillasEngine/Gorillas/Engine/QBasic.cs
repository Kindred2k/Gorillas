using Silk.NET.Input;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Gorillas.Engine;

public class QBasic
{
	private static readonly object _waitKeyLock = new();
	private static readonly Queue<char> _pendingKeys = new();
	private static TaskCompletionSource<char?>? _waitKeyTcs;
	private static char? _expectedWaitKey;

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

	// Viewport settings
	private int[] _textmodeViewportLineRange = new int[] { 1, 25 };

	// Map simple note names to frequencies (Octave 4)
	private static readonly Dictionary<string, double> _notes = new Dictionary<string, double> {
		{ "C", 261.63 }, { "D", 293.66 }, { "E", 329.63 },
		{ "F", 349.23 }, { "G", 392.00 }, { "A", 440.00 }, { "B", 493.88 }
	};

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

	public void SCREEN(int mode)
	{
		switch (mode)
		{
			case 1:
				_bufferWidth = 320;
				_bufferHeight = 200;
				_charWidth = 8;
				_charHeight = 8;
				_fontRenderer.SwitchFont("PxPlus_IBM_CGA.ttf");
				break;
			case 9:
				_bufferWidth = 640;
				_bufferHeight = 350;
				_charWidth = 8;
				_charHeight = 14;
				_fontRenderer.SwitchFont("Px437_IBM_EGA_8x14.ttf");
				break;
			case 0:
			default:
				_bufferWidth = 640;
				_bufferHeight = 350;
				_charWidth = 8;
				_charHeight = 14;
				_fontRenderer.SwitchFont("Px437_IBM_EGA_8x14.ttf");
				break;
		}

		_locateX = 0;
		_locateY = 0;

		if (_pixelBuffer != null)
		{
			Draw.FillBuffer(_pixelBuffer, 0, 0, 0, 255);
		}
	}

	public void SetPixelBuffer(byte[] pixelBuffer, int bufferWidth, int bufferHeight)
	{
		_pixelBuffer = pixelBuffer;
		_bufferWidth = bufferWidth;
		_bufferHeight = bufferHeight;
	}

	public Tuple<int, int> TranslateRowColToPixel(int row, int col)
	{
		int pixelX = (col - 1) * _charWidth;
		int pixelY = (row - 1) * _charHeight;
		return new Tuple<int, int>(pixelX, pixelY);
	}

	public Task<char?> WAITKEY(char? expectedKey = null)
	{
		lock (_waitKeyLock)
		{
			while (_pendingKeys.Count > 0)
			{
				char pendingKey = _pendingKeys.Dequeue();
				if (MatchesExpectedKey(pendingKey, expectedKey))
				{
					return Task.FromResult<char?>(pendingKey);
				}
			}

			var tcs = new TaskCompletionSource<char?>(TaskCreationOptions.RunContinuationsAsynchronously);
			_waitKeyTcs = tcs;
			_expectedWaitKey = expectedKey;
			return tcs.Task;
		}
	}

	public static void HandleKeyPressed(Key key)
	{
		char? mappedKey = MapKeyToChar(key);
		if (!mappedKey.HasValue)
		{
			return;
		}

		lock (_waitKeyLock)
		{
			if (_waitKeyTcs != null)
			{
				if (MatchesExpectedKey(mappedKey.Value, _expectedWaitKey))
				{
					var tcs = _waitKeyTcs;
					_waitKeyTcs = null;
					_expectedWaitKey = null;
					tcs.TrySetResult(mappedKey.Value);
					return;
				}
			}

			_pendingKeys.Enqueue(mappedKey.Value);
		}
	}

	private static bool MatchesExpectedKey(char value, char? expectedKey)
	{
		return !expectedKey.HasValue || value == expectedKey.Value;
	}

	private static char? MapKeyToChar(Key key)
	{
		return key switch
		{
			Key.Space => ' ',
			Key.Enter => '\n',
			Key.Tab => '\t',
			Key.Backspace => '\b',
			Key.Escape => (char)27,
			Key.A => 'a',
			Key.B => 'b',
			Key.C => 'c',
			Key.D => 'd',
			Key.E => 'e',
			Key.F => 'f',
			Key.G => 'g',
			Key.H => 'h',
			Key.I => 'i',
			Key.J => 'j',
			Key.K => 'k',
			Key.L => 'l',
			Key.M => 'm',
			Key.N => 'n',
			Key.O => 'o',
			Key.P => 'p',
			Key.Q => 'q',
			Key.R => 'r',
			Key.S => 's',
			Key.T => 't',
			Key.U => 'u',
			Key.V => 'v',
			Key.W => 'w',
			Key.X => 'x',
			Key.Y => 'y',
			Key.Z => 'z',
			Key.Number0 => '0',
			Key.Number1 => '1',
			Key.Number2 => '2',
			Key.Number3 => '3',
			Key.Number4 => '4',
			Key.Number5 => '5',
			Key.Number6 => '6',
			Key.Number7 => '7',
			Key.Number8 => '8',
			Key.Number9 => '9',
			_ => null
		};
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

	public void PLAY(IEnumerable<(string Note, long DurationMs)> sequence)
	{
		var noteProviders = new List<ISampleProvider>();

		foreach (var step in sequence)
		{
			if (step.DurationMs <= 0)
			{
				continue;
			}

			if (_notes.TryGetValue(step.Note, out double freq))
			{
				// QBasic traditionally used square waves for its internal speaker
				var signal = new SignalGenerator(44100, 1)
				{
					Type = SignalGeneratorType.Square,
					Frequency = freq,
					Gain = 0.2 // Keep volume low to protect ears
				}.Take(TimeSpan.FromMilliseconds(step.DurationMs));
				noteProviders.Add(signal);
			}
		}

		if (noteProviders.Count == 0)
		{
			return;
		}

		using WaveOutEvent outputDevice = new WaveOutEvent();
		outputDevice.Init(new ConcatenatingSampleProvider(noteProviders));
		outputDevice.Play();

		while (outputDevice.PlaybackState == PlaybackState.Playing)
		{
			Thread.Sleep(10);
		}
	}

	/// <summary>
	/// Sets the viewport for text mode rendering. The viewport is defined by a starting line and an ending line, which must be within the range of 1 to 25. This allows for partial screen updates and can be useful for creating split-screen effects or focusing on specific areas of the text display.
	/// </summary>
	/// <param name="startLine">The starting line of the viewport (1-25).</param>
	/// <param name="endLine">The ending line of the viewport (1-25).</param>
	public void VIEW(int startLine, int endLine)
	{
		// TODO: do some failsafe checks to ensure QBasic is currently rendering in a textmode environment before allowing the viewport to be set. If not, throw an exception or ignore the command.

		if (startLine < 1 || endLine > 25 || startLine >= endLine)
		{
			throw new ArgumentOutOfRangeException("Invalid line range for VIEW command.");
		}

		_textmodeViewportLineRange[0] = startLine;
		_textmodeViewportLineRange[1] = endLine;
	}

	/// <summary>
	/// Sets the color of a specific palette index to a new color value. The index must be between 0 and 15, and the color must be a valid QBasic color index (0-15). This allows for dynamic changes to the color palette during runtime, enabling effects such as flashing colors or changing themes.
	/// </summary>
	/// <param name="index">The palette index to change (0-15).</param>
	/// <param name="color">The new color value to set at the specified index (0-15).</param>
	public void PALETTE(int index, int color)
	{
		if (index < 0 || index > 15)
		{
			throw new ArgumentOutOfRangeException("Palette index must be between 0 and 15.");
		}

		if (!ColorMap.ContainsKey(color))
		{
			throw new ArgumentOutOfRangeException("Color must be a valid QBasic color index (0-15).");
		}

		CurrentPalette[index] = color;
	}
}