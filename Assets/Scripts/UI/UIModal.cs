using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Transidious
{
    public class UIModal : MonoBehaviour
    {
        /// Event listener for the close event.
        public UnityEvent onClose;

        /// The title input field.
        public TMP_InputField titleInput;

        /// The animator for the modal.
        [SerializeField] private TransformAnimator _animator;
        
        /// The close button.
        [SerializeField] Button closeButton;

        /// The content scroll view.
        public ScrollRect scrollView;

        /// The tab bar background.
        public Image tabBarImage;
        
        /// The header background.
        public Image headerImage;
        
        /// The 'no data available' text.
        public TMP_Text noDataAvailableMsg;

        /// The tab bar buttons.
        private Button[] _tabBarButtons;

        /// The tab content transforms.
        private RectTransform[] _tabs;

        /// All existing modals.
        private static List<UIModal> _modals;

        /// Callback that is invoked when the tab is changed.
        public UnityAction<int> onTabChange;

        /// The anchored position when disabled.
        private Vector2 _disabledAnchoredPos;

        /// The currently selected tab.
        public int SelectedTab { get; private set; }

        public void Initialize()
        {
            if (_modals == null)
            {
                _modals = new List<UIModal>();
            }

            _animator.Initialize();
            _animator.SetAnimationType(TransformAnimator.AnimationType.Circular, TransformAnimator.ExecutionMode.Manual);

            _disabledAnchoredPos = GetComponent<RectTransform>().anchoredPosition;
            _animator.SetTargetAnchoredPosition(new Vector2(-_disabledAnchoredPos.x, _disabledAnchoredPos.y), _disabledAnchoredPos);
            
            SelectedTab = -1;
            _modals.Add(this);
            
            onClose = new UnityEvent();
            closeButton.onClick.AddListener(this.Disable);

            var tabBar = tabBarImage.transform.GetChild(0);
            _tabBarButtons = new Button[tabBar.childCount - 1];

            var i = 0;
            for (; i < _tabBarButtons.Length; ++i)
            {
                var btn = tabBar.GetChild(i).GetComponent<Button>();
                if (!btn.gameObject.activeSelf)
                {
                    break;
                }

                var tab = i;
                _tabBarButtons[i] = btn;
                _tabBarButtons[i].onClick.AddListener(() => this.LoadTab(tab));
            }

            _tabs = new RectTransform[i];

            var viewport = scrollView.viewport;
            Debug.Assert(viewport.childCount - 1 == _tabs.Length);

            for (i = 0; i < _tabs.Length; ++i)
            {
                _tabs[i] = (RectTransform)viewport.GetChild(i + 1);
            }
        }

        public void Enable()
        {
            if (_animator.IsAnimating || (gameObject.activeSelf && SelectedTab == 0))
            {
                return;
            }

            foreach (var modal in _modals)
            {
                modal.Disable();
            }

            LoadTab(0);
            gameObject.SetActive(true);
            //
            // _animator.SetTargetAnchoredPosition(new Vector2(-_disabledAnchoredPos.x, _disabledAnchoredPos.y), _disabledAnchoredPos);
            _animator.onFinish = null;
            _animator.StartAnimation(.2f);
        }

        public void Disable()
        {
            if (_animator.IsAnimating || !gameObject.activeSelf)
            {
                return;
            }

            // _animator.SetTargetAnchoredPosition(_disabledAnchoredPos, new Vector2(-_disabledAnchoredPos.x, _disabledAnchoredPos.y));
            _animator.onFinish = () =>
            {
                gameObject.SetActive(false);
                this.onClose.Invoke();
            };

            _animator.StartAnimation(.2f);
        }

        public void SetTitle(string title, bool editable = false)
        {
            this.titleInput.text = title;
            this.titleInput.interactable = editable;
        }

        void LoadTab(int tab)
        {
            if (SelectedTab == tab)
            {
                return;
            }

            var foundActive = false;
            for (var i = 0; i < _tabs.Length; ++i)
            {
                var c = _tabs[i];
                if (i == tab)
                {
                    c.gameObject.SetActive(true);

                    scrollView.content = c;
                    _tabBarButtons[i].GetComponent<Image>().color = Color.white;

                    for (var j = 0; j < c.childCount; ++j)
                    {
                        if (c.GetChild(j).gameObject.activeSelf)
                        {
                            foundActive = true;
                            break;
                        }
                    }
                    
                    LayoutRebuilder.ForceRebuildLayoutImmediate(c);
                }
                else
                {
                    c.gameObject.SetActive(false);
                    _tabBarButtons[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, 100f / 255f);
                }
            }
            
            noDataAvailableMsg.gameObject.SetActive(!foundActive);

            SelectedTab = tab;
            onTabChange?.Invoke(tab);
        }
    }
}