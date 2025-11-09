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
    public partial class QuestionResultsForm : Form
    {
        private string _ngrokBaseUrl;
        private int _questionId;
        private int _classId;
        private HttpClient _client;

        private WebBrowser webBrowser;
        private Label lblStats;
        private Button btnClose;

        public QuestionResultsForm(int questionId, int classId)
        {
            _questionId = questionId;
            _classId = classId;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer dev-teacher-key");

            InitializeResultsUI();
            //LoadQuestionResults();
            _ = InitializeAsync();
        }

        private void InitializeResultsUI()
        {
            this.Text = "Question Results";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Statistics label
            lblStats = new Label();
            lblStats.Location = new Point(20, 20);
            lblStats.Size = new Size(700, 40);
            lblStats.Font = new Font(lblStats.Font, FontStyle.Bold);
            this.Controls.Add(lblStats);

            // Web browser for charts
            webBrowser = new WebBrowser();
            webBrowser.Location = new Point(20, 70);
            webBrowser.Size = new Size(740, 450);
            webBrowser.DocumentText = "<h3>Loading results...</h3>";
            this.Controls.Add(webBrowser);

            // Close button
            btnClose = new Button();
            btnClose.Text = "Close";
            btnClose.Location = new Point(350, 530);
            btnClose.Size = new Size(80, 30);
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }
        private async Task InitializeAsync()
        {
            _ngrokBaseUrl = await Helpers.NgrokHelper.GetNgrokBaseUrlAsync();
            if (string.IsNullOrEmpty(_ngrokBaseUrl))
            {
                webBrowser.DocumentText = "<h3>❌ Failed to retrieve ngrok URL.</h3>";
                return;
            }

            await LoadQuestionResults();
        }

        private async Task LoadQuestionResults()
        {
            try
            {
                //var response = await _client.GetAsync($"http://192.168.0.102:5000/api/questions/{_questionId}/results");
                var response = await _client.GetAsync($"{_ngrokBaseUrl}/api/questions/{_questionId}/results"); if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var results = JsonSerializer.Deserialize<JsonElement>(content);

                    DisplayResults(results);
                }
                else
                {
                    webBrowser.DocumentText = "<h3>Error loading results</h3>";
                }
            }
            catch (Exception ex)
            {
                webBrowser.DocumentText = $"<h3>Error loading results: {ex.Message}</h3>";
            }
        }

        private void DisplayResults(JsonElement results)
        {
            string questionText = results.GetProperty("question_text").GetString();
            int totalAnswers = results.GetProperty("total_answers").GetInt32();
            int correctAnswers = results.GetProperty("correct_answers").GetInt32();
            double accuracy = results.GetProperty("accuracy_percentage").GetDouble();
            string correctAnswer = results.GetProperty("correct_answer").GetString();

            // Update stats label
            lblStats.Text = $"Question: {questionText} | " +
                           $"Total Answers: {totalAnswers} | " +
                           $"Correct: {correctAnswers} | " +
                           $"Accuracy: {accuracy}% | " +
                           $"Correct Answer: {correctAnswer}";

            // Create chart HTML
            string html = GenerateChartHtml(results);
            webBrowser.DocumentText = html;
        }

        private string GenerateChartHtml(JsonElement results)
        {
            var distribution = results.GetProperty("answer_distribution");
            string correctAnswer = results.GetProperty("correct_answer").GetString();

            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<script src='https://cdn.jsdelivr.net/npm/chart.js'></script>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine(".chart-container { width: 700px; height: 400px; margin: 0 auto; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<h3>Answer Distribution</h3>");
            html.AppendLine("<div class='chart-container'>");
            html.AppendLine("<canvas id='resultsChart'></canvas>");
            html.AppendLine("</div>");
            html.AppendLine("<script>");
            html.AppendLine("var ctx = document.getElementById('resultsChart').getContext('2d');");
            html.AppendLine("var chart = new Chart(ctx, {");
            html.AppendLine("type: 'bar',");
            html.AppendLine("data: {");

            // Labels
            html.Append("labels: [");
            foreach (var prop in distribution.EnumerateObject())
            {
                html.Append($"'{prop.Name}',");
            }
            html.AppendLine("],");

            // Data
            html.Append("datasets: [{");
            html.AppendLine("label: 'Answer Distribution (%)',");
            html.Append("data: [");
            foreach (var prop in distribution.EnumerateObject())
            {
                html.Append($"{prop.Value.GetDouble()},");
            }
            html.AppendLine("],");

            // Colors - highlight correct answer
            html.Append("backgroundColor: [");
            foreach (var prop in distribution.EnumerateObject())
            {
                string color = (prop.Name == correctAnswer) ? "'#4CAF50'" : "'#2196F3'";
                html.Append($"{color},");
            }
            html.AppendLine("]");

            html.AppendLine("}]},");
            html.AppendLine("options: { ");
            html.AppendLine("responsive: true,");
            html.AppendLine("maintainAspectRatio: false,");
            html.AppendLine("scales: { ");
            html.AppendLine("y: { beginAtZero: true, max: 100, title: { display: true, text: 'Percentage (%)' } },");
            html.AppendLine("x: { title: { display: true, text: 'Answer Options' } }");
            html.AppendLine("}");
            html.AppendLine("}");
            html.AppendLine("});");
            html.AppendLine("</script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString(); // 🚨 THIS WAS MISSING - CAUSING THE ERROR
        }
    }
}