using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Win32.Serialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegistryPropertyConvertAttribute : Attribute
    {
        internal Type ConvertType { get; private set; }

        public RegistryPropertyConvertAttribute(Type convertType)
        {
            if(convertType == null)
            {
                throw new ArgumentNullException(nameof(convertType));
            }

            ConvertType = convertType;
        }
    }
}