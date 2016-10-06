using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using System.Net;
using System.Text;

namespace GPXTractor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window {
		string[] imagePaths;
		List<string> imageDates = new List<string>();

		public MainWindow() {
			InitializeComponent();
		}

		string openFile(string fileType) {
			OpenFileDialog fileDialog = new OpenFileDialog();
			fileDialog.Filter = fileType;
			fileDialog.ShowDialog();

			if(!string.IsNullOrWhiteSpace(fileDialog.FileName)) {
				return fileDialog.FileName;
			}

			return null;
		}

		string[] openFolder(TextBox textBox) {
			System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
			folderDialog.ShowDialog();

			if(!string.IsNullOrWhiteSpace(folderDialog.SelectedPath)) {
				textBox.Text = folderDialog.SelectedPath;
				return Directory.GetFiles(folderDialog.SelectedPath);
			}

			return null;
		}

		string saveFile(string fileType) {
			SaveFileDialog saveDialog = new SaveFileDialog();
			saveDialog.Filter = fileType;
			saveDialog.ShowDialog();

			if(!string.IsNullOrEmpty(saveDialog.FileName)) {
				return saveDialog.FileName;
			}

			return null;
		}

		private void writeImageExifs(ImageExif[] imageExifs, string path) {
			StreamWriter streamWriter = new StreamWriter(path);
			streamWriter.WriteLine("Image Name,File Path,Lattitude,Longitude,Model,Heading,Field Of View,Photographer");
			foreach(ImageExif imageExif in imageExifs) {
				imageExif.writeToFile(photographerTextBox.Text, streamWriter);
			}
			streamWriter.Close();
		}

		private void submitImageExifs(ImageExif[] imageExifs, string path) {
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("");
			string jsonString = buildRequestJSON(imageExifs);
			byte[] requestData = Encoding.ASCII.GetBytes(jsonString);
			request.Method = "POST";
			request.ContentType = "application/json";
			request.ContentLength = requestData.Length;
			using(Stream stream = request.GetRequestStream()) {
				stream.Write(requestData, 0, requestData.Length);
			}
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
		}

		private string buildRequestJSON(ImageExif[] imageExifs) {
			string jsonString = "[";
			foreach(ImageExif imageExif in imageExifs) {
				jsonString += buildImageJson(imageExif);
				if(imageExif != imageExifs[imageExifs.Length - 1]) {
					jsonString += ",";
				}
			}
			jsonString += "]";

			return jsonString;
		}

		private string buildImageJson(ImageExif imageExif) {
			string jsonString = "{\"name\": \"" + imageExif.name + "\",\"latitude\" : " + imageExif.latitude +",\"longitude\" : " + imageExif.longitude + ",\"date\" : \"" + imageExif.dateTimeTaken + "\",\"cameraModel\" : \"" + imageExif.model + "\",\"fieldOfView\" : " + imageExif.fieldOfView + ",\"heading\" : " + imageExif.heading + ",\"image\": \"" +  Encoding.ASCII.GetString(imageExif.imageData) + "\"}";
			return jsonString;
		}

		private void gpxButton_Click(object sender, RoutedEventArgs e) {
			string imageDirectory = openFile("GPX Files|*.gpx");
			gpxTextBox.Text = imageDirectory;
			imageDirectoryTextBox.Text = Path.GetDirectoryName(imageDirectory);
		}

		private void imageDirectoryButton_Click(object sender, RoutedEventArgs e) {
			imagePaths = openFolder(imageDirectoryTextBox);
		}

		private void outputDirectoryButton_Click(object sender, RoutedEventArgs e) {
			outputDirectoryTextBox.Text = saveFile("CSV Files|*.csv");
		}

		private void gpxCheckBox_Click(object sender, RoutedEventArgs e) {
			bool enabled = ((CheckBox)sender).IsChecked.Value;

			gpxButton.IsEnabled = enabled;
			gpxTextBox.IsEnabled = enabled;
			gpxTextBox.Text = string.Empty;

			dateTimePicker.IsEnabled = enabled;
			dateTimePicker.Text = string.Empty;
		}

		private void generateButton_Click(object sender, RoutedEventArgs e) {
			List<ImageExif> imageExifs = new List<ImageExif>();
			XmlNodeList dataPoints = null;
			bool requiredFieldsEmpty = string.IsNullOrEmpty(imageDirectoryTextBox.Text) && string.IsNullOrEmpty(outputDirectoryTextBox.Text) && string.IsNullOrEmpty(photographerTextBox.Text);
			bool nonRequiredFiledsEmpty =  string.IsNullOrEmpty(gpxTextBox.Text) && string.IsNullOrEmpty(dateTimePicker.Text);

			if((!gpxCheckBox.IsChecked.Value && !nonRequiredFiledsEmpty)  || !requiredFieldsEmpty) {
				if(imagePaths == null) {
					imagePaths = Directory.GetFiles(imageDirectoryTextBox.Text);
				}

				if(gpxCheckBox.IsChecked.Value) {
					XmlDocument gpxfile = new XmlDocument();
					gpxfile.Load(gpxTextBox.Text);
					dataPoints = gpxfile.GetElementsByTagName("trkpt");
				}

				foreach(var imagePath in imagePaths) {
					if(imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png")) {
						ImageExif imageExif = new ImageExif(imagePath, dateTimePicker.Value, dataPoints);
						imageExifs.Add(imageExif);
					}
				}

				writeImageExifs(imageExifs.ToArray(), outputDirectoryTextBox.Text);
				MessageBox.Show("Task complete.");
			} else {
				MessageBox.Show("Please make sure each field has been completed.");
			}
		}
		private void viewImageButton_Click(object sender, RoutedEventArgs e) {
			if(imageDirectoryTextBox.Text != "") {
				string firstImagePath = null;

				if(imagePaths == null) {
					imagePaths = Directory.GetFiles(imageDirectoryTextBox.Text);
				}
				foreach(var imagePath in imagePaths) {
					if(imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png")) {
						firstImagePath = imagePath;
					}
				}
				if(firstImagePath != null) {
					System.Diagnostics.Process.Start(firstImagePath);
					return;
				}
			}
			MessageBox.Show("No image found in the selected directory.");
		}
	}
}
