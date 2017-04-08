using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;

namespace POST
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Job> jobs = new List<Job>();

            //参数 
            var param = new Dictionary<string, string>() { { "first", "false" }, { "pn", "18" }, { "kd", Uri.EscapeUriString("前端") } };

            //并行处理
            Parallel.For(1, 31, i =>
            {
                if (i == 0)
                {
                    param["first"] = "true";
                }
                param["pn"] = i.ToString();

                //POST请求
                var request = CreatePostHttpResponse("https://www.lagou.com/jobs/positionAjax.json?city=" + Uri.EscapeUriString("深圳") + "&needAddtionalResult=false", param, 0, null, null);
                var result = GetResponseString(request);

                //解析结果
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(result);

                //取出记录
                foreach (dynamic item in json.content.positionResult.result as Newtonsoft.Json.Linq.JArray)
                {
                    int begin, end;
                    string salary = item.salary.ToString();
                    MatchCollection matches = Regex.Matches(salary, @"\d+");
                    if (matches.Count > 1)
                    {
                        begin = Convert.ToInt32(matches[0].Value);
                        end = Convert.ToInt32(matches[1].Value);
                    }
                    else
                    {
                        begin = end = Convert.ToInt32(matches[0].Value);
                    }

                    jobs.Add(new Job() { Begin = begin, End = end, Title = item.positionName.ToString(), Url = "https://www.lagou.com/jobs/" + item.positionId.ToString() + ".html" });

                    Console.WriteLine(string.Format("{0}\t{1}\t{2}\thttps://www.lagou.com/jobs/{3}.html", begin, end, item.positionName, item.positionId));
                }
            });

            using (Context context = new Context())
            {
                context.Database.ExecuteSqlCommand("truncate table Jobs;");
                context.Job.AddRange(jobs);
                context.SaveChanges();
            }

            Console.ReadLine();
        }


        /// <summary>  
        /// 创建POST方式的HTTP请求  
        /// </summary>  
        public static HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters, int timeout, string userAgent, CookieCollection cookies)
        {
            HttpWebRequest request = null;

            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((a, b, c, d) => true);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";

            //设置代理UserAgent和超时
            //request.UserAgent = userAgent;
            //request.Timeout = timeout; 

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            //通过Chrome、Fiddler得到的Cookie
            request.Headers.Add("Cookie", "user_trace_token=20160807143405-f00b763c-5c68-11e6-83c2-525400f775ce; LGUID=20160807143405-f00b7bc5-5c68-11e6-83c2-525400f775ce; OUTFOX_SEARCH_USER_ID_NCOO=629042951.3769003; index_location_city=%E6%B7%B1%E5%9C%B3; JSESSIONID=46E904B117BCA78AF0A35443A5DD4E5D; PRE_UTM=; PRE_HOST=; PRE_SITE=; PRE_LAND=https%3A%2F%2Fwww.lagou.com%2F; TG-TRACK-CODE=index_navigation; _ga=GA1.2.584559713.1470551645; LGSID=20170408222808-961f8b03-1c67-11e7-9d7b-5254005c3644; LGRID=20170408224012-45965707-1c69-11e7-9d7b-5254005c3644; Hm_lvt_4233e74dff0ae5bd0a3d81c6ccf756e6=1490887972,1491521596,1491566728,1491619940; Hm_lpvt_4233e74dff0ae5bd0a3d81c6ccf756e6=1491662412; SEARCH_ID=92db2fc2ed1748a19d11f9416eafdbaf");

            //发送POST数据  
            if (!(parameters == null || parameters.Count == 0))
            {
                StringBuilder buffer = new StringBuilder();
                int i = 0;
                foreach (string key in parameters.Keys)
                {
                    if (i > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, parameters[key]);
                        i++;
                    }
                }
                byte[] data = Encoding.ASCII.GetBytes(buffer.ToString());
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            string[] values = request.Headers.GetValues("Content-Type");
            return request.GetResponse() as HttpWebResponse;
        }

        /// <summary>
        /// 获取请求的数据
        /// </summary>
        public static string GetResponseString(HttpWebResponse webresponse)
        {
            using (Stream s = webresponse.GetResponseStream())
            {
                StreamReader reader = new StreamReader(s, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }
    }
}
