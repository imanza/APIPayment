﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Models
{
    public class QueryResponseModel
    {
        public MetaModel meta;
        public object data;
        public PaginationModel pagination;

    }
}