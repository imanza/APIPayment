using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace APIRestPayment.Constants
{
    public static class ClaimNames
    {
        public const string ApplicationClientName = "applicationClient";
        public const string OAuthScope = "urn:oauth:scope";
        public const string JaldaContractId = "jaldaContractId";
        public const string NameID = ClaimTypes.NameIdentifier;
        public const string Name = ClaimTypes.Name;
        public const string Role = ClaimTypes.Role;
    }
}