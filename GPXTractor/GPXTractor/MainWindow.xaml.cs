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
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	partial class MainWindow: Window {
		string[] imagePaths { get; set; }
		ProgressDialog progressDialog { get; set; }
		System.Windows.Shell.TaskbarItemProgressState progressState { get; set; }

		public MainWindow() {
			InitializeComponent();

			setupComboBox();
		}

		private void setupComboBox() {
			Dispatcher.InvokeAsync((() => {
				List<SiteResponse> sites = getSites();
				foreach (SiteResponse site in sites) {
					ComboBoxItem boxItem = new ComboBoxItem();
					boxItem.Content = boxItem.Content = site.name;
					boxItem.Tag = site.id;
					siteComboBox.Items.Add(boxItem);
				}
			}));
		}

		private void gpxMode(bool enabled) {
			gpxButton.IsEnabled = enabled;
			gpxTextBox.IsEnabled = enabled;
			gpxTextBox.Text = string.Empty;

			gpsPhotoButton.IsEnabled = enabled;
			gpsPhotoTextBox.IsEnabled = enabled;
			gpsPhotoTextBox.Text = string.Empty;

			gpsTimeTextBox.IsEnabled = enabled;
			viewImageButton.IsEnabled = enabled;
			gpsTimeTextBox.Text = string.Empty;
		}

		private string[] getImagePaths(string directory) {
			try {
				imagePaths = Directory.GetFiles(imageDirectoryTextBox.Text);
			} catch {
				MessageBox.Show("Incorrect image directory file path specified.");
				return null;
			}
			return imagePaths;
		}

		private XmlNodeList getGPXData() {
			XmlNodeList dataPoints = null;
			try {
				XmlDocument gpxfile = new XmlDocument();
				gpxfile.Load(gpxTextBox.Text);
				dataPoints = gpxfile.GetElementsByTagName("trkpt");
			} catch {
				MessageBox.Show("Incorrect gpx file path specified.");
			}
			return dataPoints;
		}

		private void setupProgressDialog() {
			progressDialog = new ProgressDialog();
			progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			progressDialog.Owner = this;
			progressDialog.progressBar.IsIndeterminate = true;
			progressDialog.ShowDialog();
		}

		private async void processImages(List<ImageExif> imageExifs, XmlNodeList dataPoints, TimeSpan timeDifference) {
			string photographer = null;
			string gpsPhoto = null;

			Dispatcher.Invoke(() => {
				photographer = photographerTextBox.Text;
				gpsPhoto = gpsPhotoTextBox.Text;
			});

			foreach (var imagePath in imagePaths) {
				bool isImage = (imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png"));
				if (imagePath != gpsPhoto && isImage) {
					ImageExif imageExif = new ImageExif(imagePath, timeDifference, dataPoints);
					imageExifs.Add(imageExif);
				}
			}

			await submitImageExifs(imageExifs.ToArray(), photographer);
			imageProcessComplete();
		}

		private void imageProcessComplete() {
			progressDialog.closeDialog();
			progressDialog = null;
			GC.Collect();
			MessageBox.Show("Task complete.");
		}
	}
}
