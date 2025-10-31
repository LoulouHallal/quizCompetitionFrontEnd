using Microsoft.Office.Tools.Ribbon;
using PowerPointAddIn1.Forms;
using PowerPointAddIn1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PowerPointAddIn1
{
    public partial class MainRibbon
    {
        private void Ribbon1_Load(object sender, RibbonUIEventArgs e)
        {

        }

        private async void button1_Click(object sender, RibbonControlEventArgs e)
        {

            try
            {

                // Show login form first
                var loginForm = new TeacherLoginForm();
                var result = loginForm.ShowDialog();

                if (result == DialogResult.OK && loginForm.IsAuthenticated)
                {
                    // Teacher is authenticated, show main control panel
                    ShowMainControlPanel(loginForm.TeacherId);
                }

            }
            catch (Exception ex)
            {
                // Show detailed error information
                MessageBox.Show($"CRITICAL ERROR in button1_Click:\n\n" +
                               $"Message: {ex.Message}\n\n" +
                               $"Type: {ex.GetType().Name}\n\n" +
                               $"Stack Trace:\n{ex.StackTrace}\n\n" +
                               $"Inner Exception: {ex.InnerException?.Message}",
                               "Critical Error",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
            }
        }


        //button 2

        private void button2_Click(object sender, RibbonControlEventArgs e)
        {
            try
            {
                // Show login form first
                var loginForm = new TeacherLoginForm();
                var result = loginForm.ShowDialog();

                if (result == DialogResult.OK && loginForm.IsAuthenticated)
                {
                    // Show Author Tab for quiz creation
                    var authorForm = new AuthorTabForm(loginForm.TeacherId);
                    //authorForm.ShowDialog();
                    var authorResult = authorForm.ShowDialog();

                    // 🚨 NEW: If author form was closed with OK (meaning they want to start session)
                    if (authorResult == DialogResult.OK && authorForm.CreatedCourseId > 0)
                    {
                        // Start session with the created course
                        StartSessionWithCourse(loginForm.TeacherId, authorForm.CreatedCourseId);
                    }
                }
                else
                {
                    MessageBox.Show("Login required for quiz authoring", "Info");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private async void StartSessionWithCourse(int teacherId, int courseId)
        {
            try
            {
                var powerpointApp = Globals.ThisAddIn.Application;
                var presentation = powerpointApp.ActivePresentation;

                int currentSlide = 1;
                try
                {
                    currentSlide = powerpointApp.ActiveWindow.View.Slide.SlideNumber;
                }
                catch (Exception slideEx)
                {
                    MessageBox.Show($"Cannot get current slide, using slide 1. Error: {slideEx.Message}", "Info");
                }

                // 🚨 USE THE ACTUAL COURSE ID FROM AUTHOR TAB, NOT HARDCODED 5
                var session = await CreateClassSession(courseId, $"{presentation.Name} - Slide {currentSlide}");

                if (session != null)
                {
                    var controlForm = new SessionControlForm(
                        session.ClassId,
                        session.ClassCode,
                        presentation.Name,
                        currentSlide,
                        teacherId,
                        session.QrCodeUrl,
                        courseId
                    );
                    controlForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting session: {ex.Message}", "Error");
            }
        }
        private async Task<ClassSession> CreateClassSession(int courseId, string sessionName)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Use the API key from your Flask config
                    string apiKey = "dev-teacher-key";

                    var sessionData = new
                    {
                        course_id = courseId,
                        class_name = sessionName
                    };

                    var json = JsonSerializer.Serialize(sessionData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // USE ConfigureAwait(false) - THIS IS CRITICAL FOR OFFICE ADD-INS
                    var response = await client.PostAsync("http://localhost:5000/api/sessions", content).ConfigureAwait(false);



                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                        // 🚨 ONLY CHECK FOR qr_url - REMOVE qr_base64 CHECK
                        string qrUrl = null;
                        if (result.TryGetProperty("qr_url", out var qrElement))
                        {
                            qrUrl = qrElement.GetString();
                        }
                 
                        return new ClassSession
                        {
                            ClassId = result.GetProperty("class_id").GetInt64(),
                            ClassCode = result.GetProperty("class_code").GetString(),
                            JoinUrl = result.GetProperty("join_url").GetString(),
                            QrCodeUrl = qrUrl   

                        };
 
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API returned {response.StatusCode}: {errorContent}");
                    }


                }
                catch (Exception ex)
                {
                    throw new Exception($"Error in CreateClassSession: {ex.Message}");
                }
            }

        }

        private async Task<bool> TestFlaskConnection()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync("http://localhost:5000/health");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
               // MessageBox.Show($"Server health check failed: {ex.Message}", "Debug - Server Down");
                return false;
            }
        }
        private async void ShowMainControlPanel(int teacherId)
        {
            try
            {
                var powerpointApp = Globals.ThisAddIn.Application;
                var presentation = powerpointApp.ActivePresentation;

                int currentSlide = 1; // Default to slide 1
                try
                {
                    currentSlide = powerpointApp.ActiveWindow.View.Slide.SlideNumber;
                }
                catch (Exception slideEx)
                {
                    MessageBox.Show($"Cannot get current slide, using slide 1. Error: {slideEx.Message}", "Info");
                }
                // Create session for this teacher
                var session = await CreateClassSession(5, $"{presentation.Name} - Slide {currentSlide}");


                // 🚨 REMOVE HARDCODED 5 - you'll need to get the actual course ID here
                // For now, let's prompt user or use a different approach
                MessageBox.Show("Please use the Author tab to create a quiz first, then start session from there.", "Info");
                return;

                // If you want to keep this functionality, you'll need to:
                // 1. Get the user's courses and let them select one
                // 2. Or always create a new course (like you were doing with ID 5)

                /* if (session != null)
                 {
                     var controlForm = new SessionControlForm(
                         session.ClassId,
                         session.ClassCode,
                         presentation.Name,
                         currentSlide,
                         teacherId,  // Pass teacher ID
                         session.QrCodeUrl   //session.QrCodeBase64 
                     );
                     controlForm.ShowDialog();
                 }*/
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }


    }



}
