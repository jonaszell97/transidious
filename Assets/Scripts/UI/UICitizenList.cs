using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UICitizenList : MonoBehaviour
    {
        /// The title object.
        public UIText title;
        
        /// Prefab for instantiating a new citizen item.
        [SerializeField] private GameObject listItemPrefab;

        /// Instantiated list items.
        private List<Tuple<GameObject, Image, TMP_Text>> listItems;

        public void Initialize(string title)
        {
            this.title.SetKey(title);
            listItems = new List<Tuple<GameObject, Image, TMP_Text>>();
        }

        public void SetCitizens(IEnumerable<Citizen> citizenEnum)
        {
            var citizens = citizenEnum.ToArray();
            while (listItems.Count < citizens.Length)
            {
                var inst = Instantiate(listItemPrefab, this.transform);
                var tf = inst.transform;
                
                listItems.Add(Tuple.Create(
                    inst,
                    tf.GetChild(0).GetComponent<Image>(),
                    tf.GetChild(1).GetComponent<TMP_Text>()));
            }

            for (var i = 0; i < listItems.Count; ++i)
            {
                var item = listItems[i];
                if (i >= citizens.Length)
                {
                    item.Item1.SetActive(false);
                    continue;
                }

                var c = citizens[i];
                item.Item3.text = c.Name;
                item.Item1.SetActive(true);

                var link = item.Item3.GetComponent<UILocationLink>();
                link.SetLocation(c);
            }
        }
    }
}