using System;

namespace Microsoft.Win32.Serialization
{
    public class RegistrySerializer<T> : IDisposable
    {
        private readonly SectionManager mr_MainSectionManager;

        public RegistrySerializer(RegistryKey mainSection)
        {
            Type objectType = typeof(T);

            if (mainSection is null)
            {
                throw new ArgumentNullException(nameof(mainSection));
            }

            mr_MainSectionManager = new SectionManager(objectType, mainSection);
        }

        public void Serialize(T value) => mr_MainSectionManager.Update(value);

        public T Deserialize() => (T)mr_MainSectionManager.GetSection();

        public void Dispose() => mr_MainSectionManager.Dispose();
    }
}