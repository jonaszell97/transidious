using System;
using UnityEngine;
using UnityEngine.UI;

namespace Transidious.UI
{
    public class UITabPanel : MonoBehaviour
    {
        /// The tab header game objects.
        public RectTransform headersContainer;

        /// The tab game objects.
        public RectTransform tabsContainer;

        /// The currently activated tab.
        public int activeTab;

        /// The active tab color.
        public Color activeColor;
        
        /// The inactive tab color.
        public Color inactiveColor;

        /// Initialize buttons.
        private void Awake()
        {
            Debug.Assert(headersContainer.childCount == tabsContainer.childCount);

            for (var i = 0; i < headersContainer.childCount; ++i)
            {
                var n = i;
                headersContainer.GetChild(i).GetComponent<Button>().onClick.AddListener(() => ActivateTab(n));
                headersContainer.GetChild(i).GetComponent<Image>().color = inactiveColor;
            }

            activeTab = -1;
            ActivateTab(0);
        }

        /// Activate a tab.
        public void ActivateTab(int n)
        {
            if (activeTab == n)
            {
                return;
            }

            if (activeTab != -1)
            {
                headersContainer.GetChild(activeTab).GetComponent<Image>().color = inactiveColor;
                tabsContainer.GetChild(activeTab).gameObject.SetActive(false);
            }

            activeTab = n;
            headersContainer.GetChild(activeTab).GetComponent<Image>().color = activeColor;
            tabsContainer.GetChild(activeTab).gameObject.SetActive(true);
        }
    }
}