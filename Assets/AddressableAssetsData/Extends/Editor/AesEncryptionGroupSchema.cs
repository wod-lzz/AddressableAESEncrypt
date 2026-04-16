using System.ComponentModel;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    [DisplayName("AES Encryption")]
    public class AesEncryptionGroupSchema : AddressableAssetGroupSchema
    {
        [SerializeField] private bool encryptBundle;

        public bool EncryptBundle
        {
            get { return encryptBundle; }
            set
            {
                if (encryptBundle == value)
                    return;

                encryptBundle = value;
                SetDirty(true);
            }
        }

        public override void OnGUI()
        {
            ShowAllProperties();
        }
    }
}