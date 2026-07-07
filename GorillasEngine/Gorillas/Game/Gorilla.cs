using Gorillas.Engine;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gorillas.Game
{
	internal class Gorilla
	{
		private QBasic _qBasic;

		private byte[] _pixelBuffer;
		private Dictionary<int, int> Palettes = new Dictionary<int, int>();

		// Constants
		public const int SPEEDCONST = 500;
		public const bool TRUE = false;     // omg
		public const bool FALSE = !TRUE;    // yeah, this is weird
		public const int HITSELF = 1;
		public const int BACKATTR = 0;
		public const int OBJECTCOLOR = 1;
		public const int WINDOWCOLOR = 14;
		public const int SUNATTR = 3;
		public const bool SUNHAPPY = FALSE;
		public const bool SUNSHOCK = TRUE;
		public const int RIGHTUP = 1;
		public const int LEFTUP = 2;
		public const int ARMSDOWN = 3;

		// Global Variables
		private int[] GorillaX = new int[2]; // X coordinate of the two gorillas
		private int[] GorillaY = new int[2]; // Y coordinate of the two gorillas
		private int LastBuilding;

		private double pi = Math.Atan(1) * 4; // pi constant
		private long[] LBan, RBan, UBan, DBan; // Graphical picture of banana
		private long[] GorD, GorL, GorR; // Graphical pictures of Gorilla arms

		private double gravity;
		private int Wind; // TODO: determine if this sould be a double

		// Screen Mode Variables
		private int ScrHeight;
		private int ScrWidth;
		private int Mode;
		private int MaxCol;

		// Screen Color Variables
		private int ExplosionColor;
		private int SunColor;
		private int BackColor;
		private int SunHit;

		private int SunHt; // Height of Sun
		private int GHeight;
		private float MachSpeed; // "Single-precision" 32-bit float

		/// <summary>
		/// Initializes a new instance of the Gorilla class with the specified pixel buffer, screen width, screen height, and mode.
		/// </summary>
		/// <param name="pixelBuffer"></param>
		/// <param name="scrWidth"></param>
		/// <param name="scrHeight"></param>
		/// <param name="mode"></param>
		public Gorilla(byte[] pixelBuffer, int scrWidth, int scrHeight, int mode, QBasic qBasic)
		{
			this._pixelBuffer = pixelBuffer;
			this._qBasic = qBasic;
			ScrWidth = scrWidth;
			ScrHeight = scrHeight;
			Mode = mode;
			// Set screen color variables
			ExplosionColor = 12; // Red
			SunColor = 14; // Yellow
			BackColor = 0; // Black
			SunHit = 4; // Blue
						// Set other variables
			SunHt = Scl(25);
			GHeight = Scl(20);
			MachSpeed = 343.0f; // Speed of sound in m/s
		}

		/// <summary>
		/// Draws the sun on the screen with optional mouth expression.
		/// </summary>
		/// <param name="mouth">If true, draws a shocked "o" mouth; otherwise, draws a smile.</param>
		public void DoSun(bool mouth)
		{
			// Set position of sun
			int x = ScrWidth / 2,
				y = Scl(25);

			// Clear old sun
			// TODO: We will likely clear the entire framebuffer between frames instead of doing this
			Draw.DrawFilledRectangle(
				_pixelBuffer,
				ScrWidth,
				ScrHeight,
				x - Scl(22),
				y - Scl(18),
				x + Scl(22),
				y + Scl(18),
				0, 0, 0, 255);

			// DRAW NEW SUN:

			// body
			Draw.DrawFilledCircle(
				_pixelBuffer, // RGBA frame buffer
				ScrWidth, ScrHeight, // Width & Height of frame buffer
				x, y, // Position
				Scl(12), // Radius
				255, 255, 85, 255); // Yellow color

			// rays
			Draw.DrawLine(_pixelBuffer, x - Scl(20), y, x + Scl(20), y, ScrWidth, ScrHeight, 255, 255, 85, 255);
			Draw.DrawLine(_pixelBuffer, x, y - Scl(15), x, y + Scl(15), ScrWidth, ScrHeight, 255, 255, 85, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(15), y - Scl(10), x + Scl(15), y + Scl(10), ScrWidth, ScrHeight, 255, 255, 85, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(15), y + Scl(10), x + Scl(15), y - Scl(10), ScrWidth, ScrHeight, 255, 255, 85, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(8), y - Scl(13), x + Scl(8), y + Scl(13), ScrWidth, ScrHeight, 255, 255, 85, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(8), y + Scl(13), x + Scl(8), y - Scl(13), ScrWidth, ScrHeight, 255, 255, 85, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(18), y - Scl(5), x + Scl(18), y + Scl(5), ScrWidth, ScrHeight, 255, 255, 85, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(18), y + Scl(5), x + Scl(18), y - Scl(5), ScrWidth, ScrHeight, 255, 255, 85, 255);

			// mouth
			if (mouth)
			{
				// draw "o" mouth
				Draw.DrawFilledCircle(_pixelBuffer, ScrWidth, ScrHeight, x, y + Scl(5), Scl(2.9f), 0, 0, 0, 255);
			}
			else
			{
				// draw smile
				Draw.DrawArc(_pixelBuffer, ScrWidth, ScrHeight, x, y, Scl(8), Convert.ToSingle(210 * pi / 180), Convert.ToSingle(330 * pi / 180), 0, 0, 0, 255);
			}

			// eyes
			Draw.DrawFilledCircle(_pixelBuffer, ScrWidth, ScrHeight, x - 3, y - 2, 1, 0, 0, 0, 255);
			Draw.DrawFilledCircle(_pixelBuffer, ScrWidth, ScrHeight, x + 3, y - 2, 1, 0, 0, 0, 255);
			Draw.DrawPixel(_pixelBuffer, x - 3, y - 2, ScrWidth, ScrHeight, 0, 0, 0, 255);
			Draw.DrawPixel(_pixelBuffer, x + 3, y - 2, ScrWidth, ScrHeight, 0, 0, 0, 255);
		}

		/// <summary>
		/// Sets the screen colors and palettes based on the current mode. If the mode is 9, it sets specific colors for explosion, background, and various palette entries. Otherwise, it sets a blank screen with a black background.
		/// </summary>
		public void SetScreen()
		{
			if (Mode == 9)
			{
				this.ExplosionColor = 2;
				this.BackColor = 1;
				Palettes.Add(0, 1);
				Palettes.Add(1, 46);
				Palettes.Add(2, 44);
				Palettes.Add(3, 54);
				Palettes.Add(5, 7);
				Palettes.Add(6, 4);
				Palettes.Add(7, 3);

				// Display Color
				// TODO: Determine what "Display Color" means in this context. It may refer to setting the color palette for the display, but without more information, it's unclear.
				Palettes.Add(9, 63);
			}
			else
			{
				this.ExplosionColor = 2;
				this.BackColor = 0;

				// Blank screen
				Draw.FillBuffer(_pixelBuffer, 0, 0, 0, 255);
			}
		}

		/// <summary>
		/// Centers the given text on the specified row of the screen. The text is printed starting from a calculated column position that centers it horizontally.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="text"></param>
		private void CENTER(int row, string text)
		{
			int Col = MaxCol / 2;
			_qBasic.LOCATE(row, Convert.ToInt32(Col - (text.Length / 2 + 0.5)));
			_qBasic.PRINT(text);
		}

		[Obsolete("This method was never implemented by Microsoft in GORILLA.BAS.")]
		public void ENDGAME()
		{
		}

		/// <summary>
		/// Displays the game introduction, including the title, copyright information, and instructions for the player. It also plays a short sound sequence and waits for user input before proceeding. The screen is cleared and set to the appropriate mode and colors.
		/// </summary>
		private void Intro()
		{
			/*
			SCREEN 0
			WIDTH 80, 25
			MaxCol = 80
			COLOR 15, 0
			CLS
			*/

			CENTER(4, "Q B a s i c    G O R I L L A S");

			_qBasic.COLOR(7);
			CENTER(6, "Copyright (C) Microsoft Corporation 1990");
			CENTER(8, "Your mission is to hit your opponent with the exploding");
			CENTER(9, "banana by varying the angle and power of your throw, taking");
			CENTER(10, "into account wind speed, gravity, and the city skyline.");
			CENTER(11, "The wind speed is shown by a directional arrow at the bottom");
			CENTER(12, "of the playing field, its length relative to its strength.");
			CENTER(24, "Press any key to continue");

			/*
			PLAY "MBT160O1L8CDEDCDL4ECC"
			SparklePause
			IF Mode = 1 THEN MaxCol = 40
			*/
		}

		public void DrawGorilla(int x, int y, int arms)
		{
			// draw head
			_qBasic.LINE(x - Scl(4), y, x + Scl(2.9f), y + Scl(6), OBJECTCOLOR, QBasic.LineBoxStyle.BF);
			_qBasic.LINE(x - Scl(5), y + Scl(2), x + Scl(4), y + Scl(4), OBJECTCOLOR, QBasic.LineBoxStyle.BF);

			// draw eyes/brow
			_qBasic.LINE(x - Scl(3), y + Scl(2), x + Scl(2), y + Scl(2), 0, QBasic.LineBoxStyle.BF);

			// draw nose if ega
			if (Mode == 9)
			{
				for (int i = -2; i <= -1; i++)
				{
					_qBasic.PSET(x + i, y + 4, 0);
					_qBasic.PSET(x + i + 3, y + 4, 0);
				}
			}

			// neck
			_qBasic.LINE(x - Scl(3), y + Scl(7), x + Scl(2), y + Scl(7), OBJECTCOLOR);

			// body
			_qBasic.LINE(x - Scl(8), y + Scl(8), x + Scl(6.9f), y + Scl(14), OBJECTCOLOR, QBasic.LineBoxStyle.BF);
			_qBasic.LINE(x - Scl(6), y + Scl(15), x + Scl(4.9f), y + Scl(20), OBJECTCOLOR, QBasic.LineBoxStyle.BF);

			// legs
			for (int i = 0; i <= 4; i++)
			{
				_qBasic.CIRCLE(x + Scl(i), y + Scl(25), Scl(10), OBJECTCOLOR, 3 * pi / 4, 9 * pi / 8);
				_qBasic.CIRCLE(x + Scl(-6) + Scl(i - .1f), y + Scl(25), Scl(10), OBJECTCOLOR, 15 * pi / 8, pi / 4);
			}
			NEXT

			// 'chest
			// CIRCLE (x - Scl(4.9), y + Scl(10)), Scl(4.9), 0, 3 * pi# / 2, 0
			// CIRCLE (x + Scl(4.9), y + Scl(10)), Scl(4.9), 0, pi#, 3 * pi# / 2

			// FOR i = -5 TO -1
			// 	SELECT CASE arms
			// 	CASE 1
			// 		'Right arm up
			// 		CIRCLE (x + Scl(i - .1), y + Scl(14)), Scl(9), OBJECTCOLOR, 3 * pi# / 4, 5 * pi# / 4
			// 		CIRCLE (x + Scl(4.9) + Scl(i), y + Scl(4)), Scl(9), OBJECTCOLOR, 7 * pi# / 4, pi# / 4
			// 		GET (x - Scl(15), y - Scl(1))-(x + Scl(14), y + Scl(28)), GorR&
			// 	CASE 2
			// 		'Left arm up
			// 		CIRCLE (x + Scl(i - .1), y + Scl(4)), Scl(9), OBJECTCOLOR, 3 * pi# / 4, 5 * pi# / 4
			// 		CIRCLE (x + Scl(4.9) + Scl(i), y + Scl(14)), Scl(9), OBJECTCOLOR, 7 * pi# / 4, pi# / 4
			// 		GET (x - Scl(15), y - Scl(1))-(x + Scl(14), y + Scl(28)), GorL&
			// 	CASE 3
			// 		'Both arms down
			// 		CIRCLE (x + Scl(i - .1), y + Scl(14)), Scl(9), OBJECTCOLOR, 3 * pi# / 4, 5 * pi# / 4
			// 		CIRCLE (x + Scl(4.9) + Scl(i), y + Scl(14)), Scl(9), OBJECTCOLOR, 7 * pi# / 4, pi# / 4
			// 		GET (x - Scl(15), y - Scl(1))-(x + Scl(14), y + Scl(28)), GorD&
			// 	END SELECT
			// NEXT i
		}


		/*
		DECLARE SUB Intro()
		DECLARE SUB SparklePause()
		DECLARE SUB GetInputs(Player1$, Player2$, NumGames)
		DECLARE SUB PlayGame(Player1$, Player2$, NumGames)
		DECLARE SUB DoExplosion(x#, y#)
		DECLARE SUB MakeCityScape (BCoor() AS ANY)
		DECLARE SUB PlaceGorillas(BCoor() AS ANY)
		DECLARE SUB UpdateScores(Record(), PlayerNum, Results)
		DECLARE SUB DrawGorilla(x, y, arms)
		DECLARE SUB GorillaIntro(Player1$, Player2$)
		DECLARE SUB Rest(t#)
		DECLARE SUB VictoryDance (Player)
		DECLARE SUB ClearGorillas ()
		DECLARE SUB DrawBan (xc#, yc#, r, bc)
		DECLARE FUNCTION GetNum# (Row, Col)
		DECLARE FUNCTION DoShot(PlayerNum, x, y)
		DECLARE FUNCTION ExplodeGorilla(x#, y#)
		DECLARE FUNCTION Getn# (Row, Col)
		DECLARE FUNCTION PlotShot (StartX, StartY, Angle#, Velocity, PlayerNum)
		DECLARE FUNCTION CalcDelay! ()
		*/

		/// <summary>
		/// Scales the given float value to an integer based on the current screen mode.
		/// </summary>
		/// <param name="n">The float value to scale.</param>
		/// <returns>The scaled integer value.</returns>
		private int Scl (float n)
		{
			if (n != Convert.ToInt32(n))
			{
				if (Mode == 1)
					n = n - 1;
			}

			if (Mode == 1)
				return Convert.ToInt32(n / 2 + .1);
			else
				return Convert.ToInt32(n);
		}

		private int FnRan(int x)
		{
			return new Random().Next(1, x);
		}
	}
}
