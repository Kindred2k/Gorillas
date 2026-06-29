using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Gorillas.Engine;

namespace Gorillas.Game
{
	internal class Gorilla
	{
		private byte[] _pixelBuffer;

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
		public Gorilla(byte[] pixelBuffer, int scrWidth, int scrHeight, int mode)
		{
			this._pixelBuffer = pixelBuffer;
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
			// set position of sun
			int x = ScrWidth / 2,
				y = Scl(25);

			// clear old sun
			Draw.DrawFilledRectangleOnCpuBuffer(
				_pixelBuffer,
				ScrWidth,
				ScrHeight,
				x - Scl(22),
				y - Scl(18),
				x + Scl(22),
				y + Scl(18),
				0, 0, 0, 255);


			// draw new sun:

			// body

			Circle(x, y), Scl(12), SUNATTR

			Paint(x, y), SUNATTR


			'rays

			Line(x - Scl(20), y) - (x + Scl(20), y), SUNATTR

			Line(x, y - Scl(15)) - (x, y + Scl(15)), SUNATTR


			Line(x - Scl(15), y - Scl(10)) - (x + Scl(15), y + Scl(10)), SUNATTR

			Line(x - Scl(15), y + Scl(10)) - (x + Scl(15), y - Scl(10)), SUNATTR


			Line(x - Scl(8), y - Scl(13)) - (x + Scl(8), y + Scl(13)), SUNATTR

			Line(x - Scl(8), y + Scl(13)) - (x + Scl(8), y - Scl(13)), SUNATTR


			Line(x - Scl(18), y - Scl(5)) - (x + Scl(18), y + Scl(5)), SUNATTR

			Line(x - Scl(18), y + Scl(5)) - (x + Scl(18), y - Scl(5)), SUNATTR


			'mouth

			If Mouth Then 'draw "o" mouth

				Circle(x, y + Scl(5)), Scl(2.9), 0

				Paint(x, y + Scl(5)), 0, 0

			Else 'draw smile

				Circle(x, y), Scl(8), 0, (210 * pi# / 180), (330 * pi# / 180)
			End If


			'eyes

			Circle(x - 3, y - 2), 1, 0

			Circle(x + 3, y - 2), 1, 0

			PSet(x - 3, y - 2), 0

			PSet(x + 3, y - 2), 0

		}


		DECLARE SUB SetScreen()
DECLARE SUB EndGame()
DECLARE SUB Center(Row, Text$)
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

		DECLARE FUNCTION GetNum# (Row, Col)
DECLARE FUNCTION DoShot(PlayerNum, x, y)
DECLARE FUNCTION ExplodeGorilla(x#, y#)
DECLARE FUNCTION Getn# (Row, Col)
DECLARE FUNCTION PlotShot (StartX, StartY, Angle#, Velocity, PlayerNum)
DECLARE FUNCTION CalcDelay! ()



		private int FnRan(int x)
		{
			return new Random().Next(1, x);
		}
	}
}
