using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Box.V2.Models;
using BoxCLI.BoxHome;
using BoxCLI.BoxPlatform.Service;
using BoxCLI.CommandUtilities;
using BoxCLI.CommandUtilities.CommandModels;
using BoxCLI.CommandUtilities.CommandOptions;
using BoxCLI.CommandUtilities.CsvModels;
using BoxCLI.CommandUtilities.Globalization;
using CsvHelper;
using Microsoft.Extensions.CommandLineUtils;

namespace BoxCLI.Commands.MetadataSubCommands
{
    public class MetadataSubCommandBase : BoxBaseCommand
    {
        protected CommandOption _asUser;
        protected readonly BoxType _t;
        public MetadataSubCommandBase(IBoxPlatformServiceBuilder boxPlatformBuilder, IBoxHome home, LocalizedStringsResource names, BoxType t)
            : base(boxPlatformBuilder, home, names)
        {
            _t = t;
        }

        public override void Configure(CommandLineApplication command)
        {
            _asUser = AsUserOption.ConfigureOption(command);
            base.Configure(command);
        }

        protected virtual void CheckForScope(string scope, CommandLineApplication app)
        {
            if (string.IsNullOrEmpty(scope))
            {
                app.ShowHelp();
                throw new Exception("The scope of the metadata object is required for this command.");
            }
        }

        protected virtual void CheckForTemplate(string template, CommandLineApplication app)
        {
            if (string.IsNullOrEmpty(template))
            {
                app.ShowHelp();
                throw new Exception("The key of the template is required for this command.");
            }
        }

        protected virtual Dictionary<string, object> MetadataKeyValuesFromCommandOption(string option)
        {
            var keyVals = option.Split('&');
            var metadata = new Dictionary<string, object>();
            foreach (var keyVal in keyVals)
            {
                var splitKeyVal = keyVal.Split('=');
                if (splitKeyVal.Length == 2)
                {
                    // Check for series of digits, optional decimal portion, and "f" 
                    // signifying this is a "float" type.
                    var regex = new Regex(@"^[0-9]*\.?[0-9]*f{1}$");
                    if (regex.IsMatch(splitKeyVal[1]))
                    {
                        var parseNum = splitKeyVal[1].Substring(0, splitKeyVal[1].Length - 1);
                        metadata.Add(splitKeyVal[0], Decimal.Parse(parseNum));
                    }
                    else
                    {
                        metadata.Add(splitKeyVal[0], splitKeyVal[1]);
                    }
                }
            }
            return metadata;
        }

        protected virtual Dictionary<string, object> MetadataKeyValuesFromConsole()
        {
            var q = "";
            var key = "";
            var val = "";
            var md = new Dictionary<string, object>();
            do
            {
                if (md.Count > 0)
                {
                    Reporter.WriteInformation("Current metadata object:");
                    foreach (var k in md.Keys)
                    {
                        Reporter.WriteInformation($"Key: {k} - Value: {md[k]}");
                    }
                }
                Reporter.WriteInformation("Enter the metadata key:");
                key = Console.ReadLine();
                Reporter.WriteInformation("Enter the metadata value:");
                val = Console.ReadLine();
                Reporter.WriteInformation("Is metadata value a float? y/N");
                var yesNoVal = "n";
                yesNoVal = Console.ReadLine();
                if (yesNoVal == "y")
                {
                    md.Add(key, Decimal.Parse(val));
                }
                else
                {
                    md.Add(key, val);
                }
                Reporter.WriteInformation("Enter to continue, q to quit.");
                q = Console.ReadLine().ToLower();
            }
            while (q != "q");
            Reporter.WriteSuccess("Finished building metadata.");
            return md;
        }

        protected virtual void PrintMetadataCollection(BoxMetadataTemplateCollection<Dictionary<string, object>> mdt)
        {
            foreach (var md in mdt.Entries)
            {
                this.PrintMetadata(md);
            }
        }
        protected virtual void PrintMetadata(Dictionary<string, object> md, bool json)
        {
            if (json)
            {
                base.OutputJson(md);
                return;
            }
            else
            {
                this.PrintMetadata(md);
            }
        }
        protected virtual void PrintMetadata(Dictionary<string, object> md)
        {
            foreach (var key in md.Keys)
            {
                Reporter.WriteInformation($"{key}: {md[key]}");
            }
        }

        protected virtual List<BoxMetadataForCsv> ReadMetadataCsvFile(string filePath)
        {
            var allMetadataOnItem = new List<BoxMetadataForCsv>();
            using (var fs = File.OpenText(filePath))
            using (var csv = new CsvReader(fs))
            {
                csv.Configuration.RegisterClassMap(typeof(BoxMetadataRequestMap));
                allMetadataOnItem = csv.GetRecords<BoxMetadataForCsv>().ToList();
            }

            return allMetadataOnItem;
        }

        protected async virtual Task AddMetadataToItemFromFile(string path, string type = "",
            bool save = false, string overrideSavePath = "", string overrideSaveFileFormat = "", bool json = false)
        {
            var boxClient = base.ConfigureBoxClient(oneCallAsUserId: this._asUser.Value(), oneCallWithToken: base._oneUseToken.Value());
            if (!string.IsNullOrEmpty(path))
            {
                path = GeneralUtilities.TranslatePath(path);
            }
            try
            {
                var metadataRequests = this.ReadMetadataCsvFile(path);
                var saveCreated = new List<BoxMetadataForCsv>();

                foreach (var metadataRequest in metadataRequests)
                {
                    Dictionary<string, object> createdMetadata = null;
                    if (metadataRequest.ItemType != null)
                    {
                        type = metadataRequest.ItemType;
                    }
                    else
                    {
                        throw new Exception("Must have a Box Item type of file or folder");
                    }
                    try
                    {
                        if (type == "file")
                        {
                            createdMetadata = await boxClient.MetadataManager.CreateFileMetadataAsync(metadataRequest.ItemId, metadataRequest.Metadata, metadataRequest.Scope, metadataRequest.TemplateKey);
                        }
                        else if (type == "folder")
                        {
                            createdMetadata = await boxClient.MetadataManager.CreateFolderMetadataAsync(metadataRequest.ItemId, metadataRequest.Metadata, metadataRequest.Scope, metadataRequest.TemplateKey);
                        }
                        else
                        {
                            throw new Exception("Metadata currently only supported on files and folders.");
                        }
                    }
                    catch (Exception e)
                    {
                        Reporter.WriteError("Couldn't add metadata...");
                        Reporter.WriteError(e.Message);
                    }
                    if (createdMetadata != null)
                    {
                        this.PrintMetadata(createdMetadata, json);
                        if (save || !string.IsNullOrEmpty(overrideSavePath) || base._settings.GetAutoSaveSetting())
                        {
                            saveCreated.Add(new BoxMetadataForCsv()
                            {
                                TemplateKey = metadataRequest.TemplateKey,
                                Scope = metadataRequest.Scope,
                                ItemId = metadataRequest.ItemId,
                                ItemType = metadataRequest.ItemType,
                                Metadata = createdMetadata
                            });
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
                    var fileName = $"{base._names.CommandNames.Metadata}-{base._names.SubCommandNames.Create}-{DateTime.Now.ToString(GeneralUtilities.GetDateFormatString())}";
                    base.WriteMetadataCollectionResultsToReport(saveCreated, fileName, savePath, fileFormat);
                }
            }
            catch (Exception e)
            {
                Reporter.WriteError(e.Message);
            }
        }


        //protected virtual bool ProcessMetadataTemplates(string path, string asUser = "",
        //    bool save = false, string overrideSavePath = "", string overrideSaveFileFormat = "")
        //{
        //    var boxClient = base.ConfigureBoxClient(asUser);
        //    if (!string.IsNullOrEmpty(path))
        //    {
        //        path = GeneralUtilities.TranslatePath(path);
        //    }

        //    try
        //    {
        //        Reporter.WriteInformation("Reading file...");
        //        var metadataTemplateRequests = base.ReadFile<BoxMetadataTemplate, Dictionary<string, object>>(path);
        //        List<BoxWebhook> saveUpdated = new List<BoxWebhook>();

        //        foreach (var webhookRequest in webhookRequests)
        //        {
        //            Reporter.WriteInformation($"Processing a webhook request: {webhookRequest.Address}");
        //            BoxWebhook updatedWebhook = null;
        //            try
        //            {
        //                updatedWebhook = await boxClient.WebhooksManager.UpdateWebhookAsync(webhookRequest);
        //            }
        //            catch (Exception e)
        //            {
        //                Reporter.WriteError("Couldn't update webhook...");
        //                Reporter.WriteError(e.Message);
        //            }
        //            Reporter.WriteSuccess("Updated a webhook:");
        //            if (updatedWebhook != null)
        //            {
        //                this.PrintWebhook(updatedWebhook);
        //                if (save || !string.IsNullOrEmpty(overrideSavePath) || base._settings.GetAutoSaveSetting())
        //                {
        //                    saveUpdated.Add(updatedWebhook);
        //                }
        //            }
        //        }
        //        Reporter.WriteInformation("Finished processing webhooks...");
        //        if (save || !string.IsNullOrEmpty(overrideSavePath) || base._settings.GetAutoSaveSetting())
        //        {
        //            var fileFormat = base._settings.GetBoxReportsFileFormatSetting();
        //            if (!string.IsNullOrEmpty(overrideSaveFileFormat))
        //            {
        //                fileFormat = overrideSaveFileFormat;
        //            }
        //            var savePath = base._settings.GetBoxReportsFolderPath();
        //            if (!string.IsNullOrEmpty(overrideSavePath))
        //            {
        //                savePath = overrideSavePath;
        //            }
        //            var fileName = $"{base._names.CommandNames.Webhooks}-{base._names.SubCommandNames.Update}-{DateTime.Now.ToString(GeneralUtilities.GetDateFormatString())}";
        //            base.WriteListResultsToReport<BoxWebhook, BoxWebhookMap>(saveUpdated, fileName, savePath, fileFormat);
        //        }
        //    }
        //    catch
        //    {

        //    }

        //    return true;
        //}

    }
}