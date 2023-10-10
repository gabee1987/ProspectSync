using ProspectSync.Infrastructure;
using System.Text;
using System.Windows;
using System.IO;
using Newtonsoft.Json.Linq;
using ProspectSync.services;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Security.Cryptography;

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

            // Get current Steam User
            GetCurrentSteamUser();
        }

        private async void CheckButton_Click( object sender, RoutedEventArgs e )
        {
            ShowProgressBar();

            string message       = await _appService.CheckForNewerVersionAsync( _driveService, CurrentSteamUserID );
            MessagesTextBox.Text = message;

            HideProgressBar();
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

            ShowProgressBar();

            string localFilePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", CurrentSteamUserID, "Prospects", "Nebula Nokedli.json" );

            var result = await _driveService.DownloadAndOverwriteAsync( "1Gok2QUvgUwwZW3lyahQG7PYJ8weiGFx3", "Nebula Nokedli.json", localFilePath );
            MessagesTextBox.Text = result.Message;

            HideProgressBar();
        }


        private async void UploadButton_Click( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                MessagesTextBox.Text = "Steam ID not detected yet.";
                return;
            }

            ShowProgressBar();

            string localFilePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ),
                "Icarus", "Saved", "PlayerData", CurrentSteamUserID, "Prospects", "Nebula Nokedli.json" );

            bool success = await _driveService.UploadAndOverwriteAsync( localFilePath, "1Gok2QUvgUwwZW3lyahQG7PYJ8weiGFx3" );

            MessagesTextBox.Text = success ? "Upload successful!" : "Error during upload.";

            HideProgressBar();
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

        private async void UnlockButton_Click( object sender, RoutedEventArgs e )
        {
            // Assuming your encrypted credentials file is named "encrypted_credentials"
            string encryptedCredentialsPath = System.IO.Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "credentials", "icarussaveshare-82d0b173d4cf.json" );

            if ( !File.Exists( encryptedCredentialsPath ) )
            {
                PasswordMessageTextBox.Text = "Error: Encrypted credentials file not found.";
                return;
            }

            byte[] encryptedCredentials = File.ReadAllBytes( encryptedCredentialsPath );

            // Decrypt the credentials
            var securityService = new SecurityService();
            string decryptedCredentials = "";

            try
            {
                decryptedCredentials = securityService.DecryptStringFromBytes_Aes( encryptedCredentials, PasswordInputBox.Password );
            }
            catch ( CryptographicException )
            {
                PasswordMessageTextBox.Text = "Invalid password or corrupted data";
            }
            catch ( Exception ex )
            {
                PasswordMessageTextBox.Text = $"Error decrypting credentials: {ex.Message}";
                return;
            }

            if ( string.IsNullOrEmpty( decryptedCredentials ) )
            {
                PasswordMessageTextBox.Text = "Invalid password. Please try again.";
                return;
            }

            // At this point, we have successfully decrypted the credentials
            PasswordOverlay.Visibility = Visibility.Collapsed;

            // Initialize the GoogleDriveService with decrypted credentials
            _driveService = new GoogleDriveService( decryptedCredentials );
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

        #region Helper Methods
        private void ShowProgressBar()
        {
            ProcessProgressBar.Visibility      = Visibility.Visible;
            ProcessProgressBar.IsIndeterminate = true;
        }

        private void HideProgressBar()
        {
            ProcessProgressBar.Visibility      = Visibility.Collapsed;
            ProcessProgressBar.IsIndeterminate = false;
        }
        #endregion
    }
}
