using System.Text.RegularExpressions;

namespace Common.Helpers
{
    /// <summary>
    /// Provides helper methods for converting enum-related identifiers into readable text.
    /// </summary>
    public static class EnumToStringHelper
    {
        /// <summary>
        /// Converts an enum type name from PascalCase into a human-readable string.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>
        /// A space-separated, human-readable version of the enum type name.
        /// </returns>
        /// <example>
        /// <code>
        /// GetEnumNameText&lt;PrintType&gt;(); // "Print Type"
        /// </code>
        /// </example>
        public static string GetEnumNameText<TEnum>() where TEnum : Enum
        {
            return Regex.Replace(
                typeof(TEnum).Name,
                "(\\B[A-Z])",
                " $1"
            );
        }
    }
}