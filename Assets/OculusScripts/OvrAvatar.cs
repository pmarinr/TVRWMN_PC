using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if AVATAR_INTERNAL
using UnityEngine.Events;
#endif

[System.Serializable]
public class AvatarLayer
{
    public int layerIndex;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(AvatarLayer))]
public class AvatarLayerPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, GUIContent.none, property);
        SerializedProperty layerIndex = property.FindPropertyRelative("layerIndex");
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        layerIndex.intValue = EditorGUI.LayerField(position, layerIndex.intValue);
        EditorGUI.EndProperty();
    }
}
#endif

[System.Serializable]
public class PacketRecordSettings
{
    internal bool RecordingFrames = false;
    public float UpdateRate = 1f / 30f; // 30 hz update of packets
    internal float AccumulatedTime;
};

public class OvrAvatar : MonoBehaviour
{
    [Header("Avatar")]
    public IntPtr sdkAvatar = IntPtr.Zero;
    public string oculusUserID;
    

    [Header("Capabilities")]
    public bool EnableBody = true;
    public bool EnableHands = true;
    public bool EnableBase = true;
    public bool EnableExpressive = false;

    [Header("Network")]
    public bool RecordPackets;
    public bool UseSDKPackets = true;
    public PacketRecordSettings PacketSettings = new PacketRecordSettings();

    [Header("Visibility")]
    public bool StartWithControllers;
    public AvatarLayer FirstPersonLayer;
    public AvatarLayer ThirdPersonLayer;
    public bool ShowFirstPerson = true;
    public bool ShowThirdPerson;
    

    [Header("Performance")]

#if UNITY_ANDROID && UNITY_5_5_OR_NEWER
    [Tooltip(
        "Enable to use combined meshes to reduce draw calls. Currently only available on mobile devices. " +
        "Will be forced to false on PC.")]
    private bool CombineMeshes = true;
#else
    private bool CombineMeshes = false;
#endif
    [Tooltip(
        "Enable to use transparent queue, disable to use geometry queue. Requires restart to take effect.")]
    public bool UseTransparentRenderQueue = true;

    [Header("Shaders")]
    public Shader Monochrome_SurfaceShader;
    public Shader Monochrome_SurfaceShader_SelfOccluding;
    public Shader Monochrome_SurfaceShader_PBS;
    public Shader Skinshaded_SurfaceShader_SingleComponent;
    public Shader Skinshaded_VertFrag_SingleComponent;
    public Shader Skinshaded_VertFrag_CombinedMesh;
    public Shader Skinshaded_Expressive_SurfaceShader_SingleComponent;
    public Shader Skinshaded_Expressive_VertFrag_SingleComponent;
    public Shader Skinshaded_Expressive_VertFrag_CombinedMesh;
    public Shader Loader_VertFrag_CombinedMesh;
    public Shader EyeLens;
    public Shader ControllerShader;

    [Header("Other")]
    public bool CanOwnMicrophone = true;
    [Tooltip(
        "Enable laughter detection and animation as part of OVRLipSync.")]
    public bool EnableLaughter = true;
    public GameObject MouthAnchor;
    public Transform LeftHandCustomPose;
    public Transform RightHandCustomPose;

    // Avatar asset
    private HashSet<UInt64> assetLoadingIds = new HashSet<UInt64>();
    private bool assetsFinishedLoading = false;

    // Material manager
   
    private bool waitingForCombinedMesh = false;

    // Global expressive system initialization
    private static bool doneExpressiveGlobalInit = false;

    // Clothing offsets
    private Vector4 clothingAlphaOffset = new Vector4(0f, 0f, 0f, 1f);
    private UInt64 clothingAlphaTexture = 0;

    // Lipsync
   

    // Consts
#if UNITY_ANDROID
    private const bool USE_MOBILE_TEXTURE_FORMAT = true;
#else
    private const bool USE_MOBILE_TEXTURE_FORMAT = false;
#endif
    private static readonly Vector3 MOUTH_HEAD_OFFSET = new Vector3(0, -0.085f, 0.09f);
    private const string MOUTH_HELPER_NAME = "MouthAnchor";
    // Initial 'silence' score, 14 viseme scores and 1 laughter score as last element
    private const int VISEME_COUNT = 16;
    // Lipsync animation speeds
    private const float ACTION_UNIT_ONSET_SPEED = 30f;
    private const float ACTION_UNIT_FALLOFF_SPEED = 20f;
    private const float VISEME_LEVEL_MULTIPLIER = 1.5f;

    // Internals
    internal UInt64 oculusUserIDInternal;
   
#if AVATAR_INTERNAL
    public AvatarControllerBlend BlendController;
    
#endif
    public UnityEvent AssetsDoneLoading = new UnityEvent();

    // Avatar packets
    public class PacketEventArgs : EventArgs
    {
        public readonly int Packet;
        public PacketEventArgs(int packet)
        {
            Packet = packet;
        }
    }
    private int CurrentUnityPacket;
    public EventHandler<PacketEventArgs> PacketRecorded;

    public enum HandType
    {
        Right,
        Left,

        Max
    };

    public enum HandJoint
    {
        HandBase,
        IndexBase,
        IndexTip,
        ThumbBase,
        ThumbTip,

        Max,
    }

    private static string[,] HandJoints = new string[(int)HandType.Max, (int)HandJoint.Max]
    {
        {
            "hands:r_hand_world",
            "hands:r_hand_world/hands:b_r_hand/hands:b_r_index1",
            "hands:r_hand_world/hands:b_r_hand/hands:b_r_index1/hands:b_r_index2/hands:b_r_index3/hands:b_r_index_ignore",
            "hands:r_hand_world/hands:b_r_hand/hands:b_r_thumb1/hands:b_r_thumb2",
            "hands:r_hand_world/hands:b_r_hand/hands:b_r_thumb1/hands:b_r_thumb2/hands:b_r_thumb3/hands:b_r_thumb_ignore"
        },
        {
            "hands:l_hand_world",
            "hands:l_hand_world/hands:b_l_hand/hands:b_l_index1",
            "hands:l_hand_world/hands:b_l_hand/hands:b_l_index1/hands:b_l_index2/hands:b_l_index3/hands:b_l_index_ignore",
            "hands:l_hand_world/hands:b_l_hand/hands:b_l_thumb1/hands:b_l_thumb2",
            "hands:l_hand_world/hands:b_l_hand/hands:b_l_thumb1/hands:b_l_thumb2/hands:b_l_thumb3/hands:b_l_thumb_ignore"
        }
    };

    static OvrAvatar()
    {
       
    }

    void OnDestroy()
    {
       
    }


    public void CombinedMeshLoadedCallback(IntPtr assetPtr)
    {
       
    }

   
}
