using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Common.DAL;
using Common.DAL.Models;

namespace Common.Services
{
    public class LocalizationService : BaseService
    {
        public SettingsService Settings { get; }

        public LocalizationService(DepotContext context, SettingsService settings)
            : base(context)
        {
            Settings = settings;
        }

        public string Get(string key, string locale = "nl-NL", List<string>? replacementStrings = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var translation = Context.Translations.FirstOrDefault(t => t.Key.ToLower() == key.ToLower() && t.Locale == locale);

            bool useReplacementString = true;
            if (translation == null)
            {
                translation = Create(key, locale, replacementStrings);
                useReplacementString = false;
            }

            var stringValue = translation.Value; // Detach the translation from the context

            if (replacementStrings != null && useReplacementString)
            {
                for (int i = 0; i < replacementStrings.Count; i++)
                {
                    stringValue = stringValue.Replace($"{{{i}}}", replacementStrings[i]);
                }
            }

            return stringValue;
        }

        private Translation Create(string key, string locale, List<string>? replacementStrings)
        {
            var translation = new Translation
            {
                Key = key,
                Locale = locale,
                Value = $"{key} | {locale}"
            };

            for (int i = 0; i < replacementStrings?.Count; i++)
                translation.Value += $" | {{{i}}} = {replacementStrings[i]}";

            var entry = Context.Translations.Add(translation);

            Context.SaveChanges();
            return entry.Entity;
        }
    }
}
