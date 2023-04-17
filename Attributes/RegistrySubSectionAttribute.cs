using System;

namespace Microsoft.Win32.Registry.Serializations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RegistrySubSectionAttribute : Attribute { }
}