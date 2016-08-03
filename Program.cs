using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SnakeGame
{
    class Program
    {
        static int Width = 40;
        static int Height = 20;
        static int Score = 0;
        static Direction CurrentDirection = Direction.Right;
        static Random random = new Random();
        static List<Position> Snake = new List<Position>();
        static Position Food;
        static bool GameOver = false;
        static Position LastTail;

        enum Direction { Up, Down, Left, Right }

        struct Position
        {
            public int X { get; set; }
            public int Y { get; set; }

            public Position(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(Position other)
            {
                return X == other.X && Y == other.Y;
            }
        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            InitializeGame();
            DrawBorders();
            DrawFood();
            DrawSnake();
            DrawScore();

            Thread inputThread = new Thread(ReadInput);
            inputThread.Start();

            while (!GameOver)
            {
                Update();
                Draw();
                Thread.Sleep(100);
            }

            Console.SetCursorPosition(Width / 2 - 5, Height / 2);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("GAME OVER!");
            Console.SetCursorPosition(Width / 2 - 7, Height / 2 + 1);
            Console.WriteLine($"Final Score: {Score}");
            Console.ResetColor();
            Console.SetCursorPosition(0, Height + 3);
            Console.CursorVisible = true;

            inputThread.Join();
        }

        static void InitializeGame()
        {
            Snake.Clear();
            Snake.Add(new Position(Width / 2, Height / 2));
            Snake.Add(new Position(Width / 2 - 1, Height / 2));
            Snake.Add(new Position(Width / 2 - 2, Height / 2));

            SpawnFood();
            Score = 0;
            CurrentDirection = Direction.Right;
            GameOver = false;
        }

        static void SpawnFood()
        {
            var availablePositions = new List<Position>();

            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    var pos = new Position(x, y);
                    if (!Snake.Any(s => s.Equals(pos)))
                    {
                        availablePositions.Add(pos);
                    }
                }
            }

            if (availablePositions.Count > 0)
            {
                Food = availablePositions[random.Next(availablePositions.Count)];
            }
            else
            {
                // Win condition - board is full!
                GameOver = true;
            }
        }

        static void ReadInput()
        {
            while (!GameOver)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            if (CurrentDirection != Direction.Down)
                                CurrentDirection = Direction.Up;
                            break;
                        case ConsoleKey.DownArrow:
                            if (CurrentDirection != Direction.Up)
                                CurrentDirection = Direction.Down;
                            break;
                        case ConsoleKey.LeftArrow:
                            if (CurrentDirection != Direction.Right)
                                CurrentDirection = Direction.Left;
                            break;
                        case ConsoleKey.RightArrow:
                            if (CurrentDirection != Direction.Left)
                                CurrentDirection = Direction.Right;
                            break;
                        case ConsoleKey.Escape:
                            GameOver = true;
                            break;
                    }
                }
            }
        }

        static void Update()
        {
            Position head = Snake[0];
            Position newHead;

            switch (CurrentDirection)
            {
                case Direction.Up:
                    newHead = new Position(head.X, head.Y - 1);
                    break;
                case Direction.Down:
                    newHead = new Position(head.X, head.Y + 1);
                    break;
                case Direction.Left:
                    newHead = new Position(head.X - 1, head.Y);
                    break;
                case Direction.Right:
                    newHead = new Position(head.X + 1, head.Y);
                    break;
                default:
                    newHead = head;
                    break;
            }

            // Check wall collision
            if (newHead.X <= 0 || newHead.X >= Width - 1 ||
                newHead.Y <= 0 || newHead.Y >= Height - 1)
            {
                GameOver = true;
                return;
            }

            // Check self collision
            if (Snake.Any(s => s.Equals(newHead)))
            {
                GameOver = true;
                return;
            }

            Snake.Insert(0, newHead);

            // Check if food eaten
            if (newHead.Equals(Food))
            {
                Score += 10;
                SpawnFood();
                LastTail = new Position(-1, -1); // No tail to erase
            }
            else
            {
                LastTail = Snake[Snake.Count - 1];
                Snake.RemoveAt(Snake.Count - 1);
            }
        }

        static void Draw()
        {
            // Erase old tail
            if (LastTail.X >= 0)
            {
                Console.SetCursorPosition(LastTail.X, LastTail.Y);
                Console.Write(" ");
            }

            // Draw new head
            Console.ForegroundColor = ConsoleColor.Green;
            Position head = Snake[0];
            Console.SetCursorPosition(head.X, head.Y);
            Console.Write("●");

            // Draw food if it changed
            DrawFood();

            // Update score
            DrawScore();

            Console.ResetColor();
        }

        static void DrawBorders()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            for (int i = 0; i < Width; i++)
            {
                Console.SetCursorPosition(i, 0);
                Console.Write("═");
                Console.SetCursorPosition(i, Height - 1);
                Console.Write("═");
            }
            for (int i = 0; i < Height; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("║");
                Console.SetCursorPosition(Width - 1, i);
                Console.Write("║");
            }
            Console.SetCursorPosition(0, 0);
            Console.Write("╔");
            Console.SetCursorPosition(Width - 1, 0);
            Console.Write("╗");
            Console.SetCursorPosition(0, Height - 1);
            Console.Write("╚");
            Console.SetCursorPosition(Width - 1, Height - 1);
            Console.Write("╝");
            Console.ResetColor();
        }

        static void DrawSnake()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var segment in Snake)
            {
                Console.SetCursorPosition(segment.X, segment.Y);
                Console.Write("●");
            }
            Console.ResetColor();
        }

        static void DrawFood()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.SetCursorPosition(Food.X, Food.Y);
            Console.Write("◆");
            Console.ResetColor();
        }

        static void DrawScore()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition(2, Height + 1);
            Console.Write($"Score: {Score} | Use Arrow Keys to move | ESC to quit");
            Console.ResetColor();
        }
    }
}
