using System.Windows;
using System.Windows.Shell;
using System;

namespace GPXTractor
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    /// 

    public delegate void cancelWork(object sender, EventArgs e);

    public partial class ProgressDialog : Window
    {

        public event cancelWork cancelWork; 
        bool shouldClose = false; //Flag to determine when ProgressDialog can be closed

        // #--Window Lifetime--#
        public ProgressDialog()
        {

            InitializeComponent();
            taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

        }

        //Closes ProgressDialog called when process is completed
        public void closeDialog()
        {
            Dispatcher.Invoke((() => //Cross threaded operations need to be invoked
            {
                shouldClose = true; //Let us allow closing of progress
                Close(); //Closes Progress

            }));

        }

        private void ProgressDialog1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            e.Cancel = !shouldClose;

        }

        //#--Progress--#
        public void setProgress(int progress, string processTitle, TaskbarItemProgressState progressState)
        {

            progressBar.Value = progress; //Sets the progressbar value
            processLabel.Text = processTitle; //Sets processlabel text
            taskBarItemInfo.ProgressState = progressState;
            taskBarItemInfo.ProgressValue = progress / 100.0;

        }
        
        //#--Buttons--#
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

            cancelWork(this, new EventArgs());
            processLabel.Text = "Canceling";
            taskBarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;

        }

    }
}
