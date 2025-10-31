using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerPointAddIn1.Models
{
    public class ClassSession
    {
        public long ClassId { get; set; }
        public string ClassCode { get; set; }
        public string JoinUrl { get; set; }
        public string QrCodeUrl { get; internal set; }

    }
}
