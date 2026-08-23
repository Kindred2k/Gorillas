using Silk.NET.Input;
using Silk.NET.OpenAL;

namespace Gorillas.Engine;

public unsafe class QBasic
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
	private AL? _audio;
	private ALContext? _audioContext;
	private Device* _audioDevice;
	private Context* _audioContextHandle;
	private bool _audioUnavailable;
	private readonly object _audioLock = new();

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
			CLS();
		}
	}

	public void CLS()
	{
		var backgroundColor = GetCurrentColor(0);
		Draw.FillBuffer(_pixelBuffer, backgroundColor.r, backgroundColor.g, backgroundColor.b, 255);
	}

	public void ClearRegion(int x, int y, int width, int height)
	{
		var backgroundColor = GetCurrentColor(0);
		Draw.DrawFilledRectangle(_pixelBuffer, _bufferWidth, _bufferHeight, x, y, width, height, backgroundColor.r, backgroundColor.g, backgroundColor.b, 255);
	}

	public void SetPixelBuffer(byte[] pixelBuffer, int bufferWidth, int bufferHeight)
	{
		_pixelBuffer = pixelBuffer;
		_bufferWidth = bufferWidth;
		_bufferHeight = bufferHeight;
	}

	public byte[] PixelBuffer => _pixelBuffer;
	public int BufferWidth => _bufferWidth;
	public int BufferHeight => _bufferHeight;
	public int CharWidth => _charWidth;
	public int CharHeight => _charHeight;

	public Tuple<int, int> TranslateRowColToPixel(int row, int col)
	{
		int pixelX = (col - 1) * _charWidth;
		int pixelY = (row - 1) * _charHeight;
		return new Tuple<int, int>(pixelX, pixelY);
	}

	public Task<char?> WAITKEY(char? expectedKey = null, CancellationToken cancellationToken = default)
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

			if (cancellationToken.CanBeCanceled)
			{
				cancellationToken.Register(() =>
				{
					lock (_waitKeyLock)
					{
						if (_waitKeyTcs == tcs)
						{
							_waitKeyTcs = null;
							_expectedWaitKey = null;
						}
					}
					tcs.TrySetCanceled(cancellationToken);
				});
			}

			return tcs.Task;
		}
	}

	public bool HasPendingKey
	{
		get
		{
			lock (_waitKeyLock)
			{
				return _pendingKeys.Count > 0;
			}
		}
	}

	public void ClearPendingKeys()
	{
		lock (_waitKeyLock)
		{
			_pendingKeys.Clear();
		}
	}

	public static void HandleKeyPressed(Key key, bool shiftPressed = false)
	{
		char? mappedKey = MapKeyToChar(key, shiftPressed);
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

	private static char? MapKeyToChar(Key key, bool shiftPressed = false)
	{
		char? mapped = key switch
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
			Key.Period => '.',
			_ => null
		};

		if (shiftPressed && mapped.HasValue && mapped.Value is >= 'a' and <= 'z')
		{
			return char.ToUpperInvariant(mapped.Value);
		}

		return mapped;
	}

	public void CIRCLE(bool step, int x, int y, int radius, int? color = null, float? start = null, float? end = null, float? aspect = null)
	{
		int drawColor = color ?? _color;

		if (start != null && end != null)
		{
			var circleColor = GetCurrentColor(drawColor);
			int packedColor = circleColor.r << 24 | circleColor.g << 16 | circleColor.b << 8 | 255;
			Draw.DrawQBasicCircle(_pixelBuffer, _bufferWidth, _bufferHeight, step, x, y, radius, packedColor, start.Value, end.Value, aspect ?? 1);
		}
		else
		{
			var mappedColor = GetCurrentColor(drawColor);
			Draw.DrawCircleOutline(_pixelBuffer, _bufferWidth, _bufferHeight, x, y, radius, mappedColor.r, mappedColor.g, mappedColor.b, 255);
		}
	}

	public void COLOR(int color)
	{
		_color = color;
	}

	public (byte r, byte g, byte b) GetColor(int color)
	{
		return GetCurrentColor(color);
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

		var currentColor = GetCurrentColor(color);
		byte r = currentColor.r;
		byte g = currentColor.g;
		byte b = currentColor.b;
		byte a = 255;

		switch (style)
		{
			case LineBoxStyle.None:
				Draw.DrawLine(buffer, x1, y1, x2, y2, width, height, r, g, b, a);
				break;
			case LineBoxStyle.B:
				// Draw the box outline
				Draw.DrawLine(buffer, x1, y1, x2, y1, width, height, r, g, b, a); // Top
				Draw.DrawLine(buffer, x1, y2, x2, y2, width, height, r, g, b, a); // Bottom
				Draw.DrawLine(buffer, x1, y1, x1, y2, width, height, r, g, b, a); // Left
				Draw.DrawLine(buffer, x2, y1, x2, y2, width, height, r, g, b, a); // Right
				break;
			case LineBoxStyle.BF:
				// Draw filled box
				int top = Math.Min(y1, y2);
				int bottom = Math.Max(y1, y2);
				for (int y = top; y <= bottom; y++)
				{
					Draw.DrawLine(buffer, Math.Min(x1, x2), y, Math.Max(x1, x2), y, width, height, r, g, b, a);
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
		PRINT(text, false);
	}

	public void PRINT(string text, bool clearBackground)
	{
		var currentColor = GetCurrentColor(_color);
		byte r = currentColor.r;
		byte g = currentColor.g;
		byte b = currentColor.b;

		Tuple<int, int> pixelCoords = TranslateRowColToPixel(_locateY, _locateX);
		if (clearBackground)
		{
			var backgroundColor = GetCurrentColor(0);
			Draw.DrawFilledRectangle(
				_pixelBuffer,
				_bufferWidth,
				_bufferHeight,
				pixelCoords.Item1,
				pixelCoords.Item2,
				pixelCoords.Item1 + text.Length * 16,
				pixelCoords.Item2 + _charHeight,
				backgroundColor.r,
				backgroundColor.g,
				backgroundColor.b,
				255);
		}
		_fontRenderer.RenderText(text, pixelCoords.Item1, pixelCoords.Item2, r, g, b);
	}

	public void PSET(int x, int y, int color)
	{
		var currentColor = GetCurrentColor(color);
		byte r = currentColor.r;
		byte g = currentColor.g;
		byte b = currentColor.b;
		byte a = 255;

		Draw.DrawPixel(_pixelBuffer, x, y, _bufferWidth, _bufferHeight, r, g, b, a);
	}

	public void PUT(byte[]? sprite, int sourceX, int sourceY, int destinationX, int destinationY)
	{
		if (sprite == null)
		{
			return;
		}

		for (int spriteY = 0; spriteY < _bufferHeight; spriteY++)
		{
			for (int spriteX = 0; spriteX < _bufferWidth; spriteX++)
			{
				int sourceIndex = (spriteY * _bufferWidth + spriteX) * 4;
				if (sprite[sourceIndex + 3] == 0)
				{
					continue;
				}

				int targetX = destinationX + spriteX - sourceX;
				int targetY = destinationY + spriteY - sourceY;
				if (targetX < 0 || targetX >= _bufferWidth || targetY < 0 || targetY >= _bufferHeight)
				{
					continue;
				}

				int targetIndex = (targetY * _bufferWidth + targetX) * 4;
				System.Buffer.BlockCopy(sprite, sourceIndex, _pixelBuffer, targetIndex, 4);
			}
		}
	}

	public void PUT(byte[]? sprite, int spriteWidth, int spriteHeight, int destinationX, int destinationY, bool compactSprite)
	{
		if (sprite == null)
		{
			return;
		}

		for (int spriteY = 0; spriteY < spriteHeight; spriteY++)
		{
			for (int spriteX = 0; spriteX < spriteWidth; spriteX++)
			{
				int sourceIndex = (spriteY * spriteWidth + spriteX) * 4;
				if (sprite[sourceIndex + 3] == 0)
				{
					continue;
				}

				int targetX = destinationX + spriteX;
				int targetY = destinationY + spriteY;
				if (targetX < 0 || targetX >= _bufferWidth || targetY < 0 || targetY >= _bufferHeight)
				{
					continue;
				}

				int targetIndex = (targetY * _bufferWidth + targetX) * 4;
				System.Buffer.BlockCopy(sprite, sourceIndex, _pixelBuffer, targetIndex, 4);
			}
		}
	}

	public void PLAY(IEnumerable<(string Note, long DurationMs)> sequence, bool background = false)
	{
		if (_audioUnavailable)
		{
			return;
		}

		List<short> samples = new();
		const int sampleRate = 44100;
		foreach (var step in sequence)
		{
			if (!_notes.TryGetValue(step.Note, out double frequency) || step.DurationMs <= 0)
			{
				continue;
			}

			int sampleCount = (int)(sampleRate * step.DurationMs / 1000.0);
			for (int sample = 0; sample < sampleCount; sample++)
			{
				double phase = sample * frequency / sampleRate;
				samples.Add((short)(Math.Sin(phase * Math.PI * 2) >= 0 ? 5000 : -5000));
			}
		}

		if (samples.Count == 0)
		{
			return;
		}

		if (background)
		{
			_ = Task.Run(() => PlaySamples(samples));
		}
		else
		{
			PlaySamples(samples);
		}
	}

	private void PlaySamples(List<short> samples)
	{
		// OpenAL calls are serialized since background playback can overlap with a later foreground call.
		lock (_audioLock)
		{
			try
			{
				if (!TryInitializeAudio())
				{
					return;
				}

				uint buffer = _audio!.GenBuffer();
				uint source = _audio.GenSource();
				_audio.BufferData(buffer, BufferFormat.Mono16, samples.ToArray(), 44100);
				_audio.SetSourceProperty(source, SourceInteger.Buffer, buffer);
				_audio.SourcePlay(source);
				_audio.GetSourceProperty(source, GetSourceInteger.SourceState, out int sourceState);
				while ((SourceState)sourceState == SourceState.Playing)
				{
					Thread.Sleep(10);
					_audio.GetSourceProperty(source, GetSourceInteger.SourceState, out sourceState);
				}
				_audio.SourceStop(source);
				_audio.DeleteSource(source);
				_audio.DeleteBuffer(buffer);
			}
			catch (Exception) when (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux() || OperatingSystem.IsWindows())
			{
				_audioUnavailable = true;
			}
		}
	}

	private bool TryInitializeAudio()
	{
		if (_audio != null)
		{
			return true;
		}

		try
		{
			_audio = AL.GetApi();
			_audioContext = ALContext.GetApi();
			_audioDevice = _audioContext.OpenDevice(null);
			if (_audioDevice == null)
			{
				_audioUnavailable = true;
				return false;
			}

			_audioContextHandle = _audioContext.CreateContext(_audioDevice, null);
			_audioContext.MakeContextCurrent(_audioContextHandle);
			return _audioContextHandle != null;
		}
		catch (Exception)
		{
			_audioUnavailable = true;
			return false;
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

		if (color < 0 || color > 63)
		{
			throw new ArgumentOutOfRangeException(nameof(color), "Color must be a valid QBasic color index (0-63).");
		}

		CurrentPalette[index] = color;
	}

	private (byte r, byte g, byte b) GetCurrentColor(int color)
	{
		int paletteColor = CurrentPalette[color];
		if (ColorMap.TryGetValue(paletteColor, out var standardColor))
		{
			return standardColor;
		}

		// EGA 6-bit color: bits 0-2 (BGR) are the bright 0xAA component, bits 3-5 the dim 0x55 component.
		int red = ((paletteColor >> 2) & 1) * 2 + ((paletteColor >> 5) & 1);
		int green = ((paletteColor >> 1) & 1) * 2 + ((paletteColor >> 4) & 1);
		int blue = (paletteColor & 1) * 2 + ((paletteColor >> 3) & 1);
		return ((byte)(red * 85), (byte)(green * 85), (byte)(blue * 85));
	}
}