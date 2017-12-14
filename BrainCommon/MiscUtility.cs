using System;
using System.Collections.Generic;
using System.Text;

namespace BrainCommon
{
    public static class Utility
    {
        public static string Show(this byte data)
        {
            return $"{data:X2}";
        }
        
        public static string Show(this byte[] data)
        {
            var sb = new StringBuilder(); 
            for (var i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].Show());
            }
            return sb.ToString();
        }
        
        public static string Show(this ArraySegment<byte> data)
        {
            var sb = new StringBuilder();
            var lst = data as IReadOnlyList<byte>;
            for (var i = 0; i < lst.Count; i++)
            {
                sb.Append(lst[i].Show());
            }
            return sb.ToString();
        }

        public static string Show(this List<ArraySegment<byte>> data)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < data.Count; i++)
            {
                sb.Append(data[i].Show());
            }
            return sb.ToString();
        }
    }}