using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //
            HttpWebRequest request = HttpWebRequest.CreateHttp("https://poc2sellui.edgecompute.app/?CreateContext=&salesOrder=eyJzYWxlc09yZGVySWQiOiJhZno2aTF1cWhrZnR5ajlwN2p6MSIsImN1c3RvbWVySWQiOiJjdXN0b21lci0xIn0%3D");
            //HttpWebRequest request = HttpWebRequest.CreateHttp("https://poc2ia.edgecompute.app/availabilities/sto/se?itemNos=10261129&zip=23335&expand=StoresList8");
            request.Method = "GET";
            //request.Accept = "application/json; version=2";
            //request.Headers.Add("X-Client-Id", "0a158dfa-fb83-470f-adf8-d3eda9757999");
            var resp = await request.GetResponseAsync();
            var reader = new StreamReader(resp.GetResponseStream());
            var json = reader.ReadToEnd();
            HttpClient req = new HttpClient();
            var content = await req.GetAsync("https://poc2ia.edgecompute.app/availabilities/sto/se?itemNos=10261129&zip=23335&expand=StoresList8");
            var aa = await content.Content.ReadAsStringAsync();
            Console.WriteLine(aa);

        }
    }
}
