using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProspectSync.Infrastructure
{
    public class GoogleDriveService
    {

        #region Init
        private readonly DriveService _driveService;

        public GoogleDriveService( string serviceAccountContent )
        {
            GoogleCredential credential;

            // Convert the content into a stream
            using ( var stream = new MemoryStream( Encoding.UTF8.GetBytes( serviceAccountContent ) ) )
            {
                credential = GoogleCredential.FromStream( stream )
                    .CreateScoped( DriveService.Scope.Drive );
            }

            _driveService = new DriveService( new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "IcarusSaveShare",
            } );
        }
        #endregion

        #region Async methods
        public async Task<Google.Apis.Drive.v3.Data.File> GetFileInfoAsync( string fileName )
        {
            var request    = _driveService.Files.List();
            request.Q      = $"name = '{fileName}'";
            request.Fields = "files(id, name, modifiedTime)";

            var files = await request.ExecuteAsync();
            return files.Files?.FirstOrDefault();
        }

        public async Task<string> GetFileContentAsync( string fileId )
        {
            try
            {
                var request = _driveService.Files.Get( fileId );
                var stream  = new MemoryStream();
                await request.DownloadAsync( stream );

                return Encoding.UTF8.GetString( stream.ToArray() );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( $"Error fetching file content: {ex.Message}" );
                return null;
            }
        }

        public async Task<string> GetFileIdByNameAsync( string fileName )
        {
            try
            {
                var request = _driveService.Files.List();
                request.Q      = $"name='{fileName}' and trashed=false";
                request.Fields = "files(id, name)";
                var result = await request.ExecuteAsync();

                return result.Files?.FirstOrDefault()?.Id;
            }
            catch ( Exception ex )
            {
                Console.WriteLine( ex.Message );
                return null;
            }
        }

        public async Task<bool> UploadAndOverwriteAsync( string localFilePath, string parentFolderId )
        {
            try
            {
                string fileName = Path.GetFileName( localFilePath );
                string fileId = await GetFileIdByNameAsync( fileName );

                if ( fileId != null )
                {
                    using ( var fileStream = new FileStream( localFilePath, FileMode.Open ) )
                    {
                        var updateRequest = _driveService.Files.Update( new Google.Apis.Drive.v3.Data.File(), fileId, fileStream, "application/octet-stream" );
                        await updateRequest.UploadAsync();
                    }
                }
                else
                {
                    var newFileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = fileName,
                        Parents = new List<string> { parentFolderId }
                    };

                    using ( var fileStream = new FileStream( localFilePath, FileMode.Open ) )
                    {
                        var createRequest = _driveService.Files.Create( newFileMetadata, fileStream, "application/octet-stream" );
                        await createRequest.UploadAsync();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(bool Success, string Message)> DownloadAndOverwriteAsync( string parentFolderId, string fileName, string localFilePath )
        {
            try
            {
                string fileId = await GetFileIdByNameAsync( fileName );
                if ( fileId == null )
                    return (false, "Error: File not found on Google Drive.");

                var request = _driveService.Files.Get( fileId );
                var stream = new MemoryStream();
                await request.DownloadAsync( stream );

                DateTime? localFileLastModified = File.Exists( localFilePath ) ? (DateTime?)File.GetLastWriteTime( localFilePath ) : null;

                using ( FileStream file = new FileStream( localFilePath, FileMode.Create, FileAccess.Write ) )
                    stream.WriteTo( file );

                DateTime downloadedFileLastModified = File.GetLastWriteTime( localFilePath );

                string message = $"Download successful!\nOriginal file last modified: {localFileLastModified?.ToString( "g" ) ?? "File did not exist previously"}\nDownloaded file last modified: {downloadedFileLastModified:g}";
                return (true, message);
            }
            catch ( Exception ex )
            {
                return (false, $"Error downloading: {ex.Message}");
            }
        }

        public async Task<string> GetFolderIdByNameAsync( string folderName )
        {
            try
            {
                var request = _driveService.Files.List();
                request.Q      = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
                request.Fields = "files(id, name)";
                var result = await request.ExecuteAsync();

                return result.Files?.FirstOrDefault()?.Id;
            }
            catch ( Exception ex )
            {
                Console.WriteLine( ex.Message );
                return null;
            }
        }
        #endregion


        #region Sync methods
        public Google.Apis.Drive.v3.Data.File GetFileInfo( string fileName )
        {
            var request    = _driveService.Files.List();
            request.Q      = $"name = '{fileName}'";  // Query to search for a file by name
            request.Fields = "files(id, name, modifiedTime)";  // We only want to fetch specific fields for the file

            var files = request.Execute().Files;

            if ( files != null && files.Count > 0 )
            {
                return files[0];  // Return the first matched file (assuming file names are unique in the Drive)
            }

            return null;
        }

        
        public string GetFileIdByName( string fileName )
        {
            try
            {
                var request = _driveService.Files.List();
                request.Q = $"name='{fileName}' and trashed=false";
                request.Fields = "files(id, name)";
                var result = request.Execute();

                if ( result != null && result.Files.Count > 0 )
                {
                    return result.Files[0].Id; // returning the ID of the first file found with that name
                }
                else
                {
                    return null;
                }
            }
            catch ( Exception ex )
            {
                // Handle error.
                Console.WriteLine( ex.Message );
                return null;
            }
        }

        
        private byte[] Compress( byte[] data )
        {
            using ( var compressedStream = new MemoryStream() )
            using ( var zipStream = new GZipStream( compressedStream, CompressionMode.Compress ) )
            {
                zipStream.Write( data, 0, data.Length );
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
        

        public void UploadFileWithBackup( string localFilePath, string parentFolderId )
        {
            string fileName = Path.GetFileName( localFilePath );
            string fileId = GetFileIdByName( fileName );

            if ( fileId != null )
            {
                // Download the file for backup
                var downloadStream = new MemoryStream();
                var downloadRequest = _driveService.Files.Get( fileId );
                downloadRequest.MediaDownloader.ProgressChanged += ( IDownloadProgress progress ) =>
                {
                    switch ( progress.Status )
                    {
                        case DownloadStatus.Completed:
                            Console.WriteLine( "Download complete." );
                            break;
                        case DownloadStatus.Failed:
                            Console.WriteLine( "Download failed." );
                            break;
                    }
                };
                downloadRequest.Download( downloadStream );

                // Compress the backup
                byte[] compressedData = Compress( downloadStream.ToArray() );

                // Upload the compressed backup
                var backupFileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName + ".backup.gz",
                    MimeType = "application/octet-stream",
                    Parents = new List<string> { parentFolderId } // Setting the parent folder
                };

                using ( var stream = new MemoryStream( compressedData ) )
                {
                    _driveService.Files.Create( backupFileMetadata, stream, "application/octet-stream" ).Upload();
                }

                // Now, overwrite the original file with the new version from `localFilePath`.
                using ( var fileStream = new FileStream( localFilePath, FileMode.Open ) )
                {
                    var updateRequest = _driveService.Files.Update( new Google.Apis.Drive.v3.Data.File(), fileId, fileStream, "application/octet-stream" );
                    updateRequest.Upload();
                }
            }
            else
            {
                // If the file doesn't exist on Drive, you can just upload it normally.
                var newFileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName,
                    Parents = new List<string> { parentFolderId } // Setting the parent folder
                };

                using ( var fileStream = new FileStream( localFilePath, FileMode.Open ) )
                {
                    _driveService.Files.Create( newFileMetadata, fileStream, "application/octet-stream" ).Upload();
                }
            }
        }


        public bool UploadAndOverwrite( string localFilePath, string parentFolderId )
        {
            try
            {
                string fileName = Path.GetFileName( localFilePath );
                string fileId = GetFileIdByName( fileName );

                if ( fileId != null ) // If the file exists on Drive, overwrite it
                {
                    using ( var fileStream = new FileStream( localFilePath, FileMode.Open ) )
                    {
                        var updateRequest = _driveService.Files.Update( new Google.Apis.Drive.v3.Data.File(), fileId, fileStream, "application/octet-stream" );
                        updateRequest.Upload();
                    }
                }
                else // If the file doesn't exist on Drive, just upload it
                {
                    var newFileMetadata = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = fileName,
                        Parents = new List<string> { parentFolderId } // Setting the parent folder
                    };

                    using ( var fileStream = new FileStream( localFilePath, FileMode.Open ) )
                    {
                        _driveService.Files.Create( newFileMetadata, fileStream, "application/octet-stream" ).Upload();
                    }
                }

                return true; // Indicates success
            }
            catch ( Exception )
            {
                return false; // Indicates failure
            }
        }

        
        public bool UploadFile( string localFilePath, string driveFolderPath, string driveFileName )
        {
            try
            {
                string folderId = GetFolderIdByName( "Icarus" );

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = driveFileName,
                    Parents = new List<string> { driveFolderPath } // This assumes that driveFolderPath is the ID of the folder on Google Drive.
                };

                FilesResource.CreateMediaUpload request;

                using ( var stream = new System.IO.FileStream( localFilePath, System.IO.FileMode.Open ) )
                {
                    request = _driveService.Files.Create( fileMetadata, stream, "application/octet-stream" );
                    request.Fields = "id";
                    request.Upload();
                }

                var file = request.ResponseBody;

                if ( file != null && !string.IsNullOrEmpty( file.Id ) )
                {
                    return true; // Indicating success
                }
                else
                {
                    return false;
                }
            }
            catch ( Exception ex )
            {
                // Handle error.
                Console.WriteLine( ex.Message );
                return false;
            }
        }


        public bool DownloadAndOverwrite( string parentFolderId, string fileName, string localFilePath, out string message )
        {
            try
            {
                string fileId = GetFileIdByName( fileName );

                if ( fileId == null )
                {
                    message = "Error: File not found on Google Drive.";
                    return false;
                }

                var request = _driveService.Files.Get( fileId );
                var stream  = new MemoryStream();
                request.Download( stream );

                // Get last modified time of the existing local file
                DateTime? localFileLastModified = null;
                if ( File.Exists( localFilePath ) )
                {
                    localFileLastModified = File.GetLastWriteTime( localFilePath );
                }

                // Overwrite the existing local file
                using ( FileStream file = new FileStream( localFilePath, FileMode.Create, FileAccess.Write ) )
                {
                    stream.WriteTo( file );
                }

                // Get last modified time of the downloaded file
                DateTime downloadedFileLastModified = File.GetLastWriteTime( localFilePath );

                message = $"Download successful!\nOriginal file last modified: {localFileLastModified?.ToString( "g" ) ?? "File did not exist previously"}\nDownloaded file last modified: {downloadedFileLastModified:g}";

                return true;
            }
            catch ( Exception ex )
            {
                message = $"Error downloading: {ex.Message}";
                return false;
            }
        }

        
        public string GetFolderIdByName( string folderName )
        {
            try
            {
                var request    = _driveService.Files.List();
                request.Q      = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and trashed=false";
                request.Fields = "files(id, name)";
                var result     = request.Execute();

                if ( result != null && result.Files.Count > 0 )
                {
                    return result.Files[0].Id; // returning the ID of the first folder found with that name
                }
                else
                {
                    return null;
                }
            }
            catch ( Exception ex )
            {
                // Handle error.
                Console.WriteLine( ex.Message );
                return null;
            }
        }
        #endregion

        #region Dev methods
        // For test only
        public IList<Google.Apis.Drive.v3.Data.File> ListFiles()
        {
            var request = _driveService.Files.List();
            request.PageSize = 10; // List only 10 files for test purposes
            request.Fields = "nextPageToken, files(id, name)";

            var result = request.Execute();
            return result.Files;
        }
        #endregion
    }
}
