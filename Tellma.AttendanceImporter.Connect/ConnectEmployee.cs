using System;
using System.Collections.Generic;
using System.Text;

namespace Tellma.AttendanceImporter.Connect
{
    public class ConnectEmployee
    {
        public string BitrixId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DutyStation { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }
    }
}
