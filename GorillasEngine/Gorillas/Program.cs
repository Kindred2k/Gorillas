using Gorillas.Engine;
using Gorillas.Game;
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

		/*
		// Test counters
		private static int xCounter = 0;
		private static int yCounter = 0;
		private static int xDirection = 1;
		private static int yDirection = 1;

		// Scroller state
		private static string scrollerText = "WELCOME TO THE GORILLAS DEMO - 1990S STYLE TEXT SCROLLER";
		private static int scrollerOffset = 0;
		private static double scrollerPhase = 0.0;
		private static int scrollerVisibleColumns = 80;
		*/

		// OpenGL Objects
		private static uint textureId;
		private static uint shaderProgram;
		private static uint vao;
		private static uint vbo;
		private static int textureLocation;
		private static int crtEnabledLocation;
		private static int zoomFactorLocation;
		private static int zoomCenterLocation;
		private static bool isFullscreen;
		private static IKeyboard? keyboard;
		private static Gorilla? _gorilla;
		private static Task? _gameTask;
		private static System.Threading.CancellationTokenSource? _gameplayCts;
		private static bool _inGameplay;

		static void Main(string[] args)
		{
			/*
			if (args.Length > 0)
			{
				scrollerText = string.Join(" ", args);
			}
			*/

			// Configure the cross-platform window properties
			var options = WindowOptions.Default;
			options.Size = new Vector2D<int>(320, 240); // Window size (4x upscale)
			options.Title = "QBasic Gorillas in C#";
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
				uniform int uCrtEnabled;
				uniform float uZoomFactor;
				uniform vec2 uZoomCenter;
				const float screenHeight = 350.0;
				void main()
				{
					vec2 uv = vec2(vTexCoord.x, 1.0 - vTexCoord.y);
					vec2 zoomedUv = clamp((uv - uZoomCenter) / uZoomFactor + uZoomCenter, 0.0, 1.0);
					vec4 color = texture(uTexture, zoomedUv);
					if (uCrtEnabled != 0)
					{
						float line = fract(uv.y * screenHeight);
						float scanline = smoothstep(0.0, 0.5, line) * smoothstep(1.0, 0.5, line);
						color.rgb *= mix(0.65, 1.0, scanline);

						vec2 vigUv = uv * (1.0 - uv);
						float vig = clamp(pow(vigUv.x * vigUv.y * 18.0, 0.25), 0.0, 1.0);
						color.rgb *= vig;
					}
					FragColor = color;
				}
				""");
			textureLocation = _gl.GetUniformLocation(shaderProgram, "uTexture");
			crtEnabledLocation = _gl.GetUniformLocation(shaderProgram, "uCrtEnabled");
			zoomFactorLocation = _gl.GetUniformLocation(shaderProgram, "uZoomFactor");
			zoomCenterLocation = _gl.GetUniformLocation(shaderProgram, "uZoomCenter");

			_gorilla = new Gorilla(_pixelBuffer, _screenWidth, _screenHeight, 9, _qBasic);
			_gameTask = RunGameAsync();
			_gameTask.ContinueWith(
				task => Console.Error.WriteLine($"Gorilla game stopped: {task.Exception?.GetBaseException()}"),
				TaskContinuationOptions.OnlyOnFaulted);
		}

		private static async Task RunGameAsync()
		{
			if (_gorilla == null)
			{
				return;
			}

			while (true)
			{
				await _gorilla.Intro();

				_gameplayCts = new System.Threading.CancellationTokenSource();
				_gorilla.BeginGameplay(_gameplayCts.Token);
				_inGameplay = true;
				try
				{
					var inputs = await _gorilla.GetInputs();
					await _gorilla.GorillaIntro(inputs.Player1, inputs.Player2);
					await _gorilla.PlayGame(inputs.Player1, inputs.Player2, inputs.NumGames);
				}
				catch (OperationCanceledException)
				{
					// Escape was pressed during gameplay; fall through and redisplay the title screen.
				}
				finally
				{
					_inGameplay = false;
					_gorilla.EndGameplay();
					_gameplayCts.Dispose();
					_gameplayCts = null;
				}
			}
		}

		private static void OnUpdate(double deltaTime)
		{
			return;
		}

		private static void OnKeyDown(IKeyboard keyboard, Key key, int arg3)
		{
			bool shiftPressed = keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight);
			QBasic.HandleKeyPressed(key, shiftPressed);

			if (key == Key.F11 || (key == Key.Enter && (keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight))))
			{
				isFullscreen = !isFullscreen;
				_window!.WindowState = isFullscreen ? WindowState.Fullscreen : WindowState.Normal;
			}
			else if (key == Key.Escape)
			{
				if (_inGameplay)
				{
					// Unwind the current game back to the title screen instead of quitting outright.
					_gameplayCts?.Cancel();
				}
				else
				{
					_window!.Close();
				}
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
			_gl.Uniform1(crtEnabledLocation, _gorilla?.CrtEffectEnabled == true ? 1 : 0);
			_gl.Uniform1(zoomFactorLocation, _gorilla?.ZoomFactor ?? 1f);
			_gl.Uniform2(zoomCenterLocation, _gorilla?.ZoomCenterX ?? 0.5f, _gorilla?.ZoomCenterY ?? 0.5f);
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

		/*
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
		*/

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
