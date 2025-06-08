using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColliderVisualizer
{
    /// <summary>
    /// コライダーオブジェクトの増減を追跡するオブジェクト
    /// </summary>
    internal class ColliderTracker : MonoBehaviour
    {
        private readonly struct SceneEntity : IEquatable<SceneEntity>
        {
            /// <summary> シーン名 </summary>
            public string SceneName { get; }
            public SceneEntity(string sceneName) => SceneName = sceneName;
            public SceneEntity(Scene scene) => SceneName = scene.name;
            public static implicit operator string(SceneEntity sceneEntity) => sceneEntity.SceneName;
            public static implicit operator SceneEntity(string sceneName) => new(sceneName);
            public static implicit operator SceneEntity(Scene scene) => new(scene);
            public bool Equals(SceneEntity other) => SceneName == other.SceneName;
        }
    
        private readonly struct RootObjectEntity : IEquatable<RootObjectEntity>
        {
            /// <summary> ルートオブジェクトのインスタンスID </summary>
            public int InstanceId { get; }
            public RootObjectEntity(int instanceId) => InstanceId = instanceId;
            public RootObjectEntity(GameObject rootObject) => InstanceId = rootObject.GetInstanceID();
            public static implicit operator int(RootObjectEntity rootObjectEntity) => rootObjectEntity.InstanceId;
            public static implicit operator RootObjectEntity(int instanceId) => new(instanceId);
            public static implicit operator RootObjectEntity(GameObject rootObject) => new(rootObject);
            public bool Equals(RootObjectEntity other) => InstanceId == other.InstanceId;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            ColliderVisualize.OnEnabled += Initialize;
            ColliderVisualize.OnDisabled += DestroyTracker;
            if (ColliderVisualize.IsEnabled) Initialize();
        }

        /// <summary>
        /// Trackerを出す
        /// </summary>
        public static void Initialize()
        {
            // 既にインスタンスが存在していたり、コライダー可視化が無効化されていたりする場合は何もしない
            if (_instance == null)
            {
                var trackerObject = new GameObject("ColliderTracker");
                _instance = trackerObject.AddComponent<ColliderTracker>();
                DontDestroyOnLoad(trackerObject);
            }
        }

        /// <summary>
        /// Trackerを破棄する
        /// </summary>
        private static void DestroyTracker()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private static ColliderTracker _instance;
        
        // シーンごとの、全ルートオブジェクトの、孫以下も含めた全子要素の数
        private readonly Dictionary<SceneEntity, Dictionary<RootObjectEntity, int>> _rootObjectChildCounts = new();
        
        private const float TrackInterval = 1f;
        private bool _forceUpdate;
        
        // 現在のシーン情報をキャッシュ（アロケーション削減）
        private readonly Dictionary<RootObjectEntity, int> _currentChildCountDict = new();
        private readonly HashSet<RootObjectEntity> _currentRootObjects = new();
        private readonly HashSet<RootObjectEntity> _previousRootObjects = new();
        private readonly Stack<Transform> _stack = new();

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            StartCoroutine(Track());
        }
        
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            StopAllCoroutines();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _forceUpdate = true;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            // アンロードされたシーンのデータを削除
            _rootObjectChildCounts.Remove(scene.name);
            _forceUpdate = true;
        }

        /// <summary>
        /// TrackInterval 秒間隔でルートオブジェクトを走査し、コライダーの増減を追跡する
        /// </summary>
        private IEnumerator Track()
        {
            // 初期化の完了を待機（５秒経っても完了しない場合は強制終了）
            yield return new WaitUntil(
                predicate: () => ColliderVisualize.IsRuntimeInitialized,
                timeout: new TimeSpan(0, 0, 5),
                onTimeout: () => Destroy(gameObject));
            
            while (true)
            {
                bool hasChanges = false;

                // 全てのロードされたシーンを走査
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    hasChanges |= TrackScene(scene);
                }

                // 変化があった or 強制更新フラグが立っている場合はコライダーを更新
                if (hasChanges || _forceUpdate)
                {
                    ColliderManager.Instance.FindAndUpdateColliders();
                    _forceUpdate = false;
                }

                // 一定時間待機
                yield return new WaitForSeconds(TrackInterval);
            }
        }

        /// <summary>
        /// 指定されたシーンの変更を追跡する
        /// </summary>
        /// <param name="scene">追跡対象のシーン</param>
        /// <returns>変化があった場合true</returns>
        private bool TrackScene(Scene scene)
        {
            bool hasChanges = false;

            // シーンデータが存在しない場合は初期化
            if (!_rootObjectChildCounts.ContainsKey(scene))
            {
                _rootObjectChildCounts[scene] = new Dictionary<RootObjectEntity, int>();
                hasChanges = true;
            }

            var childCountDict = _rootObjectChildCounts[scene];
            
            // 現在のデータをクリア（再利用でアロケーション削減）
            _currentChildCountDict.Clear();
            _currentRootObjects.Clear();

            // ルートオブジェクトが新規追加されているか・子要素数が増減しているか確認
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                _currentRootObjects.Add(rootObject);
                
                // ルートオブジェクトの孫以下も含めた全子要素の数を取得
                int currentCount = CountChildrenRecursive(rootObject.transform);
                _currentChildCountDict[rootObject] = currentCount;

                // ルートオブジェクトが新規追加されている or 子要素の数が変化している場合
                var addedRootObject = !childCountDict.TryGetValue(rootObject, out int previousCount);
                var changedCount = previousCount != currentCount;
                if (addedRootObject || changedCount)
                {
                    hasChanges = true;
                }
            }

            // 削除されたルートオブジェクトがあるか確認
            _previousRootObjects.Clear();
            foreach (var kvp in childCountDict)
            {
                _previousRootObjects.Add(kvp.Key);
            }
            if (!_previousRootObjects.SetEquals(_currentRootObjects))
            {
                hasChanges = true;
            }

            // 変更があった場合はデータを更新
            if (hasChanges)
            {
                childCountDict.Clear();
                foreach (var kvp in _currentChildCountDict)
                {
                    childCountDict[kvp.Key] = kvp.Value;
                }
            }

            return hasChanges;
        }
        
        /// <summary>
        /// 孫以下も含めた全子要素の数を返す（非再帰版でスタックオーバーフロー回避）
        /// </summary>
        /// <param name="parent">親Transform</param>
        /// <returns>子要素の総数</returns>
        private int CountChildrenRecursive(Transform parent)
        {
            int count = 0;
            _stack.Clear();
            _stack.Push(parent);

            while (_stack.Count > 0)
            {
                var current = _stack.Pop();
                int childCount = current.childCount;
                count += childCount;

                // 子要素をスタックに追加
                for (int i = 0; i < childCount; i++)
                {
                    _stack.Push(current.GetChild(i));
                }
            }

            return count;
        }
    }
}