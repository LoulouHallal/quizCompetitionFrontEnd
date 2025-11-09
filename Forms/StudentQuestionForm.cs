using System;
using System.Drawing;
using System.Net.Http;
//using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;


namespace PowerPointAddIn1.Forms
{
    public partial class StudentQuestionForm : Form
    {
        private string _ngrokBaseUrl;
        private int _sessionId;
        private int _studentId;
        private string _studentName;
        private HttpClient _client;

        // 🚨 ADD THESE FIELDS:
        private WebSocket _webSocket;
        private int _currentQuestionId; // To track the current active question


        private Label lblQuestion;
        private RadioButton[] optionButtons;
        private Button btnSubmit;
        private Label lblTimer;
        private Timer questionTimer;
        private int timeRemaining;

        public StudentQuestionForm(int sessionId, int studentId, string studentName)
        {
            _sessionId = sessionId;
            _studentId = studentId;
            _studentName = studentName;
            _client = new HttpClient();

            InitializeStudentUI();
            //SetupWebSocketListener();
            _ = InitializeAsync();
        }

        private void InitializeStudentUI()
        {
            this.Text = $"Quiz Session - {_studentName}";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Question label
            lblQuestion = new Label();
            lblQuestion.Location = new Point(20, 20);
            lblQuestion.Size = new Size(550, 60);
            lblQuestion.Font = new Font(lblQuestion.Font, FontStyle.Bold);
            lblQuestion.Text = "Waiting for next question...";
            this.Controls.Add(lblQuestion);

            // Timer label
            lblTimer = new Label();
            lblTimer.Location = new Point(500, 20);
            lblTimer.Size = new Size(80, 20);
            lblTimer.Text = "00:00";
            lblTimer.ForeColor = Color.Red;
            this.Controls.Add(lblTimer);

            // Submit button
            btnSubmit = new Button();
            btnSubmit.Location = new Point(250, 400);
            btnSubmit.Size = new Size(100, 35);
            btnSubmit.Text = "Submit Answer";
            btnSubmit.Enabled = false;
            btnSubmit.Click += BtnSubmit_Click;
            this.Controls.Add(btnSubmit);

            // Initialize timer
            questionTimer = new Timer();
            questionTimer.Interval = 1000;
            questionTimer.Tick += QuestionTimer_Tick;
        }

        private async Task InitializeAsync()
        {
            _ngrokBaseUrl = await Helpers.NgrokHelper.GetNgrokBaseUrlAsync();
            if (string.IsNullOrEmpty(_ngrokBaseUrl))
            {
                MessageBox.Show("❌ Failed to retrieve ngrok URL.");
                return;
            }

            SetupWebSocketListener();
        }
        public void DisplayQuestion(string questionText, string[] options, int timeLimit)
        {
            Invoke(new Action(() =>
            {
                // Clear previous options
                if (optionButtons != null)
                {
                    foreach (var btn in optionButtons)
                    {
                        this.Controls.Remove(btn);
                    }
                }

                // Display new question
                lblQuestion.Text = questionText;
                timeRemaining = timeLimit;
                lblTimer.Text = $"{timeRemaining}s";

                // Create option buttons
                optionButtons = new RadioButton[options.Length];
                for (int i = 0; i < options.Length; i++)
                {
                    optionButtons[i] = new RadioButton();
                    optionButtons[i].Location = new Point(40, 100 + (i * 35));
                    optionButtons[i].Size = new Size(500, 30);
                    optionButtons[i].Text = options[i];
                    optionButtons[i].Font = new Font(optionButtons[i].Font, FontStyle.Regular);
                    this.Controls.Add(optionButtons[i]);
                }

                btnSubmit.Enabled = true;
                questionTimer.Start();
            }));
        }

        private void QuestionTimer_Tick(object sender, EventArgs e)
        {
            timeRemaining--;
            //lblTimer.Text = $"{timeRemaining}s";

            if (timeRemaining <= 10)
            {
                lblTimer.ForeColor = Color.Red;
                lblTimer.Font = new Font(lblTimer.Font, FontStyle.Bold);
            }
            else if (timeRemaining <= 30)
            {
                lblTimer.ForeColor = Color.Orange;
            }

            lblTimer.Text = $"{timeRemaining}s";
            if (timeRemaining <= 0)
            {
                questionTimer.Stop();
                btnSubmit.Enabled = false;

                // 🚨 DISPLAY "TIME OVER" instead of MessageBox
                DisplayTimeOver();

                // Optional: Auto-submit if answer was selected
                AutoSubmitIfAnswered();
            }

        }
        private void DisplayTimeOver()
        {
            Invoke(new Action(() =>
            {
                // Change question text to show "TIME OVER"
                lblQuestion.Text = "⏰ TIME OVER!";
                lblQuestion.ForeColor = Color.Red;
                lblQuestion.Font = new Font(lblQuestion.Font, FontStyle.Bold);

                // Disable all option buttons
                if (optionButtons != null)
                {
                    foreach (var radio in optionButtons)
                    {
                        radio.Enabled = false;
                    }
                }

                // Show "Time Over" in timer label
                lblTimer.Text = "TIME OVER";
                lblTimer.ForeColor = Color.Red;

                // Optional: Change submit button text
                btnSubmit.Text = "Time Expired";
                btnSubmit.BackColor = Color.LightGray;
            }));
        }

        private async void AutoSubmitIfAnswered()
        {
            string selectedAnswer = GetSelectedAnswer();
            if (!string.IsNullOrEmpty(selectedAnswer))
            {
                // Auto-submit the selected answer
                await SubmitAnswer(selectedAnswer);
            }
            else
            {
                // No answer selected - just show time over
                // You could also submit a "no answer" response
                await SubmitNoAnswer();
            }
        }
        private async Task SubmitNoAnswer()
        {
            try
            {
                var answerData = new
                {
                    user_id = _studentId,
                    class_id = _sessionId,
                    question_id = _currentQuestionId,
                    answer_content = "NO_ANSWER", // or null
                    time_over = true
                };

                var json = JsonSerializer.Serialize(answerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("http://192.168.0.102:5000/api/answers/submit", content);

                if (response.IsSuccessStatusCode)
                {
                    // Optionally show a brief message
                    ShowBriefMessage("Time expired - no answer submitted");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error submitting time-over: {ex.Message}");
            }
        }

        private void ShowBriefMessage(string message)
        {
            Invoke(new Action(() =>
            {
                var tempLabel = new Label();
                tempLabel.Text = message;
                tempLabel.Location = new Point(150, 350);
                tempLabel.Size = new Size(300, 30);
                tempLabel.ForeColor = Color.Red;
                tempLabel.TextAlign = ContentAlignment.MiddleCenter;
                this.Controls.Add(tempLabel);

                // Remove after 3 seconds
                var removeTimer = new Timer();
                removeTimer.Interval = 3000;
                removeTimer.Tick += (s, e) =>
                {
                    this.Controls.Remove(tempLabel);
                    removeTimer.Stop();
                    removeTimer.Dispose();
                };
                removeTimer.Start();
            }));
        }

        private async void BtnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                string selectedAnswer = GetSelectedAnswer();
                if (string.IsNullOrEmpty(selectedAnswer))
                {
                    MessageBox.Show("Please select an answer!");
                    return;
                }

                btnSubmit.Enabled = false;
                questionTimer.Stop();

                // Submit answer to backend
                var result = await SubmitAnswer(selectedAnswer);
                if (result)
                {
                    MessageBox.Show("Answer submitted! Waiting for next question...");
                    lblQuestion.Text = "Waiting for next question...";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting answer: {ex.Message}");
            }
        }

        private string GetSelectedAnswer()
        {
            if (optionButtons != null)
            {
                foreach (var radio in optionButtons)
                {
                    if (radio.Checked)
                        return radio.Text;
                }
            }
            return null;
        }

        private async Task<bool> SubmitAnswer(string answer)
        {
            try
            {
                var answerData = new
                {
                    user_id = _studentId,
                    class_id = _sessionId,
                    question_id = _currentQuestionId, // 🚨 Use the actual question ID
                    answer_content = answer
                };

                var json = JsonSerializer.Serialize(answerData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                //var response = await _client.PostAsync("http://localhost:5000/api/answers/submit", content);
                var response = await _client.PostAsync("http://192.168.0.102:5000/api/answers/submit", content);


                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    bool isCorrect = result.GetProperty("is_correct").GetBoolean();
                    int points = result.GetProperty("points_earned").GetInt32();

                    string message = isCorrect ?
                        $"✅ Correct! You earned {points} points." :
                        $"❌ Incorrect. Better luck next time!";

                    MessageBox.Show(message, "Answer Result");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Submission failed: {errorContent}", "Error");
                    return false;
                }
                //return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Submission error: {ex.Message}");
                return false;
            }
        }
        // Add this method to handle form closing
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _webSocket?.Close();
            base.OnFormClosing(e);
        }

        private int GetCurrentQuestionId()
        {
            // This should come from the WebSocket when question is activated
            return _currentQuestionId;

        }

        private void SetupWebSocketListener()
        {
            // You'll need to implement WebSocket client to listen for:

            try
            {
                //_webSocket = new WebSocket("ws://localhost:5000/socket.io/?EIO=4&transport=websocket");
                //_webSocket = new WebSocket("ws://192.168.0.102:5000/socket.io/?EIO=4&transport=websocket");

                string wsBaseUrl = _ngrokBaseUrl.Replace("https://", "wss://");
                _webSocket = new WebSocket($"{wsBaseUrl}/socket.io/?EIO=4&transport=websocket");

                _webSocket.OnMessage += (sender, e) =>
                {
                    if (e.IsText)
                    {
                        HandleWebSocketMessage(e.Data);
                    }
                };

                _webSocket.OnOpen += (sender, e) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        // Join the session room
                        var joinData = new { session_id = _sessionId, user_id = _studentId };
                        string joinMessage = $"42[\"join_session\", {JsonSerializer.Serialize(joinData)}]";
                        _webSocket.Send(joinMessage);
                        MessageBox.Show("Connected to quiz session!", "Connected");
                    }));
                };

                _webSocket.OnError += (sender, e) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"WebSocket error: {e.Message}", "Connection Error");
                    }));
                };

                _webSocket.OnClose += (sender, e) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show("Disconnected from quiz session", "Disconnected");
                    }));
                };

                _webSocket.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebSocket setup error: {ex.Message}");
            }
        }

        // 🚨 ADD THIS MISSING METHOD RIGHT HERE:
        private void HandleWebSocketMessage(string message)
        {
            try
            {
                Console.WriteLine($"WebSocket received: {message}");

                // Socket.IO format: "42["event_name", {data}]"
                if (message.StartsWith("42[\"question_activated\""))
                {
                    // Extract JSON data from the message
                    var startIndex = message.IndexOf('{');
                    var endIndex = message.LastIndexOf('}') + 1;
                    if (startIndex >= 0 && endIndex > startIndex)
                    {
                        var jsonData = message.Substring(startIndex, endIndex - startIndex);
                        var questionData = JsonSerializer.Deserialize<QuestionData>(jsonData);

                        // Display the question to student
                        DisplayQuestion(questionData.question_text, questionData.options, questionData.time_limit);
                        _currentQuestionId = questionData.question_id; // 🚨 Store the actual question ID
                    }
                }
                else if (message.StartsWith("42[\"question_stopped\""))
                {
                    this.Invoke(new Action(() =>
                    {
                        lblQuestion.Text = "Question time ended! Waiting for next question...";
                        btnSubmit.Enabled = false;
                        if (questionTimer != null) questionTimer.Stop();
                    }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket message error: {ex.Message}");
            }
        }
    }
}
