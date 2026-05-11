namespace wfaControlCreate
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.MouseDown += Form1_MouseDown;
        }
        private void Form1_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Label x = new Label();
                x.Location = e.Location;
                x.Text = $"{x.Location.X}, {x.Location.Y}";
                x.BackColor = Color.Lime;
                x.AutoSize = true;
                this.Controls.Add(x);
            }
            if (e.Button == MouseButtons.Right)
            {
                Random rnd = new Random();
                for (int i = 0; i < 10; i++)
                {
                    Label x = new Label();
                    x.Location = new Point(
                        rnd.Next(this.ClientSize.Width),
                        rnd.Next(this.ClientSize.Height)
                    );
                    x.Text = $"{x.Location.X}, {x.Location.Y}";
                    x.BackColor = Color.FromArgb(
                        rnd.Next(256),
                        rnd.Next(256),
                        rnd.Next(256)
                    );
                    x.AutoSize = true;
                    this.Controls.Add(x);
                }
            }
        }
    }
}
