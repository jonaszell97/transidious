using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Transidious
{
    public class RandomNameGenerator
    {
        struct FirstNameInfo
        {
            internal string name;
            internal int occurences;
        }

        static FirstNameInfo[] maleFirstNames;
        static FirstNameInfo[] femaleFirstNames;
        static int totalMaleNames;
        static int totalFemaleNames;
        static string[] lastNames;
        static string[] places;

        static void LoadFile(string fileName, ref string[] result)
        {
            var resource = Resources.Load(fileName) as TextAsset;
            result = resource.text.Split(new char[] { '\n' });
        }

        static void LoadFirstNames()
        {
            var resource = Resources.Load("Names/yob2018") as TextAsset;
            var bytes = resource.bytes;
            var numBytes = bytes.Length;

            var maleNames = new List<FirstNameInfo>();
            var femaleNames = new List<FirstNameInfo>();

            var name = new StringBuilder();
            var occurenceStr = new StringBuilder();

            for (int i = 0; i < numBytes; ++i)
            {
                while (bytes[i] != ',')
                {
                    name.Append((char)bytes[i]);
                    ++i;
                }

                ++i;

                bool male = bytes[i] == 'M';
                i += 2;

                while (i < numBytes)
                {
                    occurenceStr.Append((char)bytes[i]);
                    ++i;

                    if (bytes[i] == '\r')
                    {
                        if (bytes[i + 1] == '\n')
                        {
                            ++i;
                            break;
                        }
                    }
                    else if (bytes[i] == '\n')
                    {
                        break;
                    }
                }

                int.TryParse(occurenceStr.ToString(), out int numOccurences);

                var nameInfo = new FirstNameInfo { name = name.ToString(), occurences = numOccurences };
                if (male)
                {
                    maleNames.Add(nameInfo);
                    totalMaleNames += numOccurences;
                }
                else
                {
                    femaleNames.Add(nameInfo);
                    totalFemaleNames += numOccurences;
                }

                name.Clear();
                occurenceStr.Clear();
            }

            maleFirstNames = maleNames.ToArray();
            femaleFirstNames = femaleNames.ToArray();
        }

        static string GetFirstName(FirstNameInfo[] names, int totalOccurences)
        {
            var idx = UnityEngine.Random.Range(0, totalOccurences);
            var sum = 0;
            var i = 0;

            while (true)
            {
                sum += names[i].occurences;
                if (sum >= idx)
                {
                    break;
                }

                ++i;
            }

            return names[i].name;
        }

        public static string MaleFirstName
        {
            get
            {
                if (maleFirstNames == null)
                {
                    LoadFirstNames();
                }

                return GetFirstName(maleFirstNames, totalMaleNames);
            }
        }

        public static string FemaleFirstName
        {
            get
            {
                if (femaleFirstNames == null)
                {
                    LoadFirstNames();
                }

                return GetFirstName(femaleFirstNames, totalFemaleNames);
            }
        }

        public static string LastName
        {
            get
            {
                if (lastNames == null)
                {
                    LoadFile("Names/last_names_en_US", ref lastNames);
                }

                var idx = UnityEngine.Random.Range(0, lastNames.Length);
                return lastNames[idx];
            }
        }

        public static string Place
        {
            get
            {
                if (places == null)
                {
                    LoadFile("Names/places_en_US", ref places);
                }

                var idx = UnityEngine.Random.Range(0, places.Length);
                return places[idx];
            }
        }

        public static Tuple<bool, int> GenderAndAge
        {
            get
            {
                /*
                    Age Distribution 2000's
                            Male                Female              Acc Male    Acc Female
                    0-4	    9,810,733	3.49%	9,365,065	3.33%    3.49%      52.28%
                    5-9	    10,523,277	3.74%	10,026,228	3.56%    7.23%      55.84%
                    10-14	10,520,197	3.74%	10,007,875	3.56%   10.97%      59.40%
                    15-19	10,391,004	3.69%	9,828,886	3.49%   14.66%      62.89%
                    20-24	9,687,814	3.44%	9,276,187	3.30%   18.10%      66.19%
                    25-29	9,798,760	3.48%	9,582,576	3.41%   21.58%      69.60%
                    30-34	10,321,769	3.67%	10,188,619	3.62%   25.15%      73.22%
                    35-39	11,318,696	4.02%	11,387,968	4.05%   29.17%      77.27%
                    40-44	11,129,102	3.95%	11,312,761	4.02%   33.12%      81.29%
                    45-49	9,889,506	3.51%	10,202,898	3.63%   36.63%      84.92%
                    50-54	8,607,724	3.06%	8,977,824	3.19%   39.69%      88.11%
                    55-59	6,508,729	2.31%	6,960,508	2.47%   42.00%      90.58%
                    60-64	5,136,627	1.83%	5,668,820	2.01%   43.83%      92.59%
                    65-69	4,400,362	1.56%	5,133,183	1.82%   45.39%      94.41%
                    70-74	3,902,912	1.39%	4,954,529	1.76%   46.78%      96.17%
                    75-79	3,044,456	1.08%	4,371,357	1.55%   47.86%      97.72%
                    80-84	1,834,897	0.65%	3,110,470	1.11%   48.51%      98.83%
                    85+	    1,226,998	0.44%	3,012,589	1.07%   48.95%      99.90%
                */
                var rnd = UnityEngine.Random.value;
                if (rnd < 0.0349f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(0, 5));
                }
                if (rnd < 0.0723f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(5, 10));
                }
                if (rnd < 0.1097f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(10, 15));
                }
                if (rnd < 0.1466f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(15, 20));
                }
                if (rnd < 0.1810f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(20, 25));
                }
                if (rnd < 0.2158f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(25, 30));
                }
                if (rnd < 0.2515f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(30, 35));
                }
                if (rnd < 0.2917f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(35, 40));
                }
                if (rnd < 0.3312f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(40, 45));
                }
                if (rnd < 0.3663f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(45, 50));
                }
                if (rnd < 0.3969f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(50, 55));
                }
                if (rnd < 0.4200f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(55, 60));
                }
                if (rnd < 0.4383f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(60, 65));
                }
                if (rnd < 0.4539f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(65, 70));
                }
                if (rnd < 0.4678f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(70, 75));
                }
                if (rnd < 0.4786f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(75, 80));
                }
                if (rnd < 0.4851f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(80, 85));
                }
                if (rnd < 0.4895f)
                {
                    return new Tuple<bool, int>(false, UnityEngine.Random.Range(85, 105));
                }
                if (rnd < 0.5228)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(0, 5));
                }
                if (rnd < 0.5584f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(5, 10));
                }
                if (rnd < 0.5940f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(10, 15));
                }
                if (rnd < 0.6289f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(15, 20));
                }
                if (rnd < 0.6619f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(20, 25));
                }
                if (rnd < 0.6960f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(25, 30));
                }
                if (rnd < 0.7322f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(30, 35));
                }
                if (rnd < 0.7727f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(35, 40));
                }
                if (rnd < 0.8129f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(40, 45));
                }
                if (rnd < 0.8492f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(45, 50));
                }
                if (rnd < 0.8492f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(50, 55));
                }
                if (rnd < 0.8811f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(55, 60));
                }
                if (rnd < 0.9058f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(60, 65));
                }
                if (rnd < 0.9259f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(65, 70));
                }
                if (rnd < 0.9441f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(70, 75));
                }
                if (rnd < 0.9617f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(75, 80));
                }
                if (rnd < 0.9772f)
                {
                    return new Tuple<bool, int>(true, UnityEngine.Random.Range(80, 85));
                }

                return new Tuple<bool, int>(true, UnityEngine.Random.Range(85, 105));
            }
        }
    }
}