using System.Linq;
using System.Text;

namespace tpldfexplore.Service
{
    public class Int2WordsService
    {
        static readonly string[] DecimalWords = new string[]
        {
            "zero",
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine"
        };

        public string ToWords(int i)
        {
            var sb = new StringBuilder();
            // Get index for ch numbers
            foreach (var idx in i.ToString().ToArray().Select(ch => int.Parse(ch.ToString())))
            {
                // for each numeral replace with the associated decimal word
                sb.Append(DecimalWords[idx]);
            }
            return sb.ToString();
        }
    }
}