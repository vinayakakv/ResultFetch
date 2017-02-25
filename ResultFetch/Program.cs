using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using org.jsoup;
using org.jsoup.nodes;
using org.jsoup.select;

namespace ResultFetch {
	class Student {
		private string name;
		private string usn;
		private Dictionary<string, string> grades;
		public Student(string name, string usn) {
			this.name = name;
			this.usn = usn;
			this.grades = new Dictionary<string, string>();
		}
		public void AddSubject(string subcode, string grade) {
			grades.Add(subcode, grade);
		}
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(name);
			sb.AppendLine(usn);
			foreach (var kvp in grades)
				sb.AppendLine($"{kvp.Key} : {kvp.Value}");
			return sb.ToString();
		}
		public static async Task<Student> FetchResult(string usn) {
			using (var client = new HttpClient()) {
				var values = new Dictionary<string, string> {
					["USN"] = usn,
					["submit_result"] = "Fetch Result"
				};
				var content = new FormUrlEncodedContent(values);
				var response = await client.PostAsync("http://sjce.ac.in/view-results", content);
				var responseString = await response.Content.ReadAsStringAsync();
				Document doc = Jsoup.parse(responseString);
				Elements nameAndUsn = doc.getElementsByTag("center");
				string name = nameAndUsn.select("h1").first().text().Substring(7);
				Element marks = doc.getElementsByTag("table").first();
				Student s = new Student(name, usn);
				foreach (Element row in marks.select("tr")) {
					Elements td = row.select("td");
					string value = td.text();
					if (!string.IsNullOrEmpty(value))
						s.AddSubject(value.Split()[0], value.Substring(value.LastIndexOf(" ")));
				}
				return s;
			}
		}
	}
	class Program {
		static void Main(string[] args) {
			Thread t = new Thread(() => Console.WriteLine(Student.FetchResult("4JC15CS131").Result));
			t.Start();
			Console.WriteLine("Wait ....");
			t.Join();
			Console.CancelKeyPress += (a, b) => {
				Console.WriteLine("Exiting");
				Thread.Sleep(1000);
				Environment.Exit(0);
			};
			Console.ReadKey();
		}
	}
}
