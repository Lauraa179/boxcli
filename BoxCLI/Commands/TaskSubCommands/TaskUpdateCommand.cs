﻿using System;
using System.Threading.Tasks;
using Box.V2.Models;
using BoxCLI.BoxHome;
using BoxCLI.BoxPlatform.Service;
using BoxCLI.CommandUtilities;
using BoxCLI.CommandUtilities.Globalization;
using Microsoft.Extensions.CommandLineUtils;

namespace BoxCLI.Commands.TaskSubCommands
{
    public class TaskUpdateCommand : TaskSubCommandBase
    {
        private CommandArgument _taskId;
        private CommandOption _message;
        private CommandOption _due;
        private CommandLineApplication _app;
        private IBoxHome _home;

        public TaskUpdateCommand(IBoxPlatformServiceBuilder boxPlatformBuilder, IBoxHome home, LocalizedStringsResource names)
            : base(boxPlatformBuilder, home, names)
        {
            _home = home;
        }

        public override void Configure(CommandLineApplication command)
        {
            _app = command;
            command.Description = "Update a task on a file.";
            _taskId = command.Argument("taskId",
                                   "Id of task");
            _message = command.Option("--message", "Message for task.", CommandOptionType.SingleValue);
            _due = command.Option("--due-at", "When this task is due, use format 05h for 5 hours for example.", CommandOptionType.SingleValue);
            command.OnExecute(async () =>
            {
                return await this.Execute();
            });
            base.Configure(command);
        }

        protected async override Task<int> Execute()
        {
            await this.RunUpdate();
            return await base.Execute();
        }

        private async Task RunUpdate()
        {
            base.CheckForValue(this._taskId.Value, this._app, "A task ID is required for this command");
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: base._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            var taskRequest = new BoxTaskUpdateRequest();
            if (this._message.HasValue())
            {
                taskRequest.Message = this._message.Value();
            }
            if (this._due.HasValue())
            {
                taskRequest.DueAt = GeneralUtilities.GetDateTimeFromString(this._due.Value());
            }
            var task = await boxClient.TasksManager.UpdateTaskAsync(taskRequest);
            if (base._json.HasValue() || this._home.GetBoxHomeSettings().GetOutputJsonSetting())
            {
                base.OutputJson(task);
                return;
            }
            base.PrintTask(task);
        }
    }
}
