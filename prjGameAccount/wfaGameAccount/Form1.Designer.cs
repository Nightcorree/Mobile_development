namespace wfaGameAccount
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblExpression = new Label();
            lblStats = new Label();
            lblTime = new Label();
            btnStart = new Button();
            btnYes = new Button();
            btnNo = new Button();
            SuspendLayout();
            // 
            // lblExpression
            // 
            lblExpression.AutoSize = true;
            lblExpression.Location = new Point(373, 73);
            lblExpression.Name = "lblExpression";
            lblExpression.Size = new Size(66, 20);
            lblExpression.TabIndex = 0;
            lblExpression.Text = "Пример";
            // 
            // lblStats
            // 
            lblStats.AutoSize = true;
            lblStats.Location = new Point(543, 39);
            lblStats.Name = "lblStats";
            lblStats.Size = new Size(49, 20);
            lblStats.TabIndex = 1;
            lblStats.Text = "Статы";
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.Location = new Point(87, 39);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(54, 20);
            lblTime.TabIndex = 2;
            lblTime.Text = "Вермя";
            // 
            // btnStart
            // 
            btnStart.Location = new Point(355, 271);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(94, 29);
            btnStart.TabIndex = 3;
            btnStart.Text = "Начать";
            btnStart.UseVisualStyleBackColor = true;
            // 
            // btnYes
            // 
            btnYes.Location = new Point(209, 347);
            btnYes.Name = "btnYes";
            btnYes.Size = new Size(94, 29);
            btnYes.TabIndex = 4;
            btnYes.Text = "Да";
            btnYes.UseVisualStyleBackColor = true;
            // 
            // btnNo
            // 
            btnNo.Location = new Point(485, 347);
            btnNo.Name = "btnNo";
            btnNo.Size = new Size(94, 29);
            btnNo.TabIndex = 5;
            btnNo.Text = "Нет";
            btnNo.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnNo);
            Controls.Add(btnYes);
            Controls.Add(btnStart);
            Controls.Add(lblTime);
            Controls.Add(lblStats);
            Controls.Add(lblExpression);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblExpression;
        private Label lblStats;
        private Label lblTime;
        private Button btnStart;
        private Button btnYes;
        private Button btnNo;
    }
}
