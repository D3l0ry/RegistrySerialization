using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Win32.Registry.Serializations
{
    internal class RegistrySection : IDisposable
    {
        private readonly Type _objectType;
        private readonly string _sectionName;
        private readonly RegistryKey _mainSection;
        private readonly PropertyInfo[] _objectProperties;

        public RegistrySection(Type sectionType)
        {
            if (sectionType == null)
            {
                throw new ArgumentNullException(nameof(sectionType));
            }

            RegistrySerializableAttribute? registrySerializable = sectionType.GetCustomAttribute<RegistrySerializableAttribute>();

            if (registrySerializable == null)
            {
                throw new NullReferenceException($"Аттрибут {nameof(RegistrySerializableAttribute)} не указан");
            }

            _objectType = sectionType;
            _mainSection = registrySerializable.MainSection;
            _sectionName = sectionType.Name;
            _objectProperties = GetProperties(sectionType);
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

            _objectType = sectionType;
            _mainSection = mainSection;
            _sectionName = sectionType.Name;
            _objectProperties = GetProperties(sectionType);
        }

        private RegistrySection(PropertyInfo property, RegistryKey mainSection) : this(property.PropertyType, mainSection)
        {
            _sectionName = property.Name;
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
            RegistryPropertyConvertAttribute? registryFieldConvert = selectedProperty.GetCustomAttribute<RegistryPropertyConvertAttribute>();

            if (registryFieldConvert == null)
            {
                return value;
            }

            Type[] constructorArgumentsType = new[]
            {
            registryFieldConvert.ConvertType
        };

            ConstructorInfo? constructor = selectedProperty.PropertyType.GetConstructor(constructorArgumentsType);

            if (constructor == null)
            {
                throw new ArgumentException($"Конструктор с типом, указанным в {nameof(RegistryPropertyConvertAttribute)}, не найден");
            }

            return constructor.Invoke(new object[] { value });
        }

        public object? GetSection()
        {
            using RegistryKey? currentSection = _mainSection.OpenSubKey(_sectionName);

            if (currentSection == null)
            {
                return null;
            }

            object? newObject = Activator.CreateInstance(_objectType);

            foreach (PropertyInfo currentProperty in _objectProperties)
            {
                bool isSubSection = currentProperty.PropertyType.IsDefined(typeof(RegistrySerializableAttribute));
                object? registryValue;

                if (isSubSection)
                {
                    using RegistrySection propertySection = new RegistrySection(currentProperty, currentSection);

                    registryValue = propertySection.GetSection();
                }
                else
                {
                    registryValue = currentSection.GetValue(currentProperty.Name);

                    if (registryValue == null)
                    {
                        continue;
                    }
                }

                object value = RegistryFieldValueConvert(currentProperty, registryValue!);

                currentProperty.SetValue(newObject, value);
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

            if (valueType != _objectType)
            {
                throw new ArgumentException("Тип значения не соответствует заданному типу");
            }

            RegistryKey currentSection = _mainSection.CreateSubKey(_sectionName);

            foreach (PropertyInfo currentProperty in _objectProperties)
            {
                object? fieldValue = currentProperty.GetValue(value);
                bool isSubSection = currentProperty.IsDefined(typeof(RegistrySubSectionAttribute));

                if (isSubSection)
                {
                    if (fieldValue == null)
                    {
                        continue;
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
            _mainSection.Close();
        }
    }
}