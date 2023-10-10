using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace ProspectSync.services
{
    internal class AppService
    {
        public string DetectCurrentUser()
        {
            string gameDataPath    = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Icarus", "Saved", "PlayerData" );
            var steamIdDirectories = Directory.GetDirectories( gameDataPath );

            if ( steamIdDirectories.Length == 1 )
            {
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
                                string userName = member["AccountName"].ToString();
                                return $"User: {userName}\nSteam ID: {steamId}";
                            }
                        }
                        return $"Steam ID Detected: {steamId}\nNote: User not found in save file.";
                    }
                }
                else
                {
                    return $"Steam ID detected: {steamId}, but save file not found.";
                }
            }
            else
            {
                return "No Steam ID detected.";
            }

            // Default return
            return "Unknown error.";
        }
    }
}
