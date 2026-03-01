using PrintIt.Enums;

namespace PrintIt.Helpers
{
    public static class GenerateUniqueKey
    {
        public static Guid GenerateCartItemId(Guid printId, List<PrintColor> colors)
        {
            var colorString = string.Join("-", (colors ?? new List<PrintColor>()).OrderBy(c => c).Select(c => c.ToString()));

            var combined = $"{printId}-{colorString}";

            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
            return new Guid(hash);
        }
    }
}
