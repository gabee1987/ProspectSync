using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace ProspectSync.Infrastructure
{
    public class GoogleDriveService
    {
        private readonly DriveService _service;

        public GoogleDriveService( string serviceAccountPath )
        {
            GoogleCredential credential;
            using ( var stream = new FileStream( serviceAccountPath, FileMode.Open, FileAccess.Read ) )
            {
                credential = GoogleCredential.FromStream( stream )
                    .CreateScoped( DriveService.Scope.Drive );
            }

            _service = new DriveService( new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "IcarusSaveShare",
            } );
        }

        public Google.Apis.Drive.v3.Data.File GetFileInfo( string fileName )
        {
            var request = _service.Files.List();
            request.Q = $"name = '{fileName}'";  // Query to search for a file by name
            request.Fields = "files(id, name, modifiedTime)";  // We only want to fetch specific fields for the file

            var files = request.Execute().Files;

            if ( files != null && files.Count > 0 )
            {
                return files[0];  // Return the first matched file (assuming file names are unique in the Drive)
            }

            return null;
        }



        // For test only
        public IList<Google.Apis.Drive.v3.Data.File> ListFiles()
        {
            var request = _service.Files.List();
            request.PageSize = 10; // List only 10 files for test purposes
            request.Fields = "nextPageToken, files(id, name)";

            var result = request.Execute();
            return result.Files;
        }
    }
}
