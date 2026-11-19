// Minimal stand-ins for Unity/Fusion/TMP types, ONLY to catch typos and type errors in our scripts outside Unity.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public static void Destroy(Object o) { } public static void DestroyImmediate(Object o) { } public static T[] FindObjectsByType<T>() where T : Object { return new T[0]; } public static T FindAnyObjectByType<T>() where T : Object { return null; }
        public static implicit operator bool(Object o) { return o != null; } public string name; }
    public enum FindObjectsSortMode { None }
    public class Component : Object { public GameObject gameObject; public Transform transform;
        public T GetComponent<T>() { return default(T); } public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return new T[0]; } public T GetComponentInChildren<T>() { return default(T); } }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object { public Transform transform; public bool activeSelf;
        public GameObject() { } public GameObject(string n, params Type[] t) { }
        public T GetComponent<T>() { return default(T); } public T[] GetComponentsInChildren<T>() { return new T[0]; }
        public T AddComponent<T>() { return default(T); } public void SetActive(bool b) { }
        public static GameObject CreatePrimitive(PrimitiveType t) { return null; } }
    public enum PrimitiveType { Cube, Cylinder, Quad, Capsule, Sphere }
    public class Transform : Component { public Vector3 position, localScale; public Quaternion rotation; public Vector3 forward;
        public void SetParent(Transform p, bool w) { } public void SetParent(Transform p) { } }
    public class RectTransform : Transform { public Vector2 anchoredPosition, sizeDelta, pivot; public Vector2 anchorMin, anchorMax, offsetMin, offsetMax; }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero, one; public float magnitude { get { return 0; } }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return a; } public static implicit operator Vector3(Vector2 v) { return default(Vector3); } }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up, down, zero; public float magnitude { get { return 0; } } public float sqrMagnitude { get { return 0; } } public Vector3 normalized { get { return this; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; } public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float b) { return a; } public static Vector3 operator *(float b, Vector3 a) { return a; } }
    public struct Quaternion { public static Quaternion identity; public static Quaternion Euler(float x, float y, float z) { return default(Quaternion); } }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; } public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color white, black, red, cyan, yellow; }
    public struct Color32 { public Color32(byte r, byte g, byte b, byte a) { } }
    public struct Rect { public static Rect MinMaxRect(float a, float b, float c, float d) { return default(Rect); } public bool Contains(Vector3 v) { return true; } }
    public struct Ray { public Vector3 GetPoint(float d) { return default(Vector3); } }
    public struct Plane { public Plane(Vector3 n, Vector3 p) { } public bool Raycast(Ray r, out float d) { d = 0; return true; } }
    public struct RaycastHit { public Collider collider; public Vector3 point; }
    public class Collider : Component { public Bounds bounds; }
    public struct Bounds { public Vector3 extents; }
    public static class Physics { public static bool Raycast(Ray r, out RaycastHit h, float d) { h = default(RaycastHit); return false; } public static bool Raycast(Vector3 o, Vector3 dir, out RaycastHit h, float d) { h = default(RaycastHit); return false; } }
    public class Camera : Component { public static Camera main;  public Ray ScreenPointToRay(Vector3 p) { return default(Ray); } public Vector3 WorldToScreenPoint(Vector3 p) { return p; } }
    public static class Mathf { public static float Max(float a, float b) { return a; } public static int Max(int a, int b) { return a; } public static float Min(float a, float b) { return a; } public static float Abs(float a) { return a; }
        public static float Clamp(float a, float b, float c) { return a; } public static int RoundToInt(float f) { return 0; } public static int CeilToInt(float f) { return 0; } }
    public static class Time { public static int frameCount; public static float time, unscaledTime, unscaledDeltaTime, deltaTime; }
    public static class Debug { public static void Log(object o) { } public static void LogWarning(object o) { } public static void LogError(object o) { } }
    public static class Application { public static string dataPath, persistentDataPath, streamingAssetsPath; }
    public static class Screen { public static int height; }
    public static class Gizmos { public static Color color; public static void DrawWireSphere(Vector3 p, float r) { } public static void DrawWireCube(Vector3 p, Vector3 s) { } }
    public static class GUI { public static Color color; public static void DrawTexture(Rect r, Texture t) { } }
    public class Shader { public static Shader Find(string n) { return null; } }
    public class Material : Object { public Material(Shader s) { } public Material(Material m) { } public Color color; public Texture mainTexture; }
    public class Texture : Object { } 
    public class Texture2D : Texture { public static Texture2D whiteTexture; public FilterMode filterMode; public TextureWrapMode wrapMode;
        public Texture2D(int w, int h, TextureFormat f, bool m) { } public void SetPixels32(Color32[] c) { } public void Apply(bool b) { } }
    public enum TextureFormat { RGBA32 } public enum FilterMode { Bilinear } public enum TextureWrapMode { Clamp }
    public class Renderer : Component { public bool enabled; public Material sharedMaterial, material; }
    public class SerializeFieldAttribute : Attribute { } public class HideInInspectorAttribute : Attribute { }
    public class RequireComponentAttribute : Attribute { public RequireComponentAttribute(Type t) { } }
}
namespace UnityEngine.UI { public class Graphic : Component { public Color color; } public class Image : Graphic { }
    public class CanvasScaler : Component { public enum ScaleMode { ScaleWithScreenSize } public ScaleMode uiScaleMode; public Vector2 referenceResolution; public float matchWidthOrHeight; }
    public class GraphicRaycaster : Component { } public class RectOffset { public RectOffset(int a, int b, int c, int d) { } }
    public class HorizontalLayoutGroup : Component { public bool childControlWidth, childControlHeight; public RectOffset padding; public float spacing; }
    public class GridLayoutGroup : Component { public Vector2 cellSize, spacing; } public class RectMask2D : Component { } public class LayoutElement : Component { public float preferredHeight, minHeight; }
    public class Button : Component { public bool interactable; public ButtonClickedEvent onClick = new ButtonClickedEvent(); public class ButtonClickedEvent { public void AddListener(Action a) { } } } }
namespace UnityEngine.EventSystems { public class EventSystem : Component { public static EventSystem current; public bool IsPointerOverGameObject() { return false; } } }
namespace UnityEngine.InputSystem {
    public class ButtonControl { public bool isPressed, wasPressedThisFrame, wasReleasedThisFrame; }
    public class Vec2Ctl { public Vector2 ReadValue() { return default(Vector2); } public float y; }
    public class Mouse { public static Mouse current; public ButtonControl leftButton, rightButton; public Vec2Ctl position, scroll;
        }
    public class Keyboard { public static Keyboard current; public ButtonControl wKey, aKey, sKey, dKey, upArrowKey, downArrowKey, leftArrowKey, rightArrowKey, shiftKey, escapeKey; }
}
namespace TMPro { using UnityEngine; public class TMP_Text : UnityEngine.UI.Graphic { public RectTransform rectTransform; public float outlineWidth; public Color32 outlineColor; public string text; public float fontSize; public TMP_FontAsset font; public TextAlignmentOptions alignment; }
    public class TextMeshProUGUI : TMP_Text { } public class TextMeshPro : TMP_Text { } public static class TMP_Settings { public static TMP_FontAsset defaultFontAsset; } public class TMP_FontAsset : UnityEngine.Object { } public enum TextAlignmentOptions { Center, TopLeft, Left }
    public class TMP_InputField : UnityEngine.Component { public string text; public RectTransform textViewport; public TMP_Text textComponent; public UnityEngine.UI.Graphic placeholder; } }
namespace Fusion
{
    using UnityEngine;
    public struct NetworkId { public uint Raw; public bool IsValid { get { return Raw != 0; } } }
    public struct PlayerRef { public static PlayerRef None; public static bool operator ==(PlayerRef a, PlayerRef b) { return true; } public static bool operator !=(PlayerRef a, PlayerRef b) { return false; }
        public override bool Equals(object o) { return true; } public override int GetHashCode() { return 0; } }
    public struct NetworkBool { public static implicit operator bool(NetworkBool b) { return true; } public static implicit operator NetworkBool(bool b) { return default(NetworkBool); } }
    public interface _Size { } public struct _16 : _Size { } public struct _32 : _Size { } public struct _128 : _Size { }
    public struct NetworkString<T> where T : _Size { public static implicit operator NetworkString<T>(string s) { return default(NetworkString<T>); } public override string ToString() { return ""; } }
    public class NetworkedAttribute : Attribute { }
    public enum RpcSources { InputAuthority, StateAuthority } public enum RpcTargets { StateAuthority, InputAuthority }
    public class RpcAttribute : Attribute { public RpcAttribute(RpcSources s, RpcTargets t) { } }
    public class NetworkObject : UnityEngine.MonoBehaviour { public bool IsValid; public NetworkId Id; public PlayerRef InputAuthority; public bool HasInputAuthority, HasStateAuthority; }
    public class NetworkRunner : UnityEngine.MonoBehaviour { public float DeltaTime; public bool IsServer, IsRunning; public System.Collections.Generic.IEnumerable<PlayerRef> ActivePlayers; public PlayerRef LocalPlayer;
        public NetworkObject Spawn(NetworkObject p, Vector3 pos, Quaternion r, PlayerRef owner) { return null; } public void Despawn(NetworkObject o) { }
        public bool TryFindObject(NetworkId id, out NetworkObject o) { o = null; return false; } }
    public class SimulationBehaviour : UnityEngine.MonoBehaviour { public NetworkRunner Runner; }
    public class NetworkBehaviour : SimulationBehaviour { public NetworkObject Object; public bool HasStateAuthority;
        public virtual void Spawned() { } public virtual void Despawned(NetworkRunner r, bool h) { } public virtual void FixedUpdateNetwork() { } }
    public interface IPlayerJoined { void PlayerJoined(PlayerRef p); }
}
public class GameLauncher : UnityEngine.MonoBehaviour { public Fusion.NetworkRunner Runner; public static string LastMessage = ""; public void StartHost(string n) { } public void StartClient(string n) { } public static GameLauncher Instance; public int SlotOf(Fusion.PlayerRef p) { return 1; } public void ReturnToMenu(string m) { } }

namespace UnityEngine { public class Object2 { } }
namespace Fusion { public class NetworkTransform : UnityEngine.MonoBehaviour { } }
namespace UnityEditor
{
    using UnityEngine;
    public class MenuItemAttribute : System.Attribute { public MenuItemAttribute(string s) { } public MenuItemAttribute(string s, bool v) { } }
    public class SerializedProperty { public string stringValue; public float floatValue; public Vector2 vector2Value; public UnityEngine.Object objectReferenceValue; public int arraySize;
        public SerializedProperty FindPropertyRelative(string n) { return null; } public SerializedProperty GetArrayElementAtIndex(int i) { return null; } }
    public class SerializedObject { public SerializedObject(UnityEngine.Object o) { } public SerializedProperty FindProperty(string n) { return null; } public void ApplyModifiedPropertiesWithoutUndo() { } }
    public static class AssetDatabase { public static bool IsValidFolder(string p) { return true; } public static void CreateFolder(string a, string b) { } public static T LoadAssetAtPath<T>(string p) where T : UnityEngine.Object { return null; }
        public static void CreateAsset(UnityEngine.Object o, string p) { } public static bool DeleteAsset(string p) { return true; } public static void SaveAssets() { } public static void Refresh() { } }
    public static class PrefabUtility { public static GameObject SaveAsPrefabAsset(GameObject g, string p) { return g; } public static UnityEngine.Object InstantiatePrefab(UnityEngine.Object o, Transform t) { return null; } }
}
namespace UnityEditor.SceneManagement { public static class EditorSceneManager { public static void MarkSceneDirty(UnityEngine.SceneManagement.Scene s) { } public static UnityEngine.SceneManagement.Scene GetActiveScene() { return default(UnityEngine.SceneManagement.Scene); }
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m) { return default(UnityEngine.SceneManagement.Scene); }
        public static UnityEditor.SceneAsset playModeStartScene; public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string path = null) { return true; } public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return true; } } }
namespace UnityEngine.SceneManagement { public struct Scene { } }

namespace UnityEngine { public class Canvas : Component { public RenderMode renderMode; } public enum RenderMode { ScreenSpaceOverlay } }
namespace UnityEngine.InputSystem.UI { public class InputSystemUIInputModule : UnityEngine.Component { } }
namespace UnityEditor.Events { public static class UnityEventTools { public static void AddPersistentListener(UnityEngine.UI.Button.ButtonClickedEvent e, System.Action a) { } } }
// Stand-in with the same signatures as Assets/Scripts/AI/AiGatewayClient.cs (the real one needs UnityWebRequest).
public class AiGatewayClient
{
    public AiGatewayClient(string baseUrl = "", int timeoutSeconds = 40) { }
    public System.Threading.Tasks.Task<(RTS.Core.Rules.AiAnswer answer, string error)> Ask(RTS.Core.Rules.AiSafeSnapshot snapshot, string question) { return null; }
    public System.Threading.Tasks.Task<(RTS.Core.Rules.AiAnswer answer, string error)> AskWhy() { return null; }
}
namespace UnityEditor.SceneManagement { public enum NewSceneSetup { DefaultGameObjects, EmptyScene } public enum NewSceneMode { Single } }
namespace UnityEditor { public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string p, bool e) { } } public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; } }
namespace UnityEditor { public class SceneAsset : UnityEngine.Object { } public class InitializeOnLoadAttribute : System.Attribute { }
    public static class EditorUtility { public static bool DisplayDialog(string t, string m, string ok, string cancel) { return true; } }
    public static class EditorPrefs { public static bool GetBool(string k, bool d) { return d; } public static void SetBool(string k, bool v) { } }
    public static class EditorApplication { public delegate void CallbackFunction(); public static CallbackFunction delayCall; }
    public static class Menu { public static void SetChecked(string p, bool c) { } }
    public partial class MenuItemAttribute2 { } }
namespace UnityEditor.SceneManagement { public static partial class EditorSceneManagerExt { } }
