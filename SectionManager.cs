using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Win32.Serialization
{
    internal class SectionManager : IDisposable
    {
        private readonly Type _ObjectType;
        private readonly string _SectionName;
        private readonly RegistryKey _MainSection;
        private readonly PropertyInfo[] _ObjectFields;
        private RegistryKey _CurrentSection;

        public SectionManager(Type sectionType, RegistryKey mainSection)
        {
            if (sectionType == null)
            {
                throw new ArgumentNullException(nameof(sectionType));
            }

            if (mainSection == null)
            {
                throw new ArgumentNullException(nameof(mainSection));
            }

            _ObjectType = sectionType;
            _MainSection = mainSection;
            _SectionName = sectionType.Name;
            _ObjectFields = GetProperties(sectionType);
        }

        private SectionManager(PropertyInfo property, RegistryKey mainSection) : this(property.PropertyType, mainSection) => _SectionName = property.Name;

        private static PropertyInfo[] GetProperties(Type type)
        {
            PropertyInfo[] properties = type.GetProperties()
                .Where(currentProperty => !currentProperty.IsDefined(typeof(RegistryNonSerializedAttribute)))
                .ToArray();

            return properties;
        }

        private static SectionManager CreateSection(PropertyInfo property, RegistryKey mainSection) => new SectionManager(property, mainSection);

        public object GetSection()
        {
            _CurrentSection = _MainSection.OpenSubKey(_SectionName);

            if (_CurrentSection == null)
            {
                return null;
            }

            object newObject = Activator.CreateInstance(_ObjectType);

            foreach (PropertyInfo currentProperty in _ObjectFields)
            {
                bool isSubSection = currentProperty.IsDefined(typeof(RegistrySubSectionAttribute));
                object registryValue;

                if (isSubSection)
                {
                    SectionManager sectionManager = CreateSection(currentProperty, _CurrentSection);
                    registryValue = sectionManager.GetSection();

                    sectionManager.Dispose();
                }
                else
                {
                    registryValue = _CurrentSection.GetValue(currentProperty.Name);

                    if (registryValue == null)
                    {
                        continue;
                    }
                }

                currentProperty.SetValue(newObject, registryValue);
            }

            return newObject;
        }

        public void Update(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type valueType = value.GetType();

            if (valueType != _ObjectType)
            {
                throw new ArgumentException("Данный тип не соответствует типу секции");
            }

            _CurrentSection = _MainSection.CreateSubKey(_SectionName);

            foreach (PropertyInfo currentProperty in _ObjectFields)
            {
                object fieldValue = currentProperty.GetValue(value);
                bool isSubSection = currentProperty.IsDefined(typeof(RegistrySubSectionAttribute));

                if (isSubSection)
                {
                    if (currentProperty.PropertyType == valueType)
                    {
                        throw new InvalidCastException($"Подраздел реестра с типом {currentProperty} является родительским типом, который вызовет рекурсию");
                    }

                    SectionManager sectionManager = CreateSection(currentProperty, _CurrentSection);

                    sectionManager.Update(fieldValue);

                    continue;
                }

                _CurrentSection.SetValue(currentProperty.Name, fieldValue);
            }
        }

        public void Dispose()
        {
            _MainSection.Close();
            _CurrentSection.Close();
        }
    }
}