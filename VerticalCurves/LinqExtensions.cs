using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VerticalCurves
{
    public static class LinqExtensions
    {
        public static T Next<T>(this List<T> source, T current)
        {

            return source.Skip(source.IndexOf(current) + 1).Take(1).FirstOrDefault();
        }

        public static T Previous<T>(this List<T> source, T current)
        {
            return source.Skip(source.IndexOf(current) - 1).Take(1).FirstOrDefault();
        }
    }
}
