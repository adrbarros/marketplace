﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDelivery.Domain
{
    public class subItem
    {
        public string name { get; set; }
        public decimal quantity { get; set; }
        public decimal price { get; set; }
        public decimal totalPrice { get; set; }
        public decimal discount { get; set; }
        public decimal addition { get; set; }
        public string externalId { get; set; }
        public string observations { get; set; }
    }
}
