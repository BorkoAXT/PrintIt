using System.Text.RegularExpressions;

namespace PrintIt.Helpers
{
    public class EnumToStringHelper
    {
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
