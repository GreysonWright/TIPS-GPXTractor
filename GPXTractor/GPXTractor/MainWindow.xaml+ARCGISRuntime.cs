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
	public partial class MainWindow {
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

						long addResult = await surveyTable.AddAsync(newFeature);
						FeatureEditResult editResult = await surveyTable.ApplyEditsAsync(false);
						FileStream fileStream = File.Open(imageExif.path, FileMode.Open);
						AttachmentResult addAttachmentResult = await surveyTable.AddAttachmentAsync(editResult.AddResults[0].ObjectID, fileStream, imageExif.name);
						FeatureAttachmentEditResult editAttachmentResults = await surveyTable.ApplyAttachmentEditsAsync(false);
					} catch (Exception ex) {
						MessageBox.Show($"Error: {ex.Message}");
						Environment.Exit(1);
					}
				}
			} catch (Exception ex) {
				MessageBox.Show($"Error: {ex.Message}");
				Environment.Exit(1);
			}
		}
	}
}
