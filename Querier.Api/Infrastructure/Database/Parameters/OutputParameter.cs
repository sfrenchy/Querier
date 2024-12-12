using System;

namespace Querier.Api.Tools
{
    public class OutputParameter<TValue>
    {
        private TValue? _value;
        private bool _valueSet = false;

        public TValue Value
        {
            get
            {
                if (!_valueSet)
                    throw new InvalidOperationException("Value not set.");

                return _value;
            }
        }

        public void SetValue(object value)
        {
            _valueSet = true;

            _value = null == value || Convert.IsDBNull(value) ? default : (TValue)value;
        }
    }
}
