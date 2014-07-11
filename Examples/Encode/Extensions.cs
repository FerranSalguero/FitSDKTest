using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EncodeDemo
{
    public static class Extensions
    {
        public static int RawInt(this decimal value)
        {
            var raw = value.ToString().Replace(".", string.Empty);

            return int.Parse(raw);
        }
    }
}
