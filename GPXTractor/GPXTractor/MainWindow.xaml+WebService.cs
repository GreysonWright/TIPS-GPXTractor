using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace GPXTractor {
	public partial class MainWindow {
		private List<SiteResponse> getSites() {
			HttpWebRequest request = WebRequest.Create(@"http://weatherevent.caps.ua.edu/api/sites") as HttpWebRequest;
			request.Method = "GET";
			request.ContentType = "application/json";

			HttpWebResponse response = request.GetResponse() as HttpWebResponse;
			string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
			List<SiteResponse> responseData = JsonConvert.DeserializeObject(responseString, typeof(List<SiteResponse>)) as List<SiteResponse>;

			return responseData;
		}
	}
}
