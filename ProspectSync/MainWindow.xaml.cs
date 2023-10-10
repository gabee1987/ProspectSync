﻿using ProspectSync.Infrastructure;
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
            
        }

        private void GetSaveInfoButton_Click( object sender, RoutedEventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                string saveInfo = _gameService.GetLocalSaveInfo( CurrentSteamUserID );
                MessagesTextBox.Text = saveInfo;
            }
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
            if ( !string.IsNullOrWhiteSpace( CurrentSteamUserID ) )
            {
                string backupMessage = _gameService.CreateBackup( CurrentSteamUserID );
                MessagesTextBox.Text = backupMessage;
            }
            else
            {
                MessagesTextBox.Text = "Steam ID not detected.";
            }
        }

        private void GetCurrentSteamUser()
        {
            var userInfoDict = _gameService.DetectCurrentUser();
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
