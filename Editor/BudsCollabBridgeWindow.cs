using UnityEditor;
using UnityEngine;

namespace BudsCollab.Bridge.Editor
{
    public sealed class BudsCollabBridgeWindow : EditorWindow
    {
        private enum BridgeState
        {
            NotLoggedIn,
            Connected,
            SceneNotValid,
            ReadyToUpload,
            Uploading,
            Published,
            NeedsResync
        }

        private BridgeState state = BridgeState.NotLoggedIn;
        private string spaceId = "Select a space";
        private string roomId = "Select a room";
        private string wallId = "Optional wall";
        private Vector2 scroll;

        [MenuItem("Window/BudsCollab")]
        public static void Open()
        {
            var window = GetWindow<BudsCollabBridgeWindow>("BudsCollab");
            window.minSize = new Vector2(360, 520);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("BudsCollab for Unity", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            DrawConnection();
            DrawDestination();
            DrawAssetBrowser();
            DrawValidation();
            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        private void DrawConnection()
        {
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.EnumPopup("State", state);
            if (GUILayout.Button("Login"))
            {
                state = BridgeState.Connected;
            }
            if (GUILayout.Button("Advertise Unity Presence"))
            {
                Debug.Log("BudsCollab bridge presence placeholder: Unity is open.");
            }
        }

        private void DrawDestination()
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Space / Room / Wall", EditorStyles.boldLabel);
            spaceId = EditorGUILayout.TextField("Space", spaceId);
            roomId = EditorGUILayout.TextField("Room", roomId);
            wallId = EditorGUILayout.TextField("Wall", wallId);
        }

        private void DrawAssetBrowser()
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Buds Assets", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Asset list will be populated from BudsCollab bridge jobs and room assets.",
                MessageType.Info
            );
            if (GUILayout.Button("Import from BudsCollab"))
            {
                Debug.Log("Import asset placeholder.");
            }
        }

        private void DrawValidation()
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Validator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Checks mesh bounds, poly count, textures, missing materials, scale, file size, animation clips, and web preview compatibility.",
                MessageType.None
            );
            if (GUILayout.Button("Validate Selection"))
            {
                state = BridgeState.ReadyToUpload;
                Debug.Log("Validation placeholder: selected object is ready to upload.");
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Publish", EditorStyles.boldLabel);
            if (GUILayout.Button("Upload Selected"))
            {
                state = BridgeState.Uploading;
                Debug.Log("Upload selected placeholder.");
            }
            if (GUILayout.Button("Open Web Preview"))
            {
                Application.OpenURL("https://app.budscollab.com/preview/3d");
            }
            if (GUILayout.Button("Publish to Room"))
            {
                state = BridgeState.Published;
                Debug.Log("Publish to room placeholder.");
            }
        }
    }
}
