using System;

namespace Microsoft.Win32.Serialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegistryNonSerializedAttribute : Attribute { }
}