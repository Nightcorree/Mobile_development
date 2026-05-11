using System;
using System.Windows.Forms;
using libCore;

namespace wfaGameAccount
{
    public partial class Form1 : Form
    {
        private Game _game;

        public Form1()
        {
            InitializeComponent();
            _game = new Game(Difficulty.Easy);

            // Подписка на события движка
            _game.ExpressionGenerated += UpdateExpressionUI;
            _game.StatisticsUpdated += UpdateStatsUI;
            _game.TimeTicked += UpdateTimeUI;
            _game.GameOver += OnGameOver;

            // Настройка кнопок
            btnYes.Enabled = false;
            btnNo.Enabled = false;

            btnStart.Click += (s, e) => _game.StartGame(60);
            btnYes.Click += (s, e) => _game.Answer(true);
            btnNo.Click += (s, e) => _game.Answer(false);
        }

        // InvokeRequired проверяет, нужен ли безопасный вызов из фонового потока (от таймера)
        private void UpdateExpressionUI(string text)
        {
            if (InvokeRequired) { Invoke(new Action<string>(UpdateExpressionUI), text); return; }

            lblExpression.Text = $"Вопрос #{_game.QuestionNumber}:\n{text}";
            btnYes.Enabled = true;
            btnNo.Enabled = true;
        }

        private void UpdateStatsUI()
        {
            if (InvokeRequired) { Invoke(new Action(UpdateStatsUI)); return; }

            lblStats.Text = $"Монеты: {_game.Coins} | Верно: {_game.CorrectAnswers} | Ошибки: {_game.WrongAnswers}";
        }

        private void UpdateTimeUI(int timeLeft)
        {
            if (InvokeRequired) { Invoke(new Action<int>(UpdateTimeUI), timeLeft); return; }

            lblTime.Text = $"Осталось: {timeLeft} сек.";
        }

        private void OnGameOver()
        {
            if (InvokeRequired) { Invoke(new Action(OnGameOver)); return; }

            btnYes.Enabled = false;
            btnNo.Enabled = false;
            MessageBox.Show($"Игра окончена!\nЗаработано монет: {_game.Coins}", "Результат");
        }
    }
}