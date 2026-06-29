using System;
using System.Collections.Generic;
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
		/// <param name="pixelBuffer"></param>
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
		public static void DrawFilledRectangleOnCpuBuffer(byte[] pixelBuffer, int bufferWidth, int bufferHeight, int rectX, int rectY, int rectWidth, int rectHeight, byte r, byte g, byte b, byte a)
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
					pixelBuffer[index] = b; // Blue
					pixelBuffer[index + 1] = g; // Green
					pixelBuffer[index + 2] = r; // Red
					pixelBuffer[index + 3] = a; // Alpha
				}
			}
		}
	}
}
