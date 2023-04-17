using System;

namespace Microsoft.Win32.Registry.Serializations
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RegistrySerializableAttribute : Attribute
    {
        internal RegistryKey MainSection { get; private set; }

        public RegistrySerializableAttribute(RegistryHive registryHive) => MainSection = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default);

        public RegistrySerializableAttribute(RegistryHive registryHive, string mainSubSection)
        {
            if (string.IsNullOrWhiteSpace(mainSubSection))
            {
                throw new ArgumentNullException(nameof(mainSubSection));
            }

            RegistryKey mainRegistrySection = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default);
            RegistryKey mainRegistrySubSection = mainRegistrySection.CreateSubKey(mainSubSection);

            MainSection = mainRegistrySubSection;
        }
    }
}