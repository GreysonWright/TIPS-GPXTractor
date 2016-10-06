﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Xml;

//image property stuff
//https://msdn.microsoft.com/en-us/library/windows/desktop/ms534418(v=vs.85).aspx
//https://msdn.microsoft.com/en-us/library/system.drawing.imaging.propertyitem.id(v=vs.110).aspx
//https://msdn.microsoft.com/en-us/library/windows/desktop/ms534414(v=vs.85).aspx

namespace GPXTractor {
	class ImageExif {
		public string name { get; private set; }
		public string path { get; private set; }
		public double latitude { get; private set; }
		public double longitude { get; private set; }
		public DateTime dateTimeTaken { get; private set; }
		public string model { get; private set; }
		public double fieldOfView { get; private set; }
		public double heading { get; private set; }
		public byte[] imageData { get; private set; }
		private bool gpsDidTimeOut;

		public ImageExif(string imagePath, DateTime? offsetDateTime, XmlNodeList gpxData) {
			setupImageExif(imagePath, offsetDateTime, gpxData);
		}

		public ImageExif(string imagePath, DateTime? offsetDateTime) {
			setupImageExif(imagePath, offsetDateTime, null);
		}

		private void setupImageExif(string imagePath, DateTime? offsetDateTime, XmlNodeList gpxData) {
			PropertyItem dateProperty = null;
			PropertyItem cameraModel = null;
			PropertyItem latitudeProperty = null;
			PropertyItem latitudeReferenceProperty = null;
			PropertyItem longitudeProperty = null;
			PropertyItem longitudeReferenecProperty = null;
			PropertyItem headingProperty = null;

			FileInfo imageInfo = new FileInfo(imagePath);
			using(FileStream imageStream = imageInfo.OpenRead()) {
				Image image = Image.FromStream(imageStream, false, false);

				try {
					latitudeProperty = image.GetPropertyItem(0x0002);
					latitudeReferenceProperty = image.GetPropertyItem(0x0001);
					longitudeProperty = image.GetPropertyItem(0x0004);
					longitudeReferenecProperty = image.GetPropertyItem(0x0003);
					dateProperty = image.GetPropertyItem(0x0132);
					cameraModel = image.GetPropertyItem(0x0110);
					headingProperty = image.GetPropertyItem(0x0011);
				} catch {
					Console.WriteLine("Property was null.");
				}

				imageData = new byte[imageInfo.Length];
				imageStream.Read(imageData, 0, imageData.Length);
			}

			name = imageInfo.Name;
			path = imagePath;
			if(cameraModel != null) {
				model = Encoding.UTF8.GetString(cameraModel.Value);
				fieldOfView = model.ToLower().Contains("iphone") ? 63.7 : 67.1;
			}
			if(gpxData == null) {
				if(latitudeProperty != null && latitudeReferenceProperty != null) {
					latitude = buildLatLong(latitudeProperty, latitudeReferenceProperty);
				}
				if(longitudeProperty != null && longitudeReferenecProperty != null) {
					longitude = buildLatLong(longitudeProperty, longitudeReferenecProperty);
				}
				if(headingProperty != null) {
					heading = getHeading(headingProperty);
				}
			} else {
				string takenTime = Encoding.UTF8.GetString(dateProperty.Value);
				takenTime = takenTime.Remove(takenTime.Length - 1);

				DateTime imageDateTime = DateTime.ParseExact(takenTime, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
				dateTimeTaken = correctImageDateTime(imageDateTime, offsetDateTime);

				XmlNode gpxNode = getImageDetails(imageDateTime, gpxData);
				latitude = Convert.ToDouble(gpxNode.Attributes.Item(0).Value);
				longitude = Convert.ToDouble(gpxNode.Attributes.Item(1).Value);
				heading = Convert.ToDouble(gpxNode.ChildNodes.Item(3).ChildNodes.Item(2).InnerText);
			}
		}
		
		private double getHeading(PropertyItem heading) {
			double numerator = BitConverter.ToUInt32(heading.Value, 0);
			double denominator = BitConverter.ToUInt32(heading.Value, 4);
			double headingDouble = numerator / denominator;

			return headingDouble;
		}

		private double buildLatLong(PropertyItem latLong, PropertyItem latLongRef) {
			double degreesNumerator = BitConverter.ToUInt32(latLong.Value, 0);
			double degreesDenominator = BitConverter.ToUInt32(latLong.Value, 4);
			double minutesNumerator = BitConverter.ToUInt32(latLong.Value, 8);
			double minutesDenominator = BitConverter.ToUInt32(latLong.Value, 12);
			double secondsNumerator = BitConverter.ToUInt32(latLong.Value, 16);
			double secondsDenominator = BitConverter.ToUInt32(latLong.Value, 20);
			string signString = Encoding.ASCII.GetString(latLongRef.Value);
			
			double sign = signString == "E\0" || signString == "N\0" ? 1 : -1 ;
			double degrees = degreesNumerator / degreesDenominator;
			double minutes = minutesNumerator / minutesDenominator;
			double seconds = secondsNumerator / secondsDenominator;
			
			double decimalDegrees = sign * (degrees + minutes / 60d + seconds / 3600d);
			return decimalDegrees;
		}

		private DateTime correctImageDateTime(DateTime timeTaken, DateTime? offsetDateTime) {
			DateTime timeDifference = timeTaken.Subtract(offsetDateTime.Value.TimeOfDay);
			return timeDifference.Subtract(timeDifference.TimeOfDay);
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

			gpsDidTimeOut = minDifference > TimeSpan.FromMinutes(1).Ticks;

			return index;
		}

		public void writeToFile(string photographer, StreamWriter streamWriter) {
			streamWriter.Write(name + "," + path + "," + latitude + "," + longitude + "," + model + "," + heading + "," + fieldOfView + "," + photographer);
			if(gpsDidTimeOut) {
				streamWriter.Write(",Potential GPS Error");
			}
			streamWriter.Write("\r\n");
		}
	}
}
