// 🚨 ADD THIS CLASS AT THE BOTTOM OF THE FILE (outside StudentQuestionForm)
public class QuestionData
{
    public int question_id { get; set; }
    public string question_text { get; set; }
    public string[] options { get; set; }
    public int time_limit { get; set; }
    public int points { get; set; }
}