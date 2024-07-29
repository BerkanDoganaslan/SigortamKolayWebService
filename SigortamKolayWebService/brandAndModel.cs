using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SigortamKolayWebService
{
    public class brandAndModel
    {
        public int brandCode;
        public string brandName;
        public List<models> models;
    }

    public class models
    {
        public int modelCode;
        public string modelName;
    }
}