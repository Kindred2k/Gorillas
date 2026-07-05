using System;
using System.IO;
using System.Runtime.InteropServices;
using StbTrueTypeSharp;

unsafe class Program
{
    static void Main()
    {
        var fontPath = "C:\\repos\\Gorillas\\Px437_IBM_VGA_8x16.ttf";
        Console.WriteLine(fontPath);
        var bytes = File.ReadAllBytes(fontPath);
        var fontInfo = new StbTrueType.stbtt_fontinfo();
        byte* data = (byte*)Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, (IntPtr)data, bytes.Length);
        int ok = StbTrueType.stbtt_InitFont(fontInfo, data, 0);
        Console.WriteLine($"init={ok}");
        float scale = StbTrueType.stbtt_ScaleForPixelHeight(fontInfo, 16f);
        Console.WriteLine($"scale={scale}");
        int ascent = 0, descent = 0, lineGap = 0;
        StbTrueType.stbtt_GetFontVMetrics(fontInfo, &ascent, &descent, &lineGap);
        Console.WriteLine($"metrics={ascent},{descent},{lineGap}");
        for (char c = 'A'; c <= 'Z'; c++)
        {
            int advance = 0, lsb = 0;
            int ix1 = 0, iy1 = 0, ix2 = 0, iy2 = 0;
            StbTrueType.stbtt_GetCodepointHMetrics(fontInfo, c, &advance, &lsb);
            StbTrueType.stbtt_GetCodepointBitmapBox(fontInfo, c, scale, scale, &ix1, &iy1, &ix2, &iy2);
            Console.WriteLine($"{c}: adv={advance}, lsb={lsb}, box=({ix1},{iy1})-({ix2},{iy2})");
        }
        Marshal.FreeHGlobal((IntPtr)data);
    }
}
