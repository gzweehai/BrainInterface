using System;
using System.Collections.Generic;
using System.Text;

namespace BrainCommon
{
    public static class Utility
    {
        public static int[] CopyToArray(this ArraySegment<int> seg)
        {
            if (seg.Array == null) return null;
            var result = new int[seg.Count];
            Array.Copy(seg.Array,seg.Offset,result,0,seg.Count);
            return result;
        }
        
        public static string Show(this byte data)
        {
            return $"{data:X2}";
        }
        
        public static string Show(this byte[] data,int count=-1)
        {
            var sb = new StringBuilder();
            if (count < 0) count = data.Length;
            for (var i = 0; i < count; i++)
            {
                sb.Append(data[i].Show());
                sb.Append(" ");
            }
            return sb.ToString();
        }
        
        public static string Show(this int[] data)
        {
            var sb = new StringBuilder(); 
            for (var i = 0; i < data.Length; i++)
            {
                sb.Append(data[i]);
                sb.Append(" ");
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
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string Show(this IList<ArraySegment<byte>> data)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < data.Count; i++)
            {
                sb.Append(data[i].Show());
                sb.Append(" ");
            }
            return sb.ToString();
        }
    }}