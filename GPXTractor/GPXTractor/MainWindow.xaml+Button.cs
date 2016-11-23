using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Threading.Tasks;

namespace GPXTractor {
	partial class MainWindow {
		private TimeSpan? getGPSTimeDifference(string gpsImagePath, string gpsTimeString) {
			DateTime gpsTime;
			TimeSpan? timeDifference = null;

			if (DateTime.TryParseExact(gpsTimeString, "hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out gpsTime)) {
				try {
					ImageExif gpsImage = new ImageExif(gpsImagePath, new TimeSpan(0));
					timeDifference = calculateTimeOffset(gpsImage.dateTimeTaken, gpsTime);
					return timeDifference;
				} catch {
					MessageBox.Show("The GPS image at the specified path does not exist.");
					return timeDifference;
				}
			} else {
				MessageBox.Show("Please enter a date with format \"hh:mm:ss tt.\"");
				return timeDifference;
			}
		}

		private TimeSpan calculateTimeOffset(DateTime timeTaken, DateTime? offsetDateTime) {
			if (TimeZone.CurrentTimeZone.IsDaylightSavingTime(timeTaken)) {
				TimeSpan hour = new TimeSpan(1, 0, 0);
				offsetDateTime = offsetDateTime.Value.Subtract(hour);
			}
			TimeSpan timeDifference = timeTaken.TimeOfDay.Subtract(offsetDateTime.Value.TimeOfDay);
			return timeDifference;
		}

		private void gpxButton_Click(object sender, RoutedEventArgs e) {
			string imageDirectory = openFile("GPX Files|*.gpx");
			gpxTextBox.Text = imageDirectory;
		}

		private void imageDirectoryButton_Click(object sender, RoutedEventArgs e) {
			imagePaths = openFolder(imageDirectoryTextBox);
		}

		private void gpxCheckBox_Click(object sender, RoutedEventArgs e) {
			bool checkboxEnabled = ((CheckBox)sender).IsChecked.Value;
			gpxMode(checkboxEnabled);
		}

		private void gpsPhotoButton_Click(object sender, RoutedEventArgs e) {
			string gpsImage = openFile("Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*");
			gpsPhotoTextBox.Text = gpsImage;
		}

		private void viewImageButton_Click(object sender, RoutedEventArgs e) {
			if (!string.IsNullOrEmpty(gpsPhotoTextBox.Text)) {
				System.Diagnostics.Process.Start(gpsPhotoTextBox.Text);
				return;
			}
			MessageBox.Show("No image found in the selected directory.");
		}
		
		private void submitButton_Click(object sender, RoutedEventArgs e) {
			List<ImageExif> imageExifs = new List<ImageExif>();
			XmlNodeList dataPoints = null;
			TimeSpan? timeDifference = null;
			string gpsTimeString = gpsTimeTextBox.Text;
			string gpsImagePath = gpsPhotoTextBox.Text;
			bool requiredFieldsEmpty = string.IsNullOrEmpty(imageDirectoryTextBox.Text) || string.IsNullOrEmpty(photographerTextBox.Text) || siteComboBox.SelectedItem == null;
			bool nonRequiredFiledsEmpty = string.IsNullOrEmpty(gpxTextBox.Text) && string.IsNullOrEmpty(gpsTimeTextBox.Text);
			bool usingGpx = gpxCheckBox.IsChecked.Value;

			if ((gpxCheckBox.IsChecked.Value && nonRequiredFiledsEmpty) || requiredFieldsEmpty) {
				MessageBox.Show("Please make sure each field has been completed.");
				return;
			}

			if ((imagePaths = getImagePaths(imageDirectoryTextBox.Text)) == null) {
				return;
			}

			if (usingGpx && (dataPoints = getGPXData()) == null) {
				return;
			}

			DateTime? gpsTime;
			if (usingGpx && (timeDifference = getGPSTimeDifference(gpsImagePath, gpsTimeString)) == null) {
				return;
			}

			Task.Run(() => {
				Dispatcher.Invoke(() => {
					setupProgressDialog();
				});
			});

			Task.Run(() => {
				if (timeDifference == null) {
					timeDifference = new TimeSpan(0);
				}

				processImages(imageExifs, dataPoints, timeDifference.Value);
			});
		}
	}
}

