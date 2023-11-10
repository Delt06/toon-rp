using UnityEngine;

namespace Runtime
{
    public class QualitySelectorImGui : MonoBehaviour
    {
        private GUILayoutOption[] _buttonParams;
        private string[] _levelNames;

        private void Awake()
        {
            _levelNames = QualitySettings.names;
            _buttonParams = new[]
            {
                GUILayout.Width(300.0f), GUILayout.Height(100.0f),
            };
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();

            int currentLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < _levelNames.Length; i++)
            {
                string levelName = _levelNames[i];
                GUI.enabled = currentLevel != i;

                if (GUILayout.Button(levelName, _buttonParams))
                {
                    QualitySettings.SetQualityLevel(i, true);
                }

                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();
        }
    }
}