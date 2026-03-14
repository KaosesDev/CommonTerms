using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTermsFunc.Models
{
    public class FileModel
    {
        public string FileName { get; set; } = string.Empty;
        public string FileNameCleaned { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string ParentFolder { get; set; } = string.Empty;
    }
}
