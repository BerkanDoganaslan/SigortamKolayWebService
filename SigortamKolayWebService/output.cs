using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SigortamKolayWebService
{
    public class output
    {
        public List<outputList> outList;
        public string clause;
        public double totalPremiumAmount;
        public long policyNumber;
        public string productNo;
        public string productName;
        public string policyBeginDate;
        public string policyEndDate;
        public string insurancefirmName;
    }

    public class outputList
    {
        public long policyNumber;
        public int coverCode;
        public string coverName;
        public double coverAmount;
        public double premiumAmount;
    }
}