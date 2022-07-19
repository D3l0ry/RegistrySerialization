using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace Microsoft.Win32.Serialization
{
    internal class SectionManager : IDisposable
    {
        private readonly Type mr_ObjectType;
        private readonly string mr_SectionName;

        private readonly RegistryKey mr_MainSection;
        private RegistryKey m_CurrentSection;

        private readonly PropertyInfo[] mr_ObjectFields;

        public SectionManager(Type sectionType, RegistryKey mainSection)
        {
            if (sectionType is null)
            {
                throw new ArgumentNullException(nameof(sectionType));
            }

            if (mainSection is null)
            {
                throw new ArgumentNullException(nameof(mainSection));
            }

            mr_ObjectType = sectionType;
            mr_MainSection = mainSection;

            mr_SectionName = sectionType.Name;

            mr_ObjectFields = sectionType
                .GetProperties()
                .Where(currentField => !currentField.CustomAttributes.Any(currentAttribute => currentAttribute.AttributeType == typeof(NonSerializedAttribute)))
                .ToArray();
        }

        private SectionManager(string sectionName, Type sectionType, RegistryKey mainSection) : this(sectionType, mainSection) => mr_SectionName = sectionName;

        public object GetSection()
        {
            m_CurrentSection = mr_MainSection.OpenSubKey(mr_SectionName);

            if(m_CurrentSection is null)
            {
                return null;
            }

            object newObject = Activator.CreateInstance(mr_ObjectType);

            foreach (PropertyInfo currentProperty in mr_ObjectFields)
            {
                bool isSubSection = currentProperty.CustomAttributes.Any(currentAttribute => currentAttribute.AttributeType == typeof(RegistrySubSectionAttribute));

                if (isSubSection)
                {
                    SectionManager sectionManager = new SectionManager(currentProperty.Name, currentProperty.PropertyType, m_CurrentSection);

                    object subSectionObject = sectionManager.GetSection();

                    currentProperty.SetValue(newObject, subSectionObject);

                    continue;
                }

                object registryFieldValue = m_CurrentSection.GetValue(currentProperty.Name);

                if (registryFieldValue is null)
                {
                    continue;
                }

                currentProperty.SetValue(newObject, registryFieldValue);
            }

            return newObject;
        }

        public void Update(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Type valueType = value.GetType();

            if (valueType != mr_ObjectType)
            {
                throw new ArgumentException("Данный тип не соответствует типу секции");
            }

            m_CurrentSection = mr_MainSection.CreateSubKey(mr_SectionName);

            foreach (PropertyInfo currentProperty in mr_ObjectFields)
            {
                object fieldValue = currentProperty.GetValue(value);

                if (fieldValue is null)
                {
                    continue;
                }

                bool isSubSection = currentProperty.CustomAttributes.Any(currentAttribute => currentAttribute.AttributeType == typeof(RegistrySubSectionAttribute));

                if (isSubSection)
                {
                    if(currentProperty.PropertyType == valueType)
                    {
                        throw new InvalidCastException($"Подраздел реестра с типом {currentProperty} является родительским типом, который вызовет рекурсию");
                    }

                    SectionManager fieldSectionManager = new SectionManager(currentProperty.Name, currentProperty.PropertyType, m_CurrentSection);

                    fieldSectionManager.Update(fieldValue);

                    continue;
                }

                m_CurrentSection.SetValue(currentProperty.Name, fieldValue);
            }
        }

        public void Dispose()
        {
            mr_MainSection.Close();
            m_CurrentSection.Close();
        }
    }
}