namespace SimpleLicense.Core.Utils
{

    internal static class TypeChecking
    {
        /// <summary>
        /// Check if the given object is of a numeric type
        /// </summary>
        /// <note>
        /// Implicit conversion to double for most numeric types
        /// Explicit conversion for decimal to double
        /// </note>
        /// <param name="value"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public static bool IsNumeric(object? value, out double number)
        {
            switch (value)
            {
                case byte b: number = b; return true;
                case sbyte sb: number = sb; return true;
                case short s: number = s; return true;
                case ushort us: number = us; return true;
                case int i: number = i; return true;
                case uint ui: number = ui; return true;
                case long l: number = l; return true;
                case ulong ul: number = ul; return true;
                case float f: number = f; return true;
                case double d: number = d; return true;
                case decimal dec: number = (double)dec; return true;
            }
            number = 0; return false;
        }
        
        /// <summary>
        /// Get a string description of the type of the given object
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DescribeType(object? value)
        {
            return value is null ? "null" : value.GetType().Name;
        }
    }
}
