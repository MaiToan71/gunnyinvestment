﻿using System;
using System.Collections.Generic;

#nullable disable

namespace Gunny.Models
{
    public partial class ConfigCharge
    {
        public int Id { get; set; }
        public double? Amount { get; set; }
        public double? Receive { get; set; }
        public double? Promo { get; set; }
    }
}
