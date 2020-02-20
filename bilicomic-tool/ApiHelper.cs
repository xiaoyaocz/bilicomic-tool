using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bilicomic_tool
{
    public class ApiHelper
    {
        public static UserInfo user { get; set; }

        public static async Task<RequestResult<List<ComicImage>>> GetImages(int comic_id, int epid,string quality)
        {
            try
            {
                Dictionary<string, string> header = new Dictionary<string, string>();
                header.Add("cookie", ApiHelper.user.cookie);
                var getImageIndex = await HttpHelper.Post("https://manga.bilibili.com/twirp/comic.v1.Comic/GetImageIndex?device=pc&platform=web", JsonConvert.SerializeObject(new
                {
                    ep_id = epid

                }), header);
                var obj = getImageIndex.GetJObject();
                if (obj["code"].ToInt32() == 0)
                {
                    var url = obj["data"]["host"].ToString() + obj["data"]["path"].ToString();
                    var content = ApiHelper.DecodeContent(await HttpHelper.GetBytes(url, header), comic_id, epid);
                    var jobj = JObject.Parse(content);
                    var imgs_str = JsonConvert.DeserializeObject<List<string>>(jobj["pics"].ToString());
                    for (int i = 0; i < imgs_str.Count; i++)
                    {
                        imgs_str[i] = imgs_str[i] + quality;
                    }
                    var imgs = await HttpHelper.Post("https://manga.bilibili.com/twirp/comic.v1.Comic/ImageToken?device=pc&platform=web", JsonConvert.SerializeObject(new
                    {
                        urls =JsonConvert.SerializeObject(imgs_str)
                    }), header);
                    if (imgs.status)
                    {
                        var result = JsonConvert.DeserializeObject<RequestResult<List<ComicImage>>>(imgs.results);
                        result.status = result.code == 0;
                        return result;
                    }
                    else
                    {
                        return new RequestResult<List<ComicImage>>()
                        {
                            status = false,
                            msg = imgs.message
                        };
                    }
                  
                }
                else
                {
                    return new RequestResult<List<ComicImage>>()
                    {
                        status=false,
                        msg=obj["msg"].ToString()
                    };
                 
                }
            }
            catch (Exception ex)
            {

                return new RequestResult<List<ComicImage>>()
                {
                    status = false,
                    msg = ex.Message
                };
            }
           
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <returns></returns>
        private  static string DecodeContent(byte[] data, int comic_id, int ep_id)
        {
            try
            {
                var bytes = data.Skip(9).ToArray();
                var key = new int[8] {
                    ep_id&0xff,
                    ep_id>>8&0xff,
                    ep_id>>16&0xff,
                    ep_id>>24&0xff,
                    comic_id&0xff,
                    comic_id>>8&0xff,
                    comic_id>>16&0xff,
                    comic_id>>24&0xff
                };
                for (int i = 0; i < bytes.Length; i++)
                {
                    var re = Convert.ToUInt32(bytes[i]) ^ key[i % 8];
                    bytes[i] = Convert.ToByte(re);
                }
                var content = Encoding.UTF8.GetString(DecodeZip(bytes));
                return content;
            }
            catch (Exception)
            {
                throw new Exception("解密数据失败");
            }
           
        }
        private static byte[] DecodeZip(byte[] bytes)
        {
            ZipFile zipFile = new ZipFile(new MemoryStream(bytes));
            var stream = zipFile.GetInputStream(zipFile[0]);
            byte[] overarr = new byte[10000];
            stream.Read(overarr, 0, overarr.Length);
            stream.Close();
            return overarr;
        }
    }
    public class RequestResult<T>
    {
        public bool status { get; set; }
        public int code { get; set; }
        public string msg { get; set; }
        public T data { get; set; }
    }
    public class ComicImage
    {
        public string url { get; set; }
        public string token { get; set; }
        public string img_url
        {
            get
            {
                return url + "?token=" + token;
            }
        }
    }

}
