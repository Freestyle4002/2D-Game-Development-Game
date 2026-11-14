using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
        MainMenu();
    }

    // -------------------------
    //        MAIN MENU
    // -------------------------
    static void MainMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("🚀 SPACE ESCAPE 🚀");
            Console.ResetColor();
            Console.WriteLine("\n1. Start Game");
            Console.WriteLine("2. Quit");
            Console.Write("\nSelect an option: ");

            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.D1 || key == ConsoleKey.NumPad1)
                PlayGame();

            if (key == ConsoleKey.D2 || key == ConsoleKey.NumPad2)
                Environment.Exit(0);
        }
    }

    // -------------------------
    //       MAIN GAME
    // -------------------------
    static void PlayGame()
    {
        // screen dims (play area)
        int width = 60;   // wider so stars look nicer
        int height = 20;
        int playerX = 2;
        int playerY = height / 2;

        // gameplay state
        int lives = 3;
        int score = 0;
        int level = 1;
        int speed = 70;                // frame delay (ms)
        int scoreToNextLevel = 150;

        Random rand = new Random();

        // hazards (asteroids/comets)
        List<(int x, int y, bool comet)> hazards = new List<(int, int, bool)>();

        // starfield: background stars that move left
        List<(int x, int y)> stars = new List<(int x, int y)>();

        bool gameOver = false;

        // Buffer arrays not strictly necessary here since we draw by sampling lists,
        // but we'll render line-by-line to avoid Console.Clear() flicker.
        // Pre-size console window area (best-effort)
        try
        {
            if (Console.WindowWidth < width + 2) Console.WindowWidth = Math.Min(width + 2, Console.LargestWindowWidth);
            if (Console.WindowHeight < height + 5) Console.WindowHeight = Math.Min(height + 5, Console.LargestWindowHeight);
        }
        catch { /* ignore if environment doesn't allow resizing */ }

        // Initial small star population
        for (int i = 0; i < 80; i++)
            stars.Add((rand.Next(0, width), rand.Next(0, height)));

        // Main loop
        while (!gameOver)
        {
            // ----- STAR GENERATION & MOVEMENT -----
            // occasional new stars spawn on the right edge
            if (rand.Next(0, 10) < 3) // ~30% chance per frame
                stars.Add((width - 1, rand.Next(0, height)));

            // move stars left
            for (int i = 0; i < stars.Count; i++)
            {
                var s = stars[i];
                int newX = s.x - 1;
                if (newX < 0)
                {
                    // recycle occasionally or remove
                    if (rand.Next(0, 10) < 2)
                        stars[i] = (width - 1, rand.Next(0, height));
                    else
                        stars[i] = (-1, -1); // mark for removal
                }
                else
                {
                    stars[i] = (newX, s.y);
                }
            }
            stars.RemoveAll(s => s.x < 0);

            // occasional random twinkles (chance to add)
            if (rand.Next(0, 100) < 6) // small chance
                stars.Add((rand.Next(0, width), rand.Next(0, height)));

            // ----- HAZARD SPAWNING -----
            int hazardChance = Math.Min(3 + level, 9);
            if (rand.Next(0, 10) < hazardChance)
            {
                bool isComet = rand.Next(0, 10) < Math.Min(2 + level, 8); // comets more likely at higher levels
                hazards.Add((width - 1, rand.Next(0, height), isComet));
            }

            // move hazards (asteroids/comets)
            for (int i = 0; i < hazards.Count; i++)
            {
                var h = hazards[i];
                int newX = h.x - 1;
                int newY = h.y;

                if (h.comet)
                {
                    // comets move diagonally sometimes, more chaotic at higher levels
                    if (rand.Next(0, 10) < Math.Min(3 + level, 8))
                        newY += rand.Next(0, 2) == 0 ? -1 : 1;
                    newY = Math.Clamp(newY, 0, height - 1);
                }

                hazards[i] = (newX, newY, h.comet);
            }

            hazards.RemoveAll(h => h.x < 0);

            // ----- INPUT (non-blocking) -----
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.UpArrow && playerY > 0) playerY--;
                if (key == ConsoleKey.DownArrow && playerY < height - 1) playerY++;
                if (key == ConsoleKey.Q) return; // quit to main menu
            }

            // ----- COLLISION -----
            bool hit = false;
            foreach (var h in hazards)
            {
                if (h.x == playerX && h.y == playerY)
                {
                    hit = true;
                    break;
                }
            }

            if (hit)
            {
                lives--;
                Console.Beep();
                if (lives <= 0)
                {
                    gameOver = true;
                }
            }

            // ----- RENDER (line-by-line to avoid flicker) -----
            Console.SetCursorPosition(0, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // decide what to draw at (x,y) with priority:
                    // Player > Hazard > Star > empty

                    if (x == playerX && y == playerY)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write('^');
                        Console.ResetColor();
                        continue;
                    }

                    bool drew = false;

                    // hazards check (could be optimized, but screen small)
                    for (int i = 0; i < hazards.Count; i++)
                    {
                        var h = hazards[i];
                        if (h.x == x && h.y == y)
                        {
                            if (h.comet)
                                Console.ForegroundColor = ConsoleColor.Yellow;
                            else
                                Console.ForegroundColor = ConsoleColor.Red;

                            Console.Write(h.comet ? '@' : '*');
                            Console.ResetColor();
                            drew = true;
                            break;
                        }
                    }
                    if (drew) continue;

                    // star check
                    bool starHere = false;
                    for (int i = 0; i < stars.Count; i++)
                    {
                        var s = stars[i];
                        if (s.x == x && s.y == y)
                        {
                            starHere = true;
                            break;
                        }
                    }
                    if (starHere)
                    {
                        // some stars twinkle brighter randomly
                        if (rand.Next(0, 10) < 2)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write('.');
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write('.');
                        }
                        Console.ResetColor();
                        continue;
                    }

                    // empty space
                    Console.Write(' ');
                }
                Console.WriteLine();
            }

            // HUD (on the row below the play area)
            Console.Write($"Score: {score}  Level: {level}  Lives: {lives}   (Press Q for menu)   ");
            // pad remainder of line if console is wide
            // advance score & level
            score++;

            if (score >= level * scoreToNextLevel)
            {
                level++;
                if (speed > 30) speed -= 5;
                // show a small level notification without full clear
                int notifX = Math.Max(0, (width / 2) - 6);
                int notifY = Math.Max(0, height / 2);
                Console.SetCursorPosition(notifX, notifY);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"-- LEVEL {level} --");
                Console.ResetColor();
                // let player notice the level up briefly
                Thread.Sleep(500);
            }

            // frame delay
            Thread.Sleep(speed);
        }

        // -------------------------
        //       GAME OVER
        // -------------------------
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("💥 GAME OVER 💥");
        Console.ResetColor();
        Console.WriteLine($"Final Score: {score}");
        Console.WriteLine("\nPress R to restart or M for menu.");

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.R)
                {
                    PlayGame();
                    return;
                }
                if (key == ConsoleKey.M)
                {
                    return; // back to main menu
                }
            }
            Thread.Sleep(50);
        }
    }
}
