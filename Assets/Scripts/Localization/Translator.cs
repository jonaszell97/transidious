using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transidious
{
    [Serializable]
    struct LanguageItem
    {
        public string key;
        public string text;
    }

    [Serializable]
    struct Currency
    {
        public string name;
        public string symbol;
        public bool beforeNumber;
        public float valueInDollars;
    }

    [Serializable]
    class Language
    {
        public string lang_id;
        public string language;
        public string decimalSeparator;
        public string thousandsSeparator;
        public Currency currency;
        public LanguageItem[] items;
    }

    public class Translator
    {
        Dictionary<string, string> items;
        Language loadedLanguage;

        static Translator activeTranslator;

        public Translator(string langId)
        {
            var file = Resources.Load("Languages/" + langId) as TextAsset;
            this.loadedLanguage = JsonUtility.FromJson<Language>(file.text);

            this.items = new Dictionary<string, string>();
            foreach (var item in this.loadedLanguage.items)
            {
                this.items.Add(item.key, item.text);
            }

            activeTranslator = this;
        }

        public string Translate(string key)
        {
            if (!items.TryGetValue(key, out string text))
            {
                Debug.LogError("language is missing item '" + key + "'");
                return "";
            }

            return text;
        }

        public static string Get(string key)
        {
            Debug.Assert(activeTranslator != null, "no translator set!");
            return activeTranslator.Translate(key);
        }

        public static string Get(string key, params string[] args)
        {
            Debug.Assert(activeTranslator != null, "no translator set!");

            var text = activeTranslator.Translate(key);
            for (var i = 0; i < args.Length; ++i)
            {
                text = text.Replace("$" + i, args[i]);
            }

            return text;
        }

        static void AppendInteger(StringBuilder str, ulong value, string thousandsSeparator)
        {
            var tmp = new StringBuilder();
            var n = (ulong)value;
            var i = 0;

            while (true)
            {
                tmp.Append((char)('0' + (n % 10)));

                n /= 10;
                ++i;

                if (n == 0)
                {
                    break;
                }

                if (thousandsSeparator != null && i % 3 == 0)
                {
                    tmp.Append(thousandsSeparator);
                }
            }

            str.Append(tmp.ToString().Reverse().ToArray());
        }

        static void AddCurrency(ref Language lang, StringBuilder str, float amount, int digits)
        {
            if (amount < 0)
            {
                str.Append('-');
            }

            AppendInteger(str, (ulong)amount, lang.thousandsSeparator);

            var zeroed = amount - Mathf.Floor(amount);
            var pow = Mathf.Pow(10, digits);
            var afterComma = (ulong)(zeroed * pow);

            if (afterComma == 0)
            {
                return;
            }

            str.Append(lang.decimalSeparator);
            AppendInteger(str, (ulong)afterComma, null);
        }

        public static string GetCurrency(float amount, int digits = 2)
        {
            ref var currency = ref activeTranslator.loadedLanguage.currency;

            var str = new StringBuilder();
            if (currency.beforeNumber)
            {
                str.Append(currency.symbol);
            }

            AddCurrency(ref activeTranslator.loadedLanguage, str, amount * currency.valueInDollars, 2);

            if (!currency.beforeNumber)
            {
                str.Append(currency.symbol);
            }

            return str.ToString();
        }
    }
}