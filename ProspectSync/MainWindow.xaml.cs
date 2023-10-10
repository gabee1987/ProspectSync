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

        private async void CheckButton_Click( object sender, RoutedEventArgs e )
        {
            string message       = await _appService.CheckForNewerVersionAsync( _driveService, CurrentSteamUserID );
            MessagesTextBox.Text = message;
        }

        private async void GetSaveInfoButton_Click( object sender, RoutedEventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                string saveInfo      = await _appService.GetLocalSaveInfoAsync( CurrentSteamUserID );
                MessagesTextBox.Text = saveInfo;
            }
        }

        private async void DownloadButton_Click( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                MessagesTextBox.Text = "Steam ID not detected yet.";
                return;
            }

            string localFilePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", CurrentSteamUserID, "Prospects", "Nebula Nokedli.json" );
            var result           = await _driveService.DownloadAndOverwriteAsync( "1Gok2QUvgUwwZW3lyahQG7PYJ8weiGFx3", "Nebula Nokedli.json", localFilePath );
            MessagesTextBox.Text = result.Message;
        }

        private async void UploadButton_Click( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                MessagesTextBox.Text = "Steam ID not detected yet.";
                return;
            }

            string localFilePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
                "Icarus", "Saved", "PlayerData", CurrentSteamUserID, "Prospects", "Nebula Nokedli.json" );

            bool success = await _driveService.UploadAndOverwriteAsync( localFilePath, "1Gok2QUvgUwwZW3lyahQG7PYJ8weiGFx3" );

            MessagesTextBox.Text = success ? "Upload successful!" : "Error during upload.";
        }

        private async void BackupButton_Click( object sender, RoutedEventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                string backupMessage = await _appService.CreateBackupAsync( CurrentSteamUserID );
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
    }
}
