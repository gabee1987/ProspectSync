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

        // Here you will add methods for operations like Upload, Download, ListFiles, etc.



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
