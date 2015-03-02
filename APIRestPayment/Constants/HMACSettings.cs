using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Constants
{
    public static class HMACSettings
    {
        public const string AuthenticationScheme = "amx";
        public const UInt64 RequestMaxAgeInSeconds = 300;
    }
}