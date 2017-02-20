using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;

namespace ResultFetch
{
    class Program
    {
        static async Task<string> FetchResult()
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    ["USN"] = "4JC15CS129",
                    ["Action"] = "Fetch+Result"
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync("http://sjce.ac.in/view-results", content);

                var responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }
        }
        static void Main(string[] args)
        {
            Thread t = new Thread(() => Console.WriteLine(FetchResult().Result));
            t.Start();
            for (int i = 0; i < 100; i++)
                Console.Write("*");
            t.Join();
            Console.ReadKey();
        }
    }
}
