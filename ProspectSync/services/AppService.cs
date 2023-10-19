using Google.Apis.Drive.v3;
using Newtonsoft.Json.Linq;
using ProspectSync.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ProspectSync.services
{
    internal class AppService
    {
        private static readonly HashSet<string> KnownSteamIDs = new HashSet<string>
        {
            "76561197970909442",
            "76561198046379424",
            "76561198052813074"
        };

        #region Async methods
        public async Task<string> GetLocalSaveInfoAsync( string steamUserId )
        {
            if ( string.IsNullOrWhiteSpace( steamUserId ) )
                return "Steam ID not detected yet.";

            string filePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", steamUserId, "Prospects", "Nebula Nokedli.json" );

            if ( !File.Exists( filePath ) )
                return "Error reading save file.";

            string data          = await File.ReadAllTextAsync( filePath );
            JObject saveInfo     = JObject.Parse( data );
            JObject prospectInfo = (JObject)saveInfo["ProspectInfo"];

            int elapsedTime            = prospectInfo["ElapsedTime"].Value<int>();
            string readableElapsedTime = TimeHelpers.ConvertSecondsToReadableTime( elapsedTime );

            StringBuilder displayMessage = new StringBuilder();
            displayMessage.AppendLine( $"Prospect Name: {prospectInfo["ProspectID"].Value<string>()}" );
            displayMessage.AppendLine( $"Difficulty: {prospectInfo["Difficulty"].Value<string>()}" );
            displayMessage.AppendLine( $"Elapsed Time: {readableElapsedTime}" );
            displayMessage.AppendLine( "Members:" );

            JArray associatedMembers = (JArray)prospectInfo["AssociatedMembers"];
            foreach ( JObject member in associatedMembers )
            {
                displayMessage.AppendLine( $"- {member["AccountName"].Value<string>()} ({member["CharacterName"].Value<string>()}): {( member["IsCurrentlyPlaying"].Value<bool>() ? "Currently Playing" : "Offline" )}" );
            }

            return displayMessage.ToString();
        }

        public async Task<string> CreateBackupAsync( string steamUserId )
        {
            string filePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", steamUserId, "Prospects", "Nebula Nokedli.json" );

            if ( !File.Exists( filePath ) )
                return "Original save file not found.";

            string backupFilePath = GenerateBackupFileName( filePath );

            // Check if backup already exists
            if ( File.Exists( backupFilePath ) )
                return "Backup already exists. Skipping backup creation.";

            try
            {
                await Task.Run( () => File.Copy( filePath, backupFilePath, overwrite: false ) );
                return $"Backup created successfully at {backupFilePath}";
            }
            catch ( Exception ex )
            {
                return $"Error creating backup: {ex.Message}";
            }
        }

        public async Task<string> CheckForNewerVersionAsync( GoogleDriveService driveService, string steamUserId )
        {
            if ( string.IsNullOrWhiteSpace( steamUserId ) )
                return "Steam ID not detected yet.";

            string localFilePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", steamUserId, "Prospects", "Nebula Nokedli.json" );

            if ( !File.Exists( localFilePath ) )
                return "Local save file not found.";

            int localElapsedTime     = GetElapsedTimeFromSave( localFilePath );
            string localReadableTime = TimeHelpers.ConvertSecondsToReadableTime( localElapsedTime );

            var remoteFile = await driveService.GetFileInfoAsync( "Nebula Nokedli.json" );

            if ( remoteFile == null )
                return "Error fetching the remote save file info.";

            string remoteFileContent = await driveService.GetFileContentAsync( remoteFile.Id );

            if ( string.IsNullOrEmpty( remoteFileContent ) )
                return "Error fetching the remote save file content.";

            JObject remoteSaveInfo    = JObject.Parse( remoteFileContent );
            int remoteElapsedTime     = remoteSaveInfo["ProspectInfo"]["ElapsedTime"].Value<int>();
            string remoteReadableTime = TimeHelpers.ConvertSecondsToReadableTime( remoteElapsedTime );

            if ( remoteElapsedTime > localElapsedTime )
                return $"A newer version is available on the cloud with {remoteReadableTime} elapsed time. Your local version has {localReadableTime} elapsed time.";
            else
                return $"You have the latest version with {localReadableTime} elapsed time. Cloud version has {remoteReadableTime} elapsed time.";
        }
        #endregion

        #region Sync methods
        public Dictionary<string, string> DetectCurrentUser()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            string gameDataPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData" );
            var steamIdDirectories = Directory.GetDirectories( gameDataPath );

            if ( steamIdDirectories.Length != 1 )
            {
                result["Error"] = "More than 1 steamId's were found, multiple steam accounts not supperted.";
                return result;
            }

            var steamIdDir = new DirectoryInfo( steamIdDirectories[0] );
            string steamId = steamIdDir.Name;

            string saveFilePath = Path.Combine( gameDataPath, steamId, "Prospects", "Nebula Nokedli.json" );

            if ( File.Exists( saveFilePath ) )
            {
                string fileContents      = File.ReadAllText( saveFilePath );
                JObject saveInfo         = JObject.Parse( fileContents );
                JToken associatedMembers = saveInfo["ProspectInfo"]["AssociatedMembers"];

                if ( associatedMembers is JArray members )
                {
                    foreach ( JToken member in members )
                    {
                        if ( member["UserID"].ToString() == steamId )
                        {
                            string userName   = member["AccountName"].ToString();
                            result["User"]    = userName;
                            result["SteamID"] = steamId;
                            return result;
                        }
                    }
                    result["SteamID"] = steamId;
                    result["Note"]    = "User not found in save file.";
                    return result;
                }
            }
            else
            {
                result["SteamID"] = steamId;
                result["Error"]   = "Save file not found.";
                return result;
            }

            // If you reached here without returning, no known Steam ID was found.
            result["Error"] = "No recognized Steam ID detected.";
            return result;
        }


        public string GetLocalSaveInfo( string steamUserId )
        {
            if ( string.IsNullOrWhiteSpace( steamUserId ) )
                return "Steam ID not detected yet.";

            string filePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", steamUserId, "Prospects", "Nebula Nokedli.json" );

            int elapsedTime = GetElapsedTimeFromSave( filePath );
            int hours       = elapsedTime / 3600;
            int minutes     = ( elapsedTime % 3600 ) / 60;
            int seconds     = elapsedTime % 60;

            StringBuilder displayMessage = new StringBuilder();
            displayMessage.AppendLine( $"Prospect Name: {JObject.Parse( File.ReadAllText( filePath ) )["ProspectInfo"]["ProspectID"].Value<string>()}" );
            displayMessage.AppendLine( $"Difficulty: {JObject.Parse( File.ReadAllText( filePath ) )["ProspectInfo"]["Difficulty"].Value<string>()}" );
            displayMessage.AppendLine( $"Elapsed Time: {hours}h {minutes}m {seconds}s" );
            displayMessage.AppendLine( "Members:" );

            JArray associatedMembers = (JArray)JObject.Parse( File.ReadAllText( filePath ) )["ProspectInfo"]["AssociatedMembers"];
            foreach ( JObject member in associatedMembers )
            {
                displayMessage.AppendLine( $"- {member["AccountName"].Value<string>()} ({member["CharacterName"].Value<string>()}): {( member["IsCurrentlyPlaying"].Value<bool>() ? "Currently Playing" : "Offline" )}" );
            }

            return displayMessage.ToString();
        }


        public string CreateBackup( string steamUserId )
        {
            string filePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", steamUserId, "Prospects", "Nebula Nokedli.json" );
            if ( !File.Exists( filePath ) )
                return "Original save file not found.";

            string backupFilePath = GenerateBackupFileName( filePath );

            try
            {
                File.Copy( filePath, backupFilePath, overwrite: false );
                return $"Backup created successfully at {backupFilePath}";
            }
            catch ( Exception ex )
            {
                return $"Error creating backup: {ex.Message}";
            }
        }


        private string GenerateBackupFileName( string originalFilePath )
        {
            int elapsedTime       = GetElapsedTimeFromSave( originalFilePath );
            string elapsedTimeStr = elapsedTime.ToString( "D8" );  // Format the integer with leading zeros.

            string backupFileName = Path.GetFileNameWithoutExtension( originalFilePath ) + $"_{elapsedTimeStr}_backUp" + Path.GetExtension( originalFilePath );

            // Determine backup directory
            string backupDirectory = Path.Combine( Path.GetDirectoryName( originalFilePath ), "BackUp" );

            // If BackUp folder doesn't exist, create it
            if ( !Directory.Exists( backupDirectory ) )
            {
                Directory.CreateDirectory( backupDirectory );
            }

            return Path.Combine( backupDirectory, backupFileName );
        }


        public string CheckForNewerVersion( GoogleDriveService driveService, string steamUserId )
        {
            if ( string.IsNullOrWhiteSpace( steamUserId ) )
                return "Steam ID not detected yet.";

            string localFilePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", steamUserId, "Prospects", "Nebula Nokedli.json" );

            // Check if local file exists before comparing modification dates
            if ( !File.Exists( localFilePath ) )
                return "Local save file not found.";

            DateTime localFileModifiedTime = File.GetLastWriteTime( localFilePath );

            var remoteFile = driveService.GetFileInfo( "Nebula Nokedli.json" );

            if ( remoteFile == null )
                return "Error fetching the remote save file info.";

            DateTime remoteFileModifiedTime = remoteFile.ModifiedTime.Value;

            if ( remoteFileModifiedTime > localFileModifiedTime )
                return "A newer version is available!";
            else
                return "You have the latest version.";
        }

        public int GetElapsedTimeFromSave( string filePath )
        {
            if ( !File.Exists( filePath ) )
                throw new Exception( "Error reading save file." );

            string data          = File.ReadAllText( filePath );
            JObject saveInfo     = JObject.Parse( data );
            JObject prospectInfo = (JObject)saveInfo["ProspectInfo"];

            return prospectInfo["ElapsedTime"].Value<int>();
        }

        public static class TimeHelpers
        {
            public static string ConvertSecondsToReadableTime( int seconds )
            {
                int hours   = seconds / 3600;
                int minutes = ( seconds % 3600 ) / 60;
                seconds     = seconds % 60;
                return $"{hours}h {minutes}m {seconds}s";
            }
        }
        #endregion
    }
}
