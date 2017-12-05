using System.Threading.Tasks;
using BoxCLI.BoxHome;
using BoxCLI.BoxPlatform.Service;
using BoxCLI.CommandUtilities.Globalization;
using Microsoft.Extensions.CommandLineUtils;

namespace BoxCLI.Commands.GroupSubCommands
{
    public class GroupGetCommand : GroupSubCommandBase
    {
        private CommandArgument _groupId;
        private CommandLineApplication _app;
        private IBoxHome _home;

        public GroupGetCommand(IBoxPlatformServiceBuilder boxPlatformBuilder, IBoxHome home, LocalizedStringsResource names)
            : base(boxPlatformBuilder, home, names)
        {
            _home = home;
        }

        public override void Configure(CommandLineApplication command)
        {
            _app = command;
            command.Description = "Get information about a group.";
            _groupId = command.Argument("groupId",
                                   "Id of group");

            command.OnExecute(async () =>
            {
                return await this.Execute();
            });
            base.Configure(command);
        }

        protected async override Task<int> Execute()
        {
            await this.RunGet();
            return await base.Execute();
        }

        private async Task RunGet()
        {
            base.CheckForValue(this._groupId.Value, this._app, "A group ID is required for this command");
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            var group = await boxClient.GroupsManager.GetGroupAsync(_groupId.Value);
            if (base._json.HasValue() || this._home.GetBoxHomeSettings().GetOutputJsonSetting())
            {
                base.OutputJson(group);
                return;
            }
            base.PrintGroup(group);
        }
    }
}