using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETCoreBot.Models
{
    public class WasteManagement
    {
        public int TotalWaste { get; set; }
        public int PerformanceIssues { get; set; }
        public int PortalRuns { get; set; }
        public int Escaping { get; set; }
        public int Contesting { get; set; }
        public int PlanningIssues { get; set; }
        public int PathFindingIssues { get; set; }
        public int Respawning { get; set; }
        public int MovingIssues { get; set; }
        public int PowerUps { get; set; }

    }
}
