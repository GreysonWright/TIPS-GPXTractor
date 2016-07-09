using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Xml;
using Microsoft.Win32;

namespace GPXTractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] imagePaths;
        List<string> imageDates = new List<string>();

        public MainWindow()
        { 
            InitializeComponent();
        }

        string openFile(string fileType)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = fileType;
            fileDialog.ShowDialog();

            if (!string.IsNullOrWhiteSpace(fileDialog.FileName))
            {
                return fileDialog.FileName;
            }

            return null;
        }

        string[] openFolder(TextBox textBox)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowDialog();

            if (!string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                textBox.Text = folderDialog.SelectedPath;
                return Directory.GetFiles(folderDialog.SelectedPath);
            }

            return null;
        }

        string saveFile(string fileType)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = fileType;
            saveDialog.ShowDialog();

            if (!string.IsNullOrEmpty(saveDialog.FileName))
            {
                return saveDialog.FileName;
            }

            return null;
        }

        private void writeImageExifs(ImageExif[] imageExifs, string path)
        {
            StreamWriter streamWriter = new StreamWriter(path);
            foreach (ImageExif imageExif in imageExifs)
            {
                imageExif.writeToFile(photographerTextBox.Text, streamWriter);
            }
            streamWriter.Close();
        }

        private void gpxButton_Click(object sender, RoutedEventArgs e)
        {
            gpxTextBox.Text = openFile("GPX Files|*.gpx");
        }

        private void imageDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            imagePaths = openFolder(imageDirectoryTextBox);
        }

        private void outputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            outputDirectoryTextBox.Text = saveFile("CSV Files|*.csv");
        }

        private void generateButton_Click(object sender, RoutedEventArgs e)
        {
            List<ImageExif> imageExifs = new List<ImageExif>();

            if (gpxTextBox.Text != "" && imageDirectoryTextBox.Text != "" && outputDirectoryTextBox.Text != "" && photographerTextBox.Text != "")
            {
                XmlDocument gpxfile = new XmlDocument();
                gpxfile.Load(gpxTextBox.Text);
                XmlNodeList dataPoints = gpxfile.GetElementsByTagName("trkpt");

                if (imagePaths == null)
                {
                    Directory.GetFiles(imageDirectoryTextBox.Text);
                }

                foreach (var imagePath in imagePaths)
                {
                    if (imagePath.Contains(".jpg") || imagePath.Contains(".JPG") || imagePath.Contains(".png"))
                    {
                        ImageExif imageExif = new ImageExif(imagePath, dataPoints);
                        imageExifs.Add(imageExif);
                    }
                }

                writeImageExifs(imageExifs.ToArray(), outputDirectoryTextBox.Text);
                MessageBox.Show("Done.");
            }
            else
            {
                MessageBox.Show("Please make sure each field has been completed.");
            }
        }
    }
}
