using System;
using System.Collections.ObjectModel;

namespace DriveDownloader
{
    public static class UIItemsCreator
    {
        /// <summary>
        /// Updates list of files in gDrive, then creates itemslist for VM
        /// </summary>
        /// <returns></returns>
        public static ObservableCollection<FileItem> NewFileItemsList()
        {
            // if updating was interrupted

            if (!GFilesInfoContainer.UpdateFileInfo())
                return null;

            var ListToReturn = new ObservableCollection<FileItem>();

            foreach (var file in GFilesInfoContainer.Files)
            {
                ListToReturn.Add(new FileItem()
                {
                    Id = file.Id,
                    FileRealName = file.Name,
                    FileSimplifiedName = GetSimplifiedFileName(file.Name),
                    IsSelected = false,
                    ImageSource = GetImageSource(file.Name)
                });
            }

            return ListToReturn;
        }

        private static string GetSimplifiedFileName(string fileName)
        {
            //bad practice right there
            if (fileName.Length > 9)
                return fileName.Substring(0, 9) + "...";
            else
                return fileName;

        }

        /// <summary>
        /// Helper method to 
        /// return propriate image for a file
        /// </summary>
        /// <param name="fileName"> Name of a file </param>
        /// <returns></returns>
        private static string GetImageSource(string fileName)
        {
            var IconUsed = string.Empty;

            //Check if there is an image for extension

            if (ExtensionSupported(fileName.Substring(fileName.IndexOf(".") + 1)))
            {
                IconUsed = "Icon_" + System.IO.Path.GetExtension(fileName).Substring(System.IO.Path.GetExtension(fileName).IndexOf(".") + 1) + ".png";
            }
            else
                IconUsed = ResourceStrings.DefaultImage; // if not, use default image

            return ResourceStrings.DefaultImagesLocation + IconUsed;

        }

        /// <summary>
        /// Return true if extension is supported
        /// </summary>
        /// <param name="extension"> Extension name (without dot) </param>
        /// <returns></returns>
        private static bool ExtensionSupported(string extension)
        {
            for (int i = 0; i < Enum.GetNames(typeof(SupportedExtensions)).Length; ++i)
            {
                if (extension == Enum.GetName(typeof(SupportedExtensions), i))
                {
                    return true;
                }
            }
            return false;
        }


    }
}
