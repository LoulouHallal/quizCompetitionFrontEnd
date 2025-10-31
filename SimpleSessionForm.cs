using System;
using System.Drawing;
using System.Windows.Forms;

namespace PowerPointAddIn1.Forms
{
    public partial class SimpleSessionForm : Form
    {
        public SimpleSessionForm(string classCode)  // 🚨 THIS CONSTRUCTOR
        {
            // Form settings
            this.Text = $"Simple Quiz - {classCode}";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Add a simple label
            var label = new Label();
            label.Text = $"Class Code: {classCode}\nThis is a SIMPLE form!";
            label.Location = new Point(20, 20);
            label.Size = new Size(350, 60);
            label.Font = new Font("Arial", 12, FontStyle.Bold);
            label.ForeColor = Color.Blue;
            this.Controls.Add(label);

            // Add a simple button
            var button = new Button();
            button.Text = "Click Me!";
            button.Location = new Point(20, 100);
            button.Size = new Size(120, 35);
            button.BackColor = Color.LightGreen;
            button.Click += (s, e) => {
                MessageBox.Show("Button clicked! Form is working perfectly!");
            };
            this.Controls.Add(button);
        }

        // 🚨 REMOVE any other constructors, especially the default one:
        // public SimpleSessionForm() { }  ← DELETE THIS IF IT EXISTS
    }
}