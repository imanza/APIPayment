using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace APIRestPayment.Constants
{
    public static class TransactionTypes
    {
        public const string Purchase = "Purchase";
        public const string Transfer = "Transfer";
        public const string Jalda = "Jalda";
        public const string Deposit = "Deposit";
        public const string Fees = "Fees";
        public const string TopUp = "TopUp";
        public const string Withdraw = "Withdraw";
    }

    public static class PaymentStatusTypes
    {
        public const string Canceled = "Canceled";
        public const string Completed = "Completed";        
    }

    public static class JaldaThickTypes
    {
        public const string Start = "Start";
        public const string Payment = "Payment";
        public const string Terminate = "Terminate";
    }
}