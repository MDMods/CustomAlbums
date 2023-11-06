using Il2CppInterop.Runtime;

namespace CustomAlbums.Utilities
{
    internal class Interop
    {
        /// <summary>
        /// A workaround to a memory corruption issue of creating Il2CppSystem.TypeValue types using C#'s <c>new</c> operator.
        /// This will likely be deleted/become deprecated in the future.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>A new object of type T</returns>
        public static T CreateTypeValue<T>() => (T)Activator.CreateInstance(typeof(T),
            IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<T>.NativeClassPtr));
    }
}
