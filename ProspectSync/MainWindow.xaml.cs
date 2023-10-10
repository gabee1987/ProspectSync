using ProspectSync.Infrastructure;
using System.Text;
using System.Windows;
using System.IO;
using Newtonsoft.Json.Linq;
using ProspectSync.services;
using System;

namespace ProspectSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GoogleDriveService _driveService;
        private readonly AppService _appService = new AppService();
        private string CurrentSteamUserID;

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
            string message = _appService.CheckForNewerVersion( _driveService, CurrentSteamUserID );
            MessagesTextBox.Text = message;
        }

        private void GetSaveInfoButton_Click( object sender, RoutedEventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                string saveInfo = _appService.GetLocalSaveInfo( CurrentSteamUserID );
                MessagesTextBox.Text = saveInfo;
            }
        }

        private void DownloadButton_Click( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                MessagesTextBox.Text = "Steam ID not detected yet.";
                return;
            }

            string localFilePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
                "Icarus", "Saved", "PlayerData", CurrentSteamUserID, "Prospects", "Nebula Nokedli.json" );

            bool success = _driveService.DownloadAndOverwrite( "1Gok2QUvgUwwZW3lyahQG7PYJ8weiGFx3", "Nebula Nokedli.json", localFilePath, out string message );

            MessagesTextBox.Text = message;
        }

        private void UploadButton_Click( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                MessagesTextBox.Text = "Steam ID not detected yet.";
                return;
            }

            string localFilePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
                "Icarus", "Saved", "PlayerData", CurrentSteamUserID, "Prospects", "Nebula Nokedli.json" );

            bool success = _driveService.UploadAndOverwrite( localFilePath, "1Gok2QUvgUwwZW3lyahQG7PYJ8weiGFx3" );

            MessagesTextBox.Text = success ? "Upload successful!" : "Error during upload.";
        }

        private void BackupButton_Click( object sender, RoutedEventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                string backupMessage = _appService.CreateBackup( CurrentSteamUserID );
                MessagesTextBox.Text = backupMessage;
            }
            else
            {
                MessagesTextBox.Text = "Steam ID not detected.";
            }
        }

        private void GetCurrentSteamUser()
        {
            var userInfoDict = _appService.DetectCurrentUser();
            if ( userInfoDict.ContainsKey( "User" ) && userInfoDict.ContainsKey( "SteamID" ) )
            {
                CurrentSteamUserID = userInfoDict["SteamID"];
                UserInfo.Text    = $"User: {userInfoDict["User"]}\nSteam ID: {CurrentSteamUserID}";
            }
            else if ( userInfoDict.ContainsKey( "Error" ) )
            {
                UserInfo.Text = userInfoDict["Error"];
            }
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
