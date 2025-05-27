using UnityEngine;

#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Valve.VR
{
    /// <summary>
    /// Helper class to handle input across both Unity's legacy Input Manager and new Input System
    /// </summary>
    public static class SteamVR_InputHelper
    {
        // ==================== KEYBOARD INPUT ====================

        /// <summary>
        /// Returns true during the frame the user starts pressing down the key
        /// </summary>
        public static bool GetKeyDown(KeyCode key)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            var keyControl = GetKeyControl(keyboard, key);
            return keyControl != null && keyControl.wasPressedThisFrame;
#else
            return Input.GetKeyDown(key);
#endif
        }

        /// <summary>
        /// Returns true while the user holds down the key
        /// </summary>
        public static bool GetKey(KeyCode key)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            var keyControl = GetKeyControl(keyboard, key);
            return keyControl != null && keyControl.isPressed;
#else
            return Input.GetKey(key);
#endif
        }

        /// <summary>
        /// Returns true during the frame the user releases the key
        /// </summary>
        public static bool GetKeyUp(KeyCode key)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            var keyControl = GetKeyControl(keyboard, key);
            return keyControl != null && keyControl.wasReleasedThisFrame;
#else
            return Input.GetKeyUp(key);
#endif
        }

        /// <summary>
        /// Returns true if any key was pressed this frame
        /// </summary>
        public static bool AnyKeyDown()
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
#else
            return Input.anyKeyDown;
#endif
        }

        // ==================== MOUSE INPUT ====================

        /// <summary>
        /// Returns whether the given mouse button is held down
        /// </summary>
        /// <param name="button">0 = left, 1 = right, 2 = middle</param>
        public static bool GetMouseButton(int button)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            if (mouse == null) return false;

            switch (button)
            {
                case 0: return mouse.leftButton.isPressed;
                case 1: return mouse.rightButton.isPressed;
                case 2: return mouse.middleButton.isPressed;
                default: return false;
            }
#else
            return Input.GetMouseButton(button);
#endif
        }

        /// <summary>
        /// Returns true during the frame the user pressed the given mouse button
        /// </summary>
        /// <param name="button">0 = left, 1 = right, 2 = middle</param>
        public static bool GetMouseButtonDown(int button)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            if (mouse == null) return false;

            switch (button)
            {
                case 0: return mouse.leftButton.wasPressedThisFrame;
                case 1: return mouse.rightButton.wasPressedThisFrame;
                case 2: return mouse.middleButton.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetMouseButtonDown(button);
#endif
        }

        /// <summary>
        /// Returns true during the frame the user releases the given mouse button
        /// </summary>
        /// <param name="button">0 = left, 1 = right, 2 = middle</param>
        public static bool GetMouseButtonUp(int button)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            if (mouse == null) return false;

            switch (button)
            {
                case 0: return mouse.leftButton.wasReleasedThisFrame;
                case 1: return mouse.rightButton.wasReleasedThisFrame;
                case 2: return mouse.middleButton.wasReleasedThisFrame;
                default: return false;
            }
#else
            return Input.GetMouseButtonUp(button);
#endif
        }

        /// <summary>
        /// The current mouse position in pixel coordinates
        /// </summary>
        public static Vector2 mousePosition
        {
            get
            {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        /// <summary>
        /// The current mouse scroll delta
        /// </summary>
        public static Vector2 MouseScrollDelta
        {
            get
            {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var mouse = Mouse.current;
                return mouse != null ? mouse.scroll.ReadValue() * 0.01f : Vector2.zero; // Scale to match legacy
#elif UNITY_5_6_OR_NEWER
                return Input.mouseScrollDelta;
#else
                // Unity 5.5 and older don't have mouseScrollDelta
                return new Vector2(0, Input.GetAxis("Mouse ScrollWheel"));
#endif
            }
        }

        /// <summary>
        /// Returns whether a mouse is detected
        /// </summary>
        public static bool MousePresent
        {
            get
            {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Mouse.current != null;
#else
                return Input.mousePresent;
#endif
            }
        }

        // ==================== AXIS INPUT ====================

        /// <summary>
        /// Returns the value of the virtual axis identified by axisName
        /// </summary>
        public static float GetAxis(string axisName)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // Map common axis names to new input system
            switch (axisName)
            {
                case "Horizontal": return GetHorizontalAxis();
                case "Vertical": return GetVerticalAxis();
                case "Mouse X": return GetMouseDelta().x;
                case "Mouse Y": return GetMouseDelta().y;
                case "Mouse ScrollWheel": return MouseScrollDelta.y;
                default: return 0f;
            }
#else
            return Input.GetAxis(axisName);
#endif
        }

        /// <summary>
        /// Returns the value of the virtual axis identified by axisName with no smoothing
        /// </summary>
        public static float GetAxisRaw(string axisName)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // Raw axis values (no smoothing)
            switch (axisName)
            {
                case "Horizontal": return GetHorizontalAxisRaw();
                case "Vertical": return GetVerticalAxisRaw();
                case "Mouse X": return GetMouseDelta().x;
                case "Mouse Y": return GetMouseDelta().y;
                case "Mouse ScrollWheel": return MouseScrollDelta.y;
                default: return 0f;
            }
#else
            return Input.GetAxisRaw(axisName);
#endif
        }

        // ==================== TOUCH INPUT ====================

        /// <summary>
        /// Number of touches currently active
        /// </summary>
        public static int TouchCount
        {
            get
            {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var touchscreen = Touchscreen.current;
                return touchscreen != null ? touchscreen.touches.Count : 0;
#else
                return Input.touchCount;
#endif
            }
        }

        /// <summary>
        /// Returns object representing touch at index
        /// </summary>
        public static Touch GetTouch(int index)
        {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var touchscreen = Touchscreen.current;
            if (touchscreen == null || index >= touchscreen.touches.Count)
                return default(Touch);

            var touch = touchscreen.touches[index];
            var result = new Touch();
            result.fingerId = touch.touchId.ReadValue();
            result.position = touch.position.ReadValue();
            result.deltaPosition = touch.delta.ReadValue();
            result.phase = (UnityEngine.TouchPhase)touch.phase.ReadValue();
            result.pressure = touch.pressure.ReadValue();
            return result;
#else
            return Input.GetTouch(index);
#endif
        }

        /// <summary>
        /// Returns whether touch input is supported
        /// </summary>
        public static bool TouchSupported
        {
            get
            {
#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                return Touchscreen.current != null;
#else
                return Input.touchSupported;
#endif
            }
        }

        // ==================== HELPER METHODS ====================

#if UNITY_2019_1_OR_NEWER && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static KeyControl GetKeyControl(Keyboard keyboard, KeyCode keyCode)
        {
            // Map KeyCode to Key enum
            var key = KeyCodeToKey(keyCode);
            return key != Key.None ? keyboard[key] : null;
        }

        private static System.Collections.Generic.Dictionary<KeyCode, Key> keyCodeToKeyMap = null;

        private static void InitializeKeyMap()
        {
            keyCodeToKeyMap = new System.Collections.Generic.Dictionary<KeyCode, Key>();

            var keyCodes = (KeyCode[])System.Enum.GetValues(typeof(KeyCode));

            foreach (var keyCode in keyCodes)
            {
                if (keyCode == KeyCode.None) continue;

                string keyCodeName = keyCode.ToString();
                string keyName = keyCodeName;


                if (keyCodeName.StartsWith("Alpha"))
                {
                    // Alpha0-Alpha9 -> Digit0-Digit9
                    keyName = "Digit" + keyCodeName.Substring(5);
                }
                else if (keyCodeName.StartsWith("Keypad"))
                {
                    // Keypad0-Keypad9 -> Numpad0-Numpad9
                    // KeypadPlus -> NumpadPlus, etc.
                    keyName = "Numpad" + keyCodeName.Substring(6);
                }
                else if (keyCodeName.EndsWith("Control"))
                {
                    // LeftControl -> LeftCtrl, RightControl -> RightCtrl
                    keyName = keyCodeName.Replace("Control", "Ctrl");
                }
                else if (keyCodeName == "Return")
                {
                    keyName = "Enter";
                }
                else if (keyCodeName == "BackQuote")
                {
                    keyName = "Backquote";
                }
                else if (keyCodeName == "Print")
                {
                    keyName = "PrintScreen";
                }
                else if (keyCodeName == "Numlock")
                {
                    keyName = "NumLock";
                }
                // Add more special cases as needed

                // Try to parse the transformed name
                try
                {
                    Key key = (Key)System.Enum.Parse(typeof(Key), keyName);
                    keyCodeToKeyMap[keyCode] = key;
                }
                catch
                {
                    // If parsing fails, it's a key that doesn't have a direct mapping
                    // Could add to a debug log if needed
                }
            }

            // Manual override for any edge cases that the string conversion doesn't catch
            // (Most should be handled by the rules above, but this allows for exceptions)
            // keyCodeToKeyMap[KeyCode.SomeSpecialCase] = Key.SomeSpecialMapping;
        }

        private static Key KeyCodeToKey(KeyCode keyCode)
        {
            // Initialize map on first use
            if (keyCodeToKeyMap == null)
            {
                InitializeKeyMap();
            }

            Key key;
            if (keyCodeToKeyMap.TryGetValue(keyCode, out key))
            {
                return key;
            }

            Debug.LogError("[SteamVR] InputHelper: Could not find keycode to key mapping for: " + keyCode.ToString());
            return Key.None;
        }

        private static float GetHorizontalAxis()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return 0f;

            float value = 0f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value -= 1f;
            return value;
        }

        private static float GetVerticalAxis()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return 0f;

            float value = 0f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) value += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) value -= 1f;
            return value;
        }

        private static float GetHorizontalAxisRaw()
        {
            return GetHorizontalAxis(); // Already raw in new input system
        }

        private static float GetVerticalAxisRaw()
        {
            return GetVerticalAxis(); // Already raw in new input system
        }

        private static Vector2 GetMouseDelta()
        {
            var mouse = Mouse.current;
            return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
        }
#endif
    }
}