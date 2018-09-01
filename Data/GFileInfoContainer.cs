using System.Collections.Generic;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;

namespace DriveDownloader
{ 
    public class GFilesInfoContainer
    {
        public static IList<File> Files { get; set; }

        public static bool UpdateFileInfo()
        {
            try
            {
                FilesResource.ListRequest listRequest = GAuthenticator.Service.Files.List();
                listRequest.PageSize = 1000;
                listRequest.Fields = "nextPageToken, files(id, name)";
                // List files.
                Files = listRequest.Execute().Files;
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
