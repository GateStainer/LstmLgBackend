using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace LstmLgBackend.Models
{
    [NotMapped]
    public class SVPair
    {
        public string slot { get; set; }
        public string value { get; set; }
    }

    public class Sample
    {
        [Key]
        public int id { get; set; }
        [NotMapped]
        [System.Runtime.Serialization.IgnoreDataMember]
        public List<SVPair> mySVPairs { get; set; }
        [Required]
        [Column("slotValuePair")]
        public string slotValuePair
        {
            get
            {
                if (mySVPairs == null)
                {
                    return null;
                }
                else if (mySVPairs.Count() == 0)
                {
                    return null;
                }
                else
                {
                    return JsonConvert.SerializeObject(mySVPairs);
                }
            }
            set
            {
                mySVPairs = string.IsNullOrEmpty(value) ?
                    new List<SVPair>() :
                    JsonConvert.DeserializeObject<List<SVPair>>(value);
            }
        }
        public string response { get; set; }
        public string template { get; set; }
        [NotMapped]
        public List<Token> tokens { get; set; }
        public bool? good { get; set; }
        public bool? IsModelResult { get; set; }
        [ForeignKey("Intent")]
        public int intentID { get; set; }
        public virtual Intent Intent { get; set; }

        public Sample()
        {

        }

        public Sample(Sample sample)
        {
            id = sample.id;
            slotValuePair = sample.slotValuePair;
            response = sample.response;
            template = sample.template;
            good = sample.good;
            IsModelResult = sample.IsModelResult;
            intentID = sample.intentID;
            Intent = new Intent(sample.Intent);
        }

        public void GenerateTemplate(List<SlotDescription> slotDescriptions)
        {
            if (mySVPairs == null || response == null || response.Length == 0)
            {
                return;
            }
            //List<slot,value>
            List<SVPair> tempSVPair = new List<SVPair>();
            foreach (SVPair svpair in mySVPairs)
            {
                //ignore conditional svPair
                bool conditional = false;
                //slotDescription: List<name,type,conditional>
                foreach (SlotDescription slotDescription in slotDescriptions)
                {
                    if (svpair.slot.Equals(slotDescription.name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        conditional = slotDescription.condition;
                        break;
                    }
                }
                if (!string.IsNullOrWhiteSpace(svpair.slot) && !string.IsNullOrWhiteSpace(svpair.value) && conditional == false)
                {
                    tempSVPair.Add(svpair);
                }
            }
            tempSVPair = tempSVPair.OrderByDescending(item => item.value.Length).ToList();
            template = response;
            foreach (SVPair svpair in tempSVPair)
            {
                string value = svpair.value;
                string slot = svpair.slot;
                if (!ReplaceSlotValueInTemplate(value, slot))
                {
                    value = RemoveEnding0(value);
                    ReplaceSlotValueInTemplate(value, slot);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string RemoveEnding0(string value)
        {
            if (value.Contains('.'))
            {
                int index = value.Length - 1;
                while (index > 0 && value[index] == '0')
                {
                    index--;
                }
                if (index > 0 && value[index] == '.')
                {
                    index--;
                }
                return value.Substring(0, index + 1);
            }
            else
            {
                return value;
            }
        }
        private bool ReplaceSlotValueInTemplate(string value, string slot)
        {
            bool replaced = false;
            int start = 0;
            while ((start = this.template.IndexOf(value, start, StringComparison.InvariantCultureIgnoreCase)) >= 0)
            {
                int left = start - 1;
                int right = start + value.Length;
                bool validLeftBoundary = false;
                bool validRightBoundary = false;

                if (left < 0)
                {
                    validLeftBoundary = true;
                }
                else
                {
                    char ch = this.template[left];
                    if (ch != '{')
                    {
                        if (IsAlphabet(ch))
                        {
                            if (IsAlphabet(value[0]))
                            {
                                validLeftBoundary = false;
                            }
                            else
                            {
                                validLeftBoundary = true;
                            }
                        }
                        else if (IsDigit(ch))
                        {
                            if (IsDigit(value[0]))
                            {
                                validLeftBoundary = false;
                            }
                            else
                            {
                                validLeftBoundary = true;
                            }
                        }
                        else
                        {
                            validLeftBoundary = true;
                        }
                    }
                }

                if (right >= this.template.Length)
                {
                    validRightBoundary = true;
                }
                else
                {
                    char ch = this.template[right];
                    if (ch != '}')
                    {
                        if (IsAlphabet(ch))
                        {
                            if (IsAlphabet(value[value.Length - 1]))
                            {
                                validRightBoundary = false;
                            }
                            else
                            {
                                validRightBoundary = true;
                            }
                        }
                        else if (IsDigit(ch))
                        {
                            if (IsDigit(value[value.Length - 1]))
                            {
                                validRightBoundary = false;
                            }
                            else
                            {
                                validRightBoundary = true;
                            }
                        }
                        else
                        {
                            validRightBoundary = true;
                        }
                    }
                }
                if (validLeftBoundary && validRightBoundary)
                {
                    string replaceString = this.template.Substring(start, value.Length);
                    this.template = this.template.Remove(start, value.Length);
                    this.template = this.template.Insert(start, "{" + slot + "}");
                    replaced = true;
                }
                else
                {
                    start = start + 1;
                }
            }
            return replaced;
        }

        private static bool IsAlphabet(char ch)
        {
            if ('a' <= ch && ch <= 'z')
            {
                return true;
            }
            if ('A' <= ch && ch <= 'Z')
            {
                return true;
            }
            return false;
        }

        private static bool IsDigit(char ch)
        {
            if ('0' <= ch && ch <= '9')
            {
                return true;
            }
            return false;
        }
    }
}