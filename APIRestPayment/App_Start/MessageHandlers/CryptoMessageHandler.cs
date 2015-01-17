using APIRestPayment.App_Start.MessageHandlers.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace APIRestPayment.App_Start.MessageHandlers
{
    public class CryptoMessageHandler: DelegatingHandler
    {
            protected async override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.Method == HttpMethod.Post)request =await DecryptRequest(request);
                // Call the inner handler.
                var response = await base.SendAsync(request, cancellationToken);
                
                //check status code is 200,201,.....
                if (((int)response.StatusCode) / 100 == 2)response =await EncryptResponse(response);
                
                return response;
            }

            private async Task<HttpRequestMessage> DecryptRequest(HttpRequestMessage req )
            {
                //Models.RequestResponseStructureModels.EncryptedJSONModel cipherMessageBody = (Models.RequestResponseStructureModels.EncryptedJSONModel)await req.Content.ReadAsAsync < Models.RequestResponseStructureModels.EncryptedJSONModel>().ConfigureAwait(false);
                //string decryptedJSONstring = StringCipher.Decrypt(cipherMessageBody.CipherText, "12345678");
                //req.Content = new StringContent(decryptedJSONstring,Encoding.UTF8 ,"application/json");
                return req;
            }

            private async Task<HttpResponseMessage> EncryptResponse(HttpResponseMessage res)
            {
                ////Newtonsoft.Json.Linq.JObject jobject = (Newtonsoft.Json.Linq.JObject)await res.Content.ReadAsAsync<Newtonsoft.Json.Linq.JObject>().ConfigureAwait(false);
                //string responseJSONPlainText =await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                //Models.RequestResponseStructureModels.EncryptedJSONModel cipherMessageBody = new Models.RequestResponseStructureModels.EncryptedJSONModel
                //{
                //    CipherText = StringCipher.Encrypt(responseJSONPlainText, "12345678")
                //};
                //res.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(cipherMessageBody, new Newtonsoft.Json.JsonSerializerSettings { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore })
                //    , Encoding.UTF8, "application/json");
                return res;
            }
    }
}