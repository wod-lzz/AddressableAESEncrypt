using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    [CreateAssetMenu(fileName = "BuildScriptPackedAESMode.asset", menuName = "Addressables/Content Builders/AES Build Script")]
    public class BuildScriptPackedModeAES : BuildScriptPackedMode
    {
        [SerializeField] private bool encryptAllBundles = true;

        public override string Name
        {
            get { return "AES Build Script"; }
        }

        protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
        {
            var results =  base.DoBuild<TResult>(builderInput, aaContext);
            var targetFiles = builderInput.Registry.GetFilePaths();

            if (targetFiles == null)
                return results;

            foreach (var targetPath in targetFiles)
            {
                if (!targetPath.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!ShouldEncryptBundle(targetPath, aaContext))
                    continue;

                EncryptBundleWithAES(targetPath);
            }

            return results;
        }

        private bool ShouldEncryptBundle(string bundlePath, AddressableAssetsBuildContext aaContext)
        {
            if (encryptAllBundles)
                return true;

            string bundleName = Path.GetFileNameWithoutExtension(bundlePath);
            if (string.IsNullOrEmpty(bundleName) || aaContext == null || aaContext.bundleToAssetGroup == null)
                return false;

            if (!aaContext.bundleToAssetGroup.TryGetValue(bundleName, out AddressableAssetGroup group) || group == null)
                return false;

            var schema = group.GetSchema<AesEncryptionGroupSchema>();
            return schema != null && schema.EncryptBundle;
        }

        private void EncryptBundleWithAES(string bundlePath)
        {
            SeekableAesStream.TransformFileHeaderInPlace(bundlePath);
        }
    }
}

