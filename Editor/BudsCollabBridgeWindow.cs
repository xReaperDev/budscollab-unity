using System;
using System.Collections.Generic;
using BudsCollab.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BudsCollab.Unity.Editor
{
    public sealed class BudsCollabBridgeWindow : EditorWindow
    {
        private const string WorkspaceEndpoint = "/api/creator-tools/workspace";
        private const string DefaultApiBaseUrl = "https://app.budscollab.com";

        private enum ConnectionState
        {
            NotConnected,
            Loading,
            Connected,
            ConnectionFailed,
            AssetCheckPassed,
            AssetNeedsAttention
        }

        [Serializable]
        private sealed class WorkspaceResponse
        {
            public bool ok;
            public string error;
            public WorkspaceSpace[] spaces;
        }

        [Serializable]
        private sealed class WorkspaceSpace
        {
            public string spaceId;
            public string name;
            public string role;
            public string openUrl;
            public WorkspaceRoom[] rooms;
        }

        [Serializable]
        private sealed class WorkspaceRoom
        {
            public string roomId;
            public string name;
            public string emoji;
            public string openUrl;
        }

        private readonly List<WorkspaceSpace> spaces = new();
        private readonly List<WorkspaceRoom> roomsForSelectedSpace = new();
        private ConnectionState state = ConnectionState.NotConnected;
        private string apiBaseUrl = DefaultApiBaseUrl;
        private string accessToken = string.Empty;
        private string status = "Not connected";
        private string assetCheckSummary = "No asset checked";
        private int selectedSpaceIndex;
        private int selectedRoomIndex;
        private Vector2 scroll;
        private UnityWebRequest activeRequest;

        [MenuItem("Window/BudsCollab")]
        public static void Open()
        {
            var window = GetWindow<BudsCollabBridgeWindow>("BudsCollab");
            window.minSize = new Vector2(380, 460);
        }

        private void OnDisable()
        {
            activeRequest?.Dispose();
            activeRequest = null;
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("BudsCollab for Unity", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            DrawConnection();
            DrawDestination();
            DrawSceneTools();

            EditorGUILayout.EndScrollView();
        }

        private void DrawConnection()
        {
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.EnumPopup("State", state);
            EditorGUILayout.LabelField("Status", status, EditorStyles.wordWrappedLabel);
            apiBaseUrl = EditorGUILayout.TextField("BudsCollab URL", apiBaseUrl);
            accessToken = EditorGUILayout.PasswordField("Access Token", accessToken);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open BudsCollab Login"))
                {
                    Application.OpenURL($"{NormalizeApiBaseUrl(apiBaseUrl)}/sign-in");
                }

                using (new EditorGUI.DisabledScope(state == ConnectionState.Loading))
                {
                    if (GUILayout.Button("Connect and Load Spaces"))
                    {
                        RefreshWorkspace();
                    }
                }
            }
        }

        private void DrawDestination()
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Destination", EditorStyles.boldLabel);

            if (spaces.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Connect to BudsCollab to load your spaces and rooms.",
                    MessageType.Info
                );
                return;
            }

            selectedSpaceIndex = EditorGUILayout.Popup(
                "Space",
                Mathf.Clamp(selectedSpaceIndex, 0, spaces.Count - 1),
                SpaceLabels()
            );
            RebuildRoomsForSelectedSpace();

            if (roomsForSelectedSpace.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This space has no rooms visible to the current token.",
                    MessageType.Warning
                );
                return;
            }

            selectedRoomIndex = EditorGUILayout.Popup(
                "Room",
                Mathf.Clamp(selectedRoomIndex, 0, roomsForSelectedSpace.Count - 1),
                RoomLabels()
            );

            if (GUILayout.Button("Open Selected Room"))
            {
                OpenSelectedRoom();
            }
        }

        private void DrawSceneTools()
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Scene Tools", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Asset Check", assetCheckSummary, EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Check Selected Objects"))
            {
                CheckSelectedObjects();
            }
        }

        private void RefreshWorkspace()
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                state = ConnectionState.ConnectionFailed;
                status = "Access token required";
                Repaint();
                return;
            }

            activeRequest?.Dispose();
            activeRequest = UnityWebRequest.Get($"{NormalizeApiBaseUrl(apiBaseUrl)}{WorkspaceEndpoint}");
            activeRequest.SetRequestHeader("Authorization", $"Bearer {accessToken.Trim()}");
            activeRequest.SetRequestHeader("Accept", "application/json");
            activeRequest.SetRequestHeader("User-Agent", "BudsCollab-Unity/0.1.1");

            state = ConnectionState.Loading;
            status = "Loading BudsCollab spaces...";
            var operation = activeRequest.SendWebRequest();
            EditorApplication.update += PollWorkspaceRequest;

            void PollWorkspaceRequest()
            {
                if (!operation.isDone)
                {
                    return;
                }

                EditorApplication.update -= PollWorkspaceRequest;
                CompleteWorkspaceRequest(activeRequest);
                activeRequest.Dispose();
                activeRequest = null;
                Repaint();
            }
        }

        private void CompleteWorkspaceRequest(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                state = ConnectionState.ConnectionFailed;
                status = $"Connection failed: HTTP {request.responseCode} {request.error}";
                return;
            }

            var response = JsonUtility.FromJson<WorkspaceResponse>(request.downloadHandler.text);
            if (response == null || !response.ok)
            {
                state = ConnectionState.ConnectionFailed;
                status = $"BudsCollab rejected the token: {response?.error ?? "invalid_response"}";
                return;
            }

            spaces.Clear();
            if (response.spaces != null)
            {
                spaces.AddRange(response.spaces);
            }

            selectedSpaceIndex = 0;
            selectedRoomIndex = 0;
            RebuildRoomsForSelectedSpace();
            state = ConnectionState.Connected;
            status = $"Loaded {spaces.Count} space(s), {CountRooms()} room(s)";
        }

        private void OpenSelectedRoom()
        {
            var room = SelectedRoom();
            if (room == null || string.IsNullOrWhiteSpace(room.openUrl))
            {
                status = "Select a loaded room first";
                return;
            }

            Application.OpenURL(room.openUrl);
            status = $"Opened {room.name}";
        }

        private void CheckSelectedObjects()
        {
            var report = BudsCollabSelectionValidator.Validate(Selection.gameObjects);
            assetCheckSummary = report.Summary;
            state = report.Ok ? ConnectionState.AssetCheckPassed : ConnectionState.AssetNeedsAttention;
            status = report.Ok ? "Asset check passed" : "Asset check needs attention";
        }

        private void RebuildRoomsForSelectedSpace()
        {
            roomsForSelectedSpace.Clear();
            var space = SelectedSpace();
            if (space?.rooms == null)
            {
                selectedRoomIndex = 0;
                return;
            }

            roomsForSelectedSpace.AddRange(space.rooms);
            selectedRoomIndex = Mathf.Clamp(selectedRoomIndex, 0, Math.Max(roomsForSelectedSpace.Count - 1, 0));
        }

        private WorkspaceSpace SelectedSpace()
        {
            if (spaces.Count == 0)
            {
                return null;
            }

            selectedSpaceIndex = Mathf.Clamp(selectedSpaceIndex, 0, spaces.Count - 1);
            return spaces[selectedSpaceIndex];
        }

        private WorkspaceRoom SelectedRoom()
        {
            if (roomsForSelectedSpace.Count == 0)
            {
                return null;
            }

            selectedRoomIndex = Mathf.Clamp(selectedRoomIndex, 0, roomsForSelectedSpace.Count - 1);
            return roomsForSelectedSpace[selectedRoomIndex];
        }

        private string[] SpaceLabels()
        {
            var labels = new string[spaces.Count];
            for (var i = 0; i < spaces.Count; i++)
            {
                var space = spaces[i];
                labels[i] = $"{space.name} ({space.role ?? "viewer"})";
            }

            return labels;
        }

        private string[] RoomLabels()
        {
            var labels = new string[roomsForSelectedSpace.Count];
            for (var i = 0; i < roomsForSelectedSpace.Count; i++)
            {
                var room = roomsForSelectedSpace[i];
                labels[i] = string.IsNullOrWhiteSpace(room.emoji)
                    ? room.name
                    : $"{room.emoji} {room.name}";
            }

            return labels;
        }

        private int CountRooms()
        {
            var count = 0;
            foreach (var space in spaces)
            {
                count += space.rooms?.Length ?? 0;
            }

            return count;
        }

        private static string NormalizeApiBaseUrl(string value)
        {
            var trimmed = string.IsNullOrWhiteSpace(value) ? DefaultApiBaseUrl : value.Trim();
            return trimmed.TrimEnd('/');
        }
    }
}
