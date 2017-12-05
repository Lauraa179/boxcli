using System;
using System.Collections.Generic;
using System.Linq;
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
    public class CollaborationSubCommandBase : BoxBaseCommand
    {
        protected CommandOption _asUser;
        protected readonly BoxType _t;
        public CollaborationSubCommandBase(IBoxPlatformServiceBuilder boxPlatformBuilder, IBoxHome home, LocalizedStringsResource names, BoxType t)
            : base(boxPlatformBuilder, home, names)
        {
            _t = t;
        }

        public override void Configure(CommandLineApplication command)
        {
            _asUser = AsUserOption.ConfigureOption(command);
            base.Configure(command);
        }

        protected async Task ProcessCollaborationsFromFile(string id, string path, BoxType t, bool json = false)
        {
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: this._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            if (!string.IsNullOrEmpty(path))
            {
                path = GeneralUtilities.TranslatePath(path);
            }
            try
            {
                var collaborationRequests = base.ReadFile<BoxCollaborationRequest, BoxCollaborationRequestMap>(path);
                foreach (var collaborationRequest in collaborationRequests)
                {
                    if (collaborationRequest.Item.Type != BoxType.file || collaborationRequest.Item.Type != BoxType.folder)
                    {
                        collaborationRequest.Item.Type = this._t;
                    }
                    var createdCollaboration = await boxClient.CollaborationsManager.AddCollaborationAsync(collaborationRequest);
                    this.PrintCollaboration(createdCollaboration, json);
                }
            }
            catch (Exception e)
            {
                Reporter.WriteError(e.Message);
            }
        }

        protected virtual void PrintCollaborations(BoxCollection<BoxCollaboration> collabs)
        {
            if (collabs == null || collabs.Entries.Count == 0)
            {
                Reporter.WriteInformation("This item has no collaborations.");
            }
            foreach (var collab in collabs.Entries)
            {
                base.PrintCollaboration(collab);
            }
        }

        protected virtual string ProcessRoleOptions(CommandOption editor, CommandOption viewer,
            CommandOption uploader, CommandOption previewerUploader, CommandOption viewerUploader,
            CommandOption coOwner, CommandOption previewer, CommandOption owner = null)
        {
            bool isOwnerRole = false;
            if (owner != null)
            {
                isOwnerRole = owner.HasValue();
            }
            var roles = new Dictionary<string, bool>()
                {
                    {BoxCollaborationRoles.Editor, editor.HasValue()},
                    {BoxCollaborationRoles.Viewer, viewer.HasValue()},
                    {BoxCollaborationRoles.Uploader, uploader.HasValue()},
                    {BoxCollaborationRoles.PreviewerUploader, previewerUploader.HasValue()},
                    {BoxCollaborationRoles.ViewerUploader, viewerUploader.HasValue()},
                    {BoxCollaborationRoles.CoOwner, coOwner.HasValue()},
                    {BoxCollaborationRoles.Owner, isOwnerRole},
                    {BoxCollaborationRoles.Previewer, previewer.HasValue()},
                };
            var result = roles.First(x => x.Value == true);
            return result.Key.ToLower();
        }
    }
}