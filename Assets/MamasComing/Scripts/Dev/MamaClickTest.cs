#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MamasComing.Dev
{
    /// Clicks a UI button for real (fake mouse through the Input System): exec MamasComing.Dev.MamaClickTest.Click "Option 2"
    public class MamaClickTest : MonoBehaviour
    {
        public static void Click(string buttonName) { var go = new GameObject("ClickTest"); go.AddComponent<MamaClickTest>().target = buttonName; }
        public static void PressKey(string key) { var go = new GameObject("KeyTest"); go.AddComponent<MamaClickTest>().key = key; }
        string target, key;
        IEnumerator Start()
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (!string.IsNullOrEmpty(key))
            {
                var k = (Key)System.Enum.Parse(typeof(Key), key);
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(k)); yield return null; yield return null;
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); Debug.Log("[ClickTest] pressed key " + key); Destroy(gameObject); yield break;
            }
            RectTransform rt = null;
            foreach (var t in FindObjectsByType<RectTransform>(FindObjectsSortMode.None)) if (t.name == target) rt = t;
            if (!rt) { Debug.Log("[ClickTest] no button " + target); Destroy(gameObject); yield break; }
            var cv = rt.GetComponentInParent<Canvas>().rootCanvas;
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(cv.renderMode == RenderMode.ScreenSpaceOverlay ? null : cv.worldCamera, rt.TransformPoint(rt.rect.center));
            var m = Mouse.current;
            for (int i = 0; i < 3; i++) { InputSystem.QueueStateEvent(m, new MouseState { position = sp }); yield return null; }
            for (int i = 0; i < 3; i++) { InputSystem.QueueStateEvent(m, new MouseState { position = sp }.WithButton(MouseButton.Left, true)); yield return null; }
            for (int i = 0; i < 3; i++) { InputSystem.QueueStateEvent(m, new MouseState { position = sp }); yield return null; }
            Debug.Log("[ClickTest] clicked " + target + " at " + sp + " cursor=" + Cursor.lockState);
            Destroy(gameObject);
        }
    }
}
#endif
