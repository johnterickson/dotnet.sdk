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
}
