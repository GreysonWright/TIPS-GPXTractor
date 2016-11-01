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

namespace GPXTractor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow: Window {
		string[] imagePaths { get; set; }
		//BackgroundWorker backgroundWorker { get; set; }
		ProgressDialog progressDialog { get; set; }
		string currentProcess { get; set; }
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

		//private void writeImageExifs(ImageExif[] imageExifs, string photographer, string path) {
		//	int imageCount = 0;
		//	int progress = 0;
		//	StreamWriter streamWriter = new StreamWriter(path);
		//	streamWriter.WriteLine("Image Name,File Path,Lattitude,Longitude,Model,Heading,Field Of View,Photographer");
		//	foreach (ImageExif imageExif in imageExifs) {
		//		//imageExif.writeToFile(photographer, streamWriter);
		//		progress = Convert.ToInt32(imageCount++ / Convert.ToDouble(imageExifs.Length - 1) * 100);
		//		backgroundWorker.ReportProgress(progress);
		//	}
		//	streamWriter.Close();
		//}

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
						GeodatabaseFeature newFeature = new GeodatabaseFeature(surveyTable.Schema);
						newFeature.Geometry = new Esri.ArcGISRuntime.Geometry.MapPoint(imageExif.longitude, imageExif.latitude);
						IDictionary<string, object> featureAttributes = newFeature.Attributes;
						featureAttributes["SiteId"] = Convert.ToInt16((siteComboBox.SelectedItem as ComboBoxItem).Tag);
						featureAttributes["DateTaken"] = imageExif.dateTimeTaken;
						featureAttributes["Heading"] = imageExif.heading;
						featureAttributes["Source"] = imageExif.model;
						featureAttributes["Photographer"] = photographer;

						long addResult = await surveyTable.AddAsync(newFeature);
						FeatureEditResult editResult = await surveyTable.ApplyEditsAsync(false);
						FileStream fileStream = File.Open(imageExif.path, FileMode.Open);
						AttachmentResult addAttachmentResult = await surveyTable.AddAttachmentAsync(editResult.AddResults[0].ObjectID, fileStream, imageExif.name);
						FeatureAttachmentEditResult editAttachmentResults = await surveyTable.ApplyAttachmentEditsAsync(false);
					} catch (Exception ex) {
						MessageBox.Show($"Error: {ex.Message}");
					}
				}
			} catch (Exception ex) {
				MessageBox.Show($"Error: {ex.Message}");
			}
		}

		//private void setupBackgroundWorker(List<ImageExif> imageExifs, XmlNodeList dataPoints) {
		//	List<object> arguments = new List<object>();
		//	arguments.Add(imageExifs);
		//	arguments.Add(dataPoints);

		//	backgroundWorker = new BackgroundWorker();
		//	backgroundWorker.WorkerReportsProgress = true;
		//	backgroundWorker.WorkerSupportsCancellation = true;
		//	backgroundWorker.DoWork += backgroundWorker_DoWork;
		//	backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
		//	backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
		//	backgroundWorker.RunWorkerAsync(arguments);
		//}

		//private void setupProgressDialog() {
		//	progressDialog = new ProgressDialog();
		//	progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
		//	progressDialog.Owner = this;
		//	progressDialog.ShowDialog();
		//}

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

				//setupBackgroundWorker(imageExifs, dataPoints);
				Dispatcher.Invoke((() => {
					progressDialog = new ProgressDialog();
					progressDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
					progressDialog.Owner = this;
					stuff(imageExifs, dataPoints);
					progressDialog.ShowDialog();
				}));

				progressDialog.closeDialog();
				progressDialog = null;
				GC.Collect();
				MessageBox.Show("Task complete.");
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

		private async void stuff(List<ImageExif> imageExifs, XmlNodeList dataPoints) {
			//List<object> args = e.Argument as List<object>;
			//List<ImageExif> imageExifs = args[0] as List<ImageExif>;
			//XmlNodeList dataPoints = args[1] as XmlNodeList;
			DateTime? offsetDate = null;
			string photographer = null;
			int imageCount = 0;
			int progress = 0;

			Dispatcher.Invoke((() => {
				offsetDate = dateTimePicker.Value;
				photographer = photographerTextBox.Text;
			}));

			currentProcess = "Collecting Image Data";
			//progressDialog.setProgress(0, currentProcess, progressState);
			//backgroundWorker.ReportProgress(0);
			foreach (var imagePath in imagePaths) {
				if (imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png")) {
					ImageExif imageExif = new ImageExif(imagePath, offsetDate, dataPoints);
					imageExifs.Add(imageExif);
					progress = Convert.ToInt32(imageCount++ / Convert.ToDouble(imagePaths.Length - 1) * 100);
					//progressDialog.setProgress(progress, currentProcess, progressState);
					//backgroundWorker.ReportProgress(progress);
				}
			}

			currentProcess = "Writing Images";
			//progressDialog.setProgress(0, currentProcess, progressState);
			//backgroundWorker.ReportProgress(0);
			//writeImageExifs(imageExifs.ToArray(), photographer, outputDirectory);
			await submitImageExifs(imageExifs.ToArray(), photographer);
		}

		#region Background Worker
		private async void backgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
			List<object> args = e.Argument as List<object>;
			List<ImageExif> imageExifs = args[0] as List<ImageExif>;
			XmlNodeList dataPoints = args[1] as XmlNodeList;
			DateTime? offsetDate = null;
			string photographer = null;
			int imageCount = 0;
			int progress = 0;

			Dispatcher.Invoke((() => {
				offsetDate = dateTimePicker.Value;
				photographer = photographerTextBox.Text;
			}));

			currentProcess = "Collecting Image Data";
			//backgroundWorker.ReportProgress(0);
			//progressDialog.setProgress(0, currentProcess, progressState);
			foreach (var imagePath in imagePaths) {
				if (imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png")) {
					ImageExif imageExif = new ImageExif(imagePath, offsetDate, dataPoints);
					imageExifs.Add(imageExif);
					progress = Convert.ToInt32(imageCount++ / Convert.ToDouble(imagePaths.Length - 1) * 100);
					//progressDialog.setProgress(0, currentProcess, progressState);
					//backgroundWorker.ReportProgress(progress);
				}
			}

			currentProcess = "Writing Images";
			//progressDialog.setProgress(0, currentProcess, progressState);
			//backgroundWorker.ReportProgress(0);
			//writeImageExifs(imageExifs.ToArray(), photographer, outputDirectory);
			await submitImageExifs(imageExifs.ToArray(), photographer);
		}

		//private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
		//	progressDialog.closeDialog();
		//	progressDialog = null;
		//	//backgroundWorker = null;
		//	GC.Collect();
		//	MessageBox.Show("Task complete.");
		//}

		//private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
		//	progressDialog.setProgress(e.ProgressPercentage, currentProcess, progressState);
		//}
		#endregion
	}
}
