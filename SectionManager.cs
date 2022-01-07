using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.Win32.Serialization
{
    public class SectionManager : IDisposable
    {
        private readonly Type mr_ObjectType;
        private readonly string mr_SectionName;

        private readonly Dictionary<FieldInfo, SectionManager> mr_ObjectFields;

        private readonly RegistryKey mr_MainSection;

        private readonly RegistryKey mr_CurrentSection;

        public SectionManager(Type sectionType, RegistryKey mainSection)
        {
            if(sectionType is null)
            {
                throw new ArgumentNullException(nameof(sectionType));
            }
            
            if(mainSection is null)
            {
                throw new ArgumentNullException(nameof(mainSection));
            }

            mr_ObjectType = sectionType;
            mr_MainSection = mainSection;

            mr_SectionName = mr_ObjectType.Name;

            mr_CurrentSection = mr_MainSection.CreateSubKey(mr_SectionName);

            mr_ObjectFields = GetObjectFields();
        }

        private SectionManager(string sectionName, Type sectionType, RegistryKey mainSection)
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
            mr_SectionName = sectionName;

            mr_MainSection = mainSection;

            mr_CurrentSection = mr_MainSection.CreateSubKey(mr_SectionName);

            mr_ObjectFields = GetObjectFields();
        }

        private Dictionary<FieldInfo, SectionManager> GetObjectFields()
        {
            Dictionary<FieldInfo, SectionManager> currentFields = new Dictionary<FieldInfo, SectionManager>();

            FieldInfo[] fields = mr_ObjectType.GetFields();

            foreach(FieldInfo currentField in fields)
            {
                bool isNonSerialized = currentField.CustomAttributes.Any(currentAttribute => currentAttribute.AttributeType == typeof(NonSerializedAttribute));

                if(isNonSerialized)
                {
                    continue;
                }

                if(currentField.FieldType == mr_ObjectType)
                {
                    throw new SystemException("Применение саб-секции с тем же типом, что и главная секция, вызовет рекурсию!");
                }

                bool isSubSection = currentField.CustomAttributes.Any(currentAttribute => currentAttribute.AttributeType == typeof(RegistrySubSectionAttribute));

                if (isSubSection)
                {
                    currentFields.Add(currentField,new SectionManager(currentField.Name, currentField.FieldType, mr_CurrentSection));

                    continue;
                }

                currentFields.Add(currentField, null);
            }

            return currentFields;
        }

        public void Update(object value)
        {
            if (value.GetType() != mr_ObjectType)
            {
                throw new ArgumentException("Данный тип не является настоящим типом секции!");
            }

            foreach(KeyValuePair<FieldInfo, SectionManager> currentFieldValuePair in mr_ObjectFields)
            {
                object fieldValue = currentFieldValuePair.Key.GetValue(value);

                if(fieldValue is null)
                {
                    continue;
                }

                if (currentFieldValuePair.Value is null)
                {
                    mr_CurrentSection.SetValue(currentFieldValuePair.Key.Name, fieldValue);

                    continue;
                }

                currentFieldValuePair.Value.Update(fieldValue);
            }

            mr_CurrentSection.Flush();
        }

        public SectionManager GetSubSection(string subSectonName)
        {
            if(string.IsNullOrWhiteSpace(subSectonName))
            {
                throw new ArgumentNullException(nameof(subSectonName));
            }

            return mr_ObjectFields.Values.First(currentSubSection => currentSubSection.mr_SectionName == subSectonName);
        }

        public void Dispose()
        {
            mr_MainSection.Close();
            mr_CurrentSection.Close();
        }
    }
}