using System;
using UnityEngine;

namespace Transidious.MainMenu
{
    public class MainMenu : MonoBehaviour
    {
        /// The main menu language.
        public Translator lang;

        private void Awake()
        {
            this.lang = Translator.SetActiveLanguage("en_US");
        }
    }
}