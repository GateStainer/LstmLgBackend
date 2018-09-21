using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace LstmLgBackend.Models
{
    //Used for tokenize and de_tokenize
    [NotMapped]
    public class Token
    {
        public int start;
        public int length;
        public bool needSpaceBefore;

        public Token(int start)
        {
            this.start = start;
            length = 1;
            needSpaceBefore = true;
        }

        //Not used now
        public static string tokenize(string sentence)
        {
            string template = sentence;
            List<char> special_mark = new List<char>() { '.', ',', '?', '!', ':', ';', '[', ']', '(', ')', '-', '+', '*', '%', '/', '\\', '\'','\"' };
            bool flag = true;
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] == '{')
                {
                    flag = false;
                }
                else if (template[i] == '}')
                {
                    flag = true;
                }
                else if (flag == true && special_mark.Contains(template[i]))
                {
                    char mark = template[i];
                    template = template.Remove(i, 1).Insert(i, " " + mark + " ");
                    i = i + 2;
                }
            }
            return template;
        }

        public static List<Token> GenerateTokenList(string sentence)
        {
            List<char> specialMark = new List<char>() { '.', ',', '?', '!', ':', ';', '[', ']', '(', ')', '-', '+', '*', '%', '/', '\\', '\'', '\"' };
            List<Token> result = new List<Token>();
            string template = sentence.Trim();
            if (string.IsNullOrWhiteSpace(template))
            {
                return result;
            }
            Token temp = new Token(0);
            temp.length = 0;
            bool newWord = false;
            bool isSlot = false;
            for (int index = 0; index < template.Length; index++)
            {
                if (template[index] == '{')
                {
                    isSlot = true;
                }
                else if (template[index] == '}')
                {
                    isSlot = false;
                }

                if (template[index] == ' ')
                {
                    newWord = true;
                }
                //Special_mark
                else if (specialMark.Contains(template[index]) && isSlot == false)
                {
                    result.Add(temp);
                    temp = new Token(index);
                    if (index > 0 && template[index - 1] != ' ')
                    {
                        temp.needSpaceBefore = false;
                    }
                    newWord = true;
                }
                else if (newWord == true)
                {
                    newWord = false;
                    result.Add(temp);
                    temp = new Token(index);
                    if(index > 0 && template[index-1] != ' ')
                    {
                        temp.needSpaceBefore = false;
                    }
                }
                else
                {
                    temp.length++;
                }

                //last word
                if (index == template.Length - 1)
                {
                    if (specialMark.Contains(template[index]) && temp.length == 1)
                    {
                        temp.needSpaceBefore = false;
                    }
                    result.Add(temp);
                }

            }
            return result;
        }
    }
}