using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using org.jsoup;
using org.jsoup.nodes;
using org.jsoup.select;

namespace ResultFetch {
	/// <summary>
	/// Root Namespace for SJCE Result Fetch Application
	/// </summary>

	[System.Runtime.CompilerServices.CompilerGenerated]
	class NamespaceDoc {
		//Trick to Sandcastle Help File Builder
		//For including namespace Documentation
	}

	/// <summary>
	/// Thrown when errors happen related to Student Class
	/// </summary>
	public class StudentException : Exception {
		public StudentException() : base("Bad Student Data") { }
		public StudentException(string message) : base(message) { }
		public StudentException(string message, Exception inner) : base(message, inner) { }
	}
	/// <summary>
	/// A Class for Dealing with Result Fetch tasks
	/// </summary>
	public class Student {
		private string name;
		private string usn;
		private Dictionary<string, string> grades;
		/// <summary>
		/// Initializes a new instance of Student class with USN
		/// </summary>
		/// <param name="usn">The USN of the Student</param>
		/// <exception cref="StudentException">If the USN is invalid</exception>
		public Student(string usn) {
			usn = usn.ToUpper().Trim();
			if (usn.Length != 10 || usn.Substring(0, 3) != "4JC")
				throw new StudentException($"Invalid USN Number {usn} ");
			this.usn = usn.Trim();
			this.grades = new Dictionary<string, string>();
		}
		/// <summary>
		/// Adds the Subject to Current Student
		/// </summary>
		/// <param name="subcode">Subject Code</param>
		/// <param name="grade">The Letter Grade obtained by the Student</param>
		public void AddSubject(string subcode, string grade) {
			grades.Add(subcode.Trim(), grade.Trim());
		}
		/// <summary>
		/// Obtain a String representation of Student
		/// </summary>
		/// <returns> String representing the Name,USN and Marks of Current Student</returns>
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(name);
			sb.AppendLine(usn);
			foreach (var kvp in grades)
				sb.AppendLine($"{kvp.Key} : {kvp.Value}");
			sb.AppendLine($"SGPA : {GetSgpa()}");
			return sb.ToString();
		}
		/// <summary>
		/// Fetches the Result from SJCE website
		/// </summary>
		/// <param name="usn">USN of the Student</param>
		/// <returns> A Student Object containg the Details </returns>
		/// <exception cref="StudentException">If USN is incorrect</exception>
		/// <exception cref="HttpRequestException">If an Connectictivity Error is occured</exception>
		/// <example><code lang="cs">				
		///		var result = Student.FetchResult("4jC15CS000");
		///		try {
		///			result.Wait();
		///			Console.WriteLine(result.Result);
		///		}
		///		catch (Exception e) {
		///			if (e.InnerException is HttpRequestException)
		///				Console.WriteLine(e.InnerException.Message + " Net sari mada manga!");
		///			else if (e.InnerException is StudentException)
		///				Console.WriteLine(e.InnerException.Message + " Nee JC Student Pakka na??");
		///			else
		///				Console.WriteLine("Fatal Error" + e);
		///			Thread.CurrentThread.Abort(-1);
		///		}
		///	</code></example>
		public static async Task<Student> FetchResult(string usn) {
			using (var client = new HttpClient()) {
				var values = new Dictionary<string, string> {
					["USN"] = usn,
					["submit_result"] = "Fetch Result"
				};
				try {
					Student s = new Student(usn);
					var content = new FormUrlEncodedContent(values);
					var response = await client.PostAsync("http://sjce.ac.in/view-results", content);
					if (response.StatusCode != HttpStatusCode.OK)
						throw new HttpRequestException("Website unable to Handle Request...Might be busy");
					var responseString = await response.Content.ReadAsStringAsync();
					//Jsoup.jar was used
					//Coverted using IVMC ! :)
					//Inlcude IKVM.OpenJDK.Core.dll as Reference
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
					return s;
				}
				catch (Exception e) {
					if (e is NullReferenceException)
						throw new StudentException("Might be a bad USN", e);
					else if (e is HttpRequestException)
						throw new HttpRequestException("Net sari illa marre", e);
					else
						throw;
				}
			}
		}
		/// <summary>
		/// Calculates the SGPA of Current Student
		/// </summary>
		/// <returns>SGPAE</returns>
        public double GetSgpa() {
            double totalCredits = 0;
            double earnedCredits = 0;
            foreach(var grade in grades) {
				int gp = "SABCDE".IndexOf(grade.Value.Trim());
				gp = gp == -1 ? 0 : 10 - gp;
				int cr = grade.Key.Contains("L") ? 2 : grade.Key.Contains("CS") ? 4 : 0;
				totalCredits += cr;
				earnedCredits += cr * gp;
            }
            return earnedCredits / totalCredits;
        }
	}
	class Program {
		static void Main(string[] args) {
			//VS2017!
			Console.WriteLine("Enter USN ");
			string usn = Console.ReadLine().Trim().ToUpper();
			Thread t = new Thread(() => {
				Task<Student> result = Student.FetchResult(usn);
				try {
					result.Wait();
					Console.WriteLine(result.Result);
				}
				catch (Exception e) {
					if (e.InnerException is HttpRequestException)
						Console.WriteLine(e.InnerException.Message + " Net sari mada manga!");
					else if (e.InnerException is StudentException)
						Console.WriteLine(e.InnerException.Message + " Nee JC Student Pakka na??");
					else
						Console.WriteLine("Fatal Error" + e);
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
