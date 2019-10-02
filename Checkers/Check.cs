using JetBrains.Annotations;

namespace Checkers
{
    public static class Check
    {
        [ContractAnnotation("val:null=>halt")]
        public static void NotNull(object val, string message)
        {
            if (val == null) throw new CheckFailedException(message);
        }
    }
}