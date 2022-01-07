using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Win32.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RegistrySubSectionAttribute : Attribute { }
}