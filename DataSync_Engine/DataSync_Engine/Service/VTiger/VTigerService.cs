using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataSync_Engine.Service.VTiger
{
    public class VTigerService
    {

        public string VTigerUrl { get; set; } //= "http://192.168.1.48:8888/";
        private static VTigerService _vTigerServiceInstance;

        private VTigerService() { }

        public static VTigerService GetVTigerServiceInstance()
        {
            if (_vTigerServiceInstance != null) return _vTigerServiceInstance;
            _vTigerServiceInstance = new VTigerService();
            return _vTigerServiceInstance;
        }

        public string VTigerExecuteOperation(string operation, string parameters, bool post)
        {
            return VTigerExecuteOperation(VTigerUrl, operation, parameters, post);
        }

        public string VTigerExecuteOperation(string vTigerUrl, string operation, string parameters, bool post)
        {
            string response;
            if (post)
                response = HttpPost(vTigerUrl + "webservice.php?operation=" + operation, parameters);
            else
                response = HttpGet(vTigerUrl + "webservice.php?operation=" + operation + "&" + parameters);
            if (response == null)
            {
                Console.WriteLine("Unable to connect to VTiger-Service");
            }
            return response;
        }

        private static string HttpGet(string url)
        {
            try
            {
                HttpWebRequest webRequest = GetWebRequest(url);
                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
                var jsonResponse = "";
                using (var sr = new StreamReader(response.GetResponseStream()))
                    jsonResponse = sr.ReadToEnd();
                return jsonResponse;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private static string HttpPost(string url, string parameters)
        {
            var webRequest = GetWebRequest(url);
            webRequest.ContentType = "application/x-www-form-urlencoded";

            webRequest.Method = "POST";
            var data = Encoding.ASCII.GetBytes(parameters);
            webRequest.ContentLength = data.Length;
            using (var stream = webRequest.GetRequestStream())
                stream.Write(data, 0, data.Length);

            try
            { 
                var webResponse = webRequest.GetResponse();
                if (webResponse != null)
                {
                    StreamReader sr = new StreamReader(webResponse.GetResponseStream());
                    return sr.ReadToEnd().Trim();
                }
                return null;
            }
            catch (WebException webException)
            {
                Console.WriteLine(webException);
                return null;
            }
        }

        private static HttpWebRequest GetWebRequest(string formattedUri)
        {
            Uri outUri;
            if (Uri.TryCreate(formattedUri, UriKind.Absolute, out outUri)
               && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps))
                return System.Net.WebRequest.Create(outUri) as HttpWebRequest;

            return System.Net.WebRequest.Create("http://0.0.0.0:8888") as HttpWebRequest; ;
        }

        public string GetMd5Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            byte[] data;
            using (var md5Hasher = MD5.Create())
                data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            var sBuilder = new StringBuilder();
            foreach (byte t in data)
            {
                sBuilder.Append(t.ToString("x2"));
            }
            return sBuilder.ToString();
        }

        internal VTigerResult<T> GetDeSerializeObject<T>(string o)
        {
            return Task.Factory.StartNew(() => JsonConvert.DeserializeObject<VTigerResult<T>>(o)).Result;
        }

        internal string GetSerializeObject(object o)
        {
            return Task.Factory.StartNew(() => JsonConvert.SerializeObject(o)).Result;
        }
    }
}