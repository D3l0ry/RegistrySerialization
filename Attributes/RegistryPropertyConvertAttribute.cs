using System;

namespace Microsoft.Win32.Registry.Serializations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegistryPropertyConvertAttribute : Attribute
    {
        internal Type ConvertType { get; private set; }

        public RegistryPropertyConvertAttribute(Type convertType)
        {
            if (convertType == null)
            {
                throw new ArgumentNullException(nameof(convertType));
            }

            ConvertType = convertType;
        }
    }
}