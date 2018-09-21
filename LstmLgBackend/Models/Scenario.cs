using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace LstmLgBackend.Models
{
    public class Scenario
    {
        [Key]
        public int id { get; set; }
        [Required]
        [StringLength(200)]
        [Index(IsUnique = true)]
        public string name { get; set; }
        public string description { get; set; }
        public Scenario()
        {
        }
        public Scenario(Scenario s)
        {
            id = s.id;
            name = s.name;
        }
    }
}