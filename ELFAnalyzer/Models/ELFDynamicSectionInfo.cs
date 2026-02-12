using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalTools.ELFAnalyzer.Models
{
    internal sealed class ELFDynamicSectionInfo
    {
        public required string Tag { get; set; }
        public required string Type { get; set; }
        public required string Value { get; set; }
    }
}
