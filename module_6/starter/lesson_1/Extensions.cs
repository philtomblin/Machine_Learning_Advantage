using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtensionMethods
{
    public static class MyExtensions
    {
        // extension method to convert a string to title case
        public static string ToTitle(this string s)
        {
            var sb = new StringBuilder();
            var space = false;

            for (int i = 0; i < s.Length; i++)
            {
                if (i == 0 || space)
                    sb.Append(s[i].ToString().ToUpper());
                else
                    sb.Append(s[i]);
                space = s[i] == ' ' ? true : false;
            }
            return sb.ToString();
        }
    }
}