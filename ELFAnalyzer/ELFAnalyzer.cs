using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyTool.ELFAnalyzer.Core;
using MyTool.ELFAnalyzer.Models;

namespace MyTool.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        private readonly ELFParser _parser;

        public ELFAnalyzer(string filePath)
        {
            _parser = new ELFParser(filePath);
        }

        public ELFAnalyzer(byte[] fileData)
        {
            _parser = new ELFParser(fileData);
        }

        public ELFParser Parser => _parser;

        private string GetMagicString()
        {
            var magic = new StringBuilder();
            magic.Append($"{_parser.Header.EI_MAG0:X2} ");
            magic.Append($"{_parser.Header.EI_MAG1:X2} ");
            magic.Append($"{_parser.Header.EI_MAG2:X2} ");
            magic.Append($"{_parser.Header.EI_MAG3:X2} ");
            for (int i = 0; i < 7; i++)
            {
                magic.Append($"{_parser.Header.EI_PAD[i]:X2} ");
            }
            return magic.ToString().Trim();
        }
    }
}