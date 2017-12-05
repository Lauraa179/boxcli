using System.Threading.Tasks;
using Box.V2.Models;
using BoxCLI.BoxHome;
using BoxCLI.BoxPlatform.Service;
using BoxCLI.CommandUtilities;
using BoxCLI.CommandUtilities.Globalization;
using Microsoft.Extensions.CommandLineUtils;

namespace BoxCLI.Commands.FileSubCommand
{
    public class FileRenameCommand : FileSubCommandBase
    {
        private CommandArgument _fileId;
        private CommandArgument _fileName;
        private CommandOption _description;
        private CommandOption _etag;
        private CommandLineApplication _app;
        private IBoxHome _home;

        public FileRenameCommand(IBoxPlatformServiceBuilder boxPlatformBuilder, IBoxHome home, LocalizedStringsResource names)
            : base(boxPlatformBuilder, home, names)
        {
            _home = home;
        }

        public override void Configure(CommandLineApplication command)
        {
            _app = command;
            command.Description = "Rename a file";
            _fileId = command.Argument("fileId",
                               "Id of file to rename");
            _fileName = command.Argument("fileName",
                               "New name of file");
            _description = command.Option("--description", "Change the file description", CommandOptionType.SingleValue);
            _etag = command.Option("--etag", "Only rename if etag value matches", CommandOptionType.SingleValue);
            command.OnExecute(async () =>
            {
                return await this.Execute();
            });
            base.Configure(command);
        }

        protected async override Task<int> Execute()
        {
            await this.RunRename();
            return await base.Execute();
        }

        private async Task RunRename()
        {
            base.CheckForFileId(this._fileId.Value, this._app);
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            var fileRequest = base.ConfigureFileRequest(this._fileId.Value, fileName: this._fileName.Value, description: this._description.Value());
            BoxFile renamedFile;
            if (this._etag.HasValue())
            {
                renamedFile = await boxClient.FilesManager.UpdateInformationAsync(fileRequest, this._etag.Value());
            }
            else
            {
                renamedFile = await boxClient.FilesManager.UpdateInformationAsync(fileRequest);
            }
            if (base._json.HasValue() || this._home.GetBoxHomeSettings().GetOutputJsonSetting())
            {
                base.OutputJson(renamedFile);
                return;
            }
            Reporter.WriteSuccess($"Renamed file {this._fileId.Value}");
            base.PrintFile(renamedFile);
        }
    }
}