using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Transidious
{
    [Serializable]
    public struct LanguageItem
    {
        public string key;
        public string text;
    }

    [Serializable]
    public struct Currency
    {
        public string name;
        public string symbol;
        public bool beforeNumber;
        public float valueInDollars;
    }

    [Serializable]
    public struct DateFormat
    {
        public int hours;
        public bool AM;
        public bool PM;
        public int maxTimeStringLength;
    }

    [Serializable]
    public class Language
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
        public Language loadedLanguage;
        public CultureInfo culture;

        public static Translator current;

        public Translator(TextAsset file, string langId)
        {
            this.loadedLanguage = JsonUtility.FromJson<Language>(file.text);

            this.items = new Dictionary<string, string>();
            foreach (var item in this.loadedLanguage.items)
            {
                this.items.Add(item.key, item.text);
            }

            this.loadedLanguage = JsonUtility.FromJson<Language>(file.text);

            this.items = new Dictionary<string, string>();
            foreach (var item in this.loadedLanguage.items)
            {
                this.items.Add(item.key, item.text);
            }

            this.culture = CultureInfo.CreateSpecificCulture(langId.Replace("_", "-"));
        }

        public static Translator SetActiveLanguage(string langId)
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            var file = Resources.Load("Languages/" + langId) as TextAsset;
            if (file == null)
            {
                return null;
            }

            current = new Translator(file, langId);
            EventManager.current.TriggerEvent("LanguageChange");

            return current;
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

        public enum DateFormat
        {
            DateShort,
            DateLong,
            DateTimeShort,
            DateTimeLong,
            TimeShort,
            TimeLong,
        }

        public static string GetDate(DateTime date, DateFormat format)
        {
            string fm;
            switch (format)
            {
                case DateFormat.DateShort:
                default:
                    fm = "d";
                    break;
                case DateFormat.DateLong:
                    fm = "D";
                    break;
                case DateFormat.DateTimeShort:
                    fm = "g";
                    break;
                case DateFormat.DateTimeLong:
                    fm = "f";
                    break;
                case DateFormat.TimeShort:
                    fm = "t";
                    break;
                case DateFormat.TimeLong:
                    fm = "T";
                    break;
            }

            return date.ToString(fm, current.culture);
        }

        public static string GetNumber(int amount)
        {
            return string.Format(current.culture, "{0:n0}", amount);
        }

        public static string GetNumber(float amount)
        {
            return amount.ToString("N", current.culture);
        }

        public static string GetCurrency(decimal amount, bool includeSymbol = false, bool includePlusSign = false)
        {
            ref var currency = ref current.loadedLanguage.currency;
            var str = (amount * (decimal)currency.valueInDollars).ToString("N", current.culture);

            if (includeSymbol)
            {
                if (currency.beforeNumber)
                {
                    str = currency.symbol + " " + str;
                }
                else
                {
                    str += " " + currency.symbol;
                }
            }

            if (includePlusSign && amount > 0)
            {
                str = "+" + str;
            }

            return str;

            //var str = new StringBuilder();
            //if (currency.beforeNumber)
            //{
            //    str.Append(currency.symbol);
            //}

            //AddCurrency(ref current.loadedLanguage, str, amount * currency.valueInDollars, 2);

            //if (!currency.beforeNumber)
            //{
            //    str.Append(currency.symbol);
            //}

            //return str.ToString();
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
            var isAM = hour <= 11;

            if (hour == 0)
            {
                hour = 12;
            }
            else
            {
                hour %= dateFormat.hours;
            }

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