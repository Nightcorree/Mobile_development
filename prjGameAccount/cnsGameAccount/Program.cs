using System;
using System.Threading;
using libCore;

namespace cnsGameAccount
{
    internal class Program
    {
        private static Game _game;
        private static bool _isPlaying = false;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;
            _game = new Game(Difficulty.Medium); // Можно менять сложность

            // Подписка на события
            _game.ExpressionGenerated += (expr) => RenderScreen();
            _game.StatisticsUpdated += () => RenderScreen();
            _game.TimeTicked += (time) => RenderScreen();
            _game.GameOver += OnGameOver;

            _isPlaying = true;
            _game.StartGame(30); // 30 секунд на игру

            // Игровой цикл для перехвата нажатий
            while (_isPlaying)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Y) _game.Answer(true);
                    else if (key == ConsoleKey.N) _game.Answer(false);
                    else if (key == ConsoleKey.Escape) _game.StopGame();
                }
                Thread.Sleep(50);
            }

            Console.CursorVisible = true;
        }

        static void RenderScreen()
        {
            // Очистка вызывает небольшое мерцание, но это норма для простых консольных игр
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== ИГРА 'УСТНЫЙ СЧЕТ' ===");
            Console.ResetColor();

            Console.WriteLine($"Уровень: {_game.CurrentDifficulty} | Время: {_game.TimeLeft} сек.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Монеты: {_game.Coins}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Правильно: {_game.CorrectAnswers} ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"| Ошибок: {_game.WrongAnswers}");
            Console.ResetColor();

            Console.WriteLine("----------------------------------");
            Console.WriteLine($"Вопрос #{_game.QuestionNumber}:");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\n      {_game.CurrentExpression} ?\n");
            Console.ResetColor();

            Console.WriteLine("[Y] - Правильно  |  [N] - Неправильно  |  [ESC] - Выход");
        }

        static void OnGameOver()
        {
            _isPlaying = false;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("=== ВРЕМЯ ВЫШЛО! ===");
            Console.WriteLine($"Ваш итоговый счет (монеты): {_game.Coins}");
            Console.ResetColor();
        }
    }
}