using System.Globalization;

namespace Lemegeton.Core
{

    internal class Percentage
    {

        private float _CurrentValue = 0.0f;
        public float CurrentValue
        {
            get
            {
                return _CurrentValue;
            }
            set
            {
                _CurrentValue = value;
                Validate();
            }
        }

        private float _MinValue = 0.0f;
        internal float MinValue
        {
            get
            {
                return _MinValue;
            }
            set
            {
                _MinValue = value;
                Validate();
            }
        }

        internal float _MaxValue = 100.0f;
        internal float MaxValue
        {
            get
            {
                return _MaxValue;
            }
            set
            {
                _MaxValue = value;
                Validate();
            }
        }

        private void Validate()
        {
            if (_CurrentValue < MinValue)
            {
                _CurrentValue = MinValue;
            }
            if (_CurrentValue > MaxValue)
            {
                _CurrentValue = MaxValue;
            }
        }

        public string Serialize()
        {
            return _CurrentValue.ToString(CultureInfo.InvariantCulture);
        }

        public void Deserialize(string data)
        {
            float.TryParse(data, out _CurrentValue);
            Validate();
        }

    }

}
