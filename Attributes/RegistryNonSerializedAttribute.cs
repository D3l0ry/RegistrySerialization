using System;

namespace Microsoft.Win32.Serialization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class RegistryNonSerializedAttribute : Attribute { }
}