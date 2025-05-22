// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;

namespace Microsoft.AspNetCore.StaticWebAssets.Tasks
{
    public class ResolveStaticWebAssetFingerprints : Task, ITaskHybrid
    {
        [Required]
        [Output]
        [PrecomputeInput]
        public ITaskItem[] Assets { get; set; }

        public bool ExecuteStatic() => true;

        public override bool Execute()
        {
            try
            {
                Assets = Assets
                    .Select(StaticWebAsset.FromTaskItem)
                    .Select(t => { t.ResolveFingerprintAndIntegrity(); return t; })
                    .Select(t => t.ToTaskItem())
                    .ToArray();
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
            }

            return !Log.HasLoggedErrors;
        }
    }

    public class ResolveStaticWebAssetFingerprintsUsingResolved : Task
    {
        [Required]
        [Output]
        public ITaskItem[] Assets { get; set; }
        
        [Required]
        public ITaskItem[] ResolvedAssets { get; set; }

        public bool ExecuteStatic() => true;

        public override bool Execute()
        {
            try
            {
                var lookup = ResolvedAssets.Select(StaticWebAsset.FromTaskItem).ToDictionary(t => t.Identity, t => t, OSPath.PathComparer);

                Assets = Assets
                    .Select(StaticWebAsset.FromTaskItem)
                    .Select(t =>
                    {
                        var value = lookup[t.Identity];
                        t.ResolveFingerprintAndIntegrity(value.Fingerprint, value.Integrity);
                        return t;
                    })
                    .Select(t => t.ToTaskItem())
                    .ToArray();
            }
            catch (Exception ex)
            {
                Log.LogError(ex.ToString());
            }

            return !Log.HasLoggedErrors;
        }
    }
}
