using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Gorillas.Engine
{
	/// <summary>
	/// Provides methods for drawing shapes and graphics on a CPU pixel buffer.
	/// </summary>
	internal static class Draw
	{
		/// <summary>
		/// Draws a rectangle on a CPU pixel buffer with specified color and alpha values.
		/// </summary>
		/// <param name="frameBuffer"></param>
		/// <param name="bufferWidth"></param>
		/// <param name="bufferHeight"></param>
		/// <param name="rectX"></param>
		/// <param name="rectY"></param>
		/// <param name="rectWidth"></param>
		/// <param name="rectHeight"></param>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		/// <param name="a"></param>
		public static void DrawFilledRectangle(byte[] frameBuffer, int bufferWidth, int bufferHeight, int rectX, int rectY, int rectWidth, int rectHeight, byte r, byte g, byte b, byte a)
		{
			// Clamp rectangle coordinates to avoid IndexOutOfRangeException
			int startX = Math.Max(0, rectX);
			int startY = Math.Max(0, rectY);
			int endX = Math.Min(bufferWidth, rectX + rectWidth);
			int endY = Math.Min(bufferHeight, rectY + rectHeight);

			for (int y = startY; y < endY; y++)
			{
				// Cache row index multiplier to optimize arithmetic inside the inner loop
				int rowOffset = y * bufferWidth;

				for (int x = startX; x < endX; x++)
				{
					// Calculate 1D array index for 4-channel pixel (RGBA/BGRA)
					int index = (rowOffset + x) * 4;

					// Adjust order (e.g., r, g, b, a) based on your specific format
					frameBuffer[index] = b; // Blue
					frameBuffer[index + 1] = g; // Green
					frameBuffer[index + 2] = r; // Red
					frameBuffer[index + 3] = a; // Alpha
				}
			}
		}

		public static void DrawFilledCircle(byte[] frameBuffer, int bufferWidth, int bufferHeight, int xc, int yc, int radius, byte r, byte g, byte b, byte a)
		{
			int x = 0;
			int y = radius;
			int d = 3 - 2 * radius;

			while (y >= x)
			{
				// Draw the circle in all 8 octants
				PlotCirclePoints(frameBuffer, bufferWidth, bufferHeight, xc, yc, x, y, r, g, b, a);
				PlotCirclePoints(frameBuffer, bufferWidth, bufferHeight, xc, yc, y, x, r, g, b, a);

				if (d <= 0)
				{
					d = d + 4 * x + 6;
				}
				else
				{
					d = d + 4 * (x - y) + 10;
					y--;
				}

				x++;
			}
		}
		
		private static void PlotCirclePoints(byte[] frameBuffer, int bufferWidth, int bufferHeight, int xc, int yc, int x, int y, byte r, byte g, byte b, byte a)
		{
			// Array of 8 symmetric points
			int[] px = { xc + x, xc - x, xc + x, xc - x, xc + y, xc - y, xc + y, xc - y };
			int[] py = { yc + y, yc + y, yc - y, yc - y, xc + x, xc + x, xc - x, xc - x };

			for (int i = 0; i < 8; i++)
			{
				// Boundary check
				if (px[i] >= 0 && px[i] < bufferWidth && py[i] >= 0 && py[i] < bufferHeight)
				{
					// Calculate byte index for RGBA
					int index = (py[i] * bufferWidth + px[i]) * 4;
					frameBuffer[index] = b;     // Red
					frameBuffer[index + 1] = g; // Green
					frameBuffer[index + 2] = r; // Blue
					frameBuffer[index + 3] = a; // Alpha
				}
			}
		}

		public static void DrawPixel(byte[] buffer, int x, int y, int bufferWidth, int bufferHeight, byte r, byte g, byte b, byte a)
		{
			if (x >= 0 && x < bufferWidth && y >= 0 && y < bufferHeight)
			{
				int index = (y * bufferWidth + x) * 4;
				buffer[index] = b;
				buffer[index + 1] = g;
				buffer[index + 2] = r;
				buffer[index + 3] = a;
			}
		}

		public static void DrawLine(byte[] buffer, int x0, int y0, int x1, int y1, int width, int height, byte r, byte g, byte b, byte a)
		{
			int dx = Math.Abs(x1 - x0);
			int dy = Math.Abs(y1 - y0);
			int sx = (x0 < x1) ? 1 : -1;
			int sy = (y0 < y1) ? 1 : -1;
			int err = dx - dy;

			while (true)
			{
				DrawPixel(buffer, x0, y0, width, height, r, g, b, a);

				if (x0 == x1 && y0 == y1)
					break;

				int e2 = 2 * err;

				if (e2 > -dy)
				{
					err -= dy;
					x0 += sx;
				}

				if (e2 < dx)
				{
					err += dx;
					y0 += sy;
				}
			}
		}

		public static void DrawArc(byte[] buffer, int bufferWidth, int bufferHeight, int centerX, int centerY, int radius, float startRadian, float endRadian, byte r, byte g, byte b, byte a)
		{
			// 1. Calculate arc length to determine how many discrete pixel steps we need
			float arcAngle = Math.Abs(endRadian - startRadian);
			float arcLength = radius * arcAngle;

			// Ensure we step at least once per pixel along the curve
			int steps = Math.Max(10, (int)Math.Ceiling(arcLength * 1.5f));

			// 2. Plot the points
			for (int i = 0; i <= steps; i++)
			{
				float t = (float)i / steps;
				float angle = startRadian + (endRadian - startRadian) * t;

				// Round to nearest pixel coordinate
				int x = (int)Math.Round(centerX + radius * Math.Cos(angle));
				int y = (int)Math.Round(centerY + radius * Math.Sin(angle));

				// 3. Bounds checking (prevent writing outside the array memory)
				if (x >= 0 && x < bufferWidth && y >= 0 && y < bufferHeight)
				{
					// Map 2D coordinates to the 1D flat pixel array
					int index = y * bufferWidth + x;
					buffer[index] = b;
					buffer[index + 1] = g;
					buffer[index + 2] = r;
					buffer[index + 3] = a;
				}
			}
		}

		public static void FillBuffer(byte[] framebuffer, byte r, byte g, byte b, byte a)
		{
			ReadOnlySpan<uint> readOnlyPixelSpan = MemoryMarshal.Cast<byte, uint>(framebuffer);
			ref uint reference = ref MemoryMarshal.GetReference(readOnlyPixelSpan);
			Span<uint> pixelSpan = MemoryMarshal.CreateSpan(ref reference, readOnlyPixelSpan.Length);

			uint color = (uint)(r | (g << 8) | (b << 16) | (a << 24));
			pixelSpan.Fill(color);
		}
	}
}
