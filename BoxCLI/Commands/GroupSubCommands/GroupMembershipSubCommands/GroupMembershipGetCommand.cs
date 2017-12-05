using System.Threading.Tasks;
using BoxCLI.BoxHome;
using BoxCLI.BoxPlatform.Service;
using BoxCLI.CommandUtilities.Globalization;
using Microsoft.Extensions.CommandLineUtils;

namespace BoxCLI.Commands.GroupSubCommands.GroupMembershipSubCommands
{
    public class GroupMemberGetCommand : GroupMembershipSubCommandBase
    {
        private CommandArgument _membershipId;
        private CommandLineApplication _app;
        private IBoxHome _home;

        public GroupMemberGetCommand(IBoxPlatformServiceBuilder boxPlatformBuilder, IBoxHome home, LocalizedStringsResource names)
            : base(boxPlatformBuilder, home, names)
        {
            _home = home;
        }

        public override void Configure(CommandLineApplication command)
        {
            _app = command;
            command.Description = "Get information about a group membership.";
            _membershipId = command.Argument("membershipId",
                                   "Id of group membership");

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
            base.CheckForValue(this._membershipId.Value, this._app, "A group memebership ID is required for this command.");
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            var membership = await boxClient.GroupsManager.GetGroupMembershipAsync(_membershipId.Value);
            if (base._json.HasValue() || this._home.GetBoxHomeSettings().GetOutputJsonSetting())
            {
                base.OutputJson(membership);
                return;
            }
            base.PrintGroupMember(membership);
        }
    }
}