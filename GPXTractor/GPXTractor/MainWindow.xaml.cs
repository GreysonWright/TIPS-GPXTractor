using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Xml;
using Microsoft.Win32;
using System.Net;
using System.ComponentModel;
using Newtonsoft.Json;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Tasks.Query;
using Esri.ArcGISRuntime.Tasks.Edit;
using System.Threading.Tasks;
using System.Threading;

namespace GPXTractor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window {
		string[] imagePaths { get; set; }
		ProgressDialog progressDialog { get; set; }
		System.Windows.Shell.TaskbarItemProgressState progressState { get; set; }

		public MainWindow() {
			InitializeComponent();

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

		string openFile(string fileType) {
			OpenFileDialog fileDialog = new OpenFileDialog();
			fileDialog.Filter = fileType;
			fileDialog.ShowDialog();

			if (!string.IsNullOrWhiteSpace(fileDialog.FileName)) {
				return fileDialog.FileName;
			}

			return null;
		}

		string[] openFolder(TextBox textBox) {
			System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
			folderDialog.ShowDialog();

			if (!string.IsNullOrWhiteSpace(folderDialog.SelectedPath)) {
				textBox.Text = folderDialog.SelectedPath;
				return Directory.GetFiles(folderDialog.SelectedPath);
			}

			return null;
		}

		string saveFile(string fileType) {
			SaveFileDialog saveDialog = new SaveFileDialog();
			saveDialog.Filter = fileType;
			saveDialog.ShowDialog();

			if (!string.IsNullOrEmpty(saveDialog.FileName)) {
				return saveDialog.FileName;
			}

			return null;
		}

		private List<SiteResponse> getSites() {
			HttpWebRequest request = WebRequest.Create(@"http://weatherevent.caps.ua.edu/api/sites") as HttpWebRequest;
			request.Method = "GET";
			request.ContentType = "application/json";

			HttpWebResponse response = request.GetResponse() as HttpWebResponse;
			string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
			List<SiteResponse> responseData = JsonConvert.DeserializeObject(responseString, typeof(List<SiteResponse>)) as List<SiteResponse>;

			return responseData;
		}

		private async Task submitImageExifs(ImageExif[] imageExifs, string photographer) {
			string featureURL = @"http://esri10.caps.ua.edu:6080/arcgis/rest/services/ExtremeEvents/Features/FeatureServer/0";

			ServiceFeatureTable surveyTable = new ServiceFeatureTable() {
				ServiceUri = featureURL,
				OutFields = new OutFields() { "*" }
			};
			await surveyTable.InitializeAsync();

			try {
				foreach (ImageExif imageExif in imageExifs) {
					try {
						short siteId = 0;
						Dispatcher.Invoke(() => {
							siteId = Convert.ToInt16((siteComboBox.SelectedItem as ComboBoxItem).Tag);
						});

						GeodatabaseFeature newFeature = new GeodatabaseFeature(surveyTable.Schema);
						newFeature.Geometry = new Esri.ArcGISRuntime.Geometry.MapPoint(imageExif.longitude, imageExif.latitude);
						IDictionary<string, object> featureAttributes = newFeature.Attributes;
						featureAttributes["SiteId"] = siteId;
						featureAttributes["DateTaken"] = imageExif.dateTimeTaken;
						featureAttributes["Heading"] = imageExif.heading;
						featureAttributes["Source"] = imageExif.model;
						featureAttributes["Photographer"] = photographer;

						//long addResult = await surveyTable.AddAsync(newFeature);
						//FeatureEditResult editResult = await surveyTable.ApplyEditsAsync(false);
						//FileStream fileStream = File.Open(imageExif.path, FileMode.Open);
						//AttachmentResult addAttachmentResult = await surveyTable.AddAttachmentAsync(editResult.AddResults[0].ObjectID, fileStream, imageExif.name);
						//FeatureAttachmentEditResult editAttachmentResults = await surveyTable.ApplyAttachmentEditsAsync(false);
					} catch (Exception ex) {
						MessageBox.Show($"Error: {ex.Message}");
					}
				}
			} catch (Exception ex) {
				MessageBox.Show($"Error: {ex.Message}");
			}
		}
		
		private async void processImages(List<ImageExif> imageExifs, XmlNodeList dataPoints) {
			DateTime? offsetDate = null; //dateTimePicker.Value;
			string photographer = null;// photographerTextBox.Text;


			Dispatcher.Invoke(() => {
				offsetDate = dateTimePicker.Value;
				photographer = photographerTextBox.Text;
			});

			foreach (var imagePath in imagePaths) {
				if (imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png")) {
					ImageExif imageExif = new ImageExif(imagePath, offsetDate, dataPoints);
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

		private void setupProgressDialog() {
			progressDialog = new ProgressDialog();
			progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			progressDialog.Owner = this;
			progressDialog.progressBar.IsIndeterminate = true;
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
			bool requiredFieldsEmpty = string.IsNullOrEmpty(imageDirectoryTextBox.Text) || string.IsNullOrEmpty(photographerTextBox.Text) || siteComboBox.SelectedItem == null;
			bool nonRequiredFiledsEmpty = string.IsNullOrEmpty(gpxTextBox.Text) && string.IsNullOrEmpty(dateTimePicker.Text);

			if ((!gpxCheckBox.IsChecked.Value && !nonRequiredFiledsEmpty) || !requiredFieldsEmpty) {
				if (imagePaths == null) {
					imagePaths = Directory.GetFiles(imageDirectoryTextBox.Text);
				}

				if (gpxCheckBox.IsChecked.Value) {
					XmlDocument gpxfile = new XmlDocument();
					gpxfile.Load(gpxTextBox.Text);
					dataPoints = gpxfile.GetElementsByTagName("trkpt");
				}

				Task.Run(() => {
					Dispatcher.Invoke(() => {
						setupProgressDialog();
					});
				});

				Task.Run(() => {
						processImages(imageExifs, dataPoints);
				});


			} else {
				MessageBox.Show("Please make sure each field has been completed.");
			}
		}
		private void viewImageButton_Click(object sender, RoutedEventArgs e) {
			if (imageDirectoryTextBox.Text != "") {
				string firstImagePath = null;

				if (imagePaths == null) {
					imagePaths = Directory.GetFiles(imageDirectoryTextBox.Text);
				}
				foreach (var imagePath in imagePaths) {
					if (imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png")) {
						firstImagePath = imagePath;
					}
				}
				if (firstImagePath != null) {
					System.Diagnostics.Process.Start(firstImagePath);
					return;
				}
			}
			MessageBox.Show("No image found in the selected directory.");
		}
		#endregion
	}
}
