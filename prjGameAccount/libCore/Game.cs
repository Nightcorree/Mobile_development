using Timer = System.Timers.Timer;

namespace libCore
{
    public enum Difficulty { Easy, Medium, Hard }

    public class Game
    {
        // --- Свойства (Статистика и состояние) ---
        public int QuestionNumber { get; private set; }
        public int Coins { get; private set; }
        public int CorrectAnswers { get; private set; }
        public int WrongAnswers { get; private set; }
        public string CurrentExpression { get; private set; }
        public Difficulty CurrentDifficulty { get; set; }
        public int TimeLeft { get; private set; }

        // --- Внутренние поля ---
        private int _comboMultiplier;
        private int _penaltyMultiplier;
        private bool _isActuallyCorrect;
        private Random _rnd = new Random();
        private Timer _timer;

        // --- События ---
        public event Action<string> ExpressionGenerated;
        public event Action StatisticsUpdated;
        public event Action<int> TimeTicked;
        public event Action GameOver;

        public Game(Difficulty difficulty = Difficulty.Easy)
        {
            CurrentDifficulty = difficulty;
            _timer = new Timer(1000);
            _timer.Elapsed += (s, e) =>
            {
                TimeLeft--;
                TimeTicked?.Invoke(TimeLeft);
                if (TimeLeft <= 0) StopGame();
            };
        }

        // --- Методы ---
        public void StartGame(int timeSeconds = 60)
        {
            QuestionNumber = 0;
            Coins = 0;
            CorrectAnswers = 0;
            WrongAnswers = 0;
            _comboMultiplier = 1;
            _penaltyMultiplier = 1;
            TimeLeft = timeSeconds;

            StatisticsUpdated?.Invoke();
            TimeTicked?.Invoke(TimeLeft);
            _timer.Start();

            GenerateNextExpression();
        }

        public void StopGame()
        {
            _timer.Stop();
            GameOver?.Invoke();
        }

        public void Answer(bool isYes)
        {
            if (TimeLeft <= 0) return;

            if (isYes == _isActuallyCorrect)
            {
                CorrectAnswers++;
                Coins += _comboMultiplier;
                _comboMultiplier *= 2;      // Увеличиваем комбо за правильный
                _penaltyMultiplier = 1;     // Сбрасываем штраф
            }
            else
            {
                WrongAnswers++;
                Coins -= _penaltyMultiplier;
                _penaltyMultiplier *= 2;    // Увеличиваем штраф за ошибку подряд
                _comboMultiplier = 1;       // Сбрасываем комбо
            }

            StatisticsUpdated?.Invoke();
            GenerateNextExpression();
        }

        private void GenerateNextExpression()
        {
            QuestionNumber++;

            int maxNum = CurrentDifficulty == Difficulty.Easy ? 20 : (CurrentDifficulty == Difficulty.Medium ? 60 : 100);
            int a = _rnd.Next(1, maxNum);
            int b = _rnd.Next(1, maxNum);
            int trueAnswer = 0;
            string op = "+";

            // Определение оператора в зависимости от сложности
            int opType = CurrentDifficulty == Difficulty.Easy ? 0 : _rnd.Next(0, 2);
            if (CurrentDifficulty == Difficulty.Hard) opType = _rnd.Next(0, 3); // 0:+, 1:-, 2:*

            if (opType == 0) { op = "+"; trueAnswer = a + b; }
            else if (opType == 1) { op = "-"; trueAnswer = a - b; }
            else if (opType == 2) { op = "*"; a = _rnd.Next(1, 15); b = _rnd.Next(1, 10); trueAnswer = a * b; }

            // С вероятностью 50% показываем правильный или неправильный ответ
            _isActuallyCorrect = _rnd.Next(0, 2) == 0;

            int displayedAnswer = _isActuallyCorrect
                ? trueAnswer
                : trueAnswer + _rnd.Next(-5, 6 > 0 ? 6 : 1);

            // Защита от случайного совпадения при неправильном ответе
            if (displayedAnswer == trueAnswer && !_isActuallyCorrect) displayedAnswer++;

            CurrentExpression = $"{a} {op} {b} = {displayedAnswer}";
            ExpressionGenerated?.Invoke(CurrentExpression);
        }
    }
}