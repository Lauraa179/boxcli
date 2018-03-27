using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Box.V2.Models;
using BoxCLI.BoxHome;
using BoxCLI.BoxPlatform.Service;
using BoxCLI.CommandUtilities;
using BoxCLI.CommandUtilities.CommandOptions;
using BoxCLI.CommandUtilities.CsvModels;
using BoxCLI.CommandUtilities.Globalization;
using Microsoft.Extensions.CommandLineUtils;

namespace BoxCLI.Commands.CollaborationSubCommands
{
    public class CollaborationListCommand : CollaborationSubCommandBase
    {
        private CommandArgument _id;
        private CommandOption _save;
        private CommandOption _fileFormat;
        private CommandOption _fieldsOption;
        private CommandLineApplication _app;
        private IBoxHome _home;

        public CollaborationListCommand(IBoxPlatformServiceBuilder boxPlatformBuilder, IBoxHome home, LocalizedStringsResource names, BoxType t)
            : base(boxPlatformBuilder, home, names, t)
        {
            _home = home;
        }

        public override void Configure(CommandLineApplication command)
        {
            _app = command;
            command.Description = "List all collaborations on a Box item.";
            _id = command.Argument("boxItemId",
                                   "Id of the Box item");
            _save = SaveOption.ConfigureOption(command);
            _fileFormat = FileFormatOption.ConfigureOption(command);
            _fieldsOption = FieldsOption.ConfigureOption(command);
            command.OnExecute(async () =>
            {
                return await this.Execute();
            });
            base.Configure(command);
        }

        protected async override Task<int> Execute()
        {
            await this.RunList();
            return await base.Execute();
        }

        private async Task RunList()
        {
            base.CheckForValue(this._id.Value, this._app, "An ID is required for this command.");
            var fields = base.ProcessFields(this._fieldsOption.Value(), CollaborationSubCommandBase._fields);
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            if (_save.HasValue())
            {
                var fileName = $"{base._names.CommandNames.Collaborations}-{base._names.SubCommandNames.List}-{DateTime.Now.ToString(GeneralUtilities.GetDateFormatString())}";
                Reporter.WriteInformation("Saving file...");
                BoxCollection<BoxCollaboration> saveCollabs;
                if (base._t == BoxType.file)
                {
                    saveCollabs = await boxClient.FilesManager.GetCollaborationsAsync(this._id.Value, fields: fields);
                }
                else if (base._t == BoxType.folder)
                {
                    saveCollabs = await boxClient.FoldersManager.GetCollaborationsAsync(this._id.Value, fields: fields);
                }
                else
                {
                    throw new Exception("This item doesn't currently support collaborations.");
                }
                var saved = base.WriteOffsetCollectionResultsToReport<BoxCollaboration, BoxCollaborationMap>(saveCollabs, fileName, fileFormat: this._fileFormat.Value());
                Reporter.WriteInformation($"File saved: {saved}");
                return;
            }

            BoxCollection<BoxCollaboration> collabs;
            if (base._t == BoxType.file)
            {
                collabs = await boxClient.FilesManager.GetCollaborationsAsync(this._id.Value, fields: fields);
            }
            else if (base._t == BoxType.folder)
            {
                collabs = await boxClient.FoldersManager.GetCollaborationsAsync(this._id.Value, fields: fields);
            }
            else
            {
                throw new Exception("This item doesn't currently support collaborations.");
            }

            if (base._json.HasValue() || this._home.GetBoxHomeSettings().GetOutputJsonSetting())
            {
                base.OutputJson(collabs);
                return;
            }
            base.PrintCollaborations(collabs);
        }
    }
}