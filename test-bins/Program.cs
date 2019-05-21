using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test_bins {

    class Program {

        static void Main(string[] args) {

            int r = add(69, 31);    // 100
            r += k;                 // 169
            r += get200();          // 369
            r += get50();           // 419
            r += parse_int("11");   // 430

            Console.WriteLine(r);
            Console.ReadKey();

        }

        public static int parse_int(string input) {
            try {
                return Convert.ToInt32(input);
            } catch (Exception) {
                return 0;
            }
        }

        public static int add(int a, int b) {
            int c = a + b;
            return c;
        }

        public static int get50() {
            int s = 0;
            for (int i = 0; i < 5; i++)
                s += 10;
            return s;
        }

        public static int k = 69;
        private static int get200() {
            int p = add(k, 31);
            return p == 100 ? 200 : 100;
        }

    }

}
