using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CSV_dynamic_parser.Models
{
    public class ExcelViewModel
    {
        public string columnName { get; set; }
        public List<string> columnValue { get; set; }
        public List<string> excelColumns { get; set; }
    }
}