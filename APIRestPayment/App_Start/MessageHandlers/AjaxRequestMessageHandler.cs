using APIRestPayment.App_Start.MessageHandlers.Crypto;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace APIRestPayment.App_Start.MessageHandlers
{
    public class AjaxRequestMessageHandler : DelegatingHandler
    {
            protected async override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                
                
                var response = await base.SendAsync(request, cancellationToken);
                
                //check status code is 200,201,.....
                if ((((int)response.StatusCode)/100 ==3) && IsAjaxRequest(request.GetOwinContext().Request))
                {
                    var responseError = request.CreateResponse(response.StatusCode, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)response.StatusCode,
                        },
                        data = response.Headers.Location,
                    });
                    var tsc = new TaskCompletionSource<HttpResponseMessage>();
                    tsc.SetResult(responseError);
                    return await tsc.Task;
                }
                return response;
            }
            private static bool IsAjaxRequest(IOwinRequest request)
            {
                IReadableStringCollection query = request.Query;
                if ((query != null) && (query["X-Requested-With"] == "XMLHttpRequest"))
                {
                    return true;
                }
                IHeaderDictionary headers = request.Headers;
                return ((headers != null) && (headers["X-Requested-With"] == "XMLHttpRequest"));
            }
    }
}