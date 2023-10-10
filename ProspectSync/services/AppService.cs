using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProspectSync.services
{
    internal class AppService
    {
        public Dictionary<string, string> DetectCurrentUser()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            string gameDataPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData" );
            var steamIdDirectories = Directory.GetDirectories( gameDataPath );

            if ( steamIdDirectories.Length == 1 )
            {
                var steamIdDir = new DirectoryInfo( steamIdDirectories[0] );
                string steamId = steamIdDir.Name;

                string saveFilePath = Path.Combine( gameDataPath, steamId, "Prospects", "Nebula Nokedli.json" );

                if ( File.Exists( saveFilePath ) )
                {
                    string fileContents = File.ReadAllText( saveFilePath );
                    JObject saveInfo = JObject.Parse( fileContents );
                    JToken associatedMembers = saveInfo["ProspectInfo"]["AssociatedMembers"];

                    if ( associatedMembers is JArray members )
                    {
                        foreach ( JToken member in members )
                        {
                            if ( member["UserID"].ToString() == steamId )
                            {
                                string userName = member["AccountName"].ToString();
                                result["User"] = userName;
                                result["SteamID"] = steamId;
                                return result;
                            }
                        }
                        result["SteamID"] = steamId;
                        result["Note"] = "User not found in save file.";
                        return result;
                    }
                }
                else
                {
                    result["SteamID"] = steamId;
                    result["Error"] = "Save file not found.";
                    return result;
                }
            }
            else
            {
                result["Error"] = "No Steam ID detected.";
                return result;
            }

            // Default return
            result["Error"] = "Unknown error.";
            return result;
        }


        public string GetLocalSaveInfo( string steamUserId )
        {
            if ( string.IsNullOrWhiteSpace( steamUserId ) )
                return "Steam ID not detected yet.";

            string filePath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData", steamUserId, "Prospects", "Nebula Nokedli.json" );

            if ( !File.Exists( filePath ) )
                return "Error reading save file.";

            string data = File.ReadAllText( filePath );
            JObject saveInfo = JObject.Parse( data );
            JObject prospectInfo = (JObject)saveInfo["ProspectInfo"];

            int elapsedTime = prospectInfo["ElapsedTime"].Value<int>();
            int hours = elapsedTime / 3600;
            int minutes = ( elapsedTime % 3600 ) / 60;
            int seconds = elapsedTime % 60;

            StringBuilder displayMessage = new StringBuilder();
            displayMessage.AppendLine( $"Prospect Name: {prospectInfo["ProspectID"].Value<string>()}" );
            displayMessage.AppendLine( $"Difficulty: {prospectInfo["Difficulty"].Value<string>()}" );
            displayMessage.AppendLine( $"Elapsed Time: {hours}h {minutes}m {seconds}s" );
            displayMessage.AppendLine( "Members:" );

            JArray associatedMembers = (JArray)prospectInfo["AssociatedMembers"];
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
            DateTime lastModified = File.GetLastWriteTime( originalFilePath );
            string dateStr = $"{lastModified.Year}_{lastModified.Month:00}_{lastModified.Day:00}_{lastModified.Hour:00}h_{lastModified.Minute:00}m_{lastModified.Second:00}s";
            string backupFileName = Path.GetFileNameWithoutExtension( originalFilePath ) + $"_{dateStr}_backUp" + Path.GetExtension( originalFilePath );
            return Path.Combine( Path.GetDirectoryName( originalFilePath ), backupFileName );
        }
    }
}
