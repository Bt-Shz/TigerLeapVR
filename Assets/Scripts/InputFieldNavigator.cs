using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InputFieldNavigator : MonoBehaviour
{
    [Header("Assign UI elements in order (InputFields or Buttons)")]
    public List<Selectable> uiElements = new List<Selectable>();

    private int currentIndex = 0;

    void Start()
    {
        if (uiElements.Count > 0)
        {
            ActivateElement(0);

            // Add submit listeners if it's an input field
            foreach (var element in uiElements)
            {
                TMP_InputField inputField = element.GetComponent<TMP_InputField>();
                if (inputField != null)
                {
                    inputField.onSubmit.AddListener((text) => OnSubmit(element));
                }
            }
        }
    }

    void Update()
    {
        // If Enter pressed while button is active → move next
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var current = uiElements[currentIndex];
            if (current is Button)
            {
                (current as Button).onClick.Invoke(); // trigger button click
                OnSubmit(current);
            }
        }
    }

    void OnSubmit(Selectable submittedElement)
    {
        int index = uiElements.IndexOf(submittedElement);

        if (index >= 0 && index < uiElements.Count - 1)
        {
            ActivateElement(index + 1);
        }
        else
        {
            Debug.Log("✅ Finished all elements!");
        }
    }

    void ActivateElement(int index)
    {
        currentIndex = index;
        uiElements[currentIndex].Select();

        // If it's an input field, also activate typing
        TMP_InputField inputField = uiElements[currentIndex].GetComponent<TMP_InputField>();
        if (inputField != null)
        {
            inputField.ActivateInputField();
        }
    }
}
