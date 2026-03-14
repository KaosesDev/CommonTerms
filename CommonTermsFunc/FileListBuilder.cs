using CommonTermsFunc.Models;
using Kaoses.Core.System.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTermsFunc
{
    public class FileListBuilder
    {
        private string _directoryPath;

        public FileListBuilder(string directoryPath)
        {
            _directoryPath = directoryPath;
        }   

        public List<string> GetAllFiles()
        {
            return FileHelper.GetFilesFromDirectory(_directoryPath);
        }

        public List<string> GetAllFilesRecursively()
        {
            return FileHelper.GetFilesFromDirectory(_directoryPath, true);
        }

        public List<string> GetFilesByExtension(string extension)
        {
            string searchPattern = $"*{extension}";
            return FileHelper.GetFilesFromDirectory(_directoryPath, false, searchPattern);
        }

        public List<string> GetFilesByExtensionRecursively(string extension)
        {
            string searchPattern = $"*{extension}";
            return FileHelper.GetFilesFromDirectory(_directoryPath, true, searchPattern);
        }

        public List<FileModel> BuildModelList(List<string> filePaths)
        {
            List<FileModel> fileModels = new();
            foreach (var path in filePaths)
            {
                FileInfo fileInfo = new(path);
                FileModel model = new()
                {
                    FileName = fileInfo.Name,
                    FileNameCleaned = string.Empty,
                    FilePath = fileInfo.FullName,
                    Extension = fileInfo.Extension,
                    ParentFolder = fileInfo.DirectoryName ?? string.Empty
                };
                model.FileNameCleaned = model.FileName.Replace('-',' ').Replace('_',' ');
                fileModels.Add(model);
            }
            return fileModels;
        }

        public List<FileModel> GetAndBuildAllFiles()
        {
            return BuildModelList(GetAllFiles());
        }
    }
}
