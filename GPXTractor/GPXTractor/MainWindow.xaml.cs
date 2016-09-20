using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Xml;
using Microsoft.Win32;

namespace GPXTractor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow :Window {
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

		private void generateButton_Click(object sender, RoutedEventArgs e) {
			List<ImageExif> imageExifs = new List<ImageExif>();

			if(gpxTextBox.Text != "" && imageDirectoryTextBox.Text != "" && outputDirectoryTextBox.Text != "" && photographerTextBox.Text != "") {
				XmlDocument gpxfile = new XmlDocument();
				gpxfile.Load(gpxTextBox.Text);
				XmlNodeList dataPoints = gpxfile.GetElementsByTagName("trkpt");

				if(imagePaths == null) {
					imagePaths = Directory.GetFiles(imageDirectoryTextBox.Text);
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
