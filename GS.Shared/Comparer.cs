using System.Collections.Generic;
using System.Linq;

namespace GS.Shared
{
    public class Comparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)){return 1;}
                return a.Split('.').Zip(b.Split('.'),
                    (x, y) => int.Parse(x).CompareTo(int.Parse(y)))
                    .FirstOrDefault(i => i != 0);
        }
    }
}
