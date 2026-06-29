using System;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace PixelRenderer
{
	class Program
	{
		// Internal state
		private static IWindow window;
		private static GL gl;

		// Canvas specifications
		private const int Width = 320;
		private const int Height = 240;
		private static uint[] pixelBuffer;

		// OpenGL Objects
		private static uint textureId;
		private static uint fboId; // Framebuffer Object for blitting

		static void Main(string[] args)
		{
			// Configure the cross-platform window properties
			var options = WindowOptions.Default;
			options.Size = new Vector2D<int>(320, 200); // Window size (4x upscale)
			options.Title = "Silk.NET High-Performance Pixel Renderer";
			options.VSync = true;

			window = Window.Create(options);

			// Bind lifecycle events
			window.Load += OnLoad;
			window.Update += OnUpdate;
			window.Render += OnRender;
			window.Closing += OnUnload;

			window.Run();
		}

		private static void OnLoad()
		{
			gl = GL.GetApi(window);
			pixelBuffer = new uint[Width * Height];

			textureId = gl.GenTexture();
			gl.BindTexture(TextureTarget.Texture2D, textureId);

			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			// SAFE: Pass an empty Span instead of a raw null pointer to reserve GPU memory
			gl.TexImage2D(
				TextureTarget.Texture2D,
				0,
				InternalFormat.Rgba8,
				(uint)Width,
				(uint)Height,
				0,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				ReadOnlySpan<uint>.Empty
			);

			fboId = gl.GenFramebuffer();
			gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboId);
			gl.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureId, 0);

			gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
			gl.BindTexture(TextureTarget.Texture2D, 0);
		}

		private static void OnUpdate(double deltaTime)
		{
			// SAFE: Normal managed array access remains identical
			Random rand = Random.Shared;

			for (int y = 0; y < Height; y++)
			{
				for (int x = 0; x < Width; x++)
				{
					byte r = (byte)(x ^ y);
					byte g = (byte)(rand.Next(0, 255));
					byte b = (byte)(y * 255 / Height);
					byte a = 255;

					pixelBuffer[y * Width + x] = (uint)(r | (g << 8) | (b << 16) | (a << 24));
				}
			}
		}

		private static void OnRender(double deltaTime)
		{
			gl.Clear((uint)ClearBufferMask.ColorBufferBit);
			gl.BindTexture(TextureTarget.Texture2D, textureId);

			// SAFE: Pass the raw array directly. 
			// Silk.NET automatically pins the managed memory during the driver upload call.
			gl.TexSubImage2D(
				TextureTarget.Texture2D,
				0,
				0, 0,
				(uint)Width,
				(uint)Height,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				pixelBuffer
			);

			gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fboId);
			gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

			gl.BlitFramebuffer(
				0, 0, Width, Height,
				0, 0, window.FramebufferSize.X, window.FramebufferSize.Y,
				(uint)ClearBufferMask.ColorBufferBit,
				BlitFramebufferFilter.Nearest
			);

			gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
			gl.BindTexture(TextureTarget.Texture2D, 0);
		}
		private static void OnUnload()
		{
			// Clean up native graphics context resources safely
			gl.DeleteTexture(textureId);
			gl.DeleteFramebuffer(fboId);
			gl.Dispose();
		}
	}
}
