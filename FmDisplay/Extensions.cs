using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace FmDisplay {
    public static class Extensions {
        public static Complex Conjugate(this Complex meh) {
            return new Complex(meh.Real, -meh.Imaginary);
        }
    }
}
