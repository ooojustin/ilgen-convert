using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace test_bins {

    class Program {

        static void Main(string[] args) {

            int r = add(69, 31);
            Console.WriteLine(r);
            Console.ReadKey();

        }

        public static int k = 69;

        public static int forget50() {
            int s = 0;
            for (int i = 0; i < 5; i++)
                s += 10;
            return s;
        }


        public static int add(int a, int b) {
            int c = a + b + k + get100() + forget50();
            return c;
        }

        private static int get100() {
            int p = k + 31;
            return p == 100 ? 200 : 100;
        }

    }

}
