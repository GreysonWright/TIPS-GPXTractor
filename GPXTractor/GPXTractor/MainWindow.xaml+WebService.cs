using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using System.Net;
using Newtonsoft.Json;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Tasks.Query;
using Esri.ArcGISRuntime.Tasks.Edit;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
