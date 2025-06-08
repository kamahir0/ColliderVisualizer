using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColliderVisualizer
{
    /// <summary>
    /// コライダー可視化描画パスを作るFeatureクラス
    /// </summary>
    internal class ColliderVisualizerFeature : ScriptableRendererFeature
    {
        [SerializeField] private bool _visualizeCollisions = true;
        [SerializeField] private Color _collisionColor = Color.green;
        [SerializeField] private bool _visualizeTriggers = true;
        [SerializeField] private Color _triggerColor = Color.blue;
        [Range(0, 1), SerializeField] private float _alpha = 0.5f;
        [Range(1, 10), SerializeField] private int _meshQuality = 4;

        private ColliderVisualizePass _pass;
        
        /// <summary> 現在の設定 </summary>
        public ColliderVisualSettings Settings => new(_visualizeCollisions, _collisionColor, _visualizeTriggers, _triggerColor, _alpha, _meshQuality);

        /// <inheritdoc />
        public override void Create()
        {
            if (ColliderVisualizerUtility.IsMetal && ColliderVisualizerUtility.IsMSAAEnabled)
            {
                Debug.LogWarning("MSAAが有効なiOSビルドではColliderVisualizerは動作しません。MSAAをDisableにすることで使用可能になります");
                return;
            }
            
            var settings = new ColliderVisualSettings(
                _visualizeCollisions, 
                _collisionColor, 
                _visualizeTriggers, 
                _triggerColor, 
                _alpha, 
                _meshQuality);
            _pass = new ColliderVisualizePass(settings);
            
#if UNITY_EDITOR
            OnCreateEditor();
#endif
        }

        /// <inheritdoc />
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (ColliderVisualizerUtility.IsMetal && ColliderVisualizerUtility.IsMSAAEnabled) return;
            renderer.EnqueuePass(_pass);
        }

        /// <summary> ScriptableRendererPassが作成済みであるか </summary>
        public bool IsExistsPass => _pass != null;

        /// <summary>
        /// 設定を適用する
        /// </summary>
        public void ApplySettings(ColliderVisualSettings settings)
        {
            if (_pass == null) return;
#if UNITY_EDITOR
            SerializeSettings(settings);
#else
            _pass.ApplySettings(settings);
            _visualizeCollisions = settings.VisualizeCollisions;
            _collisionColor = settings.CollisionColor;
            _visualizeTriggers = settings.VisualizeTriggers;
            _triggerColor = settings.TriggerColor;
            _alpha = settings.Alpha;
            _meshQuality = settings.MeshQuality;
#endif
        }

        /// <summary>
        /// コライダーのリストを更新する
        /// </summary>
        public void UpdateColliders(
            BoxCollider[] boxCollidersCollision, BoxCollider[] boxCollidersTrigger, 
            SphereCollider[] sphereCollidersCollision, SphereCollider[] sphereCollidersTrigger, 
            CapsuleCollider[] capsuleCollidersCollision, CapsuleCollider[] capsuleCollidersTrigger, 
            MeshCollider[] meshCollidersCollision, MeshCollider[] meshCollidersTrigger)
        {
            _pass?.UpdateColliders(boxCollidersCollision, boxCollidersTrigger, sphereCollidersCollision, sphereCollidersTrigger, capsuleCollidersCollision, capsuleCollidersTrigger, meshCollidersCollision, meshCollidersTrigger);
        }
        
#if UNITY_EDITOR
        private bool _isFirstCreate = true;
        private void OnCreateEditor()
        {
            if (_isFirstCreate)
            {
                // 初回Create時 = ColliderVisualizerFeatureがURPアセットに追加されたとき
                _isFirstCreate = false;
                // シーンを走査してコライダーを更新
                EditorApplication.delayCall += ColliderManager.Instance.FindAndUpdateColliders;
                // 再生中に追加された場合はColliderTracker出す
                if (Application.isPlaying) ColliderTracker.Initialize();
            }
            else
            {
                // シーンを走査せずColliderManagerに残っているコライダーで更新
                ColliderManager.Instance.UpdateColliders();
            }
        }
            
        // 設定をシリアライズする
        private void SerializeSettings(ColliderVisualSettings settings)
        {
            var so = new SerializedObject(this);
            so.Update();
            so.FindProperty(nameof(_visualizeCollisions)).boolValue = settings.VisualizeCollisions;
            so.FindProperty(nameof(_collisionColor)).colorValue = settings.CollisionColor;
            so.FindProperty(nameof(_visualizeTriggers)).boolValue = settings.VisualizeTriggers;
            so.FindProperty(nameof(_triggerColor)).colorValue = settings.TriggerColor;
            so.FindProperty(nameof(_alpha)).floatValue = settings.Alpha;
            so.FindProperty(nameof(_meshQuality)).intValue = settings.MeshQuality;
            so.ApplyModifiedProperties();
        }
#endif
    }
}