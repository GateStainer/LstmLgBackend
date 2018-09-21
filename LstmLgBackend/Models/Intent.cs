using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace LstmLgBackend.Models
{
    [NotMapped]
    public class SlotDescription
    {
        public string name { get; set; }

        public string type { get; set; }

        public bool condition { get; set; }
    }

    public class Intent
    {
        [Key]
        public int id { get; set; }
        [Required]
        [StringLength(200)]
        [Index("IX", 2, IsUnique = true)]
        public string name { get; set; }
        public string description { get; set; }
        [NotMapped]
        [System.Runtime.Serialization.IgnoreDataMember]
        public List<SlotDescription> mySlotDescriptions { get; set; }
        [Column("slotDescriptions")]
        public string slotDescriptions
        {
            get
            {
                if (mySlotDescriptions == null)
                {
                    return null;
                }
                else if (mySlotDescriptions.Count() == 0)
                {
                    return null;
                }
                else
                {
                    return JsonConvert.SerializeObject(mySlotDescriptions);
                }
            }
            set
            {
                mySlotDescriptions = string.IsNullOrEmpty(value) ?
                    new List<SlotDescription>() :
                    JsonConvert.DeserializeObject<List<SlotDescription>>(value);
            }
        }

        [System.Runtime.Serialization.IgnoreDataMember]
        public byte[] model { get; set; }
        //Model status:0 for no trained model, 1 for pending, 2 for success trained, 3 for failed trained
        public int modelStatus { get; set; }
        //Time for last training
        public DateTime lastTrainTime { get; set; }
        public int batchStatus { get; set; }
        public int previewStatus { get; set; }
        [System.Runtime.Serialization.IgnoreDataMember]
        public byte[] sv_dict { get; set; }
        [System.Runtime.Serialization.IgnoreDataMember]
        public byte[] token { get; set; }
        [Required]
        [ForeignKey("Scenario")]
        [Index("IX", 1, IsUnique = true)]
        public int scenarioID { get; set; }
        public virtual Scenario Scenario { get; set; }

        public Intent()
        {
            //default not trained
            modelStatus = 0;
        }

        public Intent(Intent intent)
        {
            id = intent.id;
            name = intent.name;
            description = intent.description;
            model = intent.model.ToArray();
            sv_dict = intent.sv_dict.ToArray();
            token = intent.token.ToArray();
            scenarioID = intent.scenarioID;
            Scenario = new Scenario(intent.Scenario);
        }
    }
}