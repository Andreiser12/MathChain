using System.Reflection;

namespace MathChain.Tests.Fakes
{
    public static class EntityCopy
    {
        private static readonly MethodInfo MemberwiseCloneMethod =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public static T? Detached<T>(T? entity) where T : class
        {
            return entity == null ? null : (T)MemberwiseCloneMethod.Invoke(entity, null)!;
        }
    }
}
