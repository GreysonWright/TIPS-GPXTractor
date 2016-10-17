using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPXTractor {
	class SubmitImage {
		public string name { get; set; }
		public double latitude { get; set; }
		public double longitude { get; set; }
		public DateTime date { get; set; }
		public string cameraModel { get; set; }
		public double fieldOfView { get; set; }
		public double heading { get; set; }
		public string imageData { get; set; }
	}
}
