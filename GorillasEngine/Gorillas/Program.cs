using Gorillas.Engine;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace PixelRenderer
{
	class Program
	{
		// Internal state
		private static IWindow? _window;
		private static GL? _gl;
		private static FontRenderer? _fontRenderer;
		private static QBasic? _qBasic;

		// Canvas specifications
		private const int _screenWidth = 640;
		private const int _screenHeight = 350;
		private static byte[]? _pixelBuffer;

		// Tests
		private static int xCounter = 0;
		private static int yCounter = 0;
		private static int xDirection = 1;
		private static int yDirection = 1;

		// Scroller state
		private static string scrollerText = "WELCOME TO THE GORILLAS DEMO - 1990S STYLE TEXT SCROLLER";
		private static int scrollerOffset = 0;
		private static double scrollerPhase = 0.0;
		private static int scrollerVisibleColumns = 80;

		// OpenGL Objects
		private static uint textureId;
		private static uint shaderProgram;
		private static uint vao;
		private static uint vbo;
		private static int textureLocation;
		private static bool isFullscreen;
		private static IKeyboard? keyboard;

		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				scrollerText = string.Join(" ", args);
			}

			// Configure the cross-platform window properties
			var options = WindowOptions.Default;
			options.Size = new Vector2D<int>(320, 240); // Window size (4x upscale)
			options.Title = "Silk.NET High-Performance Pixel Renderer";
			options.VSync = true;

			_window = Window.Create(options);

			// Bind lifecycle events
			_window.Load += OnLoad;
			_window.Update += OnUpdate;
			_window.Render += OnRender;
			_window.Closing += OnUnload;

			_window.Run();
		}

		private static void OnLoad()
		{
			_gl = GL.GetApi(_window);
			var inputContext = _window!.CreateInput();
			if (inputContext.Keyboards.Count > 0)
			{
				keyboard = inputContext.Keyboards[0];
				keyboard.KeyDown += OnKeyDown;
			}
			_pixelBuffer = new byte[_screenWidth * _screenHeight * 4]; // 4 bytes per pixel (RGBA)

			_fontRenderer = new FontRenderer(_gl, _pixelBuffer, _screenWidth, _screenHeight, "Px437_IBM_EGA_8x14.ttf");
			_qBasic = new QBasic(_pixelBuffer, _screenWidth, _screenHeight, _fontRenderer);

			textureId = _fontRenderer.TextureId;
			SetupFullscreenQuad();
			shaderProgram = CreateShaderProgram(
				"""
				#version 330 core
				layout(location = 0) in vec2 aPosition;
				layout(location = 1) in vec2 aTexCoord;
				out vec2 vTexCoord;
				void main()
				{
					gl_Position = vec4(aPosition, 0.0, 1.0);
					vTexCoord = aTexCoord;
				}
				""",
				"""
				#version 330 core
				out vec4 FragColor;
				in vec2 vTexCoord;
				uniform sampler2D uTexture;
				void main()
				{
					FragColor = texture(uTexture, vec2(vTexCoord.x, 1.0 - vTexCoord.y));
				}
				""");
			textureLocation = _gl.GetUniformLocation(shaderProgram, "uTexture");
		}

		private static void OnUpdate(double deltaTime)
		{
			if (_pixelBuffer == null)
				throw new InvalidOperationException("Pixel buffer is not initialized.");

			if (_fontRenderer == null)
				throw new InvalidOperationException("Font renderer is not initialized.");

			if (_qBasic == null)
				throw new InvalidOperationException("QBasic instance is not initialized.");

			Random rand = Random.Shared;

			for (int y = 0; y < _screenHeight; y++)
			{
				for (int x = 0; x < _screenWidth; x++)
				{
					byte r = (byte)(x ^ y);
					byte g = (byte)(rand.Next(0, 255));
					byte b = (byte)(y * 255 / _screenHeight);
					byte a = 255;

					_pixelBuffer[(y * _screenWidth + x) * 4 + 0] = r;
					_pixelBuffer[(y * _screenWidth + x) * 4 + 1] = g;
					_pixelBuffer[(y * _screenWidth + x) * 4 + 2] = b;
					_pixelBuffer[(y * _screenWidth + x) * 4 + 3] = a;
				}
			}

			byte randomYColor = (byte)rand.Next(0, 255);
			byte randomXColor = (byte)rand.Next(0, 255);

			for (int y = 0; y < _screenHeight; y++)
			{
				_pixelBuffer[(y * _screenWidth + xCounter) * 4 + 0] = randomYColor;
				_pixelBuffer[(y * _screenWidth + xCounter) * 4 + 1] = 0;
				_pixelBuffer[(y * _screenWidth + xCounter) * 4 + 2] = 0;
				_pixelBuffer[(y * _screenWidth + xCounter) * 4 + 3] = 255;
			}

			for (int x = 0; x < _screenWidth; x++)
			{
				_pixelBuffer[(yCounter * _screenWidth + x) * 4 + 0] = 0;
				_pixelBuffer[(yCounter * _screenWidth + x) * 4 + 1] = randomXColor;
				_pixelBuffer[(yCounter * _screenWidth + x) * 4 + 2] = randomXColor;
				_pixelBuffer[(yCounter * _screenWidth + x) * 4 + 3] = 255;
			}

			xCounter += xDirection;
			yCounter += yDirection;

			if (xCounter >= _screenWidth - 1)
				xDirection = -1;

			if (xCounter < 1)
				xDirection = 1;

			if (yCounter >= _screenHeight - 1)
				yDirection = -1;

			if (yCounter < 1)
				yDirection = 1;

			_qBasic.COLOR(15);
			_qBasic.LOCATE(1, 1);
			_qBasic.PRINT("1,1");
			_qBasic.LOCATE(76, 1);
			_qBasic.PRINT("80,1");
			_qBasic.LOCATE(1, 25);
			_qBasic.PRINT("1,25");
			_qBasic.LOCATE(75, 25);
			_qBasic.PRINT("80,25");

			// _fontRenderer.RenderText("HELLO, CLARA!", 10, 20, 255, 255, 255);
			// _fontRenderer.RenderText("This text is RED.", 10, 40, 255, 0, 0);
			// _fontRenderer.RenderText("This text is GREEN.", 10, 60, 0, 255, 0);
			// _fontRenderer.RenderText("This text is BLUE.", 10, 80, 0, 0, 255);

			for (int i = 1; i < 16; i++)
			{
				_qBasic.COLOR(i);
				_qBasic.LOCATE(1, i+2);
				_qBasic.PRINT($"Color {i}");
			}

			// Marquee scroller + sine wave
			scrollerPhase += deltaTime * 2.2;
			int cycleLength = Math.Max(1, scrollerText.Length + scrollerVisibleColumns + 8);
			scrollerOffset = (scrollerOffset + 1) % cycleLength;
			DrawScrollerText(scrollerText, scrollerOffset);


		}

		private static void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
		{
			QBasic.HandleKeyPressed(key);

			if (key == Key.F11 || (key == Key.Enter && (keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight))))
			{
				isFullscreen = !isFullscreen;
				_window!.WindowState = isFullscreen ? WindowState.Fullscreen : WindowState.Normal;
			}
			else if (key == Key.Escape)
			{
				// TODO: Implement a proper exit mechanism for the application. Show a menu?
				_window!.Close();
			}
		}

		private static void OnRender(double deltaTime)
		{
			if (_gl == null || _window == null)
				throw new InvalidOperationException("OpenGL context or window is not initialized.");

			_gl.Clear((uint)ClearBufferMask.ColorBufferBit);
			_gl.Viewport(0, 0, (uint)_window.FramebufferSize.X, (uint)_window.FramebufferSize.Y);

			_fontRenderer?.UploadTexture();
			_gl.UseProgram(shaderProgram);
			_gl.ActiveTexture(TextureUnit.Texture0);
			_gl.BindTexture(TextureTarget.Texture2D, textureId);
			_gl.Uniform1(textureLocation, 0);
			_gl.BindVertexArray(vao);
			_gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
			_gl.BindVertexArray(0);
			_gl.UseProgram(0);
			_gl.BindTexture(TextureTarget.Texture2D, 0);
		}

		private static void OnUnload()
		{
			if (_gl == null)
				return;

			// Clean up native graphics context resources safely
			if (shaderProgram != 0)
				_gl.DeleteProgram(shaderProgram);
			if (vao != 0)
				_gl.DeleteVertexArray(vao);
			if (vbo != 0)
				_gl.DeleteBuffer(vbo);
			_gl.DeleteTexture(textureId);
			_gl.Dispose();
		}

		private static void DrawScrollerText(string message, int offset)
		{
			if (string.IsNullOrWhiteSpace(message) || _qBasic == null)
				return;

			string loop = message + "   " + message;
			int visibleColumns = Math.Max(1, scrollerVisibleColumns);
			int rightEdge = visibleColumns + 4;
			int baseRow = 12;
			int amplitude = 5;

			_qBasic.COLOR(15);

			for (int i = 0; i < loop.Length; i++)
			{
				int column = rightEdge - (offset + i);
				if (column < 1 || column > visibleColumns)
					continue;

				int row = baseRow + (int)Math.Round(Math.Sin((column * 0.18) + scrollerPhase * 1.6) * amplitude);
				row = Math.Clamp(row, 2, 24);

				_qBasic.LOCATE(column, row);
				_qBasic.PRINT(loop[i].ToString());
			}
		}

		private static void SetupFullscreenQuad()
		{
			if (_gl == null)
				throw new InvalidOperationException("OpenGL context is not initialized.");

			float[] vertices =
			{
				-1f, -1f, 0f, 0f,
				 1f, -1f, 1f, 0f,
				 1f,  1f, 1f, 1f,
				-1f, -1f, 0f, 0f,
				 1f,  1f, 1f, 1f,
				-1f,  1f, 0f, 1f
			};

			vao = _gl.GenVertexArray();
			vbo = _gl.GenBuffer();
			_gl.BindVertexArray(vao);
			_gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

			unsafe
			{
				fixed (float* ptr = vertices)
				{
					_gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), ptr, BufferUsageARB.StaticDraw);
				}
			}

			_gl.EnableVertexAttribArray(0);
			_gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (nint)0);
			_gl.EnableVertexAttribArray(1);
			_gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (nint)(2 * sizeof(float)));
			_gl.BindVertexArray(0);
		}

		private static uint CreateShaderProgram(string vertexSource, string fragmentSource)
		{
			if (_gl == null)
				throw new InvalidOperationException("OpenGL context is not initialized.");

			uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
			_gl.ShaderSource(vertexShader, vertexSource);
			_gl.CompileShader(vertexShader);

			uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
			_gl.ShaderSource(fragmentShader, fragmentSource);
			_gl.CompileShader(fragmentShader);

			uint program = _gl.CreateProgram();
			_gl.AttachShader(program, vertexShader);
			_gl.AttachShader(program, fragmentShader);
			_gl.LinkProgram(program);

			_gl.DeleteShader(vertexShader);
			_gl.DeleteShader(fragmentShader);
			return program;
		}
	}
}
