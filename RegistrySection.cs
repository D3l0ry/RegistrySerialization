using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Win32.Serialization
{
    internal class RegistrySection : IDisposable
    {
        private readonly Type _ObjectType;
        private readonly string _SectionName;
        private readonly RegistryKey _MainSection;
        private readonly PropertyInfo[] _ObjectProperties;

        public RegistrySection(Type sectionType)
        {
            if (sectionType == null)
            {
                throw new ArgumentNullException(nameof(sectionType));
            }

            if (!sectionType.IsDefined(typeof(RegistrySerializableAttribute)))
            {
                throw new NullReferenceException($"Аттрибут {nameof(RegistrySerializableAttribute)} не указан");
            }

            RegistrySerializableAttribute registrySerializable = sectionType.GetCustomAttribute<RegistrySerializableAttribute>();

            _ObjectType = sectionType;
            _MainSection = registrySerializable.MainSection;
            _SectionName = sectionType.Name;
            _ObjectProperties = GetProperties(sectionType);
        }

        public RegistrySection(Type sectionType, RegistryKey mainSection)
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
            _ObjectProperties = GetProperties(sectionType);
        }

        private RegistrySection(PropertyInfo property, RegistryKey mainSection) : this(property.PropertyType, mainSection)
        {
            _SectionName = property.Name;
        }

        private static PropertyInfo[] GetProperties(Type type)
        {
            PropertyInfo[] properties = type.GetProperties()
                .Where(currentProperty => !currentProperty.IsDefined(typeof(RegistryNonSerializedAttribute)))
                .ToArray();

            return properties;
        }

        private static object RegistryFieldValueConvert(PropertyInfo selectedProperty, object value)
        {
            RegistryPropertyConvertAttribute registryFieldConvert = selectedProperty.GetCustomAttribute<RegistryPropertyConvertAttribute>();

            if (registryFieldConvert == null)
            {
                return value;
            }

            Type[] constructorArgumentsType = new[]
            {
                registryFieldConvert.ConvertType
            };

            ConstructorInfo constructor = selectedProperty.PropertyType.GetConstructor(constructorArgumentsType);

            if (constructor == null)
            {
                throw new ArgumentException($"Конструктор с типом, указанным в {nameof(RegistryPropertyConvertAttribute)}, не найден");
            }

            return constructor.Invoke(new object[] { value });
        }

        public object GetSection()
        {
            RegistryKey currentSection = _MainSection.OpenSubKey(_SectionName);

            if (currentSection == null)
            {
                return null;
            }

            object newObject = Activator.CreateInstance(_ObjectType);

            foreach (PropertyInfo currentProperty in _ObjectProperties)
            {
                bool isSubSection = currentProperty.IsDefined(typeof(RegistrySubSectionAttribute));
                object registryValue;

                if (isSubSection)
                {
                    RegistrySection propertySection = new RegistrySection(currentProperty, currentSection);
                    registryValue = propertySection.GetSection();

                    propertySection.Dispose();
                }
                else
                {
                    registryValue = currentSection.GetValue(currentProperty.Name);

                    if (registryValue == null)
                    {
                        continue;
                    }
                }

                object value = RegistryFieldValueConvert(currentProperty, registryValue);

                currentProperty.SetValue(newObject, value);
            }

            currentSection.Dispose();

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

            RegistryKey currentSection = _MainSection.CreateSubKey(_SectionName);

            foreach (PropertyInfo currentProperty in _ObjectProperties)
            {
                object fieldValue = currentProperty.GetValue(value);
                bool isSubSection = currentProperty.IsDefined(typeof(RegistrySubSectionAttribute));

                if (isSubSection)
                {
                    if (currentProperty.PropertyType == valueType)
                    {
                        throw new InvalidCastException($"Подраздел реестра с типом {currentProperty} является родительским типом, который вызовет рекурсию");
                    }

                    RegistrySection sectionManager = new RegistrySection(currentProperty, currentSection);

                    sectionManager.Update(fieldValue);

                    continue;
                }

                currentSection.SetValue(currentProperty.Name, fieldValue);
            }

            currentSection.Dispose();
        }

        public void Dispose()
        {
            _MainSection.Close();
        }
    }
}