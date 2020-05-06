using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostuffsAutoSync
{
    class WorkerArgument
    {
        public int Timeout = 5;
        public DataView Table = new DataView();
        public bool IsClearCookies = false;
        public int PriceChangedCount = 0;
        public int InvalidPriceCount = 0;
        public string Vendor;
        public string RowFilter = "";
    }
}
