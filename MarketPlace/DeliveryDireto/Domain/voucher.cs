﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryDireto.Domain
{
    public class voucher
    {
        public string type { get; set; }
        public decimal id { get; set; }
        public string description { get; set; }
        public string code { get; set; }
        public string customCode { get; set; }
    }
}
