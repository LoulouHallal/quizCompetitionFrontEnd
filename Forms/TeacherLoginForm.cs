using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using PowerPointAddIn1.Helpers;

namespace PowerPointAddIn1.Forms
{
    public partial class TeacherLoginForm : Form
    {

        public bool IsAuthenticated { get; private set; }
        public int TeacherId { get; private set; }

        private TextBox txtEmail;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblStatus;


        public TeacherLoginForm()
        {
            //InitializeComponent();
            CreateLoginUI();

        }

        private void CreateLoginUI()
        {
            this.Text = "Teacher Login";
            this.Size = new Size(350, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Email Label
            var lblEmail = new Label();
            lblEmail.Text = "Email:";
            lblEmail.Location = new Point(20, 30);
            lblEmail.Size = new Size(80, 20);
            this.Controls.Add(lblEmail);

            // Email TextBox
            txtEmail = new TextBox();
            txtEmail.Location = new Point(100, 30);
            txtEmail.Size = new Size(200, 20);
            txtEmail.Text = "teacher@demo.com"; // Default demo email
            this.Controls.Add(txtEmail);

            // Password Label
            var lblPassword = new Label();
            lblPassword.Text = "Password:";
            lblPassword.Location = new Point(20, 70);
            lblPassword.Size = new Size(80, 20);
            this.Controls.Add(lblPassword);

            // Password TextBox
            txtPassword = new TextBox();
            txtPassword.Location = new Point(100, 70);
            txtPassword.Size = new Size(200, 20);
            txtPassword.PasswordChar = '*';
            txtPassword.Text = "demo123"; // Default demo password
            this.Controls.Add(txtPassword);

            // Login Button
            btnLogin = new Button();
            btnLogin.Text = "Login";
            btnLogin.Location = new Point(100, 120);
            btnLogin.Size = new Size(120, 35);
            btnLogin.BackColor = Color.LightBlue;
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            // Status Label
            lblStatus = new Label();
            lblStatus.Location = new Point(20, 170);
            lblStatus.Size = new Size(300, 20);
            lblStatus.Text = "Use: teacher@demo.com / demo123";
            lblStatus.ForeColor = Color.Gray;
            this.Controls.Add(lblStatus);
        }
        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            try
            {
                btnLogin.Enabled = false;
                lblStatus.Text = "Authenticating...";
                lblStatus.ForeColor = Color.Blue;

                // Call your Flask backend to authenticate
                var (isAuthenticated, teacherId) = await AuthenticateTeacher(txtEmail.Text, txtPassword.Text);

                if (isAuthenticated)
                {
                    IsAuthenticated = true;
                    TeacherId = teacherId;
                    lblStatus.Text = "Login successful!";
                    lblStatus.ForeColor = Color.Green;

                    // Close form with OK result
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    lblStatus.Text = "Invalid email or password";
                    lblStatus.ForeColor = Color.Red;
                    btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
                btnLogin.Enabled = true;
            }
        }

        private async Task<(bool, int)> AuthenticateTeacher(string email, string password)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            using (var client = new HttpClient())
            {
                try
                {
                    var loginData = new
                    {
                        email = email,
                        password = password
                    };

                    var json = JsonSerializer.Serialize(loginData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Add API key for teacher authentication endpoint
                    client.DefaultRequestHeaders.Add("X-Teacher-API-Key", "dev-teacher-key");
                    client.Timeout = TimeSpan.FromSeconds(10);

                    string baseUrl = await NgrokHelper.GetNgrokBaseUrlAsync();

                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        MessageBox.Show("❌ ngrok URL is missing or invalid. Cannot authenticate.", "Login Error");
                        return (false, 0);
                    }

                    string loginUrl = $"{baseUrl}/api/teachers/login";

                    // 🔍 Optional debug log
                    Console.WriteLine($"🔗 Login URL: {loginUrl}");

                    Console.WriteLine("🔗 Sending POST to: " + loginUrl);
                    Console.WriteLine("📦 Payload: " + json);
                    MessageBox.Show($"🔗 Login URL: {loginUrl}", "Debug");
                    var response = await client.PostAsync(loginUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("📥 Response status: " + response.StatusCode);
                    Console.WriteLine("📥 Response body: " + responseContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        if (result.TryGetProperty("authenticated", out var authElem) &&
                            authElem.GetBoolean() &&
                            result.TryGetProperty("teacher_id", out var teacherIdElem))
                        {
                            return (true, teacherIdElem.GetInt32());
                        }
                    }

                    return (false, 0);
                }
                catch (Exception ex)
                {
                    // 🚨 TEMPORARY DEBUGGING    
                    MessageBox.Show($"Exception: {ex.Message}\n{ex.StackTrace}", "Debug - Error");
                    return (false, 0);
                }
            }
        }

        private void TeacherLoginForm_Load(object sender, EventArgs e)
        {

        }
    }
}
