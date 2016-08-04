using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SnakeGame
{
    class Program
    {
        private const int Width = 40;
        private const int Height = 20;
        private const int GameSpeed = 100;
        private const int PointsPerFood = 10;

        private static int _score;
        private static Direction _currentDirection;
        private static Direction _nextDirection;
        private static readonly Random _random = new Random();
        private static readonly List<Position> _snake = new List<Position>();
        private static Position _food;
        private static bool _gameOver;
        private static Position _lastTail;
        private static readonly object _directionLock = new object();

        enum Direction { Up, Down, Left, Right }

        struct Position
        {
            public int X { get; }
            public int Y { get; }

            public Position(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(Position other) => X == other.X && Y == other.Y;
        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            InitializeGame();
            RenderInitialScreen();

            var inputThread = new Thread(ReadInput);
            inputThread.Start();

            RunGameLoop();

            inputThread.Join();

            ShowGameOver(args);
        }

        private static void InitializeGame()
        {
            _snake.Clear();
            _snake.Add(new Position(Width / 2, Height / 2));
            _snake.Add(new Position(Width / 2 - 1, Height / 2));
            _snake.Add(new Position(Width / 2 - 2, Height / 2));

            SpawnFood();
            _score = 0;
            _currentDirection = Direction.Right;
            _nextDirection = Direction.Right;
            _gameOver = false;
        }

        private static void RenderInitialScreen()
        {
            DrawBorders();
            DrawFood();
            DrawSnake();
            DrawScore();
        }

        private static void RunGameLoop()
        {
            while (!_gameOver)
            {
                UpdateGameState();
                RenderFrame();
                Thread.Sleep(GameSpeed);
            }
        }

        private static void ShowGameOver(string[] args)
        {
            DisplayGameOverMessage();
            HandleRestartChoice(args);
            Console.SetCursorPosition(0, Height + 3);
            Console.CursorVisible = true;
        }

        private static void DisplayGameOverMessage()
        {
            Console.SetCursorPosition(Width / 2 - 5, Height / 2);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("GAME OVER!");
            Console.SetCursorPosition(Width / 2 - 7, Height / 2 + 1);
            Console.WriteLine($"Final Score: {_score}");
            Console.SetCursorPosition(Width / 2 - 18, Height / 2 + 3);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Press R to Restart or ESC to Quit");
            Console.ResetColor();
        }

        private static void HandleRestartChoice(string[] args)
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.R)
                {
                    Main(args);
                    return;
                }
                else if (key == ConsoleKey.Escape)
                {
                    break;
                }
            }
        }

        private static void SpawnFood()
        {
            var availablePositions = GetAvailablePositions();

            if (availablePositions.Count > 0)
            {
                _food = availablePositions[_random.Next(availablePositions.Count)];
            }
            else
            {
                _gameOver = true;
            }
        }

        private static List<Position> GetAvailablePositions()
        {
            var positions = new List<Position>();

            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    var pos = new Position(x, y);
                    if (!_snake.Any(s => s.Equals(pos)))
                    {
                        positions.Add(pos);
                    }
                }
            }

            return positions;
        }

        private static void ReadInput()
        {
            while (!_gameOver)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    HandleKeyPress(key);
                }
            }
        }

        private static void HandleKeyPress(ConsoleKey key)
        {
            lock (_directionLock)
            {
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (_currentDirection != Direction.Down)
                            _nextDirection = Direction.Up;
                        break;
                    case ConsoleKey.DownArrow:
                        if (_currentDirection != Direction.Up)
                            _nextDirection = Direction.Down;
                        break;
                    case ConsoleKey.LeftArrow:
                        if (_currentDirection != Direction.Right)
                            _nextDirection = Direction.Left;
                        break;
                    case ConsoleKey.RightArrow:
                        if (_currentDirection != Direction.Left)
                            _nextDirection = Direction.Right;
                        break;
                    case ConsoleKey.Escape:
                        _gameOver = true;
                        break;
                }
            }
        }

        private static void UpdateGameState()
        {
            UpdateDirection();
            var newHead = CalculateNewHeadPosition();

            if (CheckCollision(newHead))
            {
                _gameOver = true;
                return;
            }

            MoveSnake(newHead);
        }

        private static void UpdateDirection()
        {
            lock (_directionLock)
            {
                _currentDirection = _nextDirection;
            }
        }

        private static Position CalculateNewHeadPosition()
        {
            var head = _snake[0];

            return _currentDirection switch
            {
                Direction.Up => new Position(head.X, head.Y - 1),
                Direction.Down => new Position(head.X, head.Y + 1),
                Direction.Left => new Position(head.X - 1, head.Y),
                Direction.Right => new Position(head.X + 1, head.Y),
                _ => head
            };
        }

        private static bool CheckCollision(Position position)
        {
            return CheckWallCollision(position) || CheckSelfCollision(position);
        }

        private static bool CheckWallCollision(Position position)
        {
            return position.X <= 0 || position.X >= Width - 1 ||
                   position.Y <= 0 || position.Y >= Height - 1;
        }

        private static bool CheckSelfCollision(Position position)
        {
            return _snake.Any(s => s.Equals(position));
        }

        private static void MoveSnake(Position newHead)
        {
            _snake.Insert(0, newHead);

            if (newHead.Equals(_food))
            {
                HandleFoodEaten();
            }
            else
            {
                RemoveTail();
            }
        }

        private static void HandleFoodEaten()
        {
            _score += PointsPerFood;
            SpawnFood();
            _lastTail = new Position(-1, -1);
        }

        private static void RemoveTail()
        {
            _lastTail = _snake[_snake.Count - 1];
            _snake.RemoveAt(_snake.Count - 1);
        }

        private static void RenderFrame()
        {
            EraseTail();
            DrawHead();
            DrawFood();
            DrawScore();
            Console.ResetColor();
        }

        private static void EraseTail()
        {
            if (_lastTail.X >= 0)
            {
                Console.SetCursorPosition(_lastTail.X, _lastTail.Y);
                Console.Write(" ");
            }
        }

        private static void DrawHead()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            var head = _snake[0];
            Console.SetCursorPosition(head.X, head.Y);
            Console.Write("●");
        }

        private static void DrawBorders()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;

            DrawHorizontalBorders();
            DrawVerticalBorders();
            DrawCorners();

            Console.ResetColor();
        }

        private static void DrawHorizontalBorders()
        {
            for (int i = 0; i < Width; i++)
            {
                Console.SetCursorPosition(i, 0);
                Console.Write("═");
                Console.SetCursorPosition(i, Height - 1);
                Console.Write("═");
            }
        }

        private static void DrawVerticalBorders()
        {
            for (int i = 0; i < Height; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("║");
                Console.SetCursorPosition(Width - 1, i);
                Console.Write("║");
            }
        }

        private static void DrawCorners()
        {
            Console.SetCursorPosition(0, 0);
            Console.Write("╔");
            Console.SetCursorPosition(Width - 1, 0);
            Console.Write("╗");
            Console.SetCursorPosition(0, Height - 1);
            Console.Write("╚");
            Console.SetCursorPosition(Width - 1, Height - 1);
            Console.Write("╝");
        }

        private static void DrawSnake()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var segment in _snake)
            {
                Console.SetCursorPosition(segment.X, segment.Y);
                Console.Write("●");
            }
            Console.ResetColor();
        }

        private static void DrawFood()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.SetCursorPosition(_food.X, _food.Y);
            Console.Write("◆");
            Console.ResetColor();
        }

        private static void DrawScore()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.SetCursorPosition(2, Height + 1);
            Console.Write($"Score: {_score} | Use Arrow Keys to move | ESC to quit");
            Console.ResetColor();
        }
    }
}
