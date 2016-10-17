﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using System.Net;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;

namespace GPXTractor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window {
		string[] imagePaths;
		List<string> imageDates = new List<string>();
		BackgroundWorker backgroundWorker;
		ProgressDialog progressDialog;
		string currentProcess = string.Empty;
		System.Windows.Shell.TaskbarItemProgressState progressState;

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

		private void writeImageExifs(ImageExif[] imageExifs, string photographer, string path) {
			int imageCount = 0;
			int progress = 0;
			StreamWriter streamWriter = new StreamWriter(path);
			streamWriter.WriteLine("Image Name,File Path,Lattitude,Longitude,Model,Heading,Field Of View,Photographer");
			foreach(ImageExif imageExif in imageExifs) {
				imageExif.writeToFile(photographer, streamWriter);
				progress = Convert.ToInt32(imageCount++ / Convert.ToDouble(imageExifs.Length - 1) * 100);
				backgroundWorker.ReportProgress(progress);
			}
			string test = buildRequestJSON(imageExifs);
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
			List<SubmitImage> submitImages = new List<SubmitImage>();

			foreach(ImageExif imageExif in imageExifs) {
				SubmitImage submitImage = new SubmitImage();
				submitImage.name = imageExif.name;
				submitImage.latitude = imageExif.latitude;
				submitImage.longitude = imageExif.longitude;
				submitImage.date = imageExif.dateTimeTaken;
				submitImage.cameraModel = imageExif.model;
				submitImage.fieldOfView = imageExif.fieldOfView;
				submitImage.heading = imageExif.heading;
				submitImage.imageData = "";//Encoding.ASCII.GetString(imageExif.imageData);
				submitImages.Add(submitImage);
			}

			return JsonConvert.SerializeObject(submitImages.ToArray());
		}

		private void setupBackgroundWorer(List<ImageExif> imageExifs, XmlNodeList dataPoints) {
			List<object> arguments = new List<object>();
			arguments.Add(imageExifs);
			arguments.Add(dataPoints);

			backgroundWorker = new BackgroundWorker();
			backgroundWorker.WorkerReportsProgress = true;
			backgroundWorker.WorkerSupportsCancellation = true;
			backgroundWorker.DoWork += backgroundWorker_DoWork;
			backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
			backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
			backgroundWorker.RunWorkerAsync(arguments);
		}

		private void setupProgressDialog() {
			progressDialog = new ProgressDialog();
			progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			progressDialog.Owner = this;
			progressDialog.ShowDialog();
		}

		#region Button Actions
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
			bool nonRequiredFiledsEmpty = string.IsNullOrEmpty(gpxTextBox.Text) && string.IsNullOrEmpty(dateTimePicker.Text);

			if((!gpxCheckBox.IsChecked.Value && !nonRequiredFiledsEmpty) || !requiredFieldsEmpty) {
				if(imagePaths == null) {
					imagePaths = Directory.GetFiles(imageDirectoryTextBox.Text);
				}

				if(gpxCheckBox.IsChecked.Value) {
					XmlDocument gpxfile = new XmlDocument();
					gpxfile.Load(gpxTextBox.Text);
					dataPoints = gpxfile.GetElementsByTagName("trkpt");
				}

				setupBackgroundWorer(imageExifs, dataPoints);
				setupProgressDialog();
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
		#endregion

		#region Background Worker
		private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
			List<object> args = e.Argument as List<object>;
			List<ImageExif> imageExifs = args[0] as List<ImageExif>;
			XmlNodeList dataPoints = args[1] as XmlNodeList;
			DateTime? offsetDate = null;
			string outputDirectory = null;
			string photographer = null;
			int imageCount = 0;
			int progress = 0;

			Dispatcher.Invoke((() => {
				offsetDate = dateTimePicker.Value;
				outputDirectory = outputDirectoryTextBox.Text;
				photographer = photographerTextBox.Text;
			}));

			currentProcess = "Collecting Image Data";
			backgroundWorker.ReportProgress(0);
			foreach(var imagePath in imagePaths) {
				if(imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png")) {
					ImageExif imageExif = new ImageExif(imagePath, offsetDate, dataPoints);
					imageExifs.Add(imageExif);
					progress = Convert.ToInt32(imageCount++ / Convert.ToDouble(imagePaths.Length - 1) * 100);
					backgroundWorker.ReportProgress(progress);
				}
			}

			currentProcess = "Writing Images";
			backgroundWorker.ReportProgress(0);
			writeImageExifs(imageExifs.ToArray(), photographer, outputDirectory);
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
			progressDialog.closeDialog();
			progressDialog = null;
			backgroundWorker = null;
			GC.Collect();
			MessageBox.Show("Task complete.");
		}

		private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
			progressDialog.setProgress(e.ProgressPercentage, currentProcess, progressState);
		}
		#endregion
	}
}
