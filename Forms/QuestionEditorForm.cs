using Microsoft.Office.Interop.PowerPoint;
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

namespace PowerPointAddIn1.Forms
{
    public partial class QuestionEditorForm : Form
    {

        private int _courseId;
        private int _slideNumber;
        private HttpClient _client;

        // Question Data
        private string _questionContent = "";
        private List<AnswerOption> _answers = new List<AnswerOption>();
        private int _timeLimit = 30;
        private int _points = 100;

        // UI Controls
        private TextBox txtQuestion;
        private NumericUpDown numTimeLimit;
        private NumericUpDown numPoints;
        private ListBox listAnswers;
        private TextBox txtNewAnswer;
        private Button btnAddAnswer;
        private Button btnRemoveAnswer;
        private Button btnSetCorrect;
        private Button btnSaveQuestion;
        private Button btnCancel;
        private Label lblCorrectAnswer;

        public QuestionEditorForm()
        {
            InitializeComponent();
        }

        private void QuestionEditorForm_Load(object sender, EventArgs e)
        {
            // Your initialization logic here
        }
        public QuestionEditorForm(int courseId, int slideNumber)
        {
            _courseId = courseId;
            _slideNumber = slideNumber;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer dev-teacher-key");
            _client.Timeout = TimeSpan.FromSeconds(10);

            CreateQuestionUI();
            InitializeDefaultAnswers();

            // 🚨 AUTO-CREATE NEW SLIDE WHEN FORM OPENS
            //AutoCreateNewSlide();
        }

        private void CreateQuestionUI()
        {
            this.Text = $"Add Question to Slide {_slideNumber}";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Question Content
            var lblQuestion = new Label();
            lblQuestion.Text = "Question:";
            lblQuestion.Location = new System.Drawing.Point(20, 20);
            lblQuestion.Size = new Size(80, 20);
            this.Controls.Add(lblQuestion);

            txtQuestion = new TextBox();
            txtQuestion.Location = new System.Drawing.Point(100, 20);
            txtQuestion.Size = new Size(450, 20);
            txtQuestion.Text = "Enter your question here...";
            this.Controls.Add(txtQuestion);

            // Time Limit
            var lblTimeLimit = new Label();
            lblTimeLimit.Text = "Time Limit (sec):";
            lblTimeLimit.Location = new System.Drawing.Point(20, 50);
            lblTimeLimit.Size = new Size(80, 20);
            this.Controls.Add(lblTimeLimit);

            numTimeLimit = new NumericUpDown();
            numTimeLimit.Location = new System.Drawing.Point(100, 50);
            numTimeLimit.Size = new Size(80, 20);
            numTimeLimit.Minimum = 15;
            numTimeLimit.Maximum = 120;
            numTimeLimit.Value = 30;
            this.Controls.Add(numTimeLimit);

            // Points
            var lblPoints = new Label();
            lblPoints.Text = "Points:";
            lblPoints.Location = new System.Drawing.Point(200, 50);
            lblPoints.Size = new Size(50, 20);
            this.Controls.Add(lblPoints);

            numPoints = new NumericUpDown();
            numPoints.Location = new System.Drawing.Point(250, 50);
            numPoints.Size = new Size(80, 20);
            numPoints.Minimum = 10;
            numPoints.Maximum = 1000;
            numPoints.Value = 100;
            this.Controls.Add(numPoints);

            // Answers Section
            var lblAnswers = new Label();
            lblAnswers.Text = "Answer Options:";
            lblAnswers.Location = new System.Drawing.Point(20, 90);
            lblAnswers.Size = new Size(100, 20);
            lblAnswers.Font = new System.Drawing.Font(lblAnswers.Font, FontStyle.Bold);
            this.Controls.Add(lblAnswers);

            listAnswers = new ListBox();
            listAnswers.Location = new System.Drawing.Point(20, 120);
            listAnswers.Size = new Size(400, 150);
            listAnswers.SelectionMode = SelectionMode.MultiSimple;
            this.Controls.Add(listAnswers);

            // New Answer Input
            var lblNewAnswer = new Label();
            lblNewAnswer.Text = "New Answer:";
            lblNewAnswer.Location = new System.Drawing.Point(20, 280);
            lblNewAnswer.Size = new Size(80, 20);
            this.Controls.Add(lblNewAnswer);

            txtNewAnswer = new TextBox();
            txtNewAnswer.Location = new System.Drawing.Point(100, 280);
            txtNewAnswer.Size = new Size(250, 20);
            this.Controls.Add(txtNewAnswer);

            btnAddAnswer = new Button();
            btnAddAnswer.Text = "Add Answer";
            btnAddAnswer.Location = new System.Drawing.Point(360, 280);
            btnAddAnswer.Size = new Size(60, 23);
            btnAddAnswer.BackColor = Color.LightGreen;
            btnAddAnswer.Click += BtnAddAnswer_Click;
            this.Controls.Add(btnAddAnswer);

            // Answer Management Buttons
            btnRemoveAnswer = new Button();
            btnRemoveAnswer.Text = "Remove Selected";
            btnRemoveAnswer.Location = new System.Drawing.Point(430, 120);
            btnRemoveAnswer.Size = new Size(120, 30);
            btnRemoveAnswer.BackColor = Color.LightCoral;
            btnRemoveAnswer.Click += BtnRemoveAnswer_Click;
            this.Controls.Add(btnRemoveAnswer);

            btnSetCorrect = new Button();
            btnSetCorrect.Text = "Set as Correct";
            btnSetCorrect.Location = new System.Drawing.Point(430, 160);
            btnSetCorrect.Size = new Size(120, 30);
            btnSetCorrect.BackColor = Color.LightBlue;
            btnSetCorrect.Click += BtnSetCorrect_Click;
            this.Controls.Add(btnSetCorrect);

            // Correct Answer Indicator
            lblCorrectAnswer = new Label();
            lblCorrectAnswer.Location = new System.Drawing.Point(20, 320);
            lblCorrectAnswer.Size = new Size(400, 20);
            lblCorrectAnswer.Text = "Correct Answer: None selected";
            lblCorrectAnswer.ForeColor = Color.Red;
            this.Controls.Add(lblCorrectAnswer);

            // Action Buttons
            btnSaveQuestion = new Button();
            btnSaveQuestion.Text = "Save to Slide";
            btnSaveQuestion.Location = new System.Drawing.Point(150, 360);
            btnSaveQuestion.Size = new Size(120, 35);
            btnSaveQuestion.BackColor = Color.LightGreen;
            btnSaveQuestion.Click += BtnSaveQuestion_Click;
            this.Controls.Add(btnSaveQuestion);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new System.Drawing.Point(280, 360);
            btnCancel.Size = new Size(80, 35);
            btnCancel.BackColor = Color.LightGray;
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);
        }


        private void InitializeDefaultAnswers()
        {
            // Add some default answer options
            _answers.Add(new AnswerOption { Content = "Option A", IsCorrect = false });
            _answers.Add(new AnswerOption { Content = "Option B", IsCorrect = false });
            _answers.Add(new AnswerOption { Content = "Option C", IsCorrect = false });
            _answers.Add(new AnswerOption { Content = "Option D", IsCorrect = false });

            UpdateAnswersList();
        }
        private void UpdateAnswersList()
        {
            listAnswers.Items.Clear();
            foreach (var answer in _answers)
            {
                var displayText = answer.Content;
                if (answer.IsCorrect)
                {
                    displayText += " ✓ (Correct)";
                }
                listAnswers.Items.Add(displayText);
            }

            // Update correct answer label
            var correctAnswer = _answers.Find(a => a.IsCorrect);
            if (correctAnswer != null)
            {
                lblCorrectAnswer.Text = $"Correct Answer: {correctAnswer.Content}";
                lblCorrectAnswer.ForeColor = Color.Green;
            }
            else
            {
                lblCorrectAnswer.Text = "Correct Answer: None selected";
                lblCorrectAnswer.ForeColor = Color.Red;
            }
        }

        private void BtnAddAnswer_Click(object sender, EventArgs e)
        {
            var newAnswer = txtNewAnswer.Text.Trim();
            if (string.IsNullOrEmpty(newAnswer))
            {
                MessageBox.Show("Please enter an answer option", "Error");
                return;
            }

            if (_answers.Count >= 6)
            {
                MessageBox.Show("Maximum 6 answer options allowed", "Error");
                return;
            }

            _answers.Add(new AnswerOption { Content = newAnswer, IsCorrect = false });
            txtNewAnswer.Clear();
            UpdateAnswersList();
        }


        private void BtnRemoveAnswer_Click(object sender, EventArgs e)
        {
            if (listAnswers.SelectedIndex >= 0)
            {
                var selectedIndex = listAnswers.SelectedIndex;
                _answers.RemoveAt(selectedIndex);
                UpdateAnswersList();
            }
            else
            {
                MessageBox.Show("Please select an answer to remove", "Error");
            }
        }
        private void BtnSetCorrect_Click(object sender, EventArgs e)
        {
            if (listAnswers.SelectedIndex >= 0)
            {
                // Clear all correct flags
                foreach (var answer in _answers)
                {
                    answer.IsCorrect = false;
                }

                // Set selected as correct
                _answers[listAnswers.SelectedIndex].IsCorrect = true;
                UpdateAnswersList();
            }
            else
            {
                MessageBox.Show("Please select an answer to mark as correct", "Error");
            }
        }

        private async void BtnSaveQuestion_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(txtQuestion.Text))
                {
                    MessageBox.Show("Please enter a question", "Error");
                    return;
                }

                if (_answers.Count < 2)
                {
                    MessageBox.Show("Please add at least 2 answer options", "Error");
                    return;
                }

                var correctAnswer = _answers.Find(a => a.IsCorrect);
                if (correctAnswer == null)
                {
                    MessageBox.Show("Please select a correct answer", "Error");
                    return;
                }

                btnSaveQuestion.Enabled = false;
                btnSaveQuestion.Text = "Saving...";

                // Save question to backend
                var success = await SaveQuestionToBackend();
                if (success)
                {
                    MessageBox.Show($"Question saved successfully to Slide {_slideNumber -1}!", "Success");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to save question", "Error");
                    btnSaveQuestion.Enabled = true;
                    btnSaveQuestion.Text = "Save to Slide";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving question: {ex.Message}", "Error");
                btnSaveQuestion.Enabled = true;
                btnSaveQuestion.Text = "Save to Slide";
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private async Task<bool> SaveQuestionToBackend()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Prepare question data
                    var questionData = new
                    {
                        course_id = _courseId,
                        question_content = txtQuestion.Text.Trim(),
                        points = (int)numPoints.Value,
                        time_limit_seconds = (int)numTimeLimit.Value,
                        slide_number = _slideNumber, // 🚨 ADD SLIDE NUMBER
                        answers = _answers.ConvertAll(a => new
                        {
                            answer_content = a.Content,
                            is_correct = a.IsCorrect,
                            display_order = _answers.IndexOf(a) + 1
                        })
                    };

                    var json = JsonSerializer.Serialize(questionData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer dev-teacher-key");
                    var response = await client.PostAsync("http://localhost:5000/api/questions", content);

                    if (response.IsSuccessStatusCode)
                    {
                        // Get the question ID from response
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        var questionId = result.GetProperty("question_id").GetInt32();

                        // 🚨 STORE QUESTION-SLIDE MAPPING
                        await StoreQuestionSlideMapping(questionId, _slideNumber);

                        // Also add to current slide (we'll implement this later)
                        AddQuestionToPowerPointSlide();
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API error: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Backend error: {ex.Message}", "Error");
                    return false;
                }
            }

        }

        private async Task<bool> StoreQuestionSlideMapping(int questionId, int slideNumber)
        {
            try
            {
                var mappingData = new
                {
                    question_id = questionId,
                    slide_number = slideNumber,
                    course_id = _courseId
                };

                var json = JsonSerializer.Serialize(mappingData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("http://localhost:5000/api/question_slides", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing question-slide mapping: {ex.Message}");
                return false;
            }
        }

        private void AddQuestionToPowerPointSlide()
        {
            try
            {
                var powerpointApp = Globals.ThisAddIn.Application;
                var presentation = powerpointApp.ActivePresentation; // 🚨 DEFINE PRESENTATION HERE
                var currentSlide = powerpointApp.ActiveWindow.View.Slide;

                // 🚨 CREATE NEW SLIDE FOR EACH QUESTION instead of using current slide
                Microsoft.Office.Interop.PowerPoint.Slide newSlide = null;

                try
                {
                    // Get the current slide number to determine where to insert
                    int currentSlideNumber = powerpointApp.ActiveWindow.View.Slide.SlideNumber;

                    // Create new slide AFTER the current slide
                    newSlide = presentation.Slides.Add(currentSlideNumber + 1,
                        Microsoft.Office.Interop.PowerPoint.PpSlideLayout.ppLayoutBlank);

                    // Navigate to the new slide
                    powerpointApp.ActiveWindow.View.GotoSlide(newSlide.SlideNumber);

                    // 🚨 REPLACE AddLog WITH Console.WriteLine OR MessageBox
                    Console.WriteLine($"✅ Created new slide {newSlide.SlideNumber } for question");
                }
                catch (Exception slideEx)
                {
                    // Fallback: Use current slide if creation fails
                    newSlide = powerpointApp.ActiveWindow.View.Slide;

                    // 🚨 REPLACE AddLog WITH Console.WriteLine OR MessageBox
                    Console.WriteLine($"⚠️ Using current slide: {slideEx.Message}");
                }


                // Create a professional question box
                var questionShape = currentSlide.Shapes.AddTextbox(
                    Microsoft.Office.Core.MsoTextOrientation.msoTextOrientationHorizontal,
                    50, 100, 600, 150); // Better positioning and size


                // Format the question box
                questionShape.TextFrame.TextRange.Text = $"QUESTION: {txtQuestion.Text}\n\n" +
                                                        $"Time Limit: {numTimeLimit.Value} seconds\n" +
                                                        $"Points: {numPoints.Value}";


                // Professional formatting
                questionShape.TextFrame.TextRange.Font.Size = 18;
                questionShape.TextFrame.TextRange.Font.Bold = Microsoft.Office.Core.MsoTriState.msoTrue;
                questionShape.TextFrame.TextRange.Font.Name = "Calibri";
                questionShape.Fill.ForeColor.RGB = Color.LightBlue.ToArgb();
                questionShape.Line.ForeColor.RGB = Color.DarkBlue.ToArgb();
                questionShape.Line.Weight = 2;

                // Add answer options if it's multiple choice
                if (_answers.Count > 0)
                {
                    AddAnswerOptionsToSlide(currentSlide);
                }

                // 🚨 UPDATE THE SLIDE NUMBER FOR FUTURE QUESTIONS
                _slideNumber = newSlide.SlideNumber;
            }
            catch (Exception ex)
            {
                // Silent fail - question is still saved to backend
                Console.WriteLine($"PowerPoint integration error: {ex.Message}");
            }
        }

        private void AddAnswerOptionsToSlide(Microsoft.Office.Interop.PowerPoint.Slide slide)
        {
            int startTop = 220; // Position below question

            for (int i = 0; i < _answers.Count; i++)
            {
                var answerShape = slide.Shapes.AddTextbox(
                    Microsoft.Office.Core.MsoTextOrientation.msoTextOrientationHorizontal,
                    80, startTop + (i * 40), 500, 30);

                string correctIndicator = _answers[i].IsCorrect ? " ✓ CORRECT " : "";
                answerShape.TextFrame.TextRange.Text = $"{GetOptionLetter(i)}. {_answers[i].Content}{correctIndicator}";

                if (_answers[i].IsCorrect)
                {
                    answerShape.TextFrame.TextRange.Font.Bold = Microsoft.Office.Core.MsoTriState.msoTrue;
                    answerShape.Fill.ForeColor.RGB = Color.LightGreen.ToArgb();
                    answerShape.TextFrame.TextRange.Font.Color.RGB = Color.DarkGreen.ToArgb();
                }
                else
                {
                    answerShape.TextFrame.TextRange.Font.Bold = Microsoft.Office.Core.MsoTriState.msoFalse;
                    answerShape.Fill.ForeColor.RGB = Color.White.ToArgb();
                }
            }
        }

        private string GetOptionLetter(int index)
        {
            return ((char)('A' + index)).ToString();
        }
    }
    public class AnswerOption
    {
        public string Content { get; set; } = "";
        public bool IsCorrect { get; set; } = false;
    


       

        private void QuestionEditorForm_Load(object sender, EventArgs e)
        {

        }
    }
}
