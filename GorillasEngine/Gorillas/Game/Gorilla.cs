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
		public const bool TRUE = true;
		public const bool FALSE = false;
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
		private byte[]?[] GorillaBackgrounds = new byte[]?[2];
		private byte[]? TextBackground;

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
			SunHt = mode == 9 ? 39 : 20;
			GHeight = mode == 9 ? 25 : 12;
			MachSpeed = 343.0f; // Speed of sound in m/s
			LBan = CreateBananaSprite(false);
			RBan = CreateBananaSprite(true);
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
			var backgroundColor = _qBasic.GetColor(BACKATTR);
			Draw.DrawFilledRectangle(
				_pixelBuffer,
				ScrWidth,
				ScrHeight,
				x - Scl(22),
				y - Scl(18),
				Scl(44),
				Scl(36),
				backgroundColor.r, backgroundColor.g, backgroundColor.b, 255);

			// DRAW NEW SUN:

			// body
			Draw.DrawFilledCircle(
				_pixelBuffer, // RGBA frame buffer
				ScrWidth, ScrHeight, // Width & Height of frame buffer
				x, y, // Position
				Scl(12), // Radius
				_qBasic.GetColor(SUNATTR).r, _qBasic.GetColor(SUNATTR).g, _qBasic.GetColor(SUNATTR).b, 255); // Sun color

			// rays
			var sunColor = _qBasic.GetColor(SUNATTR);
			Draw.DrawLine(_pixelBuffer, x - Scl(20), y, x + Scl(20), y, ScrWidth, ScrHeight, sunColor.r, sunColor.g, sunColor.b, 255);
			Draw.DrawLine(_pixelBuffer, x, y - Scl(15), x, y + Scl(15), ScrWidth, ScrHeight, sunColor.r, sunColor.g, sunColor.b, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(15), y - Scl(10), x + Scl(15), y + Scl(10), ScrWidth, ScrHeight, sunColor.r, sunColor.g, sunColor.b, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(15), y + Scl(10), x + Scl(15), y - Scl(10), ScrWidth, ScrHeight, sunColor.r, sunColor.g, sunColor.b, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(8), y - Scl(13), x + Scl(8), y + Scl(13), ScrWidth, ScrHeight, sunColor.r, sunColor.g, sunColor.b, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(8), y + Scl(13), x + Scl(8), y - Scl(13), ScrWidth, ScrHeight, sunColor.r, sunColor.g, sunColor.b, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(18), y - Scl(5), x + Scl(18), y + Scl(5), ScrWidth, ScrHeight, sunColor.r, sunColor.g, sunColor.b, 255);
			Draw.DrawLine(_pixelBuffer, x - Scl(18), y + Scl(5), x + Scl(18), y - Scl(5), ScrWidth, ScrHeight, sunColor.r, sunColor.g, sunColor.b, 255);

			// mouth
			if (mouth)
			{
				// draw "o" mouth
				Draw.DrawFilledCircle(_pixelBuffer, ScrWidth, ScrHeight, x, y + Scl(5), Scl(2.9f), 0, 0, 0, 255);
			}
			else
			{
				// draw smile
				for (int mouthRadius = Scl(7); mouthRadius <= Scl(8); mouthRadius++)
				{
					Draw.DrawArc(_pixelBuffer, ScrWidth, ScrHeight, x, y, mouthRadius, Convert.ToSingle(30 * pi / 180), Convert.ToSingle(150 * pi / 180), 0, 0, 0, 255);
				}
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
				_qBasic.PALETTE(0, 1);
				_qBasic.PALETTE(1, 46);
				_qBasic.PALETTE(2, 44);
				_qBasic.PALETTE(3, 54);
				_qBasic.PALETTE(5, 7);
				_qBasic.PALETTE(6, 4);
				_qBasic.PALETTE(7, 3);

				// Display Color
				// TODO: Determine what "Display Color" means in this context. It may refer to setting the color palette for the display, but without more information, it's unclear.
				_qBasic.PALETTE(9, 63);
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
			_qBasic.PALETTE(0, 0);
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
			Task<char?> keyTask = _qBasic.WAITKEY();

			while (true)
			{
				for (int a = 1; a <= 5; a++)
				{
					_qBasic.LOCATE(1, 1);
					_qBasic.PRINT(sparkle.Substring(a - 1, 80));
					_qBasic.LOCATE(22, 1);
					_qBasic.PRINT(sparkle.Substring(5 - a, 80));

					for (int b = 2; b <= 21; b++)
					{
						bool sparkleOn = (a + b) % 5 == 1;
						_qBasic.LOCATE(b, 80);
						_qBasic.PRINT(sparkleOn ? "*" : " ");
						_qBasic.LOCATE(23 - b, 1);
						_qBasic.PRINT(sparkleOn ? "*" : " ");
					}

					await Task.Delay(100);
					if (keyTask.IsCompleted)
					{
						await keyTask;
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
					GorR = Utils.CaptureRegion(temporaryFramebuffer, ScrWidth, ScrHeight, x - Scl(15), y - Scl(1), Scl(29) + 1, Scl(29) + 1);
					break;
				case LEFTUP:
					GorL = Utils.CaptureRegion(temporaryFramebuffer, ScrWidth, ScrHeight, x - Scl(15), y - Scl(1), Scl(29) + 1, Scl(29) + 1);
					break;
				case ARMSDOWN:
					GorD = Utils.CaptureRegion(temporaryFramebuffer, ScrWidth, ScrHeight, x - Scl(15), y - Scl(1), Scl(29) + 1, Scl(29) + 1);
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
			_qBasic.COLOR(7);
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

				PutGorilla(GorD, x - 13, y);
				PutGorilla(GorD, x + 47, y);
				await Rest(1000);

				PutGorilla(GorL, x - 13, y);
				PutGorilla(GorR, x + 47, y);
				_qBasic.PLAY(new[] { ("B", 150L), ("B", 75L), ("A", 75L), ("A", 75L), ("B", 75L) });
				await Rest(300);

				PutGorilla(GorR, x - 13, y);
				PutGorilla(GorL, x + 47, y);
				_qBasic.PLAY(new[] { ("E", 150L), ("D", 75L), ("D", 75L), ("E", 75L), ("E", 75L), ("D", 75L) });
				await Rest(300);

				PutGorilla(GorL, x - 13, y);
				PutGorilla(GorR, x + 47, y);
				_qBasic.PLAY(new[] { ("G", 150L), ("E", 75L), ("E", 75L), ("G", 75L), ("G", 75L), ("E", 75L) });
				await Rest(300);

				PutGorilla(GorR, x - 13, y);
				PutGorilla(GorL, x + 47, y);
				_qBasic.PLAY(new[] { ("B", 150L), ("B", 75L), ("A", 75L), ("G", 75L), ("B", 150L) });
				await Rest(300);

				for (int i = 1; i <= 4; i++)
				{
					PutGorilla(GorL, x - 13, y);
					PutGorilla(GorR, x + 47, y);
					_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
					await Rest(100);

					PutGorilla(GorR, x - 13, y);
					PutGorilla(GorL, x + 47, y);
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
			if (!draw)
			{
				return;
			}

			byte[]? banana = rotation switch
			{
				0 => LBan,
				3 => RBan,
				1 => RBan,
				2 => LBan,
				_ => null
			};
			_qBasic.PUT(banana, 11, 7, (int)x, (int)y, true);

			/*
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
			*/
		}

		private void PutGorilla(byte[]? sprite, int x, int y)
		{
			_qBasic.PUT(sprite, Scl(29) + 1, Scl(29) + 1, x, y, true);
		}

		private byte[] CreateBananaSprite(bool right)
		{
			const int width = 11;
			const int height = 7;
			byte[] sprite = new byte[width * height * 4];
			string[] pixels = right
				? new[] { "      ##   ", "     ####  ", "    ###### ", "     ##### ", "      ###  ", "       #   ", "           " }
				: new[] { "   ##      ", "  ####     ", " ######    ", "  #####    ", "   ###     ", "    #      ", "           " };

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (pixels[y][x] != '#')
					{
						continue;
					}

					int index = (y * width + x) * 4;
					sprite[index] = 252;
					sprite[index + 1] = 252;
					sprite[index + 2] = 84;
					sprite[index + 3] = 255;
				}
			}
			return sprite;
		}

		private async Task VictoryDance(int player)
		{
			int originX = GorillaX[player - 1];
			int originY = GorillaY[player - 1];
			int width = Scl(29) + 1;
			int height = Scl(29) + 1;
			byte[] background = GorillaBackgrounds[player - 1]
				?? Utils.CaptureRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, originX, originY, width, height);

			for (int i = 1; i <= 4; i++)
			{
				Utils.RestoreRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, background, originX, originY, width, height);
				DrawGorilla(GorillaX[player - 1], GorillaY[player - 1], LEFTUP);
				PutGorilla(GorL, GorillaX[player - 1], GorillaY[player - 1]);
				_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
				await Rest(200);
				Utils.RestoreRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, background, originX, originY, width, height);
				DrawGorilla(GorillaX[player - 1], GorillaY[player - 1], RIGHTUP);
				PutGorilla(GorR, GorillaX[player - 1], GorillaY[player - 1]);
				_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
				await Rest(200);
			}

			Utils.RestoreRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, background, originX, originY, width, height);
			DrawGorilla(GorillaX[player - 1], GorillaY[player - 1], ARMSDOWN);
			PutGorilla(GorD, GorillaX[player - 1], GorillaY[player - 1]);
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

			var backgroundColor = _qBasic.GetColor(BACKATTR);
			Draw.DrawFilledCircle(
				_pixelBuffer,
				ScrWidth,
				ScrHeight,
				(int)x,
				(int)y,
				radius,
				backgroundColor.r,
				backgroundColor.g,
				backgroundColor.b,
				255);
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
				_qBasic.LINE(x, bottomLine, x + width, bottomLine - buildingHeight, Mode == 9 ? FnRan(3) + 4 : 2, QBasic.LineBoxStyle.BF);
				for (int windowX = x + 3; windowX < x + width - 3; windowX += horizontalSpacing)
				{
					for (int windowY = buildingHeight - 3; windowY >= 7; windowY -= verticalSpacing)
					{
						_qBasic.COLOR(Mode == 9 && FnRan(4) != 1 ? WINDOWCOLOR : 8);
						int windowColor = Mode == 9 && FnRan(4) != 1 ? WINDOWCOLOR : 8;
						_qBasic.LINE(windowX, bottomLine - windowY, windowX + windowWidth, bottomLine - windowY + windowHeight, windowColor, QBasic.LineBoxStyle.BF);
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
				int arrowDirection = Wind > 0 ? -2 : 2;
				int arrowX = ScrWidth / 2 + windLine;
				_qBasic.LINE(ScrWidth / 2, ScrHeight - 5, arrowX, ScrHeight - 5, ExplosionColor);
				_qBasic.LINE(arrowX, ScrHeight - 5, arrowX + arrowDirection, ScrHeight - 7, ExplosionColor);
				_qBasic.LINE(arrowX, ScrHeight - 5, arrowX + arrowDirection, ScrHeight - 3, ExplosionColor);
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
				int gorillaOriginX = GorillaX[i];
				int gorillaOriginY = GorillaY[i];
				GorillaBackgrounds[i] = Utils.CaptureRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, gorillaOriginX, gorillaOriginY, Scl(29) + 1, Scl(29) + 1);
				DrawGorilla(GorillaX[i], GorillaY[i], ARMSDOWN);
				PutGorilla(GorD, GorillaX[i], GorillaY[i]);
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

		private async Task<int> DoShot(int playerNum, int x, int y)
		{
			if (SunHit != 0)
			{
				DoSun(SUNHAPPY);
				SunHit = 0;
			}

			_qBasic.COLOR(7);
			int locateColumn = playerNum == 1 ? 1 : (Mode == 9 ? 66 : 26);
			_qBasic.LOCATE(2, locateColumn);
			_qBasic.PRINT("Angle:");
			double angle = await GetNum(2, locateColumn + 7);
			_qBasic.LOCATE(3, locateColumn);
			_qBasic.PRINT("Velocity:");
			double velocity = await GetNum(3, locateColumn + 10);
			if (playerNum == 2) angle = 180 - angle;

			if (TextBackground != null)
			{
				Utils.RestoreRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, TextBackground, 0, 0, ScrWidth, _qBasic.CharHeight * 4);
			}

			SunHit = 0;
			int playerHit = await PlotShot(x, y, angle, velocity, playerNum);
			if (playerHit == 0) return 0;
			if (playerHit == playerNum) playerNum = 3 - playerNum;
			await VictoryDance(playerNum);
			return playerHit;
		}

		private async Task<int> PlotShot(int startX, int startY, double angle, double velocity, int playerNum)
		{
			angle = angle / 180 * pi;
			double initialXVelocity = Math.Cos(angle) * velocity;
			double initialYVelocity = Math.Sin(angle) * velocity;
			int gorillaOriginX = startX;
			int gorillaOriginY = startY;
			int gorillaWidth = Scl(29) + 1;
			int gorillaHeight = Scl(29) + 1;
			byte[] gorillaBackground = GorillaBackgrounds[playerNum - 1]
				?? Utils.CaptureRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, gorillaOriginX, gorillaOriginY, gorillaWidth, gorillaHeight);
			Utils.RestoreRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, gorillaBackground, gorillaOriginX, gorillaOriginY, gorillaWidth, gorillaHeight);
			DrawGorilla(startX, startY, playerNum == 1 ? LEFTUP : RIGHTUP);
			PutGorilla(playerNum == 1 ? GorL : GorR, startX, startY);
			await Rest(100);
			Utils.RestoreRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, gorillaBackground, gorillaOriginX, gorillaOriginY, gorillaWidth, gorillaHeight);
			DrawGorilla(startX, startY, ARMSDOWN);
			PutGorilla(GorD, startX, startY);

			if (velocity < 2)
			{
				return await ExplodeGorilla(startX, startY, playerNum);
			}

			double startXPosition = startX + (playerNum == 2 ? Scl(25) : 0);
			double startYPosition = startY - Scl(4) - 3;
			int direction = playerNum == 2 ? Scl(4) : Scl(-4);
			double time = 0;
			byte[]? previousBananaBackground = null;
			int previousBananaX = 0;
			int previousBananaY = 0;
			bool shotInSun = false;
			bool leftThrower = false;
			while (true)
			{
				await Rest(20);
				if (previousBananaBackground != null)
				{
					Utils.RestoreRegion(_pixelBuffer, ScrWidth, ScrHeight, previousBananaBackground, previousBananaX, previousBananaY);
				}
				double x = startXPosition + initialXVelocity * time + .5 * (Wind / 5.0) * time * time;
				double y = startYPosition + (-initialYVelocity * time + .5 * gravity * time * time) * (ScrHeight / 350.0);
				if (x >= ScrWidth - Scl(10) || x <= 3 || y >= ScrHeight - 3) return 0;

				if (y > 0)
				{
					int checkX = (int)x + Scl(8 * (2 - playerNum));
					int checkY = (int)y;
					if (checkX < 0 || checkX >= ScrWidth || checkY < 0 || checkY >= ScrHeight)
					{
						time += .1;
						continue;
					}

					int pixelIndex = (checkY * ScrWidth + checkX) * 4;
					bool insideSunBounds = Math.Abs(ScrWidth / 2 - checkX) <= Scl(20) && checkY < SunHt;
					var sunColor = _qBasic.GetColor(SUNATTR);
					bool isSun = _pixelBuffer[pixelIndex] == sunColor.r
						&& _pixelBuffer[pixelIndex + 1] == sunColor.g
						&& _pixelBuffer[pixelIndex + 2] == sunColor.b;
					if ((isSun || insideSunBounds) && checkY < SunHt)
					{
						DoSun(SUNSHOCK);
						SunHit = 1;
						shotInSun = true;
					}
					else if (shotInSun && !insideSunBounds)
					{
						shotInSun = false;
					}

					int hitGorilla = !shotInSun ? GetHitGorilla(checkX, checkY) : 0;
					if (!leftThrower && !IsInsideGorillaBounds(x, y, playerNum))
					{
						leftThrower = true;
					}
					if (!leftThrower && hitGorilla == playerNum)
					{
						hitGorilla = 0;
					}
					if (hitGorilla != 0)
					{
						return await ExplodeGorilla(x, y, hitGorilla);
					}

					bool occupied = !shotInSun && IsCollidablePixel(pixelIndex);
					if (occupied)
					{
						await DoExplosion(checkX, checkY);
						return 0;
					}

					if (!shotInSun)
					{
						previousBananaX = Math.Clamp((int)x, 0, ScrWidth - 11);
						previousBananaY = Math.Clamp((int)y, 0, ScrHeight - 7);
						previousBananaBackground = Utils.CaptureRegion(_pixelBuffer, ScrWidth, ScrHeight, previousBananaX, previousBananaY, 11, 7);
						DrawBan(x, y, (int)(time * 10) % 4, true);
					}
				}
				time += .1;
			}
		}

		private bool IsCollidablePixel(int pixelIndex)
		{
			byte red = _pixelBuffer[pixelIndex];
			byte green = _pixelBuffer[pixelIndex + 1];
			byte blue = _pixelBuffer[pixelIndex + 2];
			(int r, int g, int b)[] collisionColors =
			{
				_qBasic.GetColor(OBJECTCOLOR),
				_qBasic.GetColor(SUNATTR),
				_qBasic.GetColor(4),
				_qBasic.GetColor(5),
				_qBasic.GetColor(6),
				_qBasic.GetColor(WINDOWCOLOR)
			};

			return collisionColors.Any(color => red == color.r && green == color.g && blue == color.b);
		}

		private int GetHitGorilla(double x, double y)
		{
			for (int player = 0; player < GorillaX.Length; player++)
			{
				int left = GorillaX[player];
				int top = GorillaY[player];
				int right = left + Scl(29);
				int bottom = top + Scl(29);
				if (x >= left && x <= right && y >= top && y <= bottom)
				{
					return player + 1;
				}
			}

			return 0;
		}

		private bool IsInsideGorillaBounds(double x, double y, int player)
		{
			int left = GorillaX[player - 1];
			int top = GorillaY[player - 1];
			return x >= left && x <= left + Scl(29) && y >= top && y <= top + Scl(29);
		}

		private async Task<int> ExplodeGorilla(double x, double y, int playerHit)
		{
			int centerX = GorillaX[playerHit - 1] + Scl(5) + Scl(4);
			int centerY = GorillaY[playerHit - 1] + Scl(12);
			int maxRadius = Math.Max(1, (int)(24 * (ScrWidth / 320.0)));
			var explosionColor = _qBasic.GetColor(ExplosionColor);
			_qBasic.PLAY(new[] { ("E", 50L), ("F", 50L), ("G", 50L), ("E", 50L), ("F", 50L), ("D", 50L), ("C", 50L) });
			for (int radius = 1; radius <= maxRadius; radius++)
			{
				Draw.DrawFilledCircle(_pixelBuffer, ScrWidth, ScrHeight, centerX, centerY, radius, explosionColor.r, explosionColor.g, explosionColor.b, 255);
				await Rest(20);
			}
			var backgroundColor = _qBasic.GetColor(BACKATTR);
			for (int radius = maxRadius; radius >= 1; radius--)
			{
				Draw.DrawFilledCircle(_pixelBuffer, ScrWidth, ScrHeight, centerX, centerY, radius, backgroundColor.r, backgroundColor.g, backgroundColor.b, 255);
				await Rest(20);
			}
			return playerHit;
		}

		public async Task PlayGame(string player1, string player2, int numGames)
		{
			Array.Clear(TotalWins);
			for (int game = 1; game <= numGames; game++)
			{
				_qBasic.CLS();
				MakeCityScape(BuildingCoordinates);
				PlaceGorillas(BuildingCoordinates);
				DoSun(SUNHAPPY);
				_qBasic.COLOR(7);
				TextBackground = Utils.CaptureRegion(_qBasic.PixelBuffer, ScrWidth, ScrHeight, 0, 0, ScrWidth, _qBasic.CharHeight * 4);
				int playerHit = 0;
				int tosser = 2;
				while (playerHit == 0)
				{
					tosser = 3 - tosser;
					_qBasic.LOCATE(1, 1);
					_qBasic.PRINT(player1);
					_qBasic.LOCATE(1, MaxCol - 1 - player2.Length);
					_qBasic.PRINT(player2);
					Center(23, $"{TotalWins[0]} >Score< {TotalWins[1]}");
					playerHit = await DoShot(tosser, GorillaX[tosser - 1], GorillaY[tosser - 1]);
					if (playerHit != 0)
					{
						UpdateScores(tosser, playerHit == tosser ? HITSELF : 0);
					}
				}
				await Rest(1000);
			}

			_qBasic.SCREEN(0);
			MaxCol = 80;
			_qBasic.PALETTE(0, 0);
			_qBasic.COLOR(7);
			_qBasic.CLS();

			Center(8, "GAME OVER!");
			Center(10, "Score:");
			_qBasic.LOCATE(11, 30);
			_qBasic.PRINT(player1);
			_qBasic.LOCATE(11, 50);
			_qBasic.PRINT(TotalWins[0].ToString());
			_qBasic.LOCATE(12, 30);
			_qBasic.PRINT(player2);
			_qBasic.LOCATE(12, 50);
			_qBasic.PRINT(TotalWins[1].ToString());
			Center(24, "Press any key to continue");
			await SparklePause();
			_qBasic.COLOR(7);
			_qBasic.CLS();
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
