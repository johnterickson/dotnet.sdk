// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Security.Cryptography;
using System.Xml;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.StaticWebAssets.Tasks;

public class GenerateStaticWebAssetEndpointsPropsFile : Task
{
    public class GenerateStaticWebAssetEndpointsPropsFile : Task, ITaskHybrid
    {
        [Required]
        [Output]
        [PrecomputeOutput]
        public ITaskItem TargetPropsFilePath { get; set; }

        public string PackagePathPrefix { get; set; } = "staticwebassets";

        [Required]
        public ITaskItem[] StaticWebAssets { get; set; }

        [Required]
        public ITaskItem[] StaticWebAssetEndpoints { get; set; }

        public bool ExecuteStatic() => true;

        public override bool Execute()
        {
            return false;
        }

        return ExecuteCore(endpoints, assets);
    }

    private bool ExecuteCore(StaticWebAssetEndpoint[] endpoints, Dictionary<string, StaticWebAsset> assets)
    {
        if (endpoints.Length == 0)
        {
            return !Log.HasLoggedErrors;
        }

        private void WriteFile(byte[] data)
        {
            var dataHash = ComputeHash(data);
            var fileExists = File.Exists(TargetPropsFilePath.ItemSpec);
            var existingFileHash = fileExists ? ComputeHash(File.ReadAllBytes(TargetPropsFilePath.ItemSpec)) : "";

            if (!fileExists)
            {
                Log.LogMessage(MessageImportance.Low, $"Creating file '{TargetPropsFilePath.ItemSpec}' does not exist.");
                File.WriteAllBytes(TargetPropsFilePath.ItemSpec, data);
            }
            else if (!string.Equals(dataHash, existingFileHash, StringComparison.Ordinal))
            {
                Log.LogMessage(MessageImportance.Low, $"Updating '{TargetPropsFilePath.ItemSpec}' file because the hash '{dataHash}' is different from existing file hash '{existingFileHash}'.");
                File.WriteAllBytes(TargetPropsFilePath.ItemSpec, data);
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, $"Skipping file update because the hash '{dataHash}' has not changed.");
            }
        }

        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
        var root = new XElement("Project", itemGroup);

        document.Add(root);

        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            CloseOutput = false,
            OmitXmlDeclaration = true,
            Indent = true,
            NewLineOnAttributes = false,
            Async = true
        };

        using var memoryStream = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
        {
            document.WriteTo(xmlWriter);
        }

        var data = memoryStream.ToArray();
        WriteFile(data);

        return !Log.HasLoggedErrors;
    }

    private void WriteFile(byte[] data)
    {
        var dataHash = ComputeHash(data);
        var fileExists = File.Exists(TargetPropsFilePath);
        var existingFileHash = fileExists ? ComputeHash(File.ReadAllBytes(TargetPropsFilePath)) : "";

        if (!fileExists)
        {
            Log.LogMessage(MessageImportance.Low, $"Creating file '{TargetPropsFilePath}' does not exist.");
            File.WriteAllBytes(TargetPropsFilePath, data);
        }
        else if (!string.Equals(dataHash, existingFileHash, StringComparison.Ordinal))
        {
            Log.LogMessage(MessageImportance.Low, $"Updating '{TargetPropsFilePath}' file because the hash '{dataHash}' is different from existing file hash '{existingFileHash}'.");
            File.WriteAllBytes(TargetPropsFilePath, data);
        }
        else
        {
            Log.LogMessage(MessageImportance.Low, $"Skipping file update because the hash '{dataHash}' has not changed.");
        }
    }

    private static string ComputeHash(byte[] data)
    {
#if !NET9_0_OR_GREATER
        using var sha256 = SHA256.Create();
        var result = sha256.ComputeHash(data);
#else
        var result = SHA256.HashData(data);
#endif
        return Convert.ToBase64String(result);
    }

    private bool ValidateArguments(StaticWebAssetEndpoint[] endpoints, Dictionary<string, StaticWebAsset> asset)
    {
        var valid = true;
        foreach (var endpoint in endpoints)
        {
            if (!asset.ContainsKey(endpoint.AssetFile))
            {
                Log.LogError($"The asset file '{endpoint.AssetFile}' specified in the endpoint '{endpoint.Route}' does not exist.");
                valid = false;
            }
        }

        return valid;
    }
}
