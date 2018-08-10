using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using Google.Apis.Download;
using FORMS = System.Windows.Forms;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;

namespace DriveDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                string[] Scopes = { DriveService.Scope.Drive, DriveService.Scope.DriveMetadata };
                // Create Drive API service.
                Drive.Authenticate(Scopes, "Drive Downloader");
                Drive.GetFileInfo();
                MainInterface.InitRowsCols(ref MainGrid);
                MainInterface.ButtonTemplate = FindResource("RoundedButtonTemplate") as ControlTemplate;
                MainInterface.RepaintInterface(Drive.files, ref MainGrid);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        public static class Drive
        {
            public static DriveService service;
            public static IList<Google.Apis.Drive.v3.Data.File> files;
            private static UserCredential credential;
            private static Google.Apis.Drive.v3.Data.File fileMetadata;
            public static bool isUploaded;
            public static bool isDeleted;
            public static void DeleteFile(string fileId)
            {
                try
                {
                    service.Files.Delete(fileId).Execute();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Not enough permissions. Probably you are deleting public file.", "Drive downloader");
                }

            }

            public static async Task GetNewRefreshTokenAsync()
            {
                isDeleted = await credential.RefreshTokenAsync(CancellationToken.None);
            }

            public static void Authenticate(string[] scopes,string ApplicationName)
            {
                try
                {


                    using (var stream =
                            new FileStream("AuthCredentials.json", FileMode.Open, FileAccess.Read))
                    {
                        string credPath = System.Environment.GetFolderPath(
                            System.Environment.SpecialFolder.Personal);
                        credPath = System.IO.Path.Combine(credPath, ".credentials/Responces.json");
                        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                            GoogleClientSecrets.Load(stream).Secrets,
                            scopes,
                            "user",
                            CancellationToken.None,
                            new FileDataStore(credPath, true)).Result;
                    }

                    UpdateService(ApplicationName);
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            public static void UpdateService(string ApplicationName)
            {
                service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });
            }
            
            public static void GetFileInfo()
            {
                // Define parameters of request.
                try
                {
                    FilesResource.ListRequest listRequest = service.Files.List();
                    listRequest.PageSize = 100;
                    listRequest.Fields = "nextPageToken, files(id, name)";
                    // List files.
                    files = listRequest.Execute().Files;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }

            }


        }

        public class MainInterface
        {
            public static string[] CurrentButtonTag = new string[2];
            public const int ColumnsAmnt = 8;
            public static int MinRowsAmnt = 10;
            public static ControlTemplate ButtonTemplate;
            public static bool CheckIfSelected(Button button)
            {
                string[] tmpTag = button.Tag as string[];
                return Convert.ToBoolean(tmpTag[1]);

            }
            public static void InitRowsCols(ref Grid MainGrid)
            {
                int RowsToAdd = (int)Math.Ceiling((decimal)(Drive.files.Count / ColumnsAmnt)) + 1;
                if (RowsToAdd < MinRowsAmnt)
                    RowsToAdd = MinRowsAmnt;
                for (int i = 0; i < RowsToAdd; ++i)
                {
                    RowDefinition CustomRow = new RowDefinition();
                    CustomRow.Height = GridLength.Auto;
                    MainGrid.RowDefinitions.Add(CustomRow);
                }
            }
            // Изменяет состояние кнопки : нажатая / не нажатая
            public static void ChangeButtonState(ref Button tmpButton)
            {
                CurrentButtonTag = tmpButton.Tag as string[];
                var bc = new BrushConverter();
                if (CheckIfSelected(tmpButton))
                {
                    tmpButton.Background = Brushes.White;
                    CurrentButtonTag[1] = "false";
                }
                else
                {
                    tmpButton.Background = (Brush)bc.ConvertFrom("#0E82F6");
                    CurrentButtonTag[1] = "true";
                }
                tmpButton.Tag = CurrentButtonTag;
            }

            public enum ExtensionsNames
            {
                exe,
                rar,
                zip,
                apk,
                doc,
                docx,
                pdf,
                png,
                jpg,
                raw
            }

            public static bool isSupportedType(string extension)
            {

                for (int i = 0; i < ExtensionsNames.GetNames(typeof(ExtensionsNames)).Length; ++i)
                {
                    if(extension == "." + ExtensionsNames.GetName(typeof(ExtensionsNames), i))
                    {
                        return true;
                    }
                }
                return false;
            }
            // принимает файл на отрисовку - в виде кнопки 
            public static void DrawButton(Google.Apis.Drive.v3.Data.File file, ControlTemplate template, ref Grid MainGrid, int Column, int Row)
            {
                try
                {
                    Button newBtn = new Button();
                    var GridInAButton = new Grid();
                    GridInAButton.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Star) });
                    GridInAButton.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    GridInAButton.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    string IconUsed = "";
                    if (isSupportedType(System.IO.Path.GetExtension(file.Name)))
                    {
                        IconUsed = "Icon_" + System.IO.Path.GetExtension(file.Name).Substring(System.IO.Path.GetExtension(file.Name).IndexOf(".") + 1) + ".png";
                    }
                    else
                    {
                        IconUsed = "Icon_unknown.png";
                    }

                    var imageSource = new BitmapImage(new Uri("pack://application:,,,/DriveDownloader;component/Images/" + IconUsed));
                    var image = new Image { Source = imageSource };
                    Grid.SetRow(image, 0);
                    Grid.SetColumn(image, 0);
                    image.Width = 45;
                    image.Height = 45;
                    GridInAButton.Children.Add(image);
                    var newTextBlock = new TextBlock();
                    string FileName = "";
                    const int MaxLength = 6;
                    if (file.Name.Length > MaxLength)
                        FileName = file.Name.Substring(0, MaxLength - 1) + "...";
                    else
                        FileName = file.Name;
                    newTextBlock.Text = FileName;
                    newTextBlock.FontFamily = new FontFamily("Product Sans");
                    newTextBlock.Foreground = Brushes.Black;
                    newTextBlock.FontSize = 16;
                    newTextBlock.HorizontalAlignment = HorizontalAlignment.Left;
                    newTextBlock.VerticalAlignment = VerticalAlignment.Center;
                    Grid.SetRow(newTextBlock, 1);
                    Grid.SetColumn(image, 0);
                    GridInAButton.Children.Add(newTextBlock);
                    newBtn.Content = GridInAButton;
                    newBtn.Height = 100;
                    newBtn.Width = 100;
                    newBtn.Background = Brushes.White;
                    newBtn.BorderBrush = Brushes.Black;
                    newBtn.SetResourceReference(Control.StyleProperty, "MaterialDesignRaisedButton");
                    newBtn.Click += MainButton_Click;
                    // массив для хранения параметров кнопки : выбрана/не выбрана , id
                    string[] ButtonInfo = new string[2];
                    ButtonInfo[0] = file.Id;
                    ButtonInfo[1] = "false";
                    newBtn.Tag = ButtonInfo;
                    Thickness TmpMargin = newBtn.Margin;
                    TmpMargin.Right = 10;
                    TmpMargin.Top = 5;
                    TmpMargin.Left = 5;
                    newBtn.Margin = TmpMargin;

                    Grid.SetColumn(newBtn, Column);
                    Grid.SetRow(newBtn, Row);
                    MainGrid.Children.Add(newBtn);
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            public static void RepaintInterface(IList<Google.Apis.Drive.v3.Data.File> files, ref Grid MainGrid)
            {
                MainGrid.Children.Clear();
                int k = 0;
                int j = 0;
                int RowsMainGridAmnt = MainGrid.RowDefinitions.Count;
                foreach (var file in files)
                {
                    if (k < RowsMainGridAmnt)
                    {
                        if (j < ColumnsAmnt)
                        {
                            DrawButton(file, ButtonTemplate, ref MainGrid, j, k);
                            ++j;
                        }
                        if (j >= ColumnsAmnt)
                        {
                            ++k;
                            j = 0;
                        }
                    }
                }
            }
        }

        public static void MainButton_Click(object sender, RoutedEventArgs e)
        {
            Button newBtn = (Button)sender;
            MainInterface.ChangeButtonState(ref newBtn);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result2 = MessageBox.Show("You sure you want to download these files?", "Drive downloader", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            switch (result2)
            {
                case MessageBoxResult.OK:
                    {
                        string[] ButtonTag = new string[2];
                        for (int i = 0; i < MainGrid.Children.Count; ++i)
                        {
                            ButtonTag = ((Button)MainGrid.Children[i]).Tag as string[];
                            if (ButtonTag[1] == "true")
                            {
                                //отправляем запрос на получение файла для того, чтобы получить его имя.
                                Google.Apis.Drive.v3.Data.File file = Drive.service.Files.Get(ButtonTag[0]).Execute();
                                var request = Drive.service.Files.Get(ButtonTag[0]);
                                var stream = new MemoryStream();
                                this.Dispatcher.Invoke(() =>
                                {
                                    StatusCaption.Text = "Accessing the server...";
                                    MainSpinner.Spin = true;
                                });
                                Task.Run(() =>
                                {
                                    request.MediaDownloader.ProgressChanged +=
                                (IDownloadProgress progress) =>
                                {
                                    switch (progress.Status)
                                    {
                                        case DownloadStatus.Downloading:
                                            {
                                                this.Dispatcher.Invoke(() =>
                                                {
                                                    DownloadProgressCaption.Text = "Downloaded..." + (int)(progress.BytesDownloaded / 1000000) + "mB";
                                                    StatusCaption.Text = "Download started...";
                                                });
                                                break;
                                            }
                                        case DownloadStatus.Completed:
                                            {
                                                this.Dispatcher.Invoke(() =>
                                                {
                                                    StatusCaption.Text = "Download complete.";
                                                    MainSpinner.Spin = false;
                                                });
                                                break;
                                            }
                                        case DownloadStatus.Failed:
                                            {
                                                StatusCaption.Text = "Download failed.";
                                                MainSpinner.Spin = false;
                                                break;
                                            }
                                    }
                                };
                                    request.Download(stream);
                                    // сохраняем файл
                                    System.IO.File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + file.Name, stream.ToArray());
                                });


                            }
                        }
                        break;
                    }

                case MessageBoxResult.Cancel:
                    {
                        break;
                    }
            }
        }

        private async void DeleteButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("You sure you want to delete these items?", "Drive downloader", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            switch (result)
            {
                case MessageBoxResult.OK:
                    {
                        string[] ButtonTag = new string[2];
                        StatusCaption.Text = "Deleting the file(s)...";
                        MainSpinner.Spin = true;
                       await Task.Run(async () =>
                         {
                            await this.Dispatcher.Invoke(async () =>
                             {
                                 for (int i = 0; i < MainGrid.Children.Count; ++i)
                                 {
                                     ButtonTag = ((Button)MainGrid.Children[i]).Tag as string[];
                                     if (ButtonTag[1] == "true")
                                     {
                                         // get new refresh token

                                         await Drive.GetNewRefreshTokenAsync();

                                         if (Drive.isDeleted)
                                         {
                                             Drive.UpdateService("Drive Downloader");
                                             Drive.DeleteFile(ButtonTag[0]);
                                         }
                                         else
                                         {
                                             MessageBox.Show("Can't refresh token!", "Drive downloader");
                                         }


                                     }

                                 }
                             }
                         );
                         });                          
                        Drive.GetFileInfo();
                        Drive.UpdateService("Drive Downloader");
                        StatusCaption.Text = "Done deleting...";
                        MainSpinner.Spin = false;
                        MainInterface.RepaintInterface(Drive.files, ref MainGrid);
                           
                        break;
                    }
                case MessageBoxResult.Cancel:
                    {
                        break;
                    }

            }

        }


        private async Task<bool> UploadFileASync(string filePath)
        {
            try
            {
                FilesResource.CreateMediaUpload request;
                // определить метаданные файла
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(filePath)
                };
                using (var stream = new System.IO.FileStream(filePath,
                                        System.IO.FileMode.Open))
                {
                    request = Drive.service.Files.Create(
                        fileMetadata, stream, "");
                    request.Fields = "name";
                  await  Task.Run(() =>
                    {
                        request.ProgressChanged +=
                            (IUploadProgress progress) =>
                            {
                                switch (progress.Status)
                                {
                                    case UploadStatus.Uploading:
                                        {
                                            this.Dispatcher.Invoke(() =>
                                            {
                                                DownloadProgressCaption.Text = "Uploaded..." + (int)(progress.BytesSent / 1000000) + "mB";
                                                StatusCaption.Text = "Upload started...";
                                            });
                                            break;
                                        }
                                    case UploadStatus.Completed:
                                        {
                                            this.Dispatcher.Invoke(() =>
                                            {
                                                StatusCaption.Text = "Upload complete.";
                                                DownloadProgressCaption.Text = "Waiting for an action...";
                                                MainSpinner.Spin = false;
                                            });
                                            break;
                                        }
                                    case UploadStatus.Failed:
                                        {
                                            StatusCaption.Text = "Upload failed.";
                                            DownloadProgressCaption.Text = "Waiting for an action...";
                                            MainSpinner.Spin = false;
                                            break;
                                        }
                                }
                            };
                        request.Upload();

                    });

                }
                var file = request.ResponseBody;
                return true;
            }
            catch (Exception e1)
            {
                MessageBox.Show("Error while uploading the file.", "Drive downloader");
                MessageBox.Show(e1.Message);
                return false;
            }
        }

        private async void UploadButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            FORMS.DialogResult result = dialog.ShowDialog();
            bool isOK = result == FORMS.DialogResult.OK;
            if (isOK)
            {
                this.Dispatcher.Invoke(() =>
                {
                    StatusCaption.Text = "Uploading file...";
                    MainSpinner.Spin = true;
                }
                );
               await Drive.GetNewRefreshTokenAsync();
               
              if(await UploadFileASync(dialog.FileName))
              {
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusCaption.Text = "File uploaded...";
                        MainSpinner.Spin = false;
                    });
                    Drive.GetFileInfo();
                    MainInterface.RepaintInterface(Drive.files, ref MainGrid);
              }
               else
               {
                    this.Dispatcher.Invoke(() =>
                    {
                        StatusCaption.Text = "Error while uploading the file...";
                        MainSpinner.Spin = false;
                    });
               }

            }
            else
            {
                MessageBox.Show("Error while choosing the file", "Drive downloader");
                this.Dispatcher.Invoke(() =>
                {
                    StatusCaption.Text = "Error while choosing the file...";
                    MainSpinner.Spin = false;
                });
            }
        }
        private void ExitImage_MouseDown(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("You sure you want to exit?", "Drive downloader", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            switch (result)
            {
                case MessageBoxResult.OK:
                    {
                        System.Windows.Application.Current.Shutdown();
                        break;
                    }
            }  
        }
        private void MinimizeImage_MouseDown(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
    }  
    
}
