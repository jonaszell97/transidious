using System.Collections;
using UnityEngine;

namespace Transidious
{
    public class DataCoroutine<T> where T: class
    {
        public Coroutine coroutine;
        public T result;
        private IEnumerator target;

        public DataCoroutine(IEnumerator target, MonoBehaviour owner = null)
        {
            owner = owner ?? GameController.instance;

            this.target = target;
            this.coroutine = owner.StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            while (target.MoveNext())
            {
                result = target.Current as T;
                yield return result;
            }
        }
    }
}
