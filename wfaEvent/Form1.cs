namespace wfaEvent
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            button2.Click += Button2_Click;
            button3.Click += delegate
            {
                MessageBox.Show("Способ 3");
            };
            button4.Click += (s, e) => MessageBox.Show("Способ 4");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Способ 1");
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Способ 2");
        }
    }
}
