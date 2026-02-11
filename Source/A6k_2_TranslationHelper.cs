using Verse;

namespace A6k_CompVariant
{
    public static class A6k_TranslationHelper
    {
        public static string T(string key, params object[] args)
        {
            string Fallback(string k) => k.Replace("A6k_Settings_", "").Replace("_", " ");
            if (key.TryTranslate(out var translated))
                return string.Format(translated, args);
            return Fallback(key);
        }
    }
}
