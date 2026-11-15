// Minimal stand-ins for Unity/Fusion/TMP types, ONLY to catch typos and type errors in our scripts outside Unity.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public static void Destroy(Object o) { } public static T[] FindObjectsByType<T>(FindObjectsSortMode m) where T : Object { return new T[0]; }
        public static implicit operator bool(Object o) { return o != null; } public string name; }
    public enum FindObjectsSortMode { None }
    public class Component : Object { public GameObject gameObject; public Transform transform;
        public T GetComponent<T>() { return default(T); } public T GetComponentInParent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>() { return new T[0]; } }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object { public Transform transform; public bool activeSelf;
        public GameObject() { } public GameObject(string n, params Type[] t) { }
        public T GetComponent<T>() { return default(T); } public T[] GetComponentsInChildren<T>() { return new T[0]; }
        public T AddComponent<T>() { return default(T); } public void SetActive(bool b) { }
        public static GameObject CreatePrimitive(PrimitiveType t) { return null; } }
    public enum PrimitiveType { Cube, Cylinder, Quad }
    public class Transform : Component { public Vector3 position, localScale; public Quaternion rotation; public Vector3 forward;
        public void SetParent(Transform p, bool w) { } }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, offsetMin, offsetMax; }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero, one; public float magnitude { get { return 0; } }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return a; } public static implicit operator Vector3(Vector2 v) { return default(Vector3); } }
    public struct Vector3 { public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up, zero; public float magnitude { get { return 0; } } public float sqrMagnitude { get { return 0; } } public Vector3 normalized { get { return this; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; } public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float b) { return a; } public static Vector3 operator *(float b, Vector3 a) { return a; } }
    public struct Quaternion { public static Quaternion identity; public static Quaternion Euler(float x, float y, float z) { return default(Quaternion); } }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; } public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color white, black, red, cyan, yellow; }
    public struct Color32 { public Color32(byte r, byte g, byte b, byte a) { } }
    public struct Rect { public static Rect MinMaxRect(float a, float b, float c, float d) { return default(Rect); } public bool Contains(Vector3 v) { return true; } }
    public struct Ray { public Vector3 GetPoint(float d) { return default(Vector3); } }
    public struct Plane { public Plane(Vector3 n, Vector3 p) { } public bool Raycast(Ray r, out float d) { d = 0; return true; } }
    public struct RaycastHit { public Collider collider; public Vector3 point; }
    public class Collider : Component { }
    public static class Physics { public static bool Raycast(Ray r, out RaycastHit h, float d) { h = default(RaycastHit); return false; } }
    public class Camera : Component { public static Camera main; public Ray ScreenPointToRay(Vector3 p) { return default(Ray); } public Vector3 WorldToScreenPoint(Vector3 p) { return p; } }
    public static class Mathf { public static float Max(float a, float b) { return a; } public static int Max(int a, int b) { return a; } public static float Min(float a, float b) { return a; } public static float Abs(float a) { return a; }
        public static float Clamp(float a, float b, float c) { return a; } public static int RoundToInt(float f) { return 0; } public static int CeilToInt(float f) { return 0; } }
    public static class Time { public static float time, unscaledTime, unscaledDeltaTime, deltaTime; }
    public static class Debug { public static void Log(object o) { } public static void LogWarning(object o) { } public static void LogError(object o) { } }
    public static class Application { public static string dataPath, persistentDataPath, streamingAssetsPath; }
    public static class Screen { public static int height; }
    public static class Gizmos { public static Color color; public static void DrawWireSphere(Vector3 p, float r) { } public static void DrawWireCube(Vector3 p, Vector3 s) { } }
    public static class GUI { public static Color color; public static void DrawTexture(Rect r, Texture t) { } }
    public class Shader { public static Shader Find(string n) { return null; } }
    public class Material : Object { public Material(Shader s) { } public Color color; public Texture mainTexture; }
    public class Texture : Object { } 
    public class Texture2D : Texture { public static Texture2D whiteTexture; public FilterMode filterMode; public TextureWrapMode wrapMode;
        public Texture2D(int w, int h, TextureFormat f, bool m) { } public void SetPixels32(Color32[] c) { } public void Apply(bool b) { } }
    public enum TextureFormat { RGBA32 } public enum FilterMode { Bilinear } public enum TextureWrapMode { Clamp }
    public class Renderer : Component { public bool enabled; public Material sharedMaterial, material; }
    public class SerializeFieldAttribute : Attribute { } public class HideInInspectorAttribute : Attribute { }
    public class RequireComponentAttribute : Attribute { public RequireComponentAttribute(Type t) { } }
}
namespace UnityEngine.UI { public class Image : Component { } public class LayoutElement : Component { public float preferredHeight, minHeight; }
    public class Button : Component { public bool interactable; public ButtonClickedEvent onClick = new ButtonClickedEvent(); public class ButtonClickedEvent { public void AddListener(Action a) { } } } }
namespace UnityEngine.EventSystems { public class EventSystem { public static EventSystem current; public bool IsPointerOverGameObject() { return false; } } }
namespace UnityEngine.InputSystem {
    public class ButtonControl { public bool isPressed, wasPressedThisFrame, wasReleasedThisFrame; }
    public class Vec2Ctl { public Vector2 ReadValue() { return default(Vector2); } public float y; }
    public class Mouse { public static Mouse current; public ButtonControl leftButton, rightButton; public Vec2Ctl position, scroll;
        }
    public class Keyboard { public static Keyboard current; public ButtonControl wKey, aKey, sKey, dKey, upArrowKey, downArrowKey, leftArrowKey, rightArrowKey, shiftKey, escapeKey; }
}
namespace TMPro { using UnityEngine; public class TMP_Text : UnityEngine.Component { public string text; public float fontSize; public Color color; public TMP_FontAsset font; public TextAlignmentOptions alignment; }
    public class TextMeshProUGUI : TMP_Text { } public class TMP_FontAsset : UnityEngine.Object { } public enum TextAlignmentOptions { Center }
    public class TMP_InputField : UnityEngine.Component { public string text; } }
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
    public class NetworkObject : UnityEngine.MonoBehaviour { public NetworkId Id; public PlayerRef InputAuthority; public bool HasInputAuthority, HasStateAuthority; }
    public class NetworkRunner : UnityEngine.MonoBehaviour { public float DeltaTime; public bool IsServer; public PlayerRef LocalPlayer;
        public NetworkObject Spawn(NetworkObject p, Vector3 pos, Quaternion r, PlayerRef owner) { return null; } public void Despawn(NetworkObject o) { }
        public bool TryFindObject(NetworkId id, out NetworkObject o) { o = null; return false; } }
    public class SimulationBehaviour : UnityEngine.MonoBehaviour { public NetworkRunner Runner; }
    public class NetworkBehaviour : SimulationBehaviour { public NetworkObject Object; public bool HasStateAuthority;
        public virtual void Spawned() { } public virtual void Despawned(NetworkRunner r, bool h) { } public virtual void FixedUpdateNetwork() { } }
    public interface IPlayerJoined { void PlayerJoined(PlayerRef p); }
}
public class GameLauncher : UnityEngine.MonoBehaviour { public static GameLauncher Instance; public int SlotOf(Fusion.PlayerRef p) { return 1; } public void ReturnToMenu(string m) { } }
