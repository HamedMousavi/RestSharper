using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;


namespace Map.Web.Services {


    public class RestSharper {


        public RestSharper(string baseUrl = null) {
            this.baseUrl = baseUrl;
        }


        public RestSharper Url(string url) {
            this.url = combineUrl(baseUrl, url);
            return this;
        }


        public RestSharper Proxy(string proxyServer) {
            this.proxyServer = proxyServer;
            return this;
        }


        public RestSharper Log(Func<long, string, string, string, Task> logger, long requestId) {
            this.logger = logger;
            this.requestId = requestId;
            return this;
        }


        public RestSharper Headers(params (string, string)[] headers) {
            this.headers = new List<(string, string)>(headers);
            this.headers.Insert(0, ("Accept-Charset", "utf-8"));
            this.headers.Insert(0, ("Accept", "application/json"));
            return this;
        }


        public RestSharper Query(params (string, string)[] queryParams) {
            this.queryParams = new List<(string, string)>(queryParams);
            return this;
        }


        public RestSharper JsonBody(object jsonBody) {
            this.jsonBody = jsonBody;
            return this;
        }


        public RestSharper XmlBody(object xmlBody) {
            this.xmlBody = xmlBody;
            return this;
        }

        internal RestSharper FormParams(params (string, string)[] formParams) {
            this.formParams = formParams;
            return this;
        }

        public static T MapJson<T>(string raw) {
            return JsonConvert.DeserializeObject<T>(raw);
        }

        public async Task<T> PostAsync<T>() {
            return JsonConvert.DeserializeObject<T>(await ExecuteAsync(Method.POST));
        }


        public Task<string> PostAsync() {
            return ExecuteAsync(Method.POST);
        }


        public async Task<T> GetAsync<T>() {
            return JsonConvert.DeserializeObject<T>(await ExecuteAsync(Method.GET));
        }


        public Task<string> GetAsync() {
            return ExecuteAsync(Method.GET);
        }


        public async Task<string> ExecuteAsync(Method method) {
            var client = new RestClient(url);
            if (!string.IsNullOrWhiteSpace(proxyServer)) {
                client.Proxy = new WebProxy(proxyServer);
            }

            var request = new RestRequest(method);
            if (headers != null) {
                foreach (var header in headers) {
                    if (isValid(header)) {
                        request.AddHeader(header.Item1, header.Item2);
                    }
                }
            }

            if (queryParams != null) {
                foreach (var query in queryParams) {
                    if (isValid(query)) {
                        request.AddQueryParameter(query.Item1, query.Item2);
                    }
                }
            }

            if (formParams != null) {
                foreach (var param in formParams) {
                    if (isValid(param)) {
                        request.AddParameter(param.Item1, param.Item2, ParameterType.GetOrPost);
                    }
                }
            }

            // default request format
            request.RequestFormat = DataFormat.Json;
            if (jsonBody != null) {
                request.AddJsonBody(jsonBody);
            }

            if (xmlBody != null) {
                request.RequestFormat = DataFormat.Xml;
                request.AddXmlBody(xmlBody);
            }

            var response = await client.ExecuteAsync(request).ConfigureAwait(false);

            await logAsync(request, response);

            if (response.StatusCode != HttpStatusCode.OK) {
                throw new Exception($"ERROR: {response.StatusCode} | CONTENT: {response.Content} | MESSAGE: {response.ErrorException}:{response.ErrorMessage}");
            }

            return response.Content;
        }


        private static string combineUrl(string baseUrl, string url) {
            var combined = $"{(string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl + "/")}{url}";
            var uri = new Uri(combined);
            combined = combined.Replace($"{uri.Scheme}://", "");
            return $"{uri.Scheme}://{combined.Replace("//", "/")}";
        }


        private async Task logAsync(RestRequest request, IRestResponse response) {
            await logger?.Invoke(requestId
                         , url
                         , JsonConvert.SerializeObject(new {
                             Method = Enum.GetName(request.Method)
                             , Format = Enum.GetName(request.RequestFormat)
                             , Proxy = proxyServer
                             , Headers = headers == null ? "null" : string.Join(",", headers.Select(h => $"{h.Item1}:{h.Item2}"))
                             , Query = queryParams == null ? "null": string.Join(",", queryParams.Select(h => $"{h.Item1}:{h.Item2}"))
                             , Form = formParams == null ? "null" : string.Join(",", formParams.Select(h => $"{h.Item1}:{h.Item2}"))
                             , Body = $"{request?.Body?.Value}"
                         })
                         , JsonConvert.SerializeObject(new { 
                             Status = response?.StatusCode,
                             ContentType = response?.ContentType,
                             ContentEncoding = response?.ContentEncoding,
                             Error = response?.ErrorException?.ToString(),
                             Headers = response?.Headers == null ? "null" : string.Join(",", response.Headers.Select(h => $"{h.Name}:{h.Value}")),
                             Content = response?.Content,
                         }));
        }


        private static bool isValid((string, string) pair) {
            return !string.IsNullOrWhiteSpace(pair.Item1) && !string.IsNullOrWhiteSpace(pair.Item2);
        }


        private string url;
        private string proxyServer;
        private List<(string, string)> headers;
        private List<(string, string)> queryParams;
        private object jsonBody;
        private object xmlBody;
        private (string, string)[] formParams;
        private Func<long, string, string, string, Task> logger;
        private long requestId;
        private readonly string baseUrl;
    }
}
