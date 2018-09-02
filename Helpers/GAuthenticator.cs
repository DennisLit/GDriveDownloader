using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DriveDownloader
{
    public static class GAuthenticator
    {
        /// <summary>
        /// Google service containing all api methods
        /// </summary>
        public static DriveService Service { get; set; }

        /// <summary>
        /// Data needed to authenticate some requests
        /// </summary>
        private static UserCredential Credential { get; set; }

        /// <summary>
        /// Contains the date when user acquired the token for 
        /// authenticating actions with google drive
        /// </summary>
        public static DateTime WhenTokenReceived { get; set; }


        private static string[] DefaultScopes => new string[] { DriveService.Scope.Drive, DriveService.Scope.DriveMetadata };


        public static void Authenticate(string ApplicationName)
        {
            try
            {
                using (var stream =
                        new FileStream("AuthCredentials.json", FileMode.Open, FileAccess.Read))
                {
                    string credPath = Environment.GetFolderPath(
                        Environment.SpecialFolder.Personal);

                    credPath = Path.Combine(credPath, ".credentials/Responces.json");
                    Credential = 
                        GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        DefaultScopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                }

                Service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = Credential,
                    ApplicationName = ApplicationName,
                });

                WhenTokenReceived = DateTime.UtcNow;


            }
            catch
            {
               
            }
        }

        public static async Task RefreshToken()
        {
            var result = await Credential.RefreshTokenAsync(CancellationToken.None);

            if (result)
                WhenTokenReceived = DateTime.UtcNow;

        }
    }
}
