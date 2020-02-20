using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace bilicomic_tool
{
    public static class HttpHelper
    {
        /// <summary>
        /// 发送一个GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public async static Task<IHttpResults> Get(string url, IDictionary<string, string> headers = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }

                    var response = await client.GetAsync(new Uri(url));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new HttpResults()
                        {
                            code = (int)response.StatusCode,
                            status = false,
                            message = StatusCodeToMessage((int)response.StatusCode)
                        };
                    }
                    response.EnsureSuccessStatusCode();
                    IHttpResults httpResults = new HttpResults()
                    {
                        code = (int)response.StatusCode,
                        status = response.StatusCode == HttpStatusCode.OK,
                        results = await response.Content.ReadAsStringAsync(),
                        message = StatusCodeToMessage((int)response.StatusCode)
                    };
                    return httpResults;
                }



            }

            catch (Exception ex)
            {
                return new HttpResults()
                {
                    code = ex.HResult,
                    status = false,
                    message = "网络请求出现错误(GET)"
                };
            }
        }

        /// <summary>
        /// 发送一个GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <returns></returns>
        public async static Task<byte[]> GetBytes(string url, IDictionary<string, string> headers = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }

                    var response = await client.GetAsync(new Uri(url));
                    if (!response.IsSuccessStatusCode)
                    {
                        return null;
                    }
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsByteArrayAsync();
                   
                }



            }

            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 发送一个POST请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public async static Task<IHttpResults> Post(string url, IDictionary<string, string> body, IDictionary<string, string> headers = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }
                 
                    var response = await client.PostAsync(new Uri(url), new FormUrlEncodedContent(body));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new HttpResults()
                        {
                            code = (int)response.StatusCode,
                            status = false,
                            message = StatusCodeToMessage((int)response.StatusCode)
                        };
                    }
                    string result = await response.Content.ReadAsStringAsync();
                    IHttpResults httpResults = new HttpResults()
                    {
                        code = (int)response.StatusCode,
                        status = response.StatusCode == HttpStatusCode.OK,
                        results = result,
                        message = StatusCodeToMessage((int)response.StatusCode)
                    };
                    return httpResults;
                }



            }
            catch (Exception ex)
            {
                return new HttpResults()
                {
                    code = ex.HResult,
                    status = false,
                    message = "网络请求出现错误(POST)"
                };
            }
        }

        /// <summary>
        /// 发送一个POST请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="headers"></param>
        /// <param name="cookie"></param>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public async static Task<IHttpResults> Post(string url, string body, IDictionary<string, string> headers = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }
                    var response = await client.PostAsync(new Uri(url), new StringContent(body, Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return new HttpResults()
                        {
                            code = (int)response.StatusCode,
                            status = false,
                            message = StatusCodeToMessage((int)response.StatusCode)
                        };
                    }
                    string result = await response.Content.ReadAsStringAsync();
                    IHttpResults httpResults = new HttpResults()
                    {
                        code = (int)response.StatusCode,
                        status = response.StatusCode == HttpStatusCode.OK,
                        results = result,
                        message = StatusCodeToMessage((int)response.StatusCode)
                    };
                    return httpResults;
                }



            }
            catch (Exception ex)
            {
                return new HttpResults()
                {
                    code = ex.HResult,
                    status = false,
                    message = "网络请求出现错误(POST)"
                };
            }
        }

        private static string StatusCodeToMessage(int code)
        {

            switch (code)
            {
                case 0:
                case 200:
                    return "请求成功";
                case 504:
                    return "请求超时了";
                case 301:
                case 302:
                case 303:
                case 305:
                case 306:
                case 400:
                case 401:
                case 402:
                case 403:
                case 404:
                case 500:
                case 501:
                case 502:
                case 503:
                case 505:
                    return "网络请求失败，代码:" + code;
                case -2147012867:
                case -2147012889:
                    return "请检查的网络连接";
                default:
                    return "未知错误";
            }
        }
    }
    public interface IHttpResults
    {
        bool status { get; set; }
        int code { get; set; }
        string message { get; set; }
        string results { get; set; }
        JObject GetJObject();
        Task<T> GetJson<T>();
    }
    public class HttpResults : IHttpResults
    {
        public int code { get; set; }
        public string message { get; set; }
        public string results { get; set; }
        public bool status { get; set; }
        public async Task<T> GetJson<T>()
        {
            return await Task.Run<T>(() =>
            {
                return JsonConvert.DeserializeObject<T>(results);
            });

        }
        public JObject GetJObject()
        {
            try
            {
                var obj = JObject.Parse(results);
                return obj;
            }
            catch (Exception)
            {
                return null;
            }

        }


    }

 

}
