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
using PowerPointAddIn1.Helpers;




namespace PowerPointAddIn1.Forms
{
    public partial class SessionControlForm : Form
    {
        // Add this field to track class ID changes
        private long _originalClassId;
        private Button _btnLiveLeaderboard;
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
        private Question _currentActiveQuestion; // 🚨 TRACK CURRENT ACTIVE QUESTION


        private List<Question> _availableQuestions = new List<Question>();
        private string _ngrokBaseUrl;

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

            _originalClassId = classId; // 🚨 STORE ORIGINAL


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

            AddLog($" Session created - ClassId: {_classId}, CourseId: {_courseId}, TeacherId: {teacherId}");
            // 🚨 Call async initializer
            _ = InitializeAsync();

        }

        private async Task InitializeAsync()
        {
            _ngrokBaseUrl = await NgrokHelper.GetNgrokBaseUrlAsync();
            if (string.IsNullOrEmpty(_ngrokBaseUrl))
            {
                AddLog("❌ Failed to retrieve ngrok URL.");
            }
            else
            {
                AddLog($"✅ ngrok URL loaded: {_ngrokBaseUrl}");
            }

            // Optionally: preload questions, check health, etc.
        }

        private void CreateUI()
        {
            // Form settings
            
            this.Text = $"Quiz Session - {_classCode}";
            this.Size = new Size(600, 500); // Override the designer size
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;


            int buttonY = 230;


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
            btnStartSession.Location = new Point(20,buttonY);
            btnStartSession.Size = new Size(120, 35);
            btnStartSession.Text = "Start Session";
            btnStartSession.BackColor = Color.LightGreen;
            btnStartSession.Click += BtnStartSession_Click;
            this.Controls.Add(btnStartSession);

            // Activate Question Button
            btnActivateQuestion = new Button();
            btnActivateQuestion.Location = new Point(150, buttonY);
            btnActivateQuestion.Size = new Size(120, 35);
            btnActivateQuestion.Text = "Activate Question";
            btnActivateQuestion.BackColor = Color.LightBlue;
            btnActivateQuestion.Enabled = false;
            btnActivateQuestion.Click += BtnActivateQuestion_Click;
            this.Controls.Add(btnActivateQuestion);

            // Refresh Leaderboard Button
            btnRefreshLeaderboard = new Button();
            btnRefreshLeaderboard.Location = new Point(280, buttonY);
            btnRefreshLeaderboard.Size = new Size(120, 35);
            btnRefreshLeaderboard.Text = "Refresh";
            btnRefreshLeaderboard.BackColor = Color.LightYellow;
            btnRefreshLeaderboard.Click += BtnRefreshLeaderboard_Click;
            this.Controls.Add(btnRefreshLeaderboard);

            // Question Selection Label
            var lblSelectQuestion = new Label();
            lblSelectQuestion.Text = "Select Question:";
            lblSelectQuestion.Location = new Point(20, 270);
            lblSelectQuestion.Size = new Size(100, 20);
            this.Controls.Add(lblSelectQuestion);

            // Question ComboBox
            cmbQuestions = new ComboBox();
            cmbQuestions.Location = new Point(120, 270);
            cmbQuestions.Size = new Size(300, 25);
            cmbQuestions.DropDownStyle = ComboBoxStyle.DropDownList;
            this.Controls.Add(cmbQuestions);

            // Refresh Questions Button
            btnRefreshQuestions = new Button();
            btnRefreshQuestions.Text = "Refresh";
            btnRefreshQuestions.Location = new Point(430, 270);
            btnRefreshQuestions.Size = new Size(80, 25);
            btnRefreshQuestions.Click += BtnRefreshQuestions_Click;
            this.Controls.Add(btnRefreshQuestions);

            // Question Status
            lblQuestionStatus = new Label();
            lblQuestionStatus.Location = new Point(20, 300);
            lblQuestionStatus.Size = new Size(300, 20);
            lblQuestionStatus.Text = "No active question";
            lblQuestionStatus.ForeColor = Color.Blue;
            this.Controls.Add(lblQuestionStatus);

            // Timer Display
            lblTimer = new Label();
            lblTimer.Location = new Point(20, 325);
            lblTimer.Size = new Size(100, 25);
            lblTimer.Text = "Time: --";
            lblTimer.Font = new Font(lblTimer.Font, FontStyle.Bold);
            lblTimer.ForeColor = Color.Red;
            this.Controls.Add(lblTimer);

            // Stop Question Button
            btnStopQuestion = new Button();
            btnStopQuestion.Text = "Stop Question";
            btnStopQuestion.Location = new Point(150, 320);
            btnStopQuestion.Size = new Size(120, 35);
            btnStopQuestion.BackColor = Color.LightCoral;
            btnStopQuestion.Enabled = false;
            btnStopQuestion.Click += BtnStopQuestion_Click;
            this.Controls.Add(btnStopQuestion);

            // Show Results Button
            btnShowResults = new Button();
            btnShowResults.Text = "Show Results";
            btnShowResults.Location = new Point(280, 320);
            btnShowResults.Size = new Size(120, 35);
            btnShowResults.BackColor = Color.LightBlue;
            btnShowResults.Click += BtnShowResults_Click;
            this.Controls.Add(btnShowResults);


            // Log ListBox
            listBoxLog = new ListBox();
            listBoxLog.Location = new Point(20, 380);
            listBoxLog.Size = new Size(550, 80);
            listBoxLog.Font = new Font("Consolas", 9);
            this.Controls.Add(listBoxLog);

            // Add this button near your other buttons (around line 110)
            _btnLiveLeaderboard = new Button();
            _btnLiveLeaderboard.Location = new Point(410, buttonY);
            _btnLiveLeaderboard.Size = new Size(120, 35);
            _btnLiveLeaderboard.Text = "Live Leaderboard";
            _btnLiveLeaderboard.BackColor = Color.LightGoldenrodYellow;
            _btnLiveLeaderboard.Click += BtnLiveLeaderboard_Click;
            this.Controls.Add(_btnLiveLeaderboard);
        }
        private void BtnLiveLeaderboard_Click(object sender, EventArgs e)
        {
            var leaderboardForm = new LiveLeaderboardForm((int)_classId);
            leaderboardForm.Show();
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
                //AddLog("Starting session...");
                btnStartSession.Enabled = false;
                lblStatus.Text = "Status: Starting session...";


                //var response = await _client.PostAsync($"http://localhost:5000/api/sessions/{_classId}/start", null);
                //var response = await _client.PostAsync($"http://192.168.0.102:5000/api/sessions/{_classId}/start", null);
                var response = await _client.PostAsync($"{_ngrokBaseUrl}/api/sessions/{_classId}/start", null);

                if (response.IsSuccessStatusCode)
                {
                    //AddLog("Session started successfully!");
                    lblStatus.Text = "Status: Session Active!";
                    btnActivateQuestion.Enabled = true;

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

        private async void BtnActivateQuestion_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbQuestions != null && cmbQuestions.SelectedIndex != -1 && _availableQuestions.Count > 0)
                {
                    var selectedQuestion = _availableQuestions[cmbQuestions.SelectedIndex];

                    //AddLog($"Attempting to activate question ID: {selectedQuestion.Id}");

                    var activationData = new { question_id = selectedQuestion.Id };
                    var json = JsonSerializer.Serialize(activationData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // 🚨 ADD AUTHORIZATION HEADER TO THE REQUEST
                    //string url = $"http://192.168.0.102:5000/api/sessions/{_classId}/activate-question";
                    string url = $"{_ngrokBaseUrl}/api/sessions/{_classId}/activate-question";
                    // Create a new HttpClient for this request to ensure clean headers
                    using (var activationClient = new HttpClient())
                    {
                        activationClient.Timeout = TimeSpan.FromSeconds(10);
                        //activationClient.DefaultRequestHeaders.Add("Authorization", "Bearer dev-teacher-key");
                        // 🚨 USE X-Teacher-API-Key INSTEAD OF Authorization
                        activationClient.DefaultRequestHeaders.Add("X-Teacher-API-Key", "dev-teacher-key");

                      

                        var response = await activationClient.PostAsync(url, content);

                        //AddLog($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

                        var responseContent = await response.Content.ReadAsStringAsync();
                        //AddLog($"Response Content: {responseContent}");

                        if (response.IsSuccessStatusCode)
                        {
                            // 🚨 STORE CURRENTLY ACTIVE QUESTION
                            _currentActiveQuestion = selectedQuestion;
                            _timeRemaining = selectedQuestion.TimeLimit;
                            StartQuestionTimer();

                            // 🚨 UPDATE UI FOR ACTIVE QUESTION
                            btnActivateQuestion.Enabled = false;
                            if (btnStopQuestion != null) btnStopQuestion.Enabled = true;
                            if (lblQuestionStatus != null)
                            {
                                lblQuestionStatus.Text = $"ACTIVE: {selectedQuestion.DisplayContent}";
                                lblQuestionStatus.ForeColor = Color.Green;
                            }
                            // 🚨 MARK THE ACTIVE QUESTION IN THE DROPDOWN
                            cmbQuestions.Enabled = false; // Disable dropdown while question is active

                            AddLog($"✅ Question '{selectedQuestion.DisplayContent}' activated successfully!");
                            AddLog($"Students have {_timeRemaining} seconds to answer");
                            lblStatus.Text = "Status: Question Active!";
                        }
                        else
                        {
                            AddLog($"❌ Activation failed: {responseContent}");
                        }
                    }
                }
                else
                {
                    await FallbackActivateQuestion();
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Exception in activation: {ex.Message}");
            }
        }
        
        private async Task FallbackActivateQuestion()
        {
            //AddLog("Using fallback question activation...");

            try
            {
                using (var fallbackClient = new HttpClient())
                {
                    fallbackClient.Timeout = TimeSpan.FromSeconds(10);
                    //fallbackClient.DefaultRequestHeaders.Add("Authorization", "Bearer dev-teacher-key");
                    fallbackClient.DefaultRequestHeaders.Add("X-Teacher-API-Key", "dev-teacher-key"); // 🚨 FIX HEADER

                    var response = await fallbackClient.PostAsync(
                        $"{_ngrokBaseUrl} /api/sessions/{_classId}/activate/0",
                        null);

                    if (response.IsSuccessStatusCode)
                    {
                        //AddLog("Fallback activation successful!");
                        lblStatus.Text = "Status: Question Active!";
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        AddLog($"Fallback activation failed: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"Fallback activation error: {ex.Message}");
            }
        }
        private async void BtnRefreshLeaderboard_Click(object sender, EventArgs e)
        {
            try
            {
                //AddLog("Refreshing leaderboard...");

                //var response = await _client.GetAsync($"http://localhost:5000/api/sessions/{_classId}/leaderboard");
                var response = await _client.GetAsync($"{_ngrokBaseUrl} /api/sessions/{_classId}/leaderboard");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    //AddLog("Leaderboard refreshed successfully");

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

        private int _previousStudentCount = 0; // Add this class-level variable

        private async Task UpdateStudentCount()
        {
            try
            {
                //var response = await _client.GetAsync($"http://localhost:5000/api/sessions/{_classId}/leaderboard");
                var response = await _client.GetAsync($"{_ngrokBaseUrl} /api/sessions/{_classId}/participants/count");
                
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

                        // 🚨 ONLY LOG WHEN STUDENT COUNT CHANGES
                        if (studentCount > 0 && studentCount != _previousStudentCount)
                        {
                            AddLog($"{studentCount} student(s) joined");
                            _previousStudentCount = studentCount; // Update the previous count
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

                //AddLog("QR code loading from URL...");
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
                    //var fullUrl = $"http://192.168.0.102:5000{qrUrl}";
                    string baseUrl = await NgrokHelper.GetNgrokBaseUrlAsync();
                    var fullUrl = $"{baseUrl}{qrUrl}";

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
                // 🚨 TEST BACKEND CONNECTION FIRST
                try
                {
                    //var healthResponse = await _client.GetAsync("http://192.168.0.102:5000/health");
                    string baseUrl = await NgrokHelper.GetNgrokBaseUrlAsync();
                    var healthResponse = await _client.GetAsync($"{baseUrl}/health");
                

             }
            catch (Exception healthEx)
                {
                    AddLog($"❌ Backend connection failed: {healthEx.Message}");
                }
                //string url = $"http://localhost:5000/api/courses/{_courseId}/questions";
                string url = $"{_ngrokBaseUrl}/api/courses/{_courseId}/questions";


                var response = await _client.GetAsync(url);

                var responseContent = await response.Content.ReadAsStringAsync();
                

                cmbQuestions.Items.Clear();


                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        _availableQuestions = JsonSerializer.Deserialize<List<Question>>(responseContent);
                        //AddLog($"Deserialized questions count: {_availableQuestions?.Count ?? 0}");

                        if (_availableQuestions != null && _availableQuestions.Count > 0)
                        {
                            foreach (var question in _availableQuestions)
                            {
                                string displayText = question.DisplayContent.Length > 50
                                    ? question.DisplayContent.Substring(0, 50) + "..."
                                    : question.DisplayContent;
                                cmbQuestions.Items.Add($"{displayText} ({question.Points} pts)");

                                //AddLog($"  - Loaded: ID={question.Id}, Content='{question.DisplayContent}'");

                            }
                            cmbQuestions.SelectedIndex = 0;
                            //AddLog($"✅ SUCCESS: Loaded {_availableQuestions.Count} questions");
                        }
                        else
                        {
                            //AddLog($"❌ No questions found in course {_courseId}");
                            cmbQuestions.Items.Add("No questions available - create questions first");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        AddLog($"❌ JSON Deserialization Error: {jsonEx.Message}");
                        cmbQuestions.Items.Add("Error: Invalid response format");
                    }
                }
                else
                {
                    AddLog($"❌ API Error: {response.StatusCode}");
                    if (responseContent.Contains("<!DOCTYPE html>") || responseContent.Contains("<html"))
                    {
                        AddLog("❌ Got HTML error page instead of JSON");
                        cmbQuestions.Items.Add("Error: Backend returned HTML error");
                    }
                    else
                    {
                        cmbQuestions.Items.Add($"Error: API returned {response.StatusCode}");
                    }
                }

                //AddLog($"=== QUESTION LOADING DEBUG END ===");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Exception loading questions: {ex.Message}");
                AddLog($"❌ Stack trace: {ex.StackTrace}");
                cmbQuestions.Items.Add("Error: Failed to load questions");
            }
                
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
            AddLog("⏰ Time's up! Auto-stopping question...");
            await StopCurrentQuestion();
           // AddLog("✅ Auto-stop completed");
        }

        private async Task StopCurrentQuestion()
        {
            try
            {
                StopQuestionTimer();

                using (var stopClient = new HttpClient())
                {
                    stopClient.Timeout = TimeSpan.FromSeconds(10);
                    stopClient.DefaultRequestHeaders.Add("X-Teacher-API-Key", "dev-teacher-key");

                    string baseUrl = await NgrokHelper.GetNgrokBaseUrlAsync();

                    string url = $"{_ngrokBaseUrl} /api/sessions/{_classId}/stop-question";
                    AddLog($"🛑 STOP CALL: URL = {url}"); // 🚨 DEBUG URL

                    var response = await stopClient.PostAsync(url,
                     new StringContent("{}", Encoding.UTF8, "application/json"));

                    AddLog($"🛑 STOP RESPONSE: {(int)response.StatusCode} {response.StatusCode}");
                   /* var response = await stopClient.PostAsync(
                        $"http://localhost:5000/api/sessions/{_classId}/stop-question",
                        new StringContent("{}", Encoding.UTF8, "application/json"));*/

                    if (response.IsSuccessStatusCode)
                    {
                        // 🚨 FORCE UI UPDATE - USE INVOKE IF NEEDED
                        if (btnActivateQuestion.InvokeRequired)
                        {
                            btnActivateQuestion.Invoke(new Action(() => {
                                btnActivateQuestion.Enabled = true;
                                //AddLog("Activate button re-enabled (via Invoke)");
                            }));
                        }
                        else
                        {
                            btnActivateQuestion.Enabled = true;
                            //AddLog("Activate button re-enabled (direct)");
                        }

                        if (btnStopQuestion != null)
                        {
                            if (btnStopQuestion.InvokeRequired)
                            {
                                btnStopQuestion.Invoke(new Action(() => {
                                    btnStopQuestion.Enabled = false;
                                    //AddLog("Stop button disabled (via Invoke)");
                                }));
                            }
                            else
                            {
                                btnStopQuestion.Enabled = false;
                                //AddLog("Stop button disabled (direct)");
                            }
                        }
                        if (cmbQuestions != null)
                        {
                            if (cmbQuestions.InvokeRequired)
                            {
                                cmbQuestions.Invoke(new Action(() => {
                                    cmbQuestions.Enabled = true;
                                    //AddLog("Dropdown re-enabled (via Invoke)");
                                }));
                            }
                            else
                            {
                                cmbQuestions.Enabled = true;
                                //AddLog("Dropdown re-enabled (direct)");
                            }
                        }

                        // Update status label
                        if (lblQuestionStatus.InvokeRequired)
                        {
                            lblQuestionStatus.Invoke(new Action(() => {
                                lblQuestionStatus.Text = $"READY: Select next question";
                                lblQuestionStatus.ForeColor = Color.Blue;
                            }));
                        }
                        else
                        {
                            lblQuestionStatus.Text = $"READY: Select next question";
                            lblQuestionStatus.ForeColor = Color.Blue;
                        }
                        lblTimer.Text = "Time: --";

                        AddLog($"✅ Question stopped. UI reset complete.");
                        //AddLog($"After stop - ActivateBtn Enabled: {btnActivateQuestion.Enabled}");

                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        AddLog($"❌ Stop API failed: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error stopping question: {ex.Message}");
                AddLog($"❌ Stack trace: {ex.StackTrace}");
                // 🚨 EMERGENCY UI RESET EVEN IF API FAILS
                try
                {
                    btnActivateQuestion.Enabled = true;
                    if (btnStopQuestion != null) btnStopQuestion.Enabled = false;
                    if (cmbQuestions != null) cmbQuestions.Enabled = true;
                    lblQuestionStatus.Text = "ERROR: But UI reset";
                    AddLog("Emergency UI reset completed");
                }
                catch (Exception uiEx)
                {
                    AddLog($"Emergency UI reset failed: {uiEx.Message}");
                }
            }            
        }

        private void StopQuestionTimer()
        {
            _questionTimer?.Stop();
            _questionTimer?.Dispose();
            _questionTimer = null;
        }

        // Add this method to debug class ID changes:
        private void DebugClassId(string context)
        {
            AddLog($"[CLASSID DEBUG] {context} - _classId: {_classId}, _originalClassId: {_originalClassId}");
        }


    }
}