using PowerPointAddIn1.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json.Serialization;



namespace PowerPointAddIn1.Forms
{
    public partial class SessionControlForm : Form
    {
        // UI Controls to add
        private ComboBox cmbQuestions;
        private Button btnStopQuestion;
        private Button btnShowResults;
        private Label lblQuestionStatus;
        private Label lblTimer;
        private Button btnRefreshQuestions;

        private long _classId;
        private long teacherId;
        private long _courseId;
        private string _classCode;
        private string _presentationName;
        private HttpClient _client;
        private Timer _questionTimer;
        private int _timeRemaining;

        private List<Question> _availableQuestions = new List<Question>();


        public class Question
        {
            [JsonPropertyName("question_id")]
            public int Id { get; set; }
            [JsonPropertyName("content")]
            public string Content { get; set; }
            [JsonPropertyName("question_content")] // 🚨 ADD THIS - backend might use this
            public string QuestionContent { get; set; }
            [JsonPropertyName("points")]
            public int Points { get; set; }
            [JsonPropertyName("time_limit_seconds")]
            public int TimeLimit { get; set; }
            // Helper property to get content from either field
            public string DisplayContent => Content ?? QuestionContent;
        }

        // UI Controls

        private Label lblClassCode;
        private Label lblStatus;
        private Label lblStudents;
        private Button btnStartSession;
        private Button btnActivateQuestion;
        private Button btnRefreshLeaderboard;
        private ListBox listBoxLog;

        public SessionControlForm(long classId, string classCode, string presentationName, int currentSlide, long teacherId, string qrCodeUrl, long courseId)
        {
           // InitializeComponent();

            _classId = classId;
            _classCode = classCode;
            _presentationName = presentationName;
            this.teacherId = teacherId;
            _courseId = courseId; // 🚨 STORE COURSE ID

            // Initialize HTTP client
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(10);

            string apiKey = "dev-teacher-key";
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");


            CreateUI();

            // Update UI
            lblClassCode.Text = $"Class Code: {_classCode}";
            lblStatus.Text = "Status: Ready";
            lblStudents.Text = "Students: 0";
            DisplayQrCode(qrCodeUrl);  // 🚨 ADD THIS!


            AddLog("Form initialized successfully");

            // 🚨 ADD THIS LINE - Start background updates
           // StartBackgroundUpdates();
        }

        private void CreateUI()
        {
            // Form settings
            
            this.Text = $"Quiz Session - {_classCode}";
            this.Size = new Size(600, 500); // Override the designer size
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;


            // Class Code Label
            lblClassCode = new Label();
            lblClassCode.Location = new Point(20, 20);
            lblClassCode.Size = new Size(300, 25);
            lblClassCode.Font = new Font("Arial", 12, FontStyle.Bold);
            lblClassCode.ForeColor = Color.DarkBlue; // 🚨 Different color
            lblClassCode.Text = "Class Code: ---";
            this.Controls.Add(lblClassCode);

            // Status Label
            lblStatus = new Label();
            lblStatus.Location = new Point(20, 50);
            lblStatus.Size = new Size(300, 20);
            lblStatus.Text = "Status: Initializing...";
            this.Controls.Add(lblStatus);

            // Students Label
            lblStudents = new Label();
            lblStudents.Location = new Point(20, 80);
            lblStudents.Size = new Size(300, 20);
            lblStudents.Text = "Students: 0";
            this.Controls.Add(lblStudents);

            // Start Session Button
            btnStartSession = new Button();
            btnStartSession.Location = new Point(20, 110);
            btnStartSession.Size = new Size(120, 35);
            btnStartSession.Text = "Start Session";
            btnStartSession.BackColor = Color.LightGreen;
            btnStartSession.Click += BtnStartSession_Click;
            this.Controls.Add(btnStartSession);

            // Activate Question Button
            btnActivateQuestion = new Button();
            btnActivateQuestion.Location = new Point(150, 110);
            btnActivateQuestion.Size = new Size(120, 35);
            btnActivateQuestion.Text = "Activate Question";
            btnActivateQuestion.BackColor = Color.LightBlue;
            btnActivateQuestion.Enabled = false;
            btnActivateQuestion.Click += BtnActivateQuestion_Click;
            this.Controls.Add(btnActivateQuestion);

            // Refresh Leaderboard Button
            btnRefreshLeaderboard = new Button();
            btnRefreshLeaderboard.Location = new Point(280, 110);
            btnRefreshLeaderboard.Size = new Size(120, 35);
            btnRefreshLeaderboard.Text = "Refresh";
            btnRefreshLeaderboard.BackColor = Color.LightYellow;
            btnRefreshLeaderboard.Click += BtnRefreshLeaderboard_Click;
            this.Controls.Add(btnRefreshLeaderboard);

            // Add after your existing controls (around line 110-120)

            // Question Selection Label
            var lblSelectQuestion = new Label();
            lblSelectQuestion.Text = "Select Question:";
            lblSelectQuestion.Location = new Point(20, 170);
            lblSelectQuestion.Size = new Size(100, 20);
            this.Controls.Add(lblSelectQuestion);

            // Question ComboBox
            cmbQuestions = new ComboBox();
            cmbQuestions.Location = new Point(120, 170);
            cmbQuestions.Size = new Size(300, 25);
            cmbQuestions.DropDownStyle = ComboBoxStyle.DropDownList;
            this.Controls.Add(cmbQuestions);

            // Refresh Questions Button
            btnRefreshQuestions = new Button();
            btnRefreshQuestions.Text = "Refresh";
            btnRefreshQuestions.Location = new Point(430, 170);
            btnRefreshQuestions.Size = new Size(80, 25);
            btnRefreshQuestions.Click += BtnRefreshQuestions_Click;
            this.Controls.Add(btnRefreshQuestions);

            // Question Status
            lblQuestionStatus = new Label();
            lblQuestionStatus.Location = new Point(20, 200);
            lblQuestionStatus.Size = new Size(300, 20);
            lblQuestionStatus.Text = "No active question";
            lblQuestionStatus.ForeColor = Color.Blue;
            this.Controls.Add(lblQuestionStatus);

            // Timer Display
            lblTimer = new Label();
            lblTimer.Location = new Point(20, 225);
            lblTimer.Size = new Size(100, 25);
            lblTimer.Text = "Time: --";
            lblTimer.Font = new Font(lblTimer.Font, FontStyle.Bold);
            lblTimer.ForeColor = Color.Red;
            this.Controls.Add(lblTimer);

            // Stop Question Button
            btnStopQuestion = new Button();
            btnStopQuestion.Text = "Stop Question";
            btnStopQuestion.Location = new Point(150, 220);
            btnStopQuestion.Size = new Size(120, 35);
            btnStopQuestion.BackColor = Color.LightCoral;
            btnStopQuestion.Enabled = false;
            btnStopQuestion.Click += BtnStopQuestion_Click;
            this.Controls.Add(btnStopQuestion);

            // Show Results Button
            btnShowResults = new Button();
            btnShowResults.Text = "Show Results";
            btnShowResults.Location = new Point(280, 220);
            btnShowResults.Size = new Size(120, 35);
            btnShowResults.BackColor = Color.LightBlue;
            btnShowResults.Click += BtnShowResults_Click;
            this.Controls.Add(btnShowResults);


            // Log ListBox
            listBoxLog = new ListBox();
            listBoxLog.Location = new Point(20, 280);
            listBoxLog.Size = new Size(550, 150);
            listBoxLog.Font = new Font("Consolas", 9);
            this.Controls.Add(listBoxLog);

            // Add initial log entry
            AddLog("Session Control Form Started");
            AddLog($"Class: {_classCode}");
            AddLog("Ready to begin quiz session");
        }



        private void AddLog(string message)
        {
            if (listBoxLog.InvokeRequired)
            {
                listBoxLog.Invoke(new Action<string>(AddLog), message);
            }
            else
            {
                listBoxLog.Items.Add($"{DateTime.Now:HH:mm:ss} - {message}");
                listBoxLog.SelectedIndex = listBoxLog.Items.Count - 1;
                listBoxLog.Refresh();
            }
        }

        private async void BtnStartSession_Click(object sender, EventArgs e)
        {
            try
            {
                AddLog("Starting session...");
                btnStartSession.Enabled = false;
                lblStatus.Text = "Status: Starting session...";


                var response = await _client.PostAsync($"http://localhost:5000/api/sessions/{_classId}/start", null);


                if (response.IsSuccessStatusCode)
                {
                    AddLog("Session started successfully!");
                    lblStatus.Text = "Status: Session Active!";
                    btnActivateQuestion.Enabled = true;
                    MessageBox.Show("Session started! Students can now join.", "Success");

                    // 🆕 ADD THIS ONE LINE ONLY - Load questions when session starts
                    if (btnRefreshQuestions != null) LoadAvailableQuestions();

                    // 🚨 ADD: Start more frequent updates when session is active
                    StartBackgroundUpdates();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    AddLog($"Failed to start session: {errorContent}");
                    lblStatus.Text = "Status: Failed to start";
                    btnStartSession.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error starting session: {ex.Message}");
                lblStatus.Text = "Status: Error";
                btnStartSession.Enabled = true;
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }
        /*
                private async void BtnActivateQuestion_Click(object sender, EventArgs e)
                {
                    try
                    {
                        AddLog("Activating question...");
                        lblStatus.Text = "Status: Activating question...";

                        var response = await _client.PostAsync($"http://localhost:5000/api/sessions/{_classId}/activate/0", null);

                        if (response.IsSuccessStatusCode)
                        {
                            AddLog("Question activated successfully!");
                            lblStatus.Text = "Status: Question Active!";
                            MessageBox.Show("Question activated! Students can now answer.", "Success");
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            AddLog($"Failed to activate question: {errorContent}");
                            lblStatus.Text = "Status: Failed to activate";
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Error activating question: {ex.Message}");
                        lblStatus.Text = "Status: Error";
                        MessageBox.Show($"Error: {ex.Message}", "Error");
                    }
                }
        */
        private async void BtnActivateQuestion_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if we have questions loaded for real-time activation
                if (cmbQuestions != null && cmbQuestions.SelectedIndex != -1 && _availableQuestions.Count > 0)
                {
                    // 🆕 NEW: Real-time question activation with selection
                    var selectedQuestion = _availableQuestions[cmbQuestions.SelectedIndex];

                   // AddLog($"Activating question: {selectedQuestion.DisplayContent}");
                    AddLog($"Activating question: {selectedQuestion.DisplayContent}");

                    lblStatus.Text = "Status: Activating question...";

                    var activationData = new { question_id = selectedQuestion.Id };
                    var json = JsonSerializer.Serialize(activationData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _client.PostAsync(
                        $"http://localhost:5000/api/sessions/{_classId}/activate-question",
                        content);

                    if (response.IsSuccessStatusCode)
                    {
                        // 🆕 NEW: Start timer and update UI
                        _timeRemaining = selectedQuestion.TimeLimit;
                        StartQuestionTimer();

                        btnActivateQuestion.Enabled = false;
                        if (btnStopQuestion != null) btnStopQuestion.Enabled = true;
                        if (lblQuestionStatus != null)
                        {
                            lblQuestionStatus.Text = $"ACTIVE: {selectedQuestion.Content}";
                            lblQuestionStatus.ForeColor = Color.Green;
                        }

                        AddLog($"Question activated: {selectedQuestion.Content}");
                        AddLog($"Students have {_timeRemaining} seconds to answer");
                        lblStatus.Text = "Status: Question Active!";
                        //MessageBox.Show("Question activated! Students can now answer.", "Success");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        AddLog($"Failed to activate question: {errorContent}");
                        lblStatus.Text = "Status: Failed to activate";

                        // 🆕 FALLBACK: Try old method if new one fails
                        await FallbackActivateQuestion();
                    }
                }
                else
                {
                    // 🎯 OLD METHOD: Fallback to original behavior
                    await FallbackActivateQuestion();
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error activating question: {ex.Message}");
                lblStatus.Text = "Status: Error";
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        // 🆕 ADD THIS HELPER METHOD FOR BACKWARD COMPATIBILITY
        private async Task FallbackActivateQuestion()
        {
            AddLog("Using fallback question activation...");
            lblStatus.Text = "Status: Activating question...";

            var response = await _client.PostAsync($"http://localhost:5000/api/sessions/{_classId}/activate/0", null);

            if (response.IsSuccessStatusCode)
            {
                AddLog("Question activated successfully!");
                lblStatus.Text = "Status: Question Active!";
                MessageBox.Show("Question activated! Students can now answer.", "Success");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                AddLog($"Failed to activate question: {errorContent}");
                lblStatus.Text = "Status: Failed to activate";
            }
        }
        private async void BtnRefreshLeaderboard_Click(object sender, EventArgs e)
        {
            try
            {
                AddLog("Refreshing leaderboard...");

                var response = await _client.GetAsync($"http://localhost:5000/api/sessions/{_classId}/leaderboard");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    AddLog("Leaderboard refreshed successfully");

                    // Parse and display leaderboard
                    var result = JsonSerializer.Deserialize<JsonElement>(content);
                    if (result.TryGetProperty("leaderboard", out var lbArray))
                    {
                        int studentCount = 0;
                        foreach (var item in lbArray.EnumerateArray())
                        {
                            var name = item.GetProperty("name").GetString();
                            var score = item.GetProperty("score").GetInt32();
                            AddLog($"  {name}: {score} points");
                            studentCount++;
                        }
                        lblStudents.Text = $"Students: {studentCount}";
                    }
                }
                else
                {
                    AddLog("Failed to get leaderboard");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error refreshing leaderboard: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _client?.Dispose();
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SessionControlForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "SessionControlForm";
            this.Load += new System.EventHandler(this.SessionControlForm_Load);
            this.ResumeLayout(false);

        }

        private void SessionControlForm_Load(object sender, EventArgs e)
        {

        }

        private async void StartBackgroundUpdates()
        {
            try
            {
                // Update student count every 3 seconds
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 3000;
                timer.Tick += async (s, e) => await UpdateStudentCount();
                timer.Start();

                // Initial update
                await UpdateStudentCount();
            }
            catch (Exception ex)
            {
                AddLog($"Background updates error: {ex.Message}");
            }
        }

        private async Task UpdateStudentCount()
        {
            try
            {
                var response = await _client.GetAsync($"http://localhost:5000/api/sessions/{_classId}/leaderboard");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    if (result.TryGetProperty("leaderboard", out var lbArray))
                    {
                        int studentCount = 0;
                        foreach (var item in lbArray.EnumerateArray())
                        {
                            studentCount++;
                        }

                        // Update UI thread-safe
                        if (lblStudents.InvokeRequired)
                        {
                            lblStudents.Invoke(new Action(() => lblStudents.Text = $"Students: {studentCount}"));
                        }
                        else
                        {
                            lblStudents.Text = $"Students: {studentCount}";
                        }

                        if (studentCount > 0)
                        {
                            AddLog($"{studentCount} student(s) joined");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail for background updates
            }
        }

        private void DisplayQrCode(string qrUrl)
        {
            try
            {
               // AddLog($"DisplayQrCode called with: {qrUrl}"); // 🚨 DEBUG

                if (string.IsNullOrEmpty(qrUrl))
                {
                 //   AddLog("QR URL is null or empty"); // 🚨 DEBUG
                    DisplayJoinCodeFallback();
                    return;
                }
               // AddLog($"QR URL length: {qrUrl.Length}"); // 🚨 DEBUG


                // Create PictureBox for QR code
                var picBox = new PictureBox();
                picBox.SizeMode = PictureBoxSizeMode.StretchImage;
                picBox.Location = new Point(400, 20);
                picBox.Size = new Size(150, 150);
                this.Controls.Add(picBox);

                // Load image asynchronously from URL
                LoadQrImageAsync(picBox, qrUrl);

                AddLog("QR code loading from URL...");
            }
            catch (Exception ex)
            {
                AddLog($"Error displaying QR code: {ex.Message}");

                DisplayJoinCodeFallback();
            }
        }
        private async void LoadQrImageAsync(PictureBox picBox, string qrUrl)
        {
            try
            {
                //AddLog($"=== LOAD QR IMAGE DEBUG START ===");
                //AddLog($"Loading QR from: {qrUrl}");

                using (var client = new HttpClient())
                {
                    // QR URL is relative, so add the base URL
                    var fullUrl = $"http://localhost:5000{qrUrl}";
                    //AddLog($"Full URL: {fullUrl}");

                    //AddLog("Sending HTTP request...");
                    var imageBytes = await client.GetByteArrayAsync(fullUrl);
                    //AddLog($"✅ HTTP request successful - received {imageBytes.Length} bytes");

                    // 🚨 CHECK IF IT'S SVG AND HANDLE DIFFERENTLY
                    if (qrUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    {
                        //AddLog("⚠️ SVG format detected - using fallback display");
                        DisplayJoinCodeFallback();
                        return;
                    }

                    //AddLog("Creating image from bytes...");
                    using (var ms = new System.IO.MemoryStream(imageBytes))
                    {
                        var image = Image.FromStream(ms);
                       // AddLog($"✅ Image created - Size: {image.Size}");

                        picBox.Image = image;
                        //AddLog("✅ QR code loaded and displayed successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ ERROR loading QR image: {ex.Message}");
                AddLog($"❌ Error type: {ex.GetType().Name}");
                DisplayJoinCodeFallback();
            }
            finally
            {
                //AddLog($"=== LOAD QR IMAGE DEBUG END ===");
            }
        }

        private void DisplayJoinCodeFallback()
        {
            // Display the join code prominently as fallback
            var lblJoinInfo = new Label();
            lblJoinInfo.Location = new Point(400, 20);
            lblJoinInfo.Size = new Size(180, 100);
            lblJoinInfo.Text = $"Students join using:\n\n🔢 CODE:\n{_classCode}\n\nOr visit join link";
            lblJoinInfo.TextAlign = ContentAlignment.MiddleCenter;
            lblJoinInfo.Font = new Font("Arial", 12, FontStyle.Bold);
            lblJoinInfo.ForeColor = Color.DarkBlue;
            lblJoinInfo.BorderStyle = BorderStyle.FixedSingle;
            this.Controls.Add(lblJoinInfo);

            AddLog("Displaying join code as fallback");
        }

        private async void LoadAvailableQuestions()
        {
            try
            {
                AddLog($"Loading questions for course {_courseId}..."); // 🚨 USE ACTUAL COURSE ID
                                                                        //var response = await _client.GetAsync($"http://localhost:5000/api/teacher/{teacherId}/questions");


                // Use the course ID from constructor
                var response = await _client.GetAsync($"http://localhost:5000/api/courses/{_courseId}/questions");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    AddLog($"Raw API response: {content}"); // 🚨 DEBUG

                    _availableQuestions = JsonSerializer.Deserialize<List<Question>>(content);

                    cmbQuestions.Items.Clear();

                    if (_availableQuestions != null && _availableQuestions.Count > 0)
                    {
                        foreach (var question in _availableQuestions)
                        {
                            string displayText = question.Content.Length > 30
                             ? question.Content.Substring(0, 30) + "..."
                             : question.Content;
                            //cmbQuestions.Items.Add($"{question.Content} ({question.Points} pts)");
                            cmbQuestions.Items.Add($"{question.DisplayContent} ({question.Points} pts)");

                        }
                        cmbQuestions.SelectedIndex = 0;
                        AddLog($"✅ Loaded {_availableQuestions.Count} questions from course {_courseId}");
                    }
                    else
                    {
                        AddLog($"❌ No questions found in course {_courseId}");
                        // Remove dummy questions - use real data only
                        cmbQuestions.Items.Add("No questions available - create questions first");
                    }
                }
                else
                {
                    AddLog($"❌ Failed to load questions: {response.StatusCode}");
                    AddDummyQuestions();
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error loading questions: {ex.Message}");
                AddDummyQuestions();
            }
        }
        // 🚨 TEMPORARY: Add dummy questions so you can test the activation
        private void AddDummyQuestions()
        {
            _availableQuestions = new List<Question>
    {
        new Question { Id = 1, Content = "What is 2+2?", Points = 100, TimeLimit = 30 },
        new Question { Id = 2, Content = "Capital of France?", Points = 100, TimeLimit = 30 },
        new Question { Id = 3, Content = "Largest planet?", Points = 100, TimeLimit = 30 }
    };

            cmbQuestions.Items.Clear();
            foreach (var question in _availableQuestions)
            {
                cmbQuestions.Items.Add($"{question.Content} ({question.Points} pts)");
            }

            if (cmbQuestions.Items.Count > 0)
                cmbQuestions.SelectedIndex = 0;

            AddLog("✅ Loaded dummy questions for testing");
        }

        private async void BtnRefreshQuestions_Click(object sender, EventArgs e)
        {
            LoadAvailableQuestions();
        }

        private async void BtnStopQuestion_Click(object sender, EventArgs e)
        {
            await StopCurrentQuestion();
        }

        private async void BtnShowResults_Click(object sender, EventArgs e)
        {
            if (cmbQuestions.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a question first");
                return;
            }

            try
            {
                var selectedQuestion = _availableQuestions[cmbQuestions.SelectedIndex];
                var resultsForm = new QuestionResultsForm(selectedQuestion.Id, (int)_classId);
                resultsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing results: {ex.Message}");
            }
        }

        private void StartQuestionTimer()
        {
            _questionTimer = new Timer();
            _questionTimer.Interval = 1000;
            _questionTimer.Tick += QuestionTimer_Tick;
            _questionTimer.Start();
            UpdateTimerDisplay();
        }

        private void QuestionTimer_Tick(object sender, EventArgs e)
        {
            _timeRemaining--;
            UpdateTimerDisplay();

            if (_timeRemaining <= 0)
            {
                StopQuestionTimer();
                AutoStopQuestion();
            }
        }

        private void UpdateTimerDisplay()
        {
            if (lblTimer.InvokeRequired)
            {
                lblTimer.Invoke(new Action(() =>
                {
                    lblTimer.Text = $"Time: {_timeRemaining}s";
                    lblTimer.ForeColor = _timeRemaining <= 10 ? Color.Red :
                                       _timeRemaining <= 20 ? Color.Orange : Color.Green;
                }));
            }
            else
            {
                lblTimer.Text = $"Time: {_timeRemaining}s";
                lblTimer.ForeColor = _timeRemaining <= 10 ? Color.Red :
                                   _timeRemaining <= 20 ? Color.Orange : Color.Green;
            }
        }

        private async void AutoStopQuestion()
        {
            await StopCurrentQuestion();
            AddLog("Time's up! Question automatically stopped.");
        }

        private async Task StopCurrentQuestion()
        {
            try
            {
                StopQuestionTimer();

                var response = await _client.PostAsync(
                    $"http://localhost:5000/api/sessions/{_classId}/stop-question",
                    new StringContent("{}", Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    btnActivateQuestion.Enabled = true;
                    btnStopQuestion.Enabled = false;
                    lblQuestionStatus.Text = "Question STOPPED";
                    lblQuestionStatus.ForeColor = Color.Red;
                    lblTimer.Text = "Time: --";

                    AddLog("Question stopped. Ready for next question.");
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error stopping question: {ex.Message}");
            }
        }

        private void StopQuestionTimer()
        {
            _questionTimer?.Stop();
            _questionTimer?.Dispose();
            _questionTimer = null;
        }


    }
}