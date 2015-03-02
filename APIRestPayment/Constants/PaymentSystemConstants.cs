using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Constants
{
    public static class PaymentSystemConstants
    {
        /// <summary>
        /// The account number in which all deposits are stored
        /// </summary>
        public const string TemporaryAccountNumber = "777001000101";
        
        /// <summary>
        /// The account number which receives all transaction fees.
        /// </summary>
        public const string TransactionFeesAccountNumber = "777001000102";

        /// <summary>
        /// all transaction fees are extracted in the following (Iran Rials) currency.
        /// </summary>
        public const string TransactionFeesAccountCurrency = "IRR";
    }
}