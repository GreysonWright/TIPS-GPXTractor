using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Xml;

namespace GPXTractor {
    class ImageExif {
        private DateTime imageDateTime;
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
            FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            Image image = Image.FromStream(imageStream, false, false);
            PropertyItem dateProperty = image.GetPropertyItem(36867);
            PropertyItem cameraModel = image.GetPropertyItem(0x0110);
            imageStream.Close();

            string takenTime = Encoding.UTF8.GetString(dateProperty.Value);
            takenTime = takenTime.Remove(takenTime.Length - 1);
            imageDateTime = DateTime.ParseExact(takenTime, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            name = Path.GetFileName(imagePath);
            path = imagePath;
            dateTimeTaken = correctImageDateTime(imageDateTime, offsetDateTime);
            model = Encoding.UTF8.GetString(cameraModel.Value);
            XmlNode gpxNode = getImageDetails(dateTimeTaken, gpxData);
            lattitude = gpxNode.Attributes.Item(0).Value;
            longitude = gpxNode.Attributes.Item(1).Value;
            heading = gpxNode.ChildNodes.Item(3).ChildNodes.Item(2).InnerText;
            fieldOfView = model.ToLower().Contains("iphone") ? "63.7" : "67.1";
        }

        public void writeToFile(string photographer, StreamWriter streamWriter) {
            streamWriter.Write(name + "," + path + "," + lattitude + "," + longitude + "," + model + "," + heading + "," + fieldOfView + "," + photographer);
            if(gpsDidTimeOut) {
                streamWriter.Write(",Potential GPS Error");
            }
            streamWriter.Write("\r\n");
        }

        private string correctImageDateTime(DateTime timeTaken, DateTime? offsetDateTime) {
            DateTime timeDifference = timeTaken.Subtract(offsetDateTime.Value.TimeOfDay);
            return timeDifference.Subtract(timeDifference.TimeOfDay).ToString();
        }

        private XmlNode getImageDetails(string dateString, XmlNodeList gpxData) {
            int index = getGPXPosition(imageDateTime, gpxData);
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
    }
}
