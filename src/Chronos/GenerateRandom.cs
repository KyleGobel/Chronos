using System;
using System.Text;

namespace Chronos
{
    public class GenerateRandom
    {
        private static Random random = new Random((int)DateTime.Now.Ticks);//thanks to McAden

        public static string String(int length)
        {
            var builder = new StringBuilder();
            char ch;
            for (int i = 0; i < length; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26*random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public static int Int(int min = 0, int max = 10)
        {
            return random.Next(min, max);
        }
    }
}