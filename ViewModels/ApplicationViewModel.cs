using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace DriveDownloader
{
    public class ApplicationViewModel : BaseViewModel
    {
        public ApplicationViewModel()
        {
            Files = null;
            OnStartup();
        }

        #region Public Fields

        /// <summary>
        /// Each file item represent file 
        /// in main folder in Google Drive
        /// </summary>    
        public ObservableCollection<FileItem> Files { get; set; } 

        /// <summary>
        /// Represents Current state i.e downloading ..etc
        /// </summary>
        public string CurrentState { get; set; } = ResourceStrings.DefaultActionState;

        /// <summary>
        /// Shows info about downloaded/uploaded data
        /// </summary>
        public string CurrentSpeedState { get; set; } = ResourceStrings.DefaultUploadDownloadState;

        /// <summary>
        /// Indicates if some action is 
        /// completed or not. Used mainly for spinner
        /// </summary>
        public bool IsActionRunning { get; set; }

        #endregion

        #region Commands

        public ICommand UploadFileCommand { get { return new RelayCommand(UploadFile); } }

        public ICommand DownloadFileCommand { get { return new RelayCommand(DownloadFile); } }

        public ICommand DeleteFileCommand { get { return new RelayCommand(DeleteFile); } }

        public ICommand ChooseFileCommand { get { return new RelayCommandWithParam(ChooseFile); } }

        #endregion

        #region Command methods

        private async void DeleteFile()
        {
            IsActionRunning = true;

            //refresh token if it's expired

            if ((DateTime.UtcNow - GAuthenticator.WhenTokenReceived).Minutes >= 5)
                await GAuthenticator.RefreshToken();

            foreach (var file in Files)
            {
                if (file.IsSelected)
                {
                    try
                    {
                        GAuthenticator.Service.Files.Delete(file.Id).Execute();
                    }
                    catch
                    {
                        CurrentState = ResourceStrings.DefaultErrorState;
                    }
                }
            }

            Files.Clear();
            Files = UIItemsCreator.NewFileItemsList();

            IsActionRunning = false;

        }

        private void ChooseFile(object passedFile)
        {
            IsActionRunning = true;
            var IdToChange = (passedFile as FileItem).Id;
            ChangeIsSelected(IdToChange);
            IsActionRunning = false;
        }

        private async void UploadFile()
        {
            IsActionRunning = true;

            var dialog = new OpenFileDialog();
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                if ((DateTime.UtcNow - GAuthenticator.WhenTokenReceived).Minutes >= 5)
                    await GAuthenticator.RefreshToken();

               await UploadFileASync(dialog.FileName);

                Files.Clear();
                Files = UIItemsCreator.NewFileItemsList();
            }
            else
                CurrentState = ResourceStrings.DefaultErrorState;

            IsActionRunning = false;
        }

        private async void DownloadFile()
        {

            IsActionRunning = true;

            //refresh token if it's expired

            if ((DateTime.UtcNow - GAuthenticator.WhenTokenReceived).Minutes >= 5)
                await GAuthenticator.RefreshToken();

            foreach (var file in Files)
            {
                if(file.IsSelected)
                {
                    await DownloadFileAsync(file);
                }
            }

            IsActionRunning = false;
        }

        #endregion

        #region Helper Methods

        private async void OnStartup()
        {
            await Task.Run(() => GAuthenticator.Authenticate("Drive downloader"));

            var filesResult = UIItemsCreator.NewFileItemsList();

            Files = (filesResult == null) ? null : filesResult;

        }

        private void ChangeIsSelected(string Id)
        {

            foreach (var item in Files)
            {
                if (item.Id == Id)
                {
                    item.IsSelected = true;
                    return;
                }

            }
        }

        private async Task DownloadFileAsync(FileItem fileToDownload)
        {


            //get request for download

            var request = GAuthenticator.Service.Files.Get(fileToDownload.Id);

            //create a stream in which to write the file

                var stream = new MemoryStream();

                CurrentState = ResourceStrings.DownloadStarted;

                await Task.Run(() =>
                {
                    request.MediaDownloader.ProgressChanged +=
                (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Completed:
                            {
                                CurrentState = ResourceStrings.DownloadComplete;
                                CurrentSpeedState = ResourceStrings.DefaultSpeed;
                                IsActionRunning = false;
                                break;
                            }
                        case DownloadStatus.Failed:
                            {
                                CurrentState = ResourceStrings.DownloadFailed;
                                CurrentSpeedState = ResourceStrings.DefaultSpeed;
                                IsActionRunning = false;
                                break;
                            }
                    }
                };
                    request.Download(stream);

                    // save the file to the disk(desktop)

                    File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + 
                        "\\" + fileToDownload.FileRealName, stream.ToArray());

                });
            
        }


        private async Task UploadFileASync(string filePath)
        {
            try
            {

                CurrentState = ResourceStrings.UploadStarted;

                FilesResource.CreateMediaUpload request;

                // define file metadata
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath),
                };

                using (var stream = new FileStream(filePath,
                                        FileMode.Open))
                {
                    request = GAuthenticator.Service.Files.Create(
                    fileMetadata, stream, "");
                    request.Fields = "name";

                    await Task.Run(() =>
                    {
                        request.ProgressChanged +=
                            (IUploadProgress progress) =>
                            {
                                switch (progress.Status)
                                {
                                    case UploadStatus.Completed:
                                        {
                                            CurrentSpeedState = ResourceStrings.DefaultSpeed;
                                            CurrentState = ResourceStrings.DefaultActionState;
                                            IsActionRunning = false;
                                            break;
                                        }
                                    case UploadStatus.Failed:
                                        {
                                            CurrentSpeedState = ResourceStrings.DefaultSpeed;
                                            CurrentState = ResourceStrings.DefaultErrorState;
                                            IsActionRunning = false;
                                            break;
                                        }
                                }
                            };

                        request.Upload();

                    });

                }

                var file = request.ResponseBody;
            }
            catch
            {
                CurrentState = ResourceStrings.DefaultErrorState;
                CurrentSpeedState = ResourceStrings.DefaultSpeed;
            }
        }

        #endregion
    }
}
