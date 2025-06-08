using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace ColliderVisualizer
{
    /// <summary>
    /// 可視化対象コライダーを管理するクラス
    /// </summary>
    internal class ColliderManager
    {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += Instance.FindAndUpdateColliders;
            EditorSceneManager.sceneOpened += (_, _) => Instance.FindAndUpdateColliders();
        }
#endif

        /// <summary> シングルトン </summary>
        public static ColliderManager Instance => _instance ??= new ColliderManager();
        private static ColliderManager _instance;
        
        // 可視化するコライダーのリスト
        private readonly List<BoxCollider> _boxCollidersCollision = new();
        private readonly List<BoxCollider> _boxCollidersTrigger = new();
        private readonly List<SphereCollider> _sphereCollidersCollision = new();
        private readonly List<SphereCollider> _sphereCollidersTrigger = new();
        private readonly List<CapsuleCollider> _capsuleCollidersCollision = new();
        private readonly List<CapsuleCollider> _capsuleCollidersTrigger = new();
        private readonly List<MeshCollider> _meshCollidersCollision = new();
        private readonly List<MeshCollider> _meshCollidersTrigger = new();

        /// <summary> 保持している全てのコライダーを取得する。isTriggerでグループ化されている </summary>
        public IReadOnlyDictionary<bool, Collider[]> GetAllColliders()
        {
            return _boxCollidersCollision
                .Select(x => (Collider)x).Concat(_boxCollidersTrigger)
                .Concat(_sphereCollidersCollision).Concat(_sphereCollidersTrigger)
                .Concat(_capsuleCollidersCollision).Concat(_capsuleCollidersTrigger)
                .Concat(_meshCollidersCollision).Concat(_meshCollidersTrigger)
                .Where(x => x != null)
                .GroupBy(x => x.isTrigger).ToDictionary(
                    group => group.Key,
                    group => group.ToArray());
        }
        
        /// <summary>
        /// シーンを走査して可視化対象コライダーの更新を反映する
        /// </summary>
        public void FindAndUpdateColliders()
        {
            //Debug.Log("<color=red>ColliderManager.FindAndUpdateColliders</color>");
            // コライダー可視化が無効化されている場合は何もしない
            if (!ColliderVisualize.IsEnabled) return;
            FindColliders();
            UpdateColliders();
        }
        
        /// <summary>
        /// シーンを走査して可視化対象コライダーを再取得する
        /// </summary>
        public void FindColliders()
        {
            //Debug.Log("<color=red>ColliderManager.FindColliders</color>");
            _boxCollidersCollision.Clear();
            _boxCollidersTrigger.Clear();
            _sphereCollidersCollision.Clear();
            _sphereCollidersTrigger.Clear();
            _capsuleCollidersCollision.Clear();
            _capsuleCollidersTrigger.Clear();
            _meshCollidersCollision.Clear();
            _meshCollidersTrigger.Clear();
            
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                // シーン内の全ルートオブジェクトを走査
                foreach (var rootObject in scene.GetRootGameObjects())
                {
                    var colliders = rootObject.GetComponents<Collider>();
                    foreach (var collider in colliders)
                    {
                        // 衝突コライダー（isTrigger == false）
                        if (!collider.isTrigger)
                        {
                            switch (collider)
                            {
                                case BoxCollider boxCollider:
                                    _boxCollidersCollision.Add(boxCollider);
                                    break;
                                case SphereCollider sphereCollider:
                                    _sphereCollidersCollision.Add(sphereCollider);
                                    break;
                                case CapsuleCollider capsuleCollider:
                                    _capsuleCollidersCollision.Add(capsuleCollider);
                                    break;
                                case MeshCollider meshCollider:
                                    _meshCollidersCollision.Add(meshCollider);
                                    break;
                            }
                        }
                        // トリガーコライダー（isTrigger == true）
                        else
                        {
                            switch (collider)
                            {
                                case BoxCollider boxCollider:
                                    _boxCollidersTrigger.Add(boxCollider);
                                    break;
                                case SphereCollider sphereCollider:
                                    _sphereCollidersTrigger.Add(sphereCollider);
                                    break;
                                case CapsuleCollider capsuleCollider:
                                    _capsuleCollidersTrigger.Add(capsuleCollider);
                                    break;
                                case MeshCollider meshCollider:
                                    _meshCollidersTrigger.Add(meshCollider);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ColliderVisualizerFeatureにコライダーを反映する（シーンを走査しない）
        /// </summary>
        public void UpdateColliders()
        {
            //Debug.Log("<color=red>ColliderManager.UpdateColliders</color>");
            // ColliderVisualizerFeatureを取得
            var feature = ColliderVisualizerUtility.GetFeature();
            if (feature == null) return;
            
            // コライダーのリストを更新
            feature.UpdateColliders(
                _boxCollidersCollision.ToArray(), _boxCollidersTrigger.ToArray(),
                _sphereCollidersCollision.ToArray(), _sphereCollidersTrigger.ToArray(),
                _capsuleCollidersCollision.ToArray(), _capsuleCollidersTrigger.ToArray(),
                _meshCollidersCollision.ToArray(), _meshCollidersTrigger.ToArray());
        }
    }
}