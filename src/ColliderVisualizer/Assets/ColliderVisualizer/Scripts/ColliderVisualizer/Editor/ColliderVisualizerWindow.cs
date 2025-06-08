using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColliderVisualizer.Editor
{
    public class ColliderVisualizerWindow : EditorWindow
    {
        [MenuItem("Window/Collider Visualizer")]
        public new static void Show()
        {
            var window = GetWindow<ColliderVisualizerWindow>();
            window.titleContent = new GUIContent("Collider Visualizer");
        }

        private ColliderVisualizerFeature _feature;

        private void OnEnable()
        {
            _feature = ColliderVisualizerUtility.GetFeature();
        }

        private void OnGUI()
        {
            if (_feature == null)
            {
                _feature = ColliderVisualizerUtility.GetFeature();
            }

            GUILayout.Label("Collider Visualizer Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();


            if (_feature == null)
            {
                EditorGUILayout.HelpBox("ColliderVisualizerFeature が見つかりません。Universal Render Pipeline Asset を確認してください。", MessageType.Info);
                DrawSelectRenderPipelineAssetButton();
                return;
            }

            // ColliderVisualizerFeatureのインスペクタ
            DrawFeatureInspector();
            
            EditorGUILayout.Space();

            // 手動更新ボタン
            DrawUpdateCollidersButton();

            // Featureアセット選択ボタン
            DrawSelectFeatureAssetButton();

            // URPアセット選択ボタン
            DrawSelectRenderPipelineAssetButton();
            
            EditorGUILayout.Space();

            // 現在ColliderManager内にあるコライダーのリスト
            DrawColliderLists();
        }

        // ColliderVisualizerFeatureのインスペクタを描画
        private void DrawFeatureInspector()
        {
            try
            {
                var serializedObject = new SerializedObject(_feature);
                serializedObject.Update();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_visualizeCollisions"), new GUIContent("Visualize Collisions"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_collisionColor"), new GUIContent("Collision Color"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_visualizeTriggers"), new GUIContent("Visualize Triggers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_triggerColor"), new GUIContent("Trigger Color"));
                EditorGUILayout.Slider(serializedObject.FindProperty("_alpha"), 0f, 1f, new GUIContent("Alpha"));
                EditorGUILayout.IntSlider(serializedObject.FindProperty("_meshQuality"), 1, 10, new GUIContent("Mesh Quality"));
                serializedObject.ApplyModifiedProperties();
            }
            catch (SystemException e) when (e is NullReferenceException or MissingReferenceException)
            {
                // Featureが削除されたりしたときのエラーは無視する
            }
        }

        // 手動更新ボタンを描画
        private void DrawUpdateCollidersButton()
        {
            if (GUILayout.Button("可視化対象コライダーリストを更新"))
            {
                ColliderManager.Instance.FindAndUpdateColliders();
                SceneView.RepaintAll();
            }
        }

        // Featureアセット選択ボタンを描画
        private void DrawSelectFeatureAssetButton()
        {
            if (GUILayout.Button("Collider Visualizer Feature アセットを選択"))
            {
                if (_feature == null) return;
                Selection.activeObject = _feature;
                EditorGUIUtility.PingObject(_feature);
            }
        }

        // URPアセットを選択するボタンを描画
        private void DrawSelectRenderPipelineAssetButton()
        {
            if (GUILayout.Button("URP アセットを選択"))
            {
                var urpAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
                if (urpAsset == null) return;
                Selection.activeObject = urpAsset;
                EditorGUIUtility.PingObject(urpAsset);
            }
        }

        private bool _showCollisionColliders = true;
        private bool _showTriggerColliders = true;

        // 現在ColliderManager内にあるコライダーのリストを描画
        private void DrawColliderLists()
        {
            // リストを描画する内部関数
            void DrawList(bool isTrigger, Collider[] colliders)
            {
                string label = isTrigger ? "Trigger Colliders" : "Collision Colliders";
                ref bool foldout = ref isTrigger ? ref _showTriggerColliders : ref _showCollisionColliders;

                foldout = EditorGUILayout.Foldout(foldout, $"{label} ({colliders.Length})", true);
                if (foldout)
                {
                    EditorGUI.indentLevel++;
                    using var _ = new EditorGUILayout.VerticalScope("box");
                    DrawListItems(colliders);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
            }

            // リストアイテムを描画する内部関数
            void DrawListItems(Collider[] colliders)
            {
                if (colliders.Length == 0)
                {
                    EditorGUILayout.LabelField("No colliders.");
                    return;
                }

                for (int i = 0; i < colliders.Length; i++)
                {
                    using var _ = new EditorGUI.DisabledScope(true);
                    if (colliders[i] != null)
                    {
                        EditorGUILayout.ObjectField($"[{i}]", colliders[i], typeof(Collider), true);
                    }
                }
            }
            // ここまで内部関数

            // コライダーリストのヘッダ
            EditorGUILayout.LabelField("Collider List", EditorStyles.boldLabel);

            // コライダーリストの描画
            var collidersDict = ColliderManager.Instance.GetAllColliders();
            var collisionColliders = collidersDict.ContainsKey(false) ? collidersDict[false] : Array.Empty<Collider>();
            var triggerColliders = collidersDict.ContainsKey(true) ? collidersDict[true] : Array.Empty<Collider>();
            DrawList(true, collisionColliders);
            DrawList(false, triggerColliders);
        }
    }
}