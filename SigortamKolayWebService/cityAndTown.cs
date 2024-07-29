using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SigortamKolayWebService
{
    public class cityAndTown
    {
        public int cityCode;
        public string cityName;
        public List<towns> towns;
    }

    public class towns
    {
        public int townCode;
        public string townName;
    }
}