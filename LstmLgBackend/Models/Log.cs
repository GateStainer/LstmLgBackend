using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace LstmLgBackend.Models
{
    public class Log
    {
        [Key]
        public int id { get; set; }
        public string log { get; set; }
        public DateTime timestamp { get; set; }
    }
}