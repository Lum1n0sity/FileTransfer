using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace FileTransfer
{
    public class FileListItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public Icon FileIcon { get; set; }

        public FileListItem(string filePath, Icon fileIcon)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            FileIcon = fileIcon;
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}