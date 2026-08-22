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

		private double pi = Math.PI; // pi constant
		private byte[]? LBan, RBan, UBan, DBan; // Graphical picture of banana
		private byte[]? GorD, GorL, GorR; // Graphical pictures of Gorilla arms

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
		private XYPoint[] BuildingCoordinates = new XYPoint[31];
		private int[] TotalWins = new int[2];

		private struct XYPoint
		{
			public int XCoor;
			public int YCoor;
		}

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
				ExplosionColor = 2;
				BackColor = 1;
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
				ExplosionColor = 2;
				BackColor = 0;

				// Blank screen
				_qBasic.CLS();
			}
		}

		/// <summary>
		/// Centers the given text on the specified row of the screen. The text is printed starting from a calculated column position that centers it horizontally.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="text"></param>
		private void Center(int row, string text)
		{
			int Col = MaxCol / 2;
			_qBasic.LOCATE(row, Convert.ToInt32(Col - (text.Length / 2 + 0.5)));
			_qBasic.PRINT(text);
		}

		[Obsolete("This method was never implemented by Microsoft in GORILLA.BAS.")]
		public void EndGame()
		{
		}

		/// <summary>
		/// Displays the game introduction, including the title, copyright information, and instructions for the player. It also plays a short sound sequence and waits for user input before proceeding. The screen is cleared and set to the appropriate mode and colors.
		/// </summary>
		public async Task Intro()
		{
			/*
			SCREEN 0
			WIDTH 80, 25
			MaxCol = 80
			COLOR 15, 0
			CLS
			*/

			_qBasic.SCREEN(0);
			MaxCol = 80;
			_qBasic.COLOR(15);
			_qBasic.CLS();

			Center(4, "Q B a s i c    G O R I L L A S");

			_qBasic.COLOR(7);
			Center(6, "Copyright (C) Microsoft Corporation 1990");
			Center(8, "Your mission is to hit your opponent with the exploding");
			Center(9, "banana by varying the angle and power of your throw, taking");
			Center(10, "into account wind speed, gravity, and the city skyline.");
			Center(11, "The wind speed is shown by a directional arrow at the bottom");
			Center(12, "of the playing field, its length relative to its strength.");
			Center(24, "Press any key to continue");

			_qBasic.PLAY(new[]
			{
				("C", 125L), ("D", 125L), ("E", 125L), ("D", 125L),
				("C", 125L), ("D", 125L), ("E", 250L), ("C", 250L)
			});
			await SparklePause();

			if (Mode == 1)
			{
				MaxCol = 40;
			}
		}

		private async Task SparklePause()
		{
			const string sparkle = "*    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    *    ";

			_qBasic.COLOR(4);
			_qBasic.ClearPendingKeys();

			while (true)
			{
				for (int a = 1; a <= 5; a++)
				{
					_qBasic.LOCATE(1, 1);
					_qBasic.PRINT(sparkle.Substring(a - 1, 80));
					_qBasic.LOCATE(22, 1);
					_qBasic.PRINT(sparkle.Substring(4 - a, 80));

					for (int b = 2; b <= 21; b++)
					{
						bool sparkleOn = (a + b) % 5 == 1;
						_qBasic.LOCATE(b, 80);
						_qBasic.PRINT(sparkleOn ? "*" : " ");
						_qBasic.LOCATE(23 - b, 1);
						_qBasic.PRINT(sparkleOn ? "*" : " ");
					}

					await Task.Delay(20);
					if (_qBasic.HasPendingKey)
					{
						await _qBasic.WAITKEY();
						return;
					}
				}
			}
		}

		public async Task<(string Player1, string Player2, int NumGames)> GetInputs()
		{
			_qBasic.COLOR(7);
			_qBasic.CLS();

			_qBasic.ClearPendingKeys();
			string player1 = await Utils.ReadLineInput(_qBasic, 8, 15, "Name of Player 1 (Default = 'Player 1'): ", "Player 1", 10);
			string player2 = await Utils.ReadLineInput(_qBasic, 10, 15, "Name of Player 2 (Default = 'Player 2'): ", "Player 2", 10);

			int numGames;
			while (true)
			{
				_qBasic.LOCATE(12, 56);
				_qBasic.PRINT(new string(' ', 25));
				string game = await Utils.ReadNumericInput(_qBasic, 12, 13, "Play to how many total points (Default = 3)", 2, false);
				if (game.Length == 0)
				{
					numGames = 3;
					break;
				}

				if (int.TryParse(game, out numGames) && numGames > 0)
				{
					break;
				}
			}

			while (true)
			{
				_qBasic.LOCATE(14, 53);
				_qBasic.PRINT(new string(' ', 28));
				string gravityInput = await Utils.ReadNumericInput(_qBasic, 14, 17, "Gravity in Meters/Sec (Earth = 9.8)", 28, true);
				if (gravityInput.Length == 0)
				{
					gravity = 9.8;
					break;
				}

				if (double.TryParse(gravityInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsedGravity) && parsedGravity > 0)
				{
					gravity = parsedGravity;
					break;
				}
			}

			return (player1, player2, numGames);
		}

		public void DrawGorilla(int x, int y, int arms)
		{
			byte[] temporaryFramebuffer = new byte[ScrWidth * ScrHeight * 4];
			_qBasic.SetPixelBuffer(temporaryFramebuffer, ScrWidth, ScrHeight);

			try
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
					_qBasic.CIRCLE(false, x + Scl(i), y + Scl(25), Scl(10), OBJECTCOLOR, Convert.ToSingle(3 * pi / 4), Convert.ToSingle(9 * pi / 8));
					_qBasic.CIRCLE(false, x + Scl(-6) + Scl(i - .1f), y + Scl(25), Scl(10), OBJECTCOLOR, Convert.ToSingle(15 * pi / 8), Convert.ToSingle(pi / 4));
				}

				// chest
				_qBasic.CIRCLE(false, x - Scl(4.9f), y + Scl(10), Scl(4.9f), 0, Convert.ToSingle(3 * pi / 2), 0);
				_qBasic.CIRCLE(false, x + Scl(4.9f), y + Scl(10), Scl(4.9f), 0, Convert.ToSingle(pi), Convert.ToSingle(3 * pi / 2));

				for (int i = -5; i <= -1; i++)
				{
					switch (arms)
					{
						case RIGHTUP:
							// Right arm up
							_qBasic.CIRCLE(false, x + Scl(i - .1f), y + Scl(14), Scl(9), OBJECTCOLOR, Convert.ToSingle(3 * pi / 4), Convert.ToSingle(5 * pi / 4));
							_qBasic.CIRCLE(false, x + Scl(4.9f) + Scl(i), y + Scl(4), Scl(9), OBJECTCOLOR, Convert.ToSingle(7 * pi / 4), Convert.ToSingle(pi / 4));
							break;
						case LEFTUP:
							// Left arm up
							_qBasic.CIRCLE(false, x + Scl(i - .1f), y + Scl(4), Scl(9), OBJECTCOLOR, Convert.ToSingle(3 * pi / 4), Convert.ToSingle(5 * pi / 4));
							_qBasic.CIRCLE(false, x + Scl(4.9f) + Scl(i), y + Scl(14), Scl(9), OBJECTCOLOR, Convert.ToSingle(7 * pi / 4), Convert.ToSingle(pi / 4));
							break;
						case ARMSDOWN:
							// Both arms down
							_qBasic.CIRCLE(false, x + Scl(i - .1f), y + Scl(14), Scl(9), OBJECTCOLOR, Convert.ToSingle(3 * pi / 4), Convert.ToSingle(5 * pi / 4));
							_qBasic.CIRCLE(false, x + Scl(4.9f) + Scl(i), y + Scl(14), Scl(9), OBJECTCOLOR, Convert.ToSingle(7 * pi / 4), Convert.ToSingle(pi / 4));
							break;
					}
				}
			}
			finally
			{
				_qBasic.SetPixelBuffer(_pixelBuffer, ScrWidth, ScrHeight);
			}

			switch (arms)
			{
				case RIGHTUP:
					GorR = temporaryFramebuffer;
					break;
				case LEFTUP:
					GorL = temporaryFramebuffer;
					break;
				case ARMSDOWN:
					GorD = temporaryFramebuffer;
					break;
			}
		}

		/// <summary>
		/// Displays gorillas on screen for the first time and allows the graphical data to be put into an array.
		/// </summary>
		/// <param name="player1">The name of player 1.</param>
		/// <param name="player2">The name of player 2.</param>
		public async Task GorillaIntro(string player1, string player2)
		{
			_qBasic.LOCATE(16, 34);
			_qBasic.PRINT("--------------");
			_qBasic.LOCATE(18, 34);
			_qBasic.PRINT("V = View Intro");
			_qBasic.LOCATE(19, 34);
			_qBasic.PRINT("P = Play Game");
			_qBasic.LOCATE(21, 35);
			_qBasic.PRINT("Your Choice?");

			// Accept input from user to either view the intro or play the game
			char? input = await _qBasic.WAITKEY();

			_qBasic.SCREEN(Mode);
			SetScreen();

			if (Mode == 1)
			{
				Center(5, "Please wait while gorillas are drawn.");
			}

			_qBasic.VIEW(9, 24);

			if (Mode == 9)
			{
				_qBasic.PALETTE(OBJECTCOLOR, BackColor);
			}

			int x = Mode == 1 ? 125 : 278;
			int y = Mode == 1 ? 100 : 175;

			DrawGorilla(x, y, ARMSDOWN);
				_qBasic.CLS();
			DrawGorilla(x, y, LEFTUP);
				_qBasic.CLS();
			DrawGorilla(x, y, RIGHTUP);
				_qBasic.CLS();

			_qBasic.VIEW(1, 25);
			if (Mode == 9)
			{
				_qBasic.PALETTE(OBJECTCOLOR, 46);
			}

			if (char.ToUpperInvariant(input ?? '\0') == 'V')
			{
				Center(2, "Q B A S I C   G O R I L L A S");
				Center(5, "             STARRING:               ");
				Center(7, $"{player1} AND {player2}");

				_qBasic.PUT(GorD, x, y, x - 13, y);
				_qBasic.PUT(GorD, x, y, x + 47, y);
				await Rest(1000);

				_qBasic.PUT(GorL, x, y, x - 13, y);
				_qBasic.PUT(GorR, x, y, x + 47, y);
				_qBasic.PLAY(new[] { ("B", 150L), ("B", 75L), ("A", 75L), ("A", 75L), ("B", 75L) });
				await Rest(300);

				_qBasic.PUT(GorR, x, y, x - 13, y);
				_qBasic.PUT(GorL, x, y, x + 47, y);
				_qBasic.PLAY(new[] { ("E", 150L), ("D", 75L), ("D", 75L), ("E", 75L), ("E", 75L), ("D", 75L) });
				await Rest(300);

				_qBasic.PUT(GorL, x, y, x - 13, y);
				_qBasic.PUT(GorR, x, y, x + 47, y);
				_qBasic.PLAY(new[] { ("G", 150L), ("E", 75L), ("E", 75L), ("G", 75L), ("G", 75L), ("E", 75L) });
				await Rest(300);

				_qBasic.PUT(GorR, x, y, x - 13, y);
				_qBasic.PUT(GorL, x, y, x + 47, y);
				_qBasic.PLAY(new[] { ("B", 150L), ("B", 75L), ("A", 75L), ("G", 75L), ("B", 150L) });
				await Rest(300);

				for (int i = 1; i <= 4; i++)
				{
					_qBasic.PUT(GorL, x, y, x - 13, y);
					_qBasic.PUT(GorR, x, y, x + 47, y);
					_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
					await Rest(100);

					_qBasic.PUT(GorR, x, y, x - 13, y);
					_qBasic.PUT(GorL, x, y, x + 47, y);
					_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
					await Rest(100);
				}
			}
		}

		private async Task Rest(int milliseconds)
		{
			await Task.Delay(milliseconds);
		}

		private float CalcDelay()
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			float count = 0;
			while (stopwatch.Elapsed.TotalSeconds < .5)
			{
				count++;
			}
			return count;
		}

		private void UpdateScores(int playerNum, int results)
		{
			if (results == HITSELF)
			{
				TotalWins[2 - playerNum]++;
			}
			else
			{
				TotalWins[playerNum - 1]++;
			}
		}

		private void ClearGorillas()
		{
			GorD = null;
			GorL = null;
			GorR = null;
		}

		private void DrawBan(double x, double y, int rotation, bool draw)
		{
			byte[]? banana = rotation switch
			{
				0 => LBan,
				1 => UBan,
				2 => DBan,
				3 => RBan,
				_ => null
			};
			if (banana != null)
			{
				_qBasic.PUT(banana, (int)x, (int)y, (int)x, (int)y);
			}
		}

		private async Task VictoryDance(int player)
		{
			for (int i = 1; i <= 4; i++)
			{
				_qBasic.PUT(GorL, GorillaX[player - 1], GorillaY[player - 1], GorillaX[player - 1], GorillaY[player - 1]);
				_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
				await Rest(200);
				_qBasic.PUT(GorR, GorillaX[player - 1], GorillaY[player - 1], GorillaX[player - 1], GorillaY[player - 1]);
				_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
				await Rest(200);
			}
		}

		private async Task DoExplosion(double x, double y)
		{
			_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
			int radius = ScrHeight / 50;
			float increment = Mode == 9 ? .5f : .41f;
			for (float circleRadius = 0; circleRadius <= radius; circleRadius += increment)
			{
				_qBasic.CIRCLE(false, (int)x, (int)y, (int)circleRadius, ExplosionColor);
			}
			for (float circleRadius = radius; circleRadius >= 0; circleRadius -= increment)
			{
				_qBasic.CIRCLE(false, (int)x, (int)y, (int)circleRadius, BACKATTR);
				await Rest(5);
			}
		}

		private void MakeCityScape(XYPoint[] buildingCoordinates)
		{
			int x = 2;
			int slope = FnRan(6);
			int newHeight = slope == 2 || slope == 6 ? 130 : 15;
			int bottomLine = Mode == 9 ? 335 : 190;
			int heightIncrement = Mode == 9 ? 10 : 6;
			int defaultWidth = Mode == 9 ? 37 : 18;
			int randomHeight = Mode == 9 ? 120 : 54;
			int windowWidth = Mode == 9 ? 3 : 1;
			int windowHeight = Mode == 9 ? 6 : 2;
			int verticalSpacing = Mode == 9 ? 15 : 5;
			int horizontalSpacing = Mode == 9 ? 10 : 4;
			int currentBuilding = 1;

			while (x <= ScrWidth - heightIncrement && currentBuilding < buildingCoordinates.Length - 1)
			{
				if (slope == 1) newHeight += heightIncrement;
				else if (slope == 2) newHeight -= heightIncrement;
				else if (slope >= 3 && slope <= 5) newHeight += x > ScrWidth / 2 ? -2 * heightIncrement : 2 * heightIncrement;

				int width = Math.Min(FnRan(defaultWidth) + defaultWidth, ScrWidth - x - 2);
				int buildingHeight = Math.Max(FnRan(randomHeight) + newHeight, heightIncrement);
				buildingCoordinates[currentBuilding] = new XYPoint { XCoor = x, YCoor = bottomLine - buildingHeight };

				_qBasic.COLOR(Mode == 9 ? FnRan(3) + 4 : 2);
				_qBasic.LINE(x, bottomLine, x + width, bottomLine - buildingHeight, 0, QBasic.LineBoxStyle.BF);
				for (int windowX = x + 3; windowX < x + width - 3; windowX += horizontalSpacing)
				{
					for (int windowY = buildingHeight - 3; windowY >= 7; windowY -= verticalSpacing)
					{
						_qBasic.COLOR(Mode == 9 && FnRan(4) != 1 ? WINDOWCOLOR : 8);
						_qBasic.LINE(windowX, bottomLine - windowY, windowX + windowWidth, bottomLine - windowY + windowHeight, 0, QBasic.LineBoxStyle.BF);
					}
				}
				x += width + 2;
				currentBuilding++;
			}

			LastBuilding = currentBuilding - 1;
			Wind = FnRan(10) - 5;
			if (FnRan(3) == 1) Wind += Wind > 0 ? FnRan(10) : -FnRan(10);
			if (Wind != 0)
			{
				int windLine = Wind * 3 * (ScrWidth / 320);
				_qBasic.COLOR(ExplosionColor);
				_qBasic.LINE(ScrWidth / 2, ScrHeight - 5, ScrWidth / 2 + windLine, ScrHeight - 5, 0);
			}
		}

		private void PlaceGorillas(XYPoint[] buildingCoordinates)
		{
			int xAdjustment = Mode == 9 ? 14 : 7;
			int yAdjustment = Mode == 9 ? 30 : 16;
			for (int i = 0; i < 2; i++)
			{
				int buildingNumber = i == 0 ? FnRan(2) + 1 : LastBuilding - FnRan(2);
				int buildingWidth = buildingCoordinates[buildingNumber + 1].XCoor - buildingCoordinates[buildingNumber].XCoor;
				GorillaX[i] = buildingCoordinates[buildingNumber].XCoor + buildingWidth / 2 - xAdjustment;
				GorillaY[i] = buildingCoordinates[buildingNumber].YCoor - yAdjustment;
				_qBasic.PUT(GorD, GorillaX[i], GorillaY[i], GorillaX[i], GorillaY[i]);
			}
		}

		private async Task<double> GetNum(int row, int column)
		{
			while (true)
			{
				string result = await Utils.ReadNumericInput(_qBasic, row, column, string.Empty, 12, true);
				if (result.Length == 0)
				{
					return 0;
				}
				if (double.TryParse(result, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value) && value <= 360)
				{
					return value;
				}
			}
		}

		private Task<double> Getn(int row, int column)
		{
			return GetNum(row, column);
		}

		private async Task<bool> DoShot(int playerNum, int x, int y)
		{
			int locateColumn = playerNum == 1 ? 1 : (Mode == 9 ? 66 : 26);
			_qBasic.LOCATE(2, locateColumn);
			_qBasic.PRINT("Angle:");
			double angle = await GetNum(2, locateColumn + 7);
			_qBasic.LOCATE(3, locateColumn);
			_qBasic.PRINT("Velocity:");
			double velocity = await GetNum(3, locateColumn + 10);
			if (playerNum == 2) angle = 180 - angle;

			for (int row = 1; row <= 4; row++)
			{
				_qBasic.LOCATE(row, 1);
				_qBasic.PRINT(new string(' ', 30 / (80 / MaxCol)));
				_qBasic.LOCATE(row, 50 / (80 / MaxCol));
				_qBasic.PRINT(new string(' ', 30 / (80 / MaxCol)));
			}

			SunHit = 0;
			int playerHit = await PlotShot(x, y, angle, velocity, playerNum);
			if (playerHit == 0) return false;
			if (playerHit == playerNum) playerNum = 3 - playerNum;
			await VictoryDance(playerNum);
			return true;
		}

		private async Task<int> PlotShot(int startX, int startY, double angle, double velocity, int playerNum)
		{
			angle = angle / 180 * pi;
			double initialXVelocity = Math.Cos(angle) * velocity;
			double initialYVelocity = Math.Sin(angle) * velocity;
			_qBasic.PUT(playerNum == 1 ? GorL : GorR, startX, startY, startX, startY);
			await Rest(100);
			_qBasic.PUT(GorD, startX, startY, startX, startY);

			if (velocity < 2)
			{
				return await ExplodeGorilla(startX, startY);
			}

			double startXPosition = startX + (playerNum == 2 ? Scl(25) : 0);
			double startYPosition = startY - Scl(4) - 3;
			int direction = playerNum == 2 ? Scl(4) : Scl(-4);
			double time = 0;
			while (true)
			{
				await Rest(20);
				double x = startXPosition + initialXVelocity * time + .5 * (Wind / 5.0) * time * time;
				double y = startYPosition + (-initialYVelocity * time + .5 * gravity * time * time) * (ScrHeight / 350.0);
				if (x >= ScrWidth - Scl(10) || x <= 3 || y >= ScrHeight - 3 || y <= 0) return 0;

				int checkX = Math.Clamp((int)x + Scl(8 * (2 - playerNum)), 0, ScrWidth - 1);
				int checkY = Math.Clamp((int)y, 0, ScrHeight - 1);
				int pixelIndex = (checkY * ScrWidth + checkX) * 4;
				bool occupied = _pixelBuffer[pixelIndex + 3] != 0;
				if (occupied)
				{
					if (checkX < ScrWidth / 2 && Math.Abs(checkX - GorillaX[0]) < Scl(16) && Math.Abs(checkY - GorillaY[0]) < Scl(30)) return await ExplodeGorilla(x, y);
					if (checkX >= ScrWidth / 2 && Math.Abs(checkX - GorillaX[1]) < Scl(16) && Math.Abs(checkY - GorillaY[1]) < Scl(30)) return await ExplodeGorilla(x, y);
					await DoExplosion(x, y);
					return 0;
				}

				DrawBan(x, y, (int)(time * 10) % 4, true);
				time += .1;
			}
		}

		private async Task<int> ExplodeGorilla(double x, double y)
		{
			int playerHit = x < ScrWidth / 2 ? 1 : 2;
			int centerX = GorillaX[playerHit - 1] + Scl(5) + Scl(4);
			int centerY = GorillaY[playerHit - 1] + Scl(12);
			for (int radius = 1; radius <= 24 * (ScrWidth / 320.0); radius++)
			{
				_qBasic.CIRCLE(false, centerX, centerY, radius, ExplosionColor);
				await Rest(5);
			}
			for (int radius = 24 * (ScrWidth / 320); radius >= 1; radius--)
			{
				_qBasic.CIRCLE(false, centerX, centerY, radius, BACKATTR);
				await Rest(5);
			}
			return playerHit;
		}

		public async Task PlayGame(string player1, string player2, int numGames)
		{
			ClearGorillas();
			Array.Clear(TotalWins);
			for (int game = 1; game <= numGames; game++)
			{
				_qBasic.CLS();
				MakeCityScape(BuildingCoordinates);
				PlaceGorillas(BuildingCoordinates);
				DoSun(SUNHAPPY);
				bool hit = false;
				int tosser = 1;
				while (!hit)
				{
					tosser = 3 - tosser;
					_qBasic.LOCATE(1, 1);
					_qBasic.PRINT(player1);
					_qBasic.LOCATE(1, MaxCol - 1 - player2.Length);
					_qBasic.PRINT(player2);
					Center(23, $"{TotalWins[0]} >Score< {TotalWins[1]}");
					hit = await DoShot(tosser, GorillaX[tosser - 1], GorillaY[tosser - 1]);
					if (SunHit != 0) DoSun(SUNHAPPY);
					if (hit) UpdateScores(tosser, hit ? 1 : 0);
				}
				await Rest(1000);
			}
		}

		/// <summary>
		/// Scales the given float value to an integer based on the current screen mode.
		/// </summary>
		/// <param name="n">The float value to scale.</param>
		/// <returns>The scaled integer value.</returns>
		/// <remarks>
		/// Passing 9.0f to this function will return 5 in mode 1 and 9 in any other mode.
		/// 9 / 2 == 4.5 + .1 == 4.6, Convert.ToInt32(4.6) == 5 (rounded up to nearest integer)
		/// </remarks>
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
