using System;

namespace Microsoft.Win32.Registry.Serializations
{
    public class RegistrySerializer<T> : IDisposable where T : class
    {
        private readonly RegistrySection _MainSection;

        public RegistrySerializer()
        {
            Type objectType = typeof(T);

            _MainSection = new RegistrySection(objectType);
        }

        public RegistrySerializer(RegistryKey mainSection)
        {
            Type objectType = typeof(T);

            if (mainSection == null)
            {
                throw new ArgumentNullException(nameof(mainSection));
            }

            _MainSection = new RegistrySection(objectType, mainSection);
        }

        public void Serialize(T value) => _MainSection.Update(value);

        public T? Deserialize() => (T?)_MainSection.GetSection();

        public void Dispose() => _MainSection.Dispose();
    }
}