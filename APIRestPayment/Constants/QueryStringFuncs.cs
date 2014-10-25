using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace APIRestPayment.Constants
{
    public class QueryStringFuncs
    {
        public static string GenerateRequestStringAsync(string uri, string[] parameters, string[] paramValues)
        {
                var delimiter = (uri.IndexOf('?') == -1) ? '?' : '&';
                for (int i = 0; i < parameters.Length; i++)
                {
                    uri += delimiter + encodeURIComponent(parameters[i]) + '=' + encodeURIComponent(paramValues[i]);
                    delimiter = '&';
                }

            return uri;
        }

        private static string encodeURIComponent(string paramValue)
        {
            return  HttpUtility.UrlEncode(paramValue);
        }
    }
}