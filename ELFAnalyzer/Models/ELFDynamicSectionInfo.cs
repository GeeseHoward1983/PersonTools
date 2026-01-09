using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFDynamicSectionInfo
    {
        public required string Tag;
        public required string Type;
        public required string Value;
    }
}
