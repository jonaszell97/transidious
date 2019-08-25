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
    struct DateFormat
    {
        public int hours;
        public bool AM;
        public bool PM;
        public int maxTimeStringLength;
    }

    [Serializable]
    class Language
    {
        public string lang_id;
        public string language;
        public string decimalSeparator;
        public string thousandsSeparator;
        public Currency currency;
        public DateFormat dateFormat;
        public LanguageItem[] items;
    }

    public class Translator
    {
        Dictionary<string, string> items;
        Language loadedLanguage;

        public static Translator current;

        public Translator(string langId)
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            var file = Resources.Load("Languages/" + langId) as TextAsset;
            this.loadedLanguage = JsonUtility.FromJson<Language>(file.text);

            this.items = new Dictionary<string, string>();
            foreach (var item in this.loadedLanguage.items)
            {
                this.items.Add(item.key, item.text);
            }

            current = this;
        }

        public string Translate(string key)
        {
            if (!items.TryGetValue(key, out string text))
            {
                Debug.LogWarning("language is missing item '" + key + "'");
#if DEBUG
                return key;
#else
                return "";
#endif
            }

            return text;
        }

        public string CurrentLanguageID
        {
            get
            {
                return loadedLanguage.lang_id;
            }
        }

        public string CurrentLanguageName
        {
            get
            {
                return loadedLanguage.language;
            }
        }

        public static string Get(string key)
        {
            Debug.Assert(current != null, "no translator set!");
            return current.Translate(key);
        }

        public static string Get(string key, params string[] args)
        {
            Debug.Assert(current != null, "no translator set!");

            var text = current.Translate(key);
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
            ref var currency = ref current.loadedLanguage.currency;

            var str = new StringBuilder();
            if (currency.beforeNumber)
            {
                str.Append(currency.symbol);
            }

            AddCurrency(ref current.loadedLanguage, str, amount * currency.valueInDollars, 2);

            if (!currency.beforeNumber)
            {
                str.Append(currency.symbol);
            }

            return str.ToString();
        }

        public static int MaxTimeStringLength
        {
            get
            {
                return current.loadedLanguage.dateFormat.maxTimeStringLength;
            }
        }

        public static string FormatTime(DateTime time)
        {
            var bytes = new char[MaxTimeStringLength];
            FormatTime(time, ref bytes);

            return new string(bytes);
        }

        public static void FormatTime(DateTime time, ref char[] bytes)
        {
            Debug.Assert(bytes.Length >= MaxTimeStringLength);

            var i = 0;

            var dateFormat = current.loadedLanguage.dateFormat;
            var hour = time.Hour;
            var isAM = hour <= 12;

            hour %= dateFormat.hours;

            if (hour >= 10)
            {
                bytes[i++] = (char)('0' + (hour / 10));
                bytes[i++] = (char)('0' + (hour % 10));
            }
            else
            {
                bytes[i++] = '0';
                bytes[i++] = (char)('0' + hour);
            }

            bytes[i++] = ':';

            var min = time.Minute;
            if (min >= 10)
            {
                bytes[i++] = (char)('0' + (min / 10));
                bytes[i++] = (char)('0' + (min % 10));
            }
            else
            {
                bytes[i++] = '0';
                bytes[i++] = (char)('0' + min);
            }

            if (isAM && dateFormat.AM)
            {
                bytes[i++] = ' ';
                bytes[i++] = 'A';
                bytes[i++] = 'M';
            }
            else if (!isAM && dateFormat.PM)
            {
                bytes[i++] = ' ';
                bytes[i++] = 'P';
                bytes[i++] = 'M';
            }
        }
    }
}