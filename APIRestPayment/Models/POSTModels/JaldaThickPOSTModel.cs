﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models.POSTModels
{
    public class JaldaThickPOSTModel
    {
        public string JaldaThickType  { set; get; }
        public uint? SerialNumber  { set; get; }
        public string OrderNumber { set; get; }
    }
}