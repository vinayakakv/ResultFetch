using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using org.jsoup;
using org.jsoup.nodes;
using org.jsoup.select;

namespace ResultFetch {

	public class StudentException : Exception {
		public StudentException() : base("Bad Student Data") { }
		public StudentException(string message) : base(message) { }
		public StudentException(string message, Exception inner) : base(message, inner) { }
	}

	class Student {
		private string name;
		private string usn;
		private Dictionary<string, string> grades;
		public Student(string usn) {
			if (usn.Trim().Length != 10 || usn.Trim().Substring(0, 3) != "4JC")
				throw new StudentException($"Invalid USN Number {usn.Trim()} ");
			this.usn = usn.Trim();
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
				Student s = null;
				try {
					s = new Student(usn);
					var content = new FormUrlEncodedContent(values);
					var response = await client.PostAsync("http://sjce.ac.in/view-results", content);
					var responseString = await response.Content.ReadAsStringAsync();
					Document doc = Jsoup.parse(responseString);
					Elements nameAndUsn = doc.getElementsByTag("center");
					string name = nameAndUsn.select("h1").first().text().Substring(7);
					s.name = name;
					Element marks = doc.getElementsByTag("table").first();
					foreach (Element row in marks.select("tr")) {
						Elements td = row.select("td");
						string value = td.text();
						if (!string.IsNullOrEmpty(value))
							s.AddSubject(value.Split()[0], value.Substring(value.LastIndexOf(" ")));
					}
				}
				catch (Exception e) {
					if (e is ArgumentNullException)
						throw new StudentException("Might be a bad USN", e);
					else if (e is HttpRequestException)
						throw new StudentException("Net sati illa marre", e);
					else
						throw;
				}
				return s;
			}
		}
	}
	class Program {
		static void Main(string[] args) {
			Thread t = new Thread(() => {
				var result = Student.FetchResult("4JC15CS129");
				try {
					result.Wait();
					Console.WriteLine(result.Result?.ToString());
				}
				catch (Exception e) {
					if (e.InnerException is HttpRequestException)
						Console.WriteLine("Net sari illa marre");
					if (e.InnerException is StudentException)
						Console.WriteLine(e.InnerException.Message);
					Thread.CurrentThread.Abort(-1);
				}
			});
			t.Start();
			Console.WriteLine("Please wait while the result is being fetched");
			t.Join();
			Console.ReadKey();
		}
	}
}
