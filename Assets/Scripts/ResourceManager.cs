using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Transidious
{
    public class ResourceManager : MonoBehaviour
    {
        public struct ResourceLimits
        {
            /// Maximum number of active paths.
            public int maxActivePaths;

            /// Maximum number of happiness change sprites.
            public int maxTemporarySprites;
        }

        /// Static reference to the singleton instance.
        public static ResourceManager instance;

        /// The resource limits.
        public ResourceLimits resourceLimits;

        /// Prefab for ActivePath.
        [SerializeField] private GameObject _activePathPrefab;

        /// Set of unused ActivePath instances.
        private Stack<ActivePath> _unusedActivePaths;

        /// Number of total ActivePath instances.
        private int _activePaths;

        /// Set of unused happiness sprites.
        private Stack<SpriteRenderer> _unusedSprites;

        /// Number of total ActivePath instances.
        private int _temporarySprites;

        /// The info panel card prefab.
        public GameObject infoPanelCardPrefab;
        
        /// The line logo prefab.
        public GameObject lineLogoPrefab;

        private void Awake()
        {
            resourceLimits = new ResourceLimits
            {
                maxActivePaths = 10_000,
                maxTemporarySprites = 10,
            };

            _unusedActivePaths = new Stack<ActivePath>();
            _unusedSprites = new Stack<SpriteRenderer>();
            
            instance = this;
        }

        /// Get an available ActivePath or instantiate one if none are available.
        public ActivePath GetActivePath(bool force = false)
        {
            if (_unusedActivePaths.Count > 0)
            {
                return _unusedActivePaths.Pop();
            }

            if (!force && _activePaths >= resourceLimits.maxActivePaths)
            {
                return null;
            }

            ++_activePaths;

            var obj = Instantiate(_activePathPrefab);
            obj.SetActive(false);

            return obj.GetComponent<ActivePath>();
        }

        /// Reclaim an active path.
        public void Reclaim(ActivePath path)
        {
            path.gameObject.SetActive(false);
            _unusedActivePaths.Push(path);
        }
        
        /// Get an available ActivePath or instantiate one if none are available.
        public SpriteRenderer GetTemporarySprite(bool force = false)
        {
            if (_unusedSprites.Count > 0)
            {
                return _unusedSprites.Pop();
            }

            if (!force && _temporarySprites >= resourceLimits.maxTemporarySprites)
            {
                return null;
            }

            ++_temporarySprites;

            var obj = new GameObject();
            obj.SetActive(false);

            obj.AddComponent<TransformAnimator>();
            return obj.AddComponent<SpriteRenderer>();
        }

        /// Reclaim an active path.
        public void Reclaim(SpriteRenderer sr)
        {
            sr.gameObject.SetActive(false);
            _unusedSprites.Push(sr);
        }
    }
}