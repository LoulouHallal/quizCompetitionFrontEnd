using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PowerPointAddIn1.Forms
{
    public partial class AuthorTabForm : Form
    {
        private int _teacherId;
        private int _courseId;
        private HttpClient _client;
        public int CreatedCourseId { get; private set; }

        // UI Controls
        private TabControl tabControl;
        private TextBox txtQuizTitle;
        private Button btnCreateQuiz;
        private Label lblStatus;
        private ListBox listQuestions;
        private Button btnAddQuestion;
        private Button btnEditQuestion;
        private Button btnDeleteQuestion;

        private void AuthorTabForm_Load(object sender, EventArgs e)
        {
            // Your initialization logic here
        }
        public AuthorTabForm(int teacherId)
        {
            _teacherId = teacherId;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer dev-teacher-key");
            _client.Timeout = TimeSpan.FromSeconds(10);
            CreatedCourseId = 0; // Initialize as 0

            CreateAuthorUI();
            LoadExistingQuizzes();
            // InitializeComponent();
        }

        private void CreateAuthorUI()
        {
            this.Text = "Quiz Author - Create & Manage Quizzes";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Create Tab Control
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            this.Controls.Add(tabControl);


            // 🚨 ADD THIS: Show current quiz info
            var lblCurrentQuiz = new Label();
            lblCurrentQuiz.Name = "lblCurrentQuiz";
            lblCurrentQuiz.Location = new Point(20, 150);
            lblCurrentQuiz.Size = new Size(400, 20);
            lblCurrentQuiz.Text = "No quiz created yet";
            lblCurrentQuiz.ForeColor = Color.Blue;
            this.Controls.Add(lblCurrentQuiz);


            // Tab 1: Create New Quiz
            var tabCreate = new TabPage("Create New Quiz");
            tabControl.Controls.Add(tabCreate);
            CreateQuizTab(tabCreate);

            // Tab 2: Manage Questions
            var tabManage = new TabPage("Manage Questions");
            tabControl.Controls.Add(tabManage);
            CreateManageTab(tabManage);
        }
        private void AddStartSessionButton(TabPage tab)
        {
            var btnStartSession = new Button();
            btnStartSession.Text = "Start Session with These Questions";
            btnStartSession.Location = new Point(20, 320);
            btnStartSession.Size = new Size(200, 35);
            btnStartSession.BackColor = Color.LightGreen;
            btnStartSession.Click += (s, e) => {
                if (_courseId == 0)
                {
                    MessageBox.Show("Please create a course first");
                    return;
                }

                // Close AuthorTabForm and open SessionControlForm
                this.DialogResult = DialogResult.OK; // Or some custom result
                this.Close();

                // You'd need to pass the courseId to the session somehow
            };
            tab.Controls.Add(btnStartSession);
        }

        private void CreateQuizTab(TabPage tab)
        {
            // Quiz Title
            var lblTitle = new Label();
            lblTitle.Text = "Quiz Title:";
            lblTitle.Location = new Point(20, 20);
            lblTitle.Size = new Size(80, 20);
            tab.Controls.Add(lblTitle);

            txtQuizTitle = new TextBox();
            txtQuizTitle.Location = new Point(100, 20);
            txtQuizTitle.Size = new Size(300, 20);
            txtQuizTitle.Text = "My Quiz";
            tab.Controls.Add(txtQuizTitle);

            // Create Quiz Button
            btnCreateQuiz = new Button();
            btnCreateQuiz.Text = "Create New Quiz";
            btnCreateQuiz.Location = new Point(100, 60);
            btnCreateQuiz.Size = new Size(120, 35);
            btnCreateQuiz.BackColor = Color.LightGreen;
            btnCreateQuiz.Click += BtnCreateQuiz_Click;
            tab.Controls.Add(btnCreateQuiz);

            // Status Label
            lblStatus = new Label();
            lblStatus.Location = new Point(20, 110);
            lblStatus.Size = new Size(400, 20);
            lblStatus.Text = "Enter quiz title and click Create";
            tab.Controls.Add(lblStatus);

            // Current Slide Info
            var lblSlideInfo = new Label();
            lblSlideInfo.Location = new Point(20, 140);
            lblSlideInfo.Size = new Size(400, 40);
            lblSlideInfo.Text = GetCurrentSlideInfo();
            tab.Controls.Add(lblSlideInfo);
        }

        private void CreateManageTab(TabPage tab)
        {
            // Questions List
            var lblQuestions = new Label();
            lblQuestions.Text = "Questions:";
            lblQuestions.Location = new Point(20, 20);
            lblQuestions.Size = new Size(80, 20);
            tab.Controls.Add(lblQuestions);

            listQuestions = new ListBox();
            listQuestions.Location = new Point(20, 50);
            listQuestions.Size = new Size(400, 200);
            tab.Controls.Add(listQuestions);

            // Question Management Buttons
            btnAddQuestion = new Button();
            btnAddQuestion.Text = "Add Question to Current Slide";
            btnAddQuestion.Location = new Point(20, 260);
            btnAddQuestion.Size = new Size(180, 35);
            btnAddQuestion.BackColor = Color.LightBlue;
            btnAddQuestion.Click += BtnAddQuestion_Click;
            tab.Controls.Add(btnAddQuestion);

            btnEditQuestion = new Button();
            btnEditQuestion.Text = "Edit Selected Question";
            btnEditQuestion.Location = new Point(210, 260);
            btnEditQuestion.Size = new Size(120, 35);
            btnEditQuestion.BackColor = Color.LightYellow;
            btnEditQuestion.Click += BtnEditQuestion_Click;
            tab.Controls.Add(btnEditQuestion);

            btnDeleteQuestion = new Button();
            btnDeleteQuestion.Text = "Delete Question";
            btnDeleteQuestion.Location = new Point(340, 260);
            btnDeleteQuestion.Size = new Size(80, 35);
            btnDeleteQuestion.BackColor = Color.LightCoral;
            btnDeleteQuestion.Click += BtnDeleteQuestion_Click;
            tab.Controls.Add(btnDeleteQuestion);


            // ADD THIS: Start Session Button
            var btnStartSession = new Button();
            btnStartSession.Text = "Start Live Session";
            btnStartSession.Location = new Point(20, 310);
            btnStartSession.Size = new Size(180, 35);
            btnStartSession.BackColor = Color.LightGreen;
            btnStartSession.Click += BtnStartSession_Click;
            tab.Controls.Add(btnStartSession);


        }

        private void BtnStartSession_Click(object sender, EventArgs e)
        {
            if (_courseId == 0)
            {
                MessageBox.Show("Please create a course and add questions first", "Error");
                return;
            }

            if (listQuestions.Items.Count == 0)
            {
                MessageBox.Show("Please add at least one question before starting a session", "Error");
                return;
            }

            // Close AuthorTabForm with OK result and courseId
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        private string GetCurrentSlideInfo()
        {
            try
            {
                var powerpointApp = Globals.ThisAddIn.Application;
                var presentation = powerpointApp.ActivePresentation;
                var currentSlide = powerpointApp.ActiveWindow.View.Slide;

                return $"Current Slide: {currentSlide.SlideNumber} - {presentation.Name}";
            }
            catch (Exception)
            {
                return "Cannot access PowerPoint slide information";
            }
        }
        private async void BtnCreateQuiz_Click(object sender, EventArgs e)
        {
            try
            {
                // 🚨 ADD THIS CHECK: Prevent creating multiple quizzes
                if (_courseId > 0)
                {
                    MessageBox.Show("You already have a quiz created. Add questions to the existing quiz.", "Info");
                    tabControl.SelectedIndex = 1; // Switch to manage tab
                    return;
                }

                var quizTitle = txtQuizTitle.Text.Trim();
                if (string.IsNullOrEmpty(quizTitle))
                {
                    MessageBox.Show("Please enter a quiz title", "Error");
                    return;
                }

                btnCreateQuiz.Enabled = false;
                lblStatus.Text = "Creating quiz...";
                lblStatus.ForeColor = Color.Blue;

                // Create course in backend
                var course = await CreateCourse(quizTitle);
                if (course != null)
                {
                    _courseId = course.CourseId;
                    CreatedCourseId = course.CourseId; // 🚨 SET THE CREATED COURSE ID
                    lblStatus.Text = $"✅ Quiz created! Course ID: {_courseId}";
                    lblStatus.ForeColor = Color.Green;

                    // 🚨 UPDATE CURRENT QUIZ DISPLAY
                    var lblCurrentQuiz = this.Controls.Find("lblCurrentQuiz", true).FirstOrDefault() as Label;

                    if (lblCurrentQuiz != null)
                    {
                        lblCurrentQuiz.Text = $"Current Quiz: {quizTitle} (ID: {_courseId})";
                    }

                    // 🚨 ADD THIS LINE: Load questions for the new course
                    LoadQuestions();


                    // Enable question management
                    btnAddQuestion.Enabled = true;
                    tabControl.SelectedIndex = 1; // Switch to manage tab
                }
                else
                {
                    lblStatus.Text = "❌ Failed to create quiz";
                    lblStatus.ForeColor = Color.Red;
                }

                btnCreateQuiz.Enabled = true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
                btnCreateQuiz.Enabled = true;
            }
        }

        private void BtnAddQuestion_Click(object sender, EventArgs e)
        {
            if (_courseId == 0)
            {
                MessageBox.Show("Please create a quiz first", "Error");
                return;
            }

            // Open question editor form
            var questionForm = new QuestionEditorForm(_courseId, GetCurrentSlideNumber());
            var result = questionForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                LoadQuestions(); // Refresh question list
                MessageBox.Show("Question added successfully!", "Success");

                // 🚨 ADD THIS: Debug to confirm
                DebugCurrentQuiz();
            }

        }
        private void BtnEditQuestion_Click(object sender, EventArgs e)
        {
            if (listQuestions.SelectedItem != null)
            {
                // Get selected question and open editor
                // We'll implement this after creating QuestionEditorForm
                MessageBox.Show("Edit question feature coming soon!", "Info");
            }
            else
            {
                MessageBox.Show("Please select a question to edit", "Error");
            }
        }

        private void BtnDeleteQuestion_Click(object sender, EventArgs e)
        {
            if (listQuestions.SelectedItem != null)
            {
                // Delete selected question
                // We'll implement this after creating question management
                MessageBox.Show("Delete question feature coming soon!", "Info");
            }
            else
            {
                MessageBox.Show("Please select a question to delete", "Error");
            }
        }

        private async void LoadExistingQuizzes()
        {
            // Load teacher's existing courses/quizzes
            // We'll implement this later
            try
            {
                var response = await _client.GetAsync($"http://localhost:5000/api/teachers/{_teacherId}/courses");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var courses = JsonSerializer.Deserialize<List<Course>>(content);

                    // You might want to show these in a combo box for selection
                    if (courses != null && courses.Count > 0)
                    {
                        _courseId = courses[0].CourseId; // Auto-select first course
                        LoadQuestions(); // Load questions for this course
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail - teacher can create new course
                Console.WriteLine($"Error loading existing courses: {ex.Message}");
            }

        }

        private async void LoadQuestions()
        {
            if (_courseId == 0) return;

            try
            {
                // Load questions for current course
                // We'll implement this after creating question API
                var response = await _client.GetAsync($"http://localhost:5000/api/courses/{_courseId}/questions");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    // Add debug output to see what's being returned
                    Console.WriteLine($"Questions API Response: {content}");

                    var questions = JsonSerializer.Deserialize<List<Question>>(content);

                    listQuestions.Items.Clear();
                    if (questions != null && questions.Count > 0)
                    {
                        foreach (var question in questions)
                        {
                            // Show truncated question text
                            string displayText = question.Content.Length > 50
                                ? question.Content.Substring(0, 50) + "..."
                                : question.Content;

                            listQuestions.Items.Add($"{displayText} ({question.Points} pts)");
                        }
                    }
                    else
                    {
                        listQuestions.Items.Add("No questions found. Click 'Add Question' to create one.");
                    }
                }
                else
                {
                    MessageBox.Show("Failed to load questions", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading questions: {ex.Message}", "Error");
            }
        }

        private int GetCurrentSlideNumber()
        {
            try
            {
                var powerpointApp = Globals.ThisAddIn.Application;
                return powerpointApp.ActiveWindow.View.Slide.SlideNumber;
            }
            catch (Exception)
            {
                return 1; // Default to slide 1 if cannot access
            }
        }

        private async Task<Course> CreateCourse(string courseName)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var courseData = new
                    {
                        course_name = courseName,
                        user_id = _teacherId
                    };

                    var json = JsonSerializer.Serialize(courseData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer dev-teacher-key");

                    // 🚨 ADD TIMEOUT
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // 🚨 SHOW DEBUG INFO IN MESSAGEBOX TOO
                    MessageBox.Show($"Sending request to: http://localhost:5000/api/courses\nData: {json}", "DEBUG - Request");

                    // 🚨 ADD DEBUG INFO
                    Console.WriteLine($"Sending course creation request: {json}");

                    var response = await client.PostAsync("http://localhost:5000/api/courses", content);

                    // 🚨 ADD RESPONSE DEBUGGING
                    Console.WriteLine($"Response Status: {response.StatusCode}");



                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response Content: {responseContent}");

                    MessageBox.Show($"Response Status: {(int)response.StatusCode} {response.StatusCode}\nResponse: {responseContent}", "DEBUG - Response");

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        // 🚨 CHECK IF PROPERTIES EXIST
                        if (result.TryGetProperty("course_id", out var courseIdElem) &&
                            result.TryGetProperty("course_name", out var courseNameElem))
                        {
                            return new Course
                            {
                                CourseId = result.GetProperty("course_id").GetInt32(),
                                CourseName = result.GetProperty("course_name").GetString()
                            };
                        }
                        else
                        {
                            throw new Exception($"Missing course_id or course_name in response: {responseContent}");
                        }

                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
  
                        // 🚨 BETTER ERROR MESSAGE
                        throw new Exception($"API returned {response.StatusCode}: {responseContent}");

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to create course: {ex.Message}", "Error");
                    return null;
                }
            }
        }

        private async void DebugCurrentQuiz()
        {
            if (_courseId == 0) return;

            try
            {
                var response = await _client.GetAsync($"http://localhost:5000/api/courses/{_courseId}/questions");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var questions = JsonSerializer.Deserialize<List<Question>>(content);

                    MessageBox.Show($"Course {_courseId} has {questions?.Count ?? 0} questions", "Debug");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Debug error: {ex.Message}", "Error");
            }
        }
    }
    public class Course
    {
        [JsonPropertyName("course_id")]
        public int CourseId { get; set; }
        [JsonPropertyName("course_name")]
        public string CourseName { get; set; }
    
       /* //private void AuthorTabForm_Load(object sender, EventArgs e)
        //{

        //}*/
    }
    public class Question
    {
        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("question_content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("points")]
        public int Points { get; set; } = 100;

        [JsonPropertyName("time_limit_seconds")]
        public int? TimeLimitSeconds { get; set; }
    }
}
