using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace GPXTractor
{
    class ImageExif
    {
        public string name;
        public string path;
        public string lattitude;
        public string longitude;
        public string dateTimeTaken;
        public string model;
        public string fieldOfView;
        public string heading;

        public ImageExif(string imagePath, XmlNodeList gpxData)
        {
            FileStream imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            Image image = Image.FromStream(imageStream, false, false);
            PropertyItem dateProperty = image.GetPropertyItem(36867);
            PropertyItem cameraModel = image.GetPropertyItem(0x0110);
            imageStream.Close();

            name = Path.GetFileName(imagePath);
            path = imagePath;
            dateTimeTaken = Encoding.UTF8.GetString(dateProperty.Value);
            model = Encoding.UTF8.GetString(cameraModel.Value);
            XmlNode gpxNode = getImageDetails(dateTimeTaken, gpxData);
            lattitude = gpxNode.Attributes.Item(0).Value;
            longitude = gpxNode.Attributes.Item(1).Value;
            heading = gpxNode.ChildNodes.Item(3).ChildNodes.Item(2).InnerText;
        }

        public void writeToFile(string photographer, StreamWriter streamWriter)
        {
            streamWriter.WriteLine(name + "," + path + "," + lattitude + "," + longitude +  "," + model + "," + heading + "," + photographer); //Dont forget to add field of view later
        }

        private XmlNode getImageDetails(string dateString, XmlNodeList gpxData)
        {
            dateString = dateString.Remove(dateString.Length - 1);
            DateTime date = DateTime.ParseExact(dateString, "yyyy:MM:dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            int index = getGPXPosition(date, gpxData);
            return gpxData[index];
        }

        private int getGPXPosition(DateTime date, XmlNodeList gpxData)
        {
            long minDifference = long.MaxValue;
            int index = 0;

            for (int i = 0; i < gpxData.Count; i++)
            {
                DateTime gpxDate = Convert.ToDateTime(gpxData.Item(i).ChildNodes.Item(1).InnerText);
                long difference = Math.Abs(gpxDate.TimeOfDay.Ticks - date.TimeOfDay.Ticks);
                if (minDifference > difference)
                {
                    minDifference = difference;
                    index = i;
                }
            }

            return index;
        }
    }
}
