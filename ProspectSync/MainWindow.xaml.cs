using ProspectSync.Infrastructure;
using System.Text;
using System.Windows;
using System.IO;
using Newtonsoft.Json.Linq;
using ProspectSync.services;

namespace ProspectSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GoogleDriveService _driveService;
        private readonly AppService _gameService = new AppService();

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the Drive service
            _driveService = new GoogleDriveService( "credentials/icarussaveshare-82d0b173d4cf.json" );

            // Get current Steam User
            GetCurrentSteamUser();
        }

        private void CheckButton_Click( object sender, RoutedEventArgs e )
        {
            
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

        private void GetCurrentSteamUser()
        {
            string userInfo = _gameService.DetectCurrentUser();
            UserInfo.Text   = userInfo;
        }

        private void ListFileFromFolder()
        {
            // Fetch the list of files.
            var files = _driveService.ListFiles();

            // Use a StringBuilder for efficiency when concatenating strings.
            StringBuilder messageBuilder = new StringBuilder();

            if ( files != null && files.Count > 0 )
            {
                messageBuilder.AppendLine( $"Found {files.Count} file(s):" );

                // Iterate through each file and append its name to the message.
                foreach ( var file in files )
                {
                    if ( file.MimeType != "application/vnd.google-apps.folder" )
                    {
                        messageBuilder.AppendLine( file.Name );
                    }
                }
            }
            else
            {
                messageBuilder.AppendLine( "No files found." );
            }

            // Set the text of the MessagesTextBox to the built message.
            MessagesTextBox.Text = messageBuilder.ToString();
        }
    }
}
