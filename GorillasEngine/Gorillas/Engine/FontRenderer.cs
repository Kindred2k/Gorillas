using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using StbTrueTypeSharp;

namespace Gorillas.Engine
{
    public sealed class FontRenderer : IDisposable
    {
        private const string _defaultFontFileName = "Px437_IBM_EGA_8x14.ttf";
        private const float _fontSize = 16f;
        private readonly GL _gl;
        private readonly int _bufferWidth;
        private readonly int _bufferHeight;
        private readonly byte[] _pixelBuffer;
        private readonly uint _textureId;
        private StbTrueType.stbtt_fontinfo _fontInfo;
        private GCHandle _fontBufferHandle; // Keep font data alive
        private string _fontFileName;
        private bool _disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="FontRenderer"/> class with the specified OpenGL context, pixel buffer, buffer width, buffer height, and optional font file name.
		/// </summary>
		/// <param name="gl">The OpenGL context.</param>
		/// <param name="pixelBuffer">The pixel buffer to render text onto.</param>
		/// <param name="bufferWidth">The width of the pixel buffer.</param>
		/// <param name="bufferHeight">The height of the pixel buffer.</param>
		/// <param name="fontFileName">The name of the font file to use.</param>
        public unsafe FontRenderer(GL gl, byte[] pixelBuffer, int bufferWidth, int bufferHeight, string? fontFileName = null)
        {
            ArgumentNullException.ThrowIfNull(gl);
            if (bufferWidth <= 0) throw new ArgumentOutOfRangeException(nameof(bufferWidth));
            if (bufferHeight <= 0) throw new ArgumentOutOfRangeException(nameof(bufferHeight));

            _gl = gl;
            _bufferWidth = bufferWidth;
            _bufferHeight = bufferHeight;
            _pixelBuffer = pixelBuffer;

            _fontFileName = fontFileName ?? _defaultFontFileName;
            LoadFont(_fontFileName);

            _textureId = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _textureId);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8,
                (uint)_bufferWidth,
                (uint)_bufferHeight,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                ReadOnlySpan<byte>.Empty);
        }

        public int BufferWidth => _bufferWidth;
        public int BufferHeight => _bufferHeight;
        public uint TextureId => _textureId;
        public byte[] PixelBuffer => _pixelBuffer;

        public void SwitchFont(string fontFileName)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(fontFileName);

            if (string.Equals(fontFileName, _fontFileName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            LoadFont(fontFileName);
        }

        public void Clear()
        {
            ThrowIfDisposed();
            Array.Clear(_pixelBuffer, 0, _pixelBuffer.Length);
        }

        public void RenderText(string text, int startX, int startY, byte r, byte g, byte b, byte a = 255)
        {
            ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            //Clear();
            RenderTextToBuffer(text, startX, startY, r, g, b, a);
        }

        public unsafe void UploadTexture()
        {
            ThrowIfDisposed();
            _gl.BindTexture(TextureTarget.Texture2D, _textureId);

            fixed (byte* pData = _pixelBuffer)
            {
                _gl.TexSubImage2D(
                    TextureTarget.Texture2D,
                    0,
                    0,
                    0,
                    (uint)_bufferWidth,
                    (uint)_bufferHeight,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pData);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _gl.DeleteTexture(_textureId);

            // Release the pinned font buffer
            if (_fontBufferHandle.IsAllocated)
            {
                _fontBufferHandle.Free();
            }

            _disposed = true;
        }

        private unsafe void RenderTextToBuffer(string text, int startX, int startY, byte r, byte g, byte b, byte a = 255)
        {
            float scale = StbTrueType.stbtt_ScaleForPixelHeight(_fontInfo, _fontSize);

            int ascent = 0;
            int descent = 0;
            int lineGap = 0;
            unsafe
            {
                int* pAscent = &ascent;
                int* pDescent = &descent;
                int* pLineGap = &lineGap;
                StbTrueType.stbtt_GetFontVMetrics(_fontInfo, pAscent, pDescent, pLineGap);
            }

            int baseline = (int)(ascent * scale);

            int currentX = startX;

            foreach (char c in text)
            {
                int advanceWidth = 0;
                int leftSideBearing = 0;
                unsafe
                {
                    int* pAdvanceWidth = &advanceWidth;
                    int* pLeftSideBearing = &leftSideBearing;
                    StbTrueType.stbtt_GetCodepointHMetrics(_fontInfo, c, pAdvanceWidth, pLeftSideBearing);
                }

                int ix1 = 0;
                int iy1 = 0;
                int ix2 = 0;
                int iy2 = 0;
                unsafe
                {
                    int* pIx1 = &ix1;
                    int* pIy1 = &iy1;
                    int* pIx2 = &ix2;
                    int* pIy2 = &iy2;
                    StbTrueType.stbtt_GetCodepointBitmapBox(_fontInfo, c, scale, scale, pIx1, pIy1, pIx2, pIy2);
                }

                int glyphWidth = ix2 - ix1;
                int glyphHeight = iy2 - iy1;

                if (glyphWidth > 0 && glyphHeight > 0)
                {
                    byte[] monoBitmap = new byte[glyphWidth * glyphHeight];

                    fixed (byte* pBitmap = monoBitmap)
                    {
                        StbTrueType.stbtt_MakeCodepointBitmap(_fontInfo, pBitmap, glyphWidth, glyphHeight, glyphWidth, scale, scale, c);
                    }

                    int targetY = startY + baseline + iy1;

                    for (int srcY = 0; srcY < glyphHeight; srcY++)
                    {
                        int pixelY = targetY + srcY;
                        if (pixelY < 0 || pixelY >= _bufferHeight)
                        {
                            continue;
                        }

                        for (int srcX = 0; srcX < glyphWidth; srcX++)
                        {
                            int pixelX = currentX + (int)(leftSideBearing * scale) + srcX;
                            if (pixelX < 0 || pixelX >= _bufferWidth)
                            {
                                continue;
                            }

                            byte alphaSample = monoBitmap[srcY * glyphWidth + srcX];
                            if (alphaSample > 0)
                            {
                                int bufferIndex = (pixelY * _bufferWidth + pixelX) * 4;
                                _pixelBuffer[bufferIndex + 0] = r;
                                _pixelBuffer[bufferIndex + 1] = g;
                                _pixelBuffer[bufferIndex + 2] = b;
                                _pixelBuffer[bufferIndex + 3] = a;
                            }
                        }
                    }
                }

                currentX += (int)(advanceWidth * scale);
            }
        }

        private unsafe void LoadFont(string fontFileName)
        {
            string fontPath = ResolveFontPath(fontFileName);
            byte[] fontBuffer = File.ReadAllBytes(fontPath);

            if (_fontBufferHandle.IsAllocated)
            {
                _fontBufferHandle.Free();
            }

            _fontInfo = new StbTrueType.stbtt_fontinfo();
            _fontBufferHandle = GCHandle.Alloc(fontBuffer, GCHandleType.Pinned);

            try
            {
                IntPtr fontPtr = _fontBufferHandle.AddrOfPinnedObject();
                int success = StbTrueType.stbtt_InitFont(_fontInfo, (byte*)fontPtr, 0);
                if (success == 0)
                {
                    throw new InvalidOperationException("Failed to initialize the requested TrueType font.");
                }

                _fontFileName = fontFileName;
            }
            catch
            {
                if (_fontBufferHandle.IsAllocated)
                {
                    _fontBufferHandle.Free();
                }
                throw;
            }
        }

        private static string ResolveFontPath(string fontFileName)
        {
            if (Path.IsPathRooted(fontFileName))
            {
                return fontFileName;
            }

            string currentDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), fontFileName);
            if (File.Exists(currentDirectoryPath))
            {
                return currentDirectoryPath;
            }

            string applicationBasePath = Path.Combine(AppContext.BaseDirectory, fontFileName);
            if (File.Exists(applicationBasePath))
            {
                return applicationBasePath;
            }

            throw new FileNotFoundException($"Could not locate font file '{fontFileName}'.", fontFileName);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FontRenderer));
            }
        }
    }
}
