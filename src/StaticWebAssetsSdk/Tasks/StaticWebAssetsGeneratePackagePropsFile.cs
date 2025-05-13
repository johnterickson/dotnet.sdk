// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Xml;
using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.StaticWebAssets.Tasks
{
    public class StaticWebAssetsGeneratePackagePropsFile : Task, ITaskHybrid
    {
        [Required]
        public string PropsFileImport { get; set; }

        public ITaskItem[] AdditionalImports { get; set; }

        [Required]
        [Output]
        [PrecomputeOutput]
        public ITaskItem BuildTargetPath { get; set; }

        public bool ExecuteStatic() => true;

        public override bool Execute()
        {
            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            var elements = (AdditionalImports ?? []).Select(e => e.ItemSpec).Prepend(PropsFileImport)
                .OrderBy(id => id, StringComparer.Ordinal);
            var root = new XElement("Project");
            foreach (var element in elements)
            {
                root.Add(new XElement("Import", new XAttribute("Project", element)));
            }

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
            var fileExists = File.Exists(BuildTargetPath.ItemSpec);
            var existingFileHash = fileExists ? ComputeHash(File.ReadAllBytes(BuildTargetPath.ItemSpec)) : "";

            if (!fileExists)
            {
                Log.LogMessage(MessageImportance.Low, $"Creating file '{BuildTargetPath.ItemSpec}' does not exist.");
                File.WriteAllBytes(BuildTargetPath.ItemSpec, data);
            }
            else if (!string.Equals(dataHash, existingFileHash, StringComparison.Ordinal))
            {
                Log.LogMessage(MessageImportance.Low, $"Updating '{BuildTargetPath.ItemSpec}' file because the hash '{dataHash}' is different from existing file hash '{existingFileHash}'.");
                File.WriteAllBytes(BuildTargetPath.ItemSpec, data);
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, $"Skipping file update because the hash '{dataHash}' has not changed.");
            }
        }

        private static string ComputeHash(byte[] data)
        {
            using var sha256 = SHA256.Create();

            var result = sha256.ComputeHash(data);
            return Convert.ToBase64String(result);
        }
    }
}
