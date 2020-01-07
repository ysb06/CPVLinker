using System;

namespace CPVLinker.Tools.Ini
{
    public struct IniValue
    {
        private static bool TryParseInt(string text, out int value)
        {
            if (int.TryParse(text,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out int res))
            {
                value = res;
                return true;
            }
            value = 0;
            return false;
        }

        private static bool TryParseDouble(string text, out double value)
        {
            if (double.TryParse(text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double res))
            {
                value = res;
                return true;
            }
            value = double.NaN;
            return false;
        }

        public string Value;

        public IniValue(object value)
        {
            if (value is IFormattable formattable)
            {
                Value = formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                Value = value != null ? value.ToString() : null;
            }
        }

        public IniValue(string value)
        {
            Value = value;
        }

        public bool ToBool(bool valueIfInvalid = false)
        {
            if (TryConvertBool(out bool res))
            {
                return res;
            }
            return valueIfInvalid;
        }

        public bool TryConvertBool(out bool result)
        {
            if (Value == null)
            {
                result = default(bool);
                return false;
            }
            var boolStr = Value.Trim().ToLowerInvariant();
            if (boolStr == "true")
            {
                result = true;
                return true;
            }
            else if (boolStr == "false")
            {
                result = false;
                return true;
            }
            result = default(bool);
            return false;
        }

        public int ToInt(int valueIfInvalid = 0)
        {
            int res;
            if (TryConvertInt(out res))
            {
                return res;
            }
            return valueIfInvalid;
        }

        public bool TryConvertInt(out int result)
        {
            if (Value == null)
            {
                result = default(int);
                return false;
            }
            if (TryParseInt(Value.Trim(), out result))
            {
                return true;
            }
            return false;
        }

        public double ToDouble(double valueIfInvalid = 0)
        {
            double res;
            if (TryConvertDouble(out res))
            {
                return res;
            }
            return valueIfInvalid;
        }

        public bool TryConvertDouble(out double result)
        {
            if (Value == null)
            {
                result = default(double);
                return false; ;
            }
            if (TryParseDouble(Value.Trim(), out result))
            {
                return true;
            }
            return false;
        }

        public string GetString()
        {
            return GetString(true, false);
        }

        public string GetString(bool preserveWhitespace)
        {
            return GetString(true, preserveWhitespace);
        }

        public string GetString(bool allowOuterQuotes, bool preserveWhitespace)
        {
            if (Value == null)
            {
                return "";
            }
            var trimmed = Value.Trim();
            if (allowOuterQuotes && trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
            {
                var inner = trimmed.Substring(1, trimmed.Length - 2);
                return preserveWhitespace ? inner : inner.Trim();
            }
            else
            {
                return preserveWhitespace ? Value : Value.Trim();
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator IniValue(byte o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(short o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(int o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(sbyte o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(ushort o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(uint o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(float o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(double o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(bool o)
        {
            return new IniValue(o);
        }

        public static implicit operator IniValue(string o)
        {
            return new IniValue(o);
        }

        public static IniValue Default { get; } = new IniValue();
    }
}
