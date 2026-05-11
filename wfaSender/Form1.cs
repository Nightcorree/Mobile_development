namespace wfaSender
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                MessageBox.Show("Нажата клавиша Enter");

            switch (e.KeyCode)
            {
                case Keys.Left:
                    label1.Text = "Left";
                    break;
                case Keys.Up:
                    label1.Text = "Up";
                    break;
                case Keys.Right:
                    label1.Text = "Right";
                    break;
                case Keys.Down:
                    label1.Text = "Down";
                    break;
                default:
                    label1.Text = $"Другая клавиша = {e.KeyCode}";
                    break;
        }
    }
    }
}
