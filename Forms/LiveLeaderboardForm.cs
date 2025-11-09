// LiveLeaderboardForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;
using PowerPointAddIn1.Helpers;

namespace PowerPointAddIn1.Forms
{

    public partial class LiveLeaderboardForm : Form
    {
        private int _sessionId;
        private HttpClient _client;
        private WebSocket _webSocket;
        private DataGridView _leaderboardGrid;
        private Label _lblTitle;
        private Timer _refreshTimer;

        public class LeaderboardEntry
        {
            public string StudentName { get; set; }
            public int Score { get; set; }
            public int CorrectAnswers { get; set; }
            public int TotalAnswers { get; set; }
            public double Accuracy { get; set; }
            public string Rank { get; set; }
        }
        private string _ngrokBaseUrl;

        public LiveLeaderboardForm(int sessionId)
        {
            _sessionId = sessionId;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer dev-teacher-key");

            InitializeUI();

            // 🚨 Async init
            _ = InitializeAsync();


            SetupWebSocket();
            StartAutoRefresh();
        }

        private void InitializeUI()
        {
            this.Text = $"Live Leaderboard - Session {_sessionId}";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Title
            _lblTitle = new Label();
            _lblTitle.Text = "🏆 LIVE LEADERBOARD 🏆";
            _lblTitle.Font = new Font("Arial", 16, FontStyle.Bold);
            _lblTitle.ForeColor = Color.DarkBlue;
            _lblTitle.Location = new Point(20, 10);
            _lblTitle.Size = new Size(400, 30);
            this.Controls.Add(_lblTitle);

            // Leaderboard Grid
            _leaderboardGrid = new DataGridView();
            _leaderboardGrid.Location = new Point(20, 50);
            _leaderboardGrid.Size = new Size(550, 300);
            _leaderboardGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _leaderboardGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _leaderboardGrid.ReadOnly = true;
            _leaderboardGrid.RowHeadersVisible = false;

            // Add columns
            _leaderboardGrid.Columns.Add("Rank", "Rank");
            _leaderboardGrid.Columns.Add("StudentName", "Student");
            _leaderboardGrid.Columns.Add("Score", "Score");
            _leaderboardGrid.Columns.Add("Accuracy", "Accuracy");
            _leaderboardGrid.Columns.Add("Correct", "Correct");

            this.Controls.Add(_leaderboardGrid);

            // Style the grid
            _leaderboardGrid.Columns["Rank"].Width = 60;
            _leaderboardGrid.Columns["Score"].Width = 80;
            _leaderboardGrid.Columns["Accuracy"].Width = 80;
            _leaderboardGrid.Columns["Correct"].Width = 80;
        }

        private async Task InitializeAsync()
        {
            _ngrokBaseUrl = await NgrokHelper.GetNgrokBaseUrlAsync();
            if (string.IsNullOrEmpty(_ngrokBaseUrl))
            {
                MessageBox.Show("❌ Failed to retrieve ngrok URL.");
                return;
            }

            SetupWebSocket();
            StartAutoRefresh();
            _ = RefreshLeaderboard(); // Initial load
        }

        private void SetupWebSocket()
        {
            try
            {
                string wsBaseUrl = _ngrokBaseUrl.Replace("https://", "wss://");
                _webSocket = new WebSocket($"{wsBaseUrl}/socket.io/?EIO=4&transport=websocket");

                _webSocket.OnMessage += (sender, e) =>
                {
                    if (e.IsText && e.Data.Contains("leaderboard_update"))
                    {
                        Invoke(new Action(() => RefreshLeaderboard()));
                    }
                };

                _webSocket.OnOpen += (sender, e) =>
                {
                    // Join leaderboard room
                    var joinData = new { session_id = _sessionId, type = "leaderboard" };
                    string joinMessage = $"42[\"join_leaderboard\", {JsonSerializer.Serialize(joinData)}]";
                    _webSocket.Send(joinMessage);
                };

                _webSocket.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebSocket error: {ex.Message}");
            }
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 2000; // Refresh every 2 seconds
            _refreshTimer.Tick += async (s, e) => await RefreshLeaderboard();
            _refreshTimer.Start();
        }

        private async Task RefreshLeaderboard()
        {
            try
            {
                //var response = await _client.GetAsync($"http://192.168.0.102:5000/api/sessions/{_sessionId}/leaderboard");
                var response = await _client.GetAsync($"{_ngrokBaseUrl}/api/sessions/{_sessionId}/leaderboard");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(content);

                    if (result.TryGetProperty("leaderboard", out var lbArray))
                    {
                        var leaderboard = new List<LeaderboardEntry>();
                        int rank = 1;

                        foreach (var item in lbArray.EnumerateArray())
                        {
                            var entry = new LeaderboardEntry
                            {
                                Rank = GetRankEmoji(rank),
                                StudentName = item.GetProperty("name").GetString(),
                                Score = item.GetProperty("score").GetInt32(),
                                CorrectAnswers = item.TryGetProperty("correct_answers", out var correct) ? correct.GetInt32() : 0,
                                TotalAnswers = item.TryGetProperty("total_answers", out var total) ? total.GetInt32() : 0
                            };

                            entry.Accuracy = entry.TotalAnswers > 0 ?
                                Math.Round((double)entry.CorrectAnswers / entry.TotalAnswers * 100, 1) : 0;

                            leaderboard.Add(entry);
                            rank++;
                        }

                        UpdateLeaderboardGrid(leaderboard);
                    }
                }
            }
            catch (Exception ex)
            {
                // Silent fail for background refresh
            }
        }

        private string GetRankEmoji(int rank)
        {
            switch (rank)
            {
                case 1: return "🥇";
                case 2: return "🥈";
                case 3: return "🥉";
                default: return $"{rank}.";
            }
        }

        private void UpdateLeaderboardGrid(List<LeaderboardEntry> leaderboard)
        {
            _leaderboardGrid.Rows.Clear();

            foreach (var entry in leaderboard)
            {
                _leaderboardGrid.Rows.Add(
                    entry.Rank,
                    entry.StudentName,
                    entry.Score,
                    $"{entry.Accuracy}%",
                    $"{entry.CorrectAnswers}/{entry.TotalAnswers}"
                );

                // Color the top 3 rows
                int rowIndex = _leaderboardGrid.Rows.Count - 1;
                if (entry.Rank == "🥇")
                    _leaderboardGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.Gold;
                else if (entry.Rank == "🥈")
                    _leaderboardGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGray;
                else if (entry.Rank == "🥉")
                    _leaderboardGrid.Rows[rowIndex].DefaultCellStyle.BackColor = Color.SandyBrown;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _webSocket?.Close();
            _refreshTimer?.Stop();
            base.OnFormClosing(e);
        }
        private void LiveLeaderboardForm_Load(object sender, EventArgs e)
        {
            // This can be empty if you don't need load-time logic
            // Or put your initialization code here
            SetupWebSocket();
            StartAutoRefresh();
            _ = RefreshLeaderboard(); // Initial load
        }
    }
}