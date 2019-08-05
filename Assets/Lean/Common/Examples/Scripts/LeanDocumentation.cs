#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;

namespace Lean.Common.Examples
{
	[CustomEditor(typeof(TextAsset))]
	public class LeanDocumentation_Inspector : Editor
	{
		private static GUIStyle titleStyle;

		private static GUIStyle headerStyle;

		private static GUIStyle bodyStyle;

		private static GUIStyle rateStyle;

		private Dictionary<string, string> infos = new Dictionary<string, string>();

		private Texture2D icon;

		private Texture2D thumb;

		public static void UpdateStyles()
		{
			if (bodyStyle == null)
			{
				bodyStyle = new GUIStyle(EditorStyles.label);
				bodyStyle.wordWrap = true;
				bodyStyle.fontSize = 14;

				titleStyle = new GUIStyle(bodyStyle);
				titleStyle.fontSize = 26;
				titleStyle.alignment = TextAnchor.MiddleCenter;

				headerStyle = new GUIStyle(bodyStyle);
				headerStyle.fontSize = 18 ;

				rateStyle = new GUIStyle(EditorStyles.toolbarButton);

				rateStyle.fontSize = 20;
			}
		}

		public override void OnInspectorGUI()
		{
			var path = AssetDatabase.GetAssetPath(target);

			if (path.Contains("Lean") == true && path.EndsWith("DOCUMENTATION.html") == true)
			{
				UpdateStyles();

				EditorGUI.EndDisabledGroup();

				EditorGUILayout.LabelField("Thank You For Using " + Info("Title", "this asset") + "!", headerStyle);
				EditorGUILayout.LabelField("The documentation is in HTML format. You can open it by double clicking on this file, or by clicking below.", bodyStyle);

				if (GUILayout.Button(new GUIContent("Open Documentation", "Open In Web Browser")) == true)
				{
					System.Diagnostics.Process.Start(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), path));
				}

				EditorGUILayout.Separator();
				EditorGUILayout.Separator();

				EditorGUILayout.LabelField("Need Help?", headerStyle);
				EditorGUILayout.LabelField("If you read the documentation and still have questions, feel free to ask!", bodyStyle);

				if (infos.ContainsKey("Forum") == true)
				{
					if (GUILayout.Button("Forum Thread") == true)
					{
						Application.OpenURL(Info("Forum"));
					}
				}

				if (infos.ContainsKey("YouTube") == true)
				{
					if (GUILayout.Button("YouTube Channel") == true)
					{
						Application.OpenURL(Info("YouTube"));
					}
				}
				
				if (GUILayout.Button(new GUIContent("E-Mail Me", "carlos.wilkes@gmail.com")) == true)
				{
					Application.OpenURL("mailto:carlos.wilkes@gmail.com");
				}

				if (GUILayout.Button(new GUIContent("Private Message", "Unity Forum Profile")) == true)
				{
					Application.OpenURL("http://forum.unity.com/members/41960");
				}

				EditorGUILayout.Separator();
				EditorGUILayout.Separator();

				EditorGUILayout.LabelField("You're Awesome!", headerStyle);
				EditorGUILayout.LabelField("If you haven't already, please consider rating this asset. It really helps me out!", bodyStyle);

				if (GUILayout.Button(new GUIContent("Rate This Asset", Info("Title") + " Asset Page")) == true)
				{
					Application.OpenURL("http://CarlosWilkes.com/Get/" + Info("Link"));
				}

				EditorGUILayout.Separator();
				EditorGUILayout.Separator();

				EditorGUILayout.LabelField("Made Something Cool?", headerStyle);
				EditorGUILayout.LabelField("If you've finished a project using " + Info("Title") + " then let me know! I can shout you out, link to you from my website, and much more!", bodyStyle);

				if (GUILayout.Button(new GUIContent("E-Mail Me", "carlos.wilkes@gmail.com")) == true)
				{
					Application.OpenURL("mailto:carlos.wilkes@gmail.com");
				}

				EditorGUILayout.Separator();
				EditorGUILayout.Separator();

				EditorGUILayout.LabelField("Want More?", headerStyle);
				EditorGUILayout.LabelField("Check out all my other great assets, I'm sure there's something there that can help you!", bodyStyle);

				if (GUILayout.Button(new GUIContent("My Website", "CarlosWilkes.com")) == true)
				{
					Application.OpenURL("http://CarlosWilkes.com" + Info("Link"));
				}
			}
			else
			{
				base.OnInspectorGUI();
			}
		}

		protected override void OnHeaderGUI()
		{
			UpdateStyles();

			GUILayout.BeginHorizontal("In BigTitle");
			{
				var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth/3f - 20f, 128f);
				var content   = new GUIContent(Info("Title", "Documentation").Replace(' ', '\n'));

				var height = Mathf.Max(titleStyle.CalcHeight(content, EditorGUIUtility.currentViewWidth - iconWidth), iconWidth);

				if (icon != null)
				{
					GUILayout.Label(icon, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
				}

				GUILayout.Label(content, titleStyle, GUILayout.Height(height));
			}
			GUILayout.EndHorizontal();
		}

		protected virtual void OnEnable()
		{
			var textAsset = (TextAsset)target;
			var text      = textAsset.text;
			var blockA    = text.IndexOf("<!--");
			var blockB    = text.IndexOf("-->");

			if (blockA >= 0 && blockB >= 0)
			{
				var section = text.Substring(blockA, blockB - blockA);
				var lines   = section.Split('\r', '\n');

				foreach (var line in lines)
				{
					var tokens = line.Split(':');

					if (tokens.Length == 2)
					{
						var k = tokens[0];
						var v = tokens[1];

						if (k == "Icon")
						{
							icon = new Texture2D(1, 1);

							icon.LoadImage(System.Convert.FromBase64String(v));
						}
						else if (k == "Thumb")
						{
							thumb = new Texture2D(1, 1);

							thumb.LoadImage(System.Convert.FromBase64String(v));
						}
						else
						{
							infos.Add(k, v);
						}
					}
				}
			}
		}

		private string Info(string key, string fallback = null)
		{
			var value = default(string);

			if (infos.TryGetValue(key, out value) == false)
			{
				value = fallback;
			}

			return value;
		}
	}
}

namespace Lean.Common.Examples
{
	/// <summary>Unity hijacks html file opening and passes it to the default text editor. For documentation files we want to use an actual browser for this, so hijack it back!</summary>
	public static class LeanDocumentation
	{
		[OnOpenAsset(1)]
		public static bool step1(int instanceID, int line)
		{
			var path = AssetDatabase.GetAssetPath(instanceID);

			if (path.Contains("Lean") == true && path.EndsWith("DOCUMENTATION.html") == true)
			{
				System.Diagnostics.Process.Start(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), path));

				return true;
			}

			return false;
		}
	}
}
#endif