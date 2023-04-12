using System;

namespace Lemegeton.Core
{

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class AttributeOrderNumber : Attribute
    {

        private int _OrderNumber;
        public int OrderNumber
        {
            get
            {
                return _OrderNumber;
            }
            set
            {
                _OrderNumber = value;
            }
        }

        public AttributeOrderNumber(int num = 0)
        {
            OrderNumber = num;
        }

    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class DebugOption : Attribute
    {
    }

}
