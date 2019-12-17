using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Syy.Tools
{
    /// <summary>
    /// 見本画像をSceneViewに描画してUI位置調整可能にするEditor拡張
    /// </summary>
    public class uGUISampleImage : EditorWindow
    {
        [MenuItem("Window/" + nameof(uGUISampleImage))]
        public static void Open()
        {
            GetWindow<uGUISampleImage>(nameof(uGUISampleImage));
        }

        void OnEnable()
        {
            Init();

            SceneView.duringSceneGui += OnSceneGui;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;
            ResetDraw();
            _commandBuffer.Dispose();
        }

        void Init()
        {
            // mesh作成
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.SetVertices(vertices);
                _mesh.SetUVs(0, uvs);
                _mesh.SetTriangles(triangles, 0);
            }

            // texture作成
            if (_texture == null)
            {
                _texture = new Texture2D(1, 1);
                string tmp = EditorUserSettings.GetConfigValue(Key_TexturePath);
                if (!string.IsNullOrEmpty(tmp) && File.Exists(tmp))
                {
                    _path = tmp;
                    _texture.LoadImage(File.ReadAllBytes(_path));
                }
            }

            // material作成
            if (_material == null)
            {
                _material = new Material(Shader.Find("UI/Default"));
                _material.mainTexture = _texture;
                _material.color = Color.white;
            }

            // commandBuffer作成
            if (_commandBuffer == null)
            {
                _commandBuffer = new CommandBuffer();
                _commandBuffer.name = nameof(uGUISampleImage);
            }

            // rectTransform取得
            if (_targetRectTransform == null)
            {
                _targetRectTransform = (GameObject.FindObjectOfType<Canvas>()?.transform as RectTransform);
            }
        }

        void OnGUI()
        {
            if (!IsInit())
            {
                Init();
            }

            // 描画サイズターゲット
            var canvasGameObject = (GameObject)EditorGUILayout.ObjectField("描画サイズターゲット", _targetRectTransform?.gameObject, typeof(GameObject), true);
            if (_targetRectTransform?.gameObject != canvasGameObject)
            {
                if (canvasGameObject == null)
                {
                    // 参照外されたら描画やめる
                    ResetDraw();
                    _targetRectTransform = null;
                }
                else
                {
                    _targetRectTransform = canvasGameObject?.GetComponent<RectTransform>();
                }
            }

            // 透明度変更
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                _alpha = EditorGUILayout.FloatField("透明度", _alpha);
                _alpha = Mathf.Clamp01(_alpha);
                if (check.changed)
                {
                    _material.color = new Color(1, 1, 1, _alpha);
                }
            }

            // 画像選択
            if (GUILayout.Button("画像選択", GUILayout.Width(150)))
            {
                string tmp = EditorUtility.OpenFilePanelWithFilters("Select Texture", $"{Application.dataPath}/Assets", new string[] { "Image files", "png,jpg,jpeg" });
                if (!string.IsNullOrEmpty(tmp) && File.Exists(tmp))
                {
                    _path = tmp;
                    EditorUserSettings.SetConfigValue(Key_TexturePath, _path);
                    _texture.LoadImage(File.ReadAllBytes(_path));
                }
            }
            EditorGUILayout.LabelField("画像 : " + _path);
            if (_texture != null)
            {
                float ratio = 80 / (float)_texture.height;
                GUILayout.Box(GUIContent.none, GUILayout.Width(ratio * _texture.width), GUILayout.Height(80));
                EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetLastRect(), _texture);
            }
        }

        void OnSceneGui(SceneView sceneView)
        {
            if (_targetRectTransform == null) return;
            if (_texture == null) return;
            if (string.IsNullOrEmpty(_path)) return;

            // texture書き込み
            float ratio = (_targetRectTransform.sizeDelta.y * _targetRectTransform.lossyScale.y) / (float)_texture.height;
            Vector3 scale = new Vector3(_texture.width * ratio, _texture.height * ratio, 1);
            Matrix4x4 matrix = Matrix4x4.TRS(_targetRectTransform.position, _targetRectTransform.rotation, scale);
            _commandBuffer.Clear();
            _commandBuffer.DrawMesh(_mesh, matrix, _material);
            sceneView.camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
            sceneView.camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
        }

        void ResetDraw()
        {
            if (_commandBuffer == null) return;
            foreach (var sceneView in SceneView.sceneViews.Cast<SceneView>())
            {
                sceneView.camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
                sceneView.Repaint();
            }
        }

        bool IsInit()
        {
            return _commandBuffer != null && _mesh != null && _texture != null && _material != null;
        }

        CommandBuffer _commandBuffer;
        Mesh _mesh;
        Texture2D _texture;
        Material _material;

        [SerializeField]
        float _alpha = 1;

        [SerializeField]
        string _path;

        RectTransform _targetRectTransform;

        private static readonly List<Vector3> vertices = new List<Vector3>()
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
        };

        private static readonly List<int> triangles = new List<int>()
        {
            0, 1, 2,
            2, 3, 0,
        };

        private static readonly List<Vector2> uvs = new List<Vector2>()
        {
            new Vector2 (0, 0),
            new Vector2 (0, 1),
            new Vector2 (1, 1),
            new Vector2 (1, 0),
        };

        private static string Key_TexturePath = $"{nameof(uGUISampleImage)}-Key_TexturePath";
    }
}
