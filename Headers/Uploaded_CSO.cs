using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cuneiform_Style_Analyser.Headers
{
    public class Uploaded_CSO
    {
        public List<CSO_Table> All_CSO_Tables { get; set; } = new List<CSO_Table>();
        public List<int> VariationsNumber { get; set; } = new List<int>();
    }
}
