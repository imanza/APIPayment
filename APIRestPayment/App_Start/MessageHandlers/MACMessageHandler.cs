using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace APIRestPayment.App_Start.MessageHandlers
{
    public class MACMessageHandler : DelegatingHandler
    {
        private CASPaymentDAO.DataHandler.ApplicationDataHandler applicationHandler;
        protected async override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post && !request.IsLocal())
            {
                if (!await HandleHMACAuthentication(request))
                {
                    //sets the response and says the request is invalid or an replay attack is possibly happend
                    var responseError = request.CreateResponse(HttpStatusCode.Forbidden, new Models.QueryResponseModel
                    {
                        meta = new Models.MetaModel
                        {
                            code = (int)HttpStatusCode.Forbidden,
                            errorMessage = "Request is rejected due to replay attack",
                        }
                    });
                    var tsc = new TaskCompletionSource<HttpResponseMessage>();
                    tsc.SetResult(responseError);
                    return await tsc.Task;
                }

            }
            // Call the inner handler.
            var response = await base.SendAsync(request, cancellationToken);
            return response;
        }

        private async Task<bool> HandleHMACAuthentication(HttpRequestMessage request)
        {
            return await Task.Run(async () =>
                {
                    var req = request;

                    if (req.Headers.Authorization != null && Constants.HMACSettings.AuthenticationScheme.Equals(req.Headers.Authorization.Scheme, StringComparison.OrdinalIgnoreCase))
                    {
                        var rawAuthzHeader = req.Headers.Authorization.Parameter;

                        var autherizationHeaderArray = GetAutherizationHeaderValues(rawAuthzHeader);

                        if (autherizationHeaderArray != null)
                        {
                            var APPId = autherizationHeaderArray[0];
                            var incomingBase64Signature = autherizationHeaderArray[1];
                            var nonce = autherizationHeaderArray[2];
                            var requestTimeStamp = autherizationHeaderArray[3];

                            var isValid = await isValidRequest(req, APPId, incomingBase64Signature, nonce, requestTimeStamp);

                            if (isValid)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }).ConfigureAwait(false);
        }

        private async Task<bool> isValidRequest(HttpRequestMessage req, string APPId, string incomingBase64Signature, string nonce, string requestTimeStamp)
        {
            return await Task.Run(async () =>
                {
                    applicationHandler = new CASPaymentDAO.DataHandler.ApplicationDataHandler(WebApiApplication.SessionFactory);
                    string requestContentBase64String = "";
                    string requestUri = HttpUtility.UrlEncode(req.RequestUri.AbsoluteUri.ToLower());
                    string requestHttpMethod = req.Method.Method;

                    CASPaymentDTO.Domain.Application clientApplication = applicationHandler.Search(new CASPaymentDTO.Domain.Application { ClientID = APPId }).Cast<CASPaymentDTO.Domain.Application>().FirstOrDefault();
                    if (object.Equals(default(CASPaymentDTO.Domain.Application), clientApplication))
                    {
                        //Did not find app Id in database
                        return false;
                    }

                    var sharedKey = clientApplication.Secrethash;

                    if (isReplayRequest(nonce, requestTimeStamp))
                    {
                        //Replay attack detected
                        return false;
                    }

                    byte[] hash = await ComputeHash(req.Content);

                    if (hash != null)
                    {
                        requestContentBase64String = Convert.ToBase64String(hash);
                    }

                    string data = String.Format("{0}{1}{2}{3}{4}{5}", APPId, requestHttpMethod, requestUri, requestTimeStamp, nonce, requestContentBase64String);

                    var secretKeyBytes = Convert.FromBase64String(sharedKey);

                    byte[] signature = Encoding.UTF8.GetBytes(data);

                    using (HMACSHA256 hmac = new HMACSHA256(secretKeyBytes))
                    {
                        byte[] signatureBytes = hmac.ComputeHash(signature);

                        return (incomingBase64Signature.Equals(Convert.ToBase64String(signatureBytes), StringComparison.Ordinal));
                    }
                }).ConfigureAwait(false);
        }

        private bool isReplayRequest(string nonce, string requestTimeStamp)
        {
            if (System.Runtime.Caching.MemoryCache.Default.Contains(nonce))
            {
                //Duplicate nonces found. Risk of replay attack
                return true;
            }

            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan currentTs = DateTime.UtcNow - epochStart;

            var serverTotalSeconds = Convert.ToUInt64(currentTs.TotalSeconds);
            var requestTotalSeconds = Convert.ToUInt64(requestTimeStamp);

            if ((serverTotalSeconds - requestTotalSeconds) > Constants.HMACSettings.RequestMaxAgeInSeconds)
            {
                //The age of request is expired
                return true;
            }

            System.Runtime.Caching.MemoryCache.Default.Add(nonce, requestTimeStamp, DateTimeOffset.UtcNow.AddSeconds(Constants.HMACSettings.RequestMaxAgeInSeconds));
            return false;
        }

        private static async Task<byte[]> ComputeHash(HttpContent httpContent)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = null;
                var content = await httpContent.ReadAsByteArrayAsync();
                if (content.Length != 0)
                {
                    hash = md5.ComputeHash(content);
                }
                return hash;
            }
        }

        private string[] GetAutherizationHeaderValues(string rawAuthzHeader)
        {
            var credArray = rawAuthzHeader.Split(':');

            if (credArray.Length == 4)
            {
                return credArray;
            }
            else
            {
                return null;
            }
        }

    }
}
