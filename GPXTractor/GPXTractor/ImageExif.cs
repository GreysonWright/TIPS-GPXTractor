﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Xml;

namespace GPXTractor {
	class ImageExif {
		public string name;
		public string path;
		public string lattitude;
		public string longitude;
		public string dateTimeTaken;
		public string model;
		public string fieldOfView;
		public string heading;
		public bool gpsDidTimeOut;

		public ImageExif(string imagePath, DateTime? offsetDateTime, XmlNodeList gpxData) {
			setupImageExif(imagePath, offsetDateTime, gpxData);
		}

		public ImageExif(string imagePath, DateTime? offsetDateTime) {
			setupImageExif(imagePath, offsetDateTime, null);
		}

		private void setupImageExif(string imagePath, DateTime? offsetDateTime, XmlNodeList gpxData) {
			FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
			Image image = Image.FromStream(imageStream, false, false);
			PropertyItem dateProperty = image.GetPropertyItem(0x0132);
			PropertyItem cameraModel = image.GetPropertyItem(0x0110);
			PropertyItem latitudeProperty = image.GetPropertyItem(0x0002);
			PropertyItem longitudeProperty = image.GetPropertyItem(0x0004);
			PropertyItem headingProperty = null;
			try {
				headingProperty = image.GetPropertyItem(0x0011);
			} catch (Exception e) {
				
			}
			imageStream.Close();
			
			name = Path.GetFileName(imagePath);
			path = imagePath;
			model = Encoding.UTF8.GetString(cameraModel.Value);
			fieldOfView = model.ToLower().Contains("iphone") ? "63.7" : "67.1";
			if(gpxData == null) {
				lattitude = buildLatLong(latitudeProperty);
				longitude = buildLatLong(longitudeProperty);
				if(headingProperty != null) {
					heading = getHeading(headingProperty);
				}
			} else {
				string takenTime = Encoding.UTF8.GetString(dateProperty.Value);
				takenTime = takenTime.Remove(takenTime.Length - 1);
				DateTime imageDateTime = DateTime.ParseExact(takenTime, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
				dateTimeTaken = correctImageDateTime(imageDateTime, offsetDateTime);
				XmlNode gpxNode = getImageDetails(imageDateTime, gpxData);
				lattitude = gpxNode.Attributes.Item(0).Value;
				longitude = gpxNode.Attributes.Item(1).Value;
				heading = gpxNode.ChildNodes.Item(3).ChildNodes.Item(2).InnerText;
			}
		}
		
		private string getHeading(PropertyItem heading) {
			double numerator = BitConverter.ToUInt32(heading.Value, 0);
			double denominator = BitConverter.ToUInt32(heading.Value, 4);
			double headingDouble = numerator / denominator;

			return headingDouble.ToString();
		}

		private string buildLatLong(PropertyItem latLong) {
			double degreesNumerator = BitConverter.ToUInt32(latLong.Value, 0);
			double degreesDenominator = BitConverter.ToUInt32(latLong.Value, 4);
			double minutesNumerator = BitConverter.ToUInt32(latLong.Value, 8);
			double minutesDenominator = BitConverter.ToUInt32(latLong.Value, 12);
			double secondsNumerator = BitConverter.ToUInt32(latLong.Value, 16);
			double secondsDenominator = BitConverter.ToUInt32(latLong.Value, 20);

			double degrees = degreesNumerator / degreesDenominator;
			double minutes = minutesNumerator / minutesDenominator;
			double seconds = secondsNumerator / secondsDenominator;
			double decimalDegrees = degrees + minutes / 60d + seconds / 3600d;

			string latLongString = decimalDegrees.ToString();
			return latLongString;
		}

		private string correctImageDateTime(DateTime timeTaken, DateTime? offsetDateTime) {
			DateTime timeDifference = timeTaken.Subtract(offsetDateTime.Value.TimeOfDay);
			return timeDifference.Subtract(timeDifference.TimeOfDay).ToString();
		}

		private XmlNode getImageDetails(DateTime dateTime, XmlNodeList gpxData) {
			int index = getGPXPosition(dateTime, gpxData);
			return gpxData[index];
		}

		private int getGPXPosition(DateTime currentDateTime, XmlNodeList gpxData) {
			long minDifference = long.MaxValue;
			int index = 0;

			for(int i = 0; i < gpxData.Count; i++) {
				DateTime gpxDate = Convert.ToDateTime(gpxData.Item(i).ChildNodes.Item(1).InnerText);
				long difference = Math.Abs(gpxDate.TimeOfDay.Ticks - currentDateTime.TimeOfDay.Ticks);
				if(minDifference > difference) {
					minDifference = difference;
					index = i;
				}
			}

			if(minDifference > TimeSpan.FromMinutes(1).Ticks) {
				gpsDidTimeOut = true;
			}

			return index;
		}

		public void writeToFile(string photographer, StreamWriter streamWriter) {
			streamWriter.Write(name + "," + path + "," + lattitude + "," + longitude + "," + model + "," + heading + "," + fieldOfView + "," + photographer);
			if(gpsDidTimeOut) {
				streamWriter.Write(",Potential GPS Error");
			}
			streamWriter.Write("\r\n");
		}
	}
}
