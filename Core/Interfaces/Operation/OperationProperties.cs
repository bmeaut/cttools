using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Core.Interfaces.Operation
{
    public abstract class OperationProperties : ICustomTypeDescriptor
    {
        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(new Attribute[0]);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var props = new List<PropertyDescriptor>();
            var properties = GetType().GetProperties();

            var attrArray = new Attribute[0];

            foreach (var property in properties)
            {
                // TODO: type checking
                props.Add(new CustomPropertyDescriptor(this, property, property.Name, attrArray));
            }

            return new PropertyDescriptorCollection(props.ToArray());
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(attributes, true);
        }
    }

    class CustomPropertyDescriptor : PropertyDescriptor
    {
        public object Instance { get; set; }

        public PropertyInfo Property { get; set; }

        public CustomPropertyDescriptor(
                object instance,
                PropertyInfo property,
                string name,
                Attribute[] attrs) : base(name, attrs)
        {
            Instance = instance;
            Property = property;
        }

        public override Type ComponentType
        {
            get { return Instance.GetType(); }
        }

        public override bool IsReadOnly
        {
            get { return false; }
        }

        public override Type PropertyType
        {
            get { return Property.PropertyType; }
        }

        public override bool CanResetValue(object component)
        {
            return (GetValue(component).Equals("") == false);
        }

        public override void ResetValue(object component)
        {
            SetValue(component, default);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            return Property.GetMethod.Invoke(Instance, null);
        }

        public override void SetValue(object component, object value)
        {
            // TODO: exception?
            if (PropertyType == value.GetType())
            {
                Property.SetMethod.Invoke(Instance, new object[] { value });
            }
        }
    }
}
