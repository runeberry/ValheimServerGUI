using System.Collections.Generic;

namespace ValheimServerGUI.Tools
{
    public static class ObjectExtensions
    {
        public static bool IsAnyValue<T>(this T value, params T[] allowedValues)
        {
            if (allowedValues == null || allowedValues.Length == 0) return false;

            foreach (var allowedValue in allowedValues)
            {
                if (EqualityComparer<T>.Default.Equals(value, allowedValue))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
