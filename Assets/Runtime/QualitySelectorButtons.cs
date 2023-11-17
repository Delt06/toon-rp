using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime
{
    public class QualitySelectorButtons : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private readonly List<Button> _buttons = new();
        private string[] _levelNames;

        private void Awake()
        {
            _levelNames = QualitySettings.names;

            _buttons.Add(_button);

            for (int i = 1; i < _levelNames.Length; i++)
            {
                Button button = Instantiate(_button, _button.transform.parent);
                _buttons.Add(button);
            }

            for (int i = 0; i < _levelNames.Length; i++)
            {
                Button button = _buttons[i];
                button.interactable = i != QualitySettings.GetQualityLevel();
                button.GetComponentInChildren<Text>().text = _levelNames[i];
                int index = i;
                button.onClick.AddListener(() => OnClicked(index));
            }
        }

        private void OnClicked(int index)
        {
            for (int i = 0; i < _levelNames.Length; i++)
            {
                _buttons[i].interactable = i != index;
            }

            QualitySettings.SetQualityLevel(index, true);
        }
    }
}