using PropertyChanged;

namespace DriveDownloader
{
    [AddINotifyPropertyChangedInterface]

    public class FileItem
    {
        public string Id { get; set; }

        public string FileRealName { get; set; }

        public string FileSimplifiedName { get; set; }

        public string ImageSource { get; set; }

        public bool IsSelected { get; set; }

    }
}
