using ProspectSync.Infrastructure;
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

namespace ProspectSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GoogleDriveService _driveService;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the Drive service
            _driveService = new GoogleDriveService( "credentials/icarussaveshare-82d0b173d4cf.json" );
        }

        private void CheckButton_Click( object sender, RoutedEventArgs e )
        {
            // Example logic for the Check button. You can add the logic you need.
            var files = _driveService.ListFiles();
            if ( files != null && files.Count > 0 )
            {
                MessageBox.Show( $"Found {files.Count} file(s)." );
            }
            else
            {
                MessageBox.Show( "No files found." );
            }
        }

        private void GetSaveInfoButton_Click( object sender, RoutedEventArgs e )
        {
            // Add logic for getting save info
        }

        private void DownloadButton_Click( object sender, RoutedEventArgs e )
        {
            // Add logic for downloading the file
        }

        private void UploadButton_Click( object sender, RoutedEventArgs e )
        {
            // Add logic for uploading the file
        }

        private void BackupButton_Click( object sender, RoutedEventArgs e )
        {
            // Add logic for creating a backup
        }
    }
}
