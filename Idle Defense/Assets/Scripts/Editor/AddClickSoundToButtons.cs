using Assets.Scripts.Systems.Audio;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

public class AddClickSoundToButtons : EditorWindow
{
    private GameObject canvasObject;
    private AudioManager audioManager;

    [MenuItem("Tools/UI/Add Click Sound To Buttons")]
    public static void ShowWindow()
    {
        GetWindow<AddClickSoundToButtons>("Click Sound Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Add 'Click' Sound to All Buttons", EditorStyles.boldLabel);

        canvasObject = (GameObject)EditorGUILayout.ObjectField("Canvas", canvasObject, typeof(GameObject), true);
        audioManager = (AudioManager)EditorGUILayout.ObjectField("Audio Manager", audioManager, typeof(AudioManager), true);

        if (GUILayout.Button("Apply Click Sound"))
        {
            if (canvasObject == null || audioManager == null)
            {
                Debug.LogError("Please assign both the Canvas and AudioManager.");
                return;
            }

            AddClickToButtons(canvasObject, audioManager);
        }
    }

    private void AddClickToButtons(GameObject canvas, AudioManager audioManager)
    {
        int count = 0;
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            bool alreadyAssigned = false;
            int totalEvents = button.onClick.GetPersistentEventCount();

            for (int i = 0; i < totalEvents; i++)
            {
                if (button.onClick.GetPersistentTarget(i) == audioManager &&
                    button.onClick.GetPersistentMethodName(i) == "PlayClick")
                {
                    alreadyAssigned = true;
                    break;
                }
            }

            if (!alreadyAssigned)
            {
                UnityEventTools.AddPersistentListener(button.onClick, audioManager.PlayClick);
                EditorUtility.SetDirty(button); // mark scene dirty so it's saved
                count++;
            }
        }

        Debug.Log($"Click sound added to {count} button(s).");
    }

}
