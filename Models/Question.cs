using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerPointAddIn1.Models
{
    public class Question
    {
        public long QuestionId { get; set; }
        public string Content { get; set; }
        public int Points { get; set; }
        public int TimeLimitSeconds { get; set; }
        public List<Answer> Answers { get; set; } = new List<Answer>();

        public class Answer
        {
            public long AnswerId { get; set; }
            public string Content { get; set; }
            public bool IsCorrect { get; set; }
        }

    }
}
