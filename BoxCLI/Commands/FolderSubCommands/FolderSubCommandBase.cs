using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Box.V2.Models;
using BoxCLI.BoxHome;
using BoxCLI.BoxPlatform.Service;
using BoxCLI.CommandUtilities;
using BoxCLI.CommandUtilities.CsvModels;
using BoxCLI.CommandUtilities.Globalization;
using Microsoft.Extensions.CommandLineUtils;

namespace BoxCLI.Commands.FolderSubCommands
{
    public class FolderSubCommandBase : BoxItemCommandBase
    {
        private CommandLineApplication _app;
        protected readonly List<string> _fields;

        public FolderSubCommandBase(IBoxPlatformServiceBuilder boxPlatformBuilder, IBoxHome home, LocalizedStringsResource names)
            : base(boxPlatformBuilder, home, names)
        {
            this._fields = new List<string>()
            {
                "type",
                "id",
                "sync_state",
                "has_collaborations",
                "sequence_id",
                "etag",
                "name",
                "created_at",
                "modified_at",
                "description",
                "size",
                "path_collection",
                "created_by",
                "modified_by",
                "owned_by",
                "shared_link",
                "folder_upload_email",
                "parent",
                "item_status",
                "item_collection"
            };
        }

        public override void Configure(CommandLineApplication command)
        {
            _app = command;
            base.Configure(command);
        }

        protected async virtual Task<BoxFolder> MoveFolder(string folderId, string parentFolderId, string etag = "")
        {
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            var folderRequest = new BoxFolderRequest()
            {
                Id = folderId,
                Parent = new BoxItemRequest()
                {
                    Id = parentFolderId
                }
            };
            return await boxClient.FoldersManager.UpdateInformationAsync(folderRequest, etag: etag);
        }
        protected async virtual Task<BoxFolder> CopyFolder(string folderId, string parentFolderId, string name = "")
        {
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            var folderRequest = new BoxFolderRequest()
            {
                Id = folderId,
                Parent = new BoxItemRequest()
                {
                    Id = parentFolderId
                }
            };
            if (!string.IsNullOrEmpty(name))
            {
                folderRequest.Name = name;
            }
            return await boxClient.FoldersManager.CopyAsync(folderRequest);
        }

        protected async virtual Task UploadFolder(string path, string folderId)
        {
            path = GeneralUtilities.TranslatePath(path);
            var fileList = new List<FileStream>();
            var taskList = new List<Task>();
            Parallel.ForEach(Directory.GetFiles(path), (file) =>
            {
                var name = GeneralUtilities.ResolveItemName(file);
                taskList.Add(base.UploadFile(file, folderId, name));
            });
            await Task.WhenAll(taskList);
        }

        protected async virtual Task CreateFoldersFromFile(string path,
            bool save = false, string overrideSavePath = "", string overrideSaveFileFormat = "", bool json = false)
        {
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            if (!string.IsNullOrEmpty(path))
            {
                path = GeneralUtilities.TranslatePath(path);
            }
            try
            {
                var folderRequests = base.ReadFile<BoxFolderRequest, BoxFolderCreateRequestMap>(path);
                List<BoxFolder> saveCreated = new List<BoxFolder>();

                foreach (var folderRequest in folderRequests)
                {
                    BoxFolder createdFolder = null;
                    try
                    {
                        createdFolder = await boxClient.FoldersManager.CreateAsync(folderRequest);
                        var update = new BoxFolderRequest();
                        if (!string.IsNullOrEmpty(folderRequest.Description))
                        {
                            update.Description = folderRequest.Description;
                            update.Id = createdFolder.Id;
                            createdFolder = await boxClient.FoldersManager.UpdateInformationAsync(update);
                        }
                    }
                    catch (Exception e)
                    {
                        Reporter.WriteError("Couldn't create folder...");
                        Reporter.WriteError(e.Message);
                    }
                    if (createdFolder != null)
                    {
                        this.PrintFolder(createdFolder, json);
                        if (save || !string.IsNullOrEmpty(overrideSavePath) || base._settings.GetAutoSaveSetting())
                        {
                            saveCreated.Add(createdFolder);
                        }
                    }
                }
                if (save || !string.IsNullOrEmpty(overrideSavePath) || base._settings.GetAutoSaveSetting())
                {
                    var fileFormat = base._settings.GetBoxReportsFileFormatSetting();
                    if (!string.IsNullOrEmpty(overrideSaveFileFormat))
                    {
                        fileFormat = overrideSaveFileFormat;
                    }
                    var savePath = base._settings.GetBoxReportsFolderPath();
                    if (!string.IsNullOrEmpty(overrideSavePath))
                    {
                        savePath = overrideSavePath;
                    }
                    var fileName = $"{base._names.CommandNames.Folders}-{base._names.SubCommandNames.Create}-{DateTime.Now.ToString(GeneralUtilities.GetDateFormatString())}";
                    base.WriteListResultsToReport<BoxFolder, BoxFolderMap>(saveCreated, fileName, savePath, fileFormat);
                }
            }
            catch (Exception e)
            {
                Reporter.WriteError(e.Message);
            }
        }

        protected async virtual Task UpdateFoldersFromFile(string path,
            bool save = false, string overrideSavePath = "", string overrideSaveFileFormat = "", bool json = false)
        {
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            if (!string.IsNullOrEmpty(path))
            {
                path = GeneralUtilities.TranslatePath(path);
            }
            try
            {
                var folderRequests = base.ReadFile<BoxFolderRequest, BoxFolderUpdateRequestMap>(path);
                List<BoxFolder> saveUpdated = new List<BoxFolder>();

                foreach (var folderRequest in folderRequests)
                {
                    BoxFolder updatedFolder = null;
                    try
                    {
                        updatedFolder = await boxClient.FoldersManager.UpdateInformationAsync(folderRequest);
                    }
                    catch (Exception e)
                    {
                        Reporter.WriteError("Couldn't update folder...");
                        Reporter.WriteError(e.Message);
                    }
                    if (updatedFolder != null)
                    {
                        this.PrintFolder(updatedFolder, json);
                        if (save || !string.IsNullOrEmpty(overrideSavePath) || base._settings.GetAutoSaveSetting())
                        {
                            saveUpdated.Add(updatedFolder);
                        }
                    }
                }
                if (save || !string.IsNullOrEmpty(overrideSavePath) || base._settings.GetAutoSaveSetting())
                {
                    var fileFormat = base._settings.GetBoxReportsFileFormatSetting();
                    if (!string.IsNullOrEmpty(overrideSaveFileFormat))
                    {
                        fileFormat = overrideSaveFileFormat;
                    }
                    var savePath = base._settings.GetBoxReportsFolderPath();
                    if (!string.IsNullOrEmpty(overrideSavePath))
                    {
                        savePath = overrideSavePath;
                    }
                    var fileName = $"{base._names.CommandNames.Folders}-{base._names.SubCommandNames.Update}-{DateTime.Now.ToString(GeneralUtilities.GetDateFormatString())}";
                    base.WriteListResultsToReport<BoxFolder, BoxFolderMap>(saveUpdated, fileName, savePath, fileFormat);
                }
            }
            catch (Exception e)
            {
                Reporter.WriteError(e.Message);
            }
        }
    }
}