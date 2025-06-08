using System;
using UnityEngine;

namespace ColliderVisualizer
{
    /// <summary>
    /// コライダー可視化の外部操作API
    /// </summary>
    public static class ColliderVisualize
    {
        /// <summary>
        /// ランタイムにおいて初期化が完了しているか
        /// </summary>
        public static bool IsRuntimeInitialized
        {
            get
            {
                // 非ランタイムならfalseを返す
                if (!Application.isPlaying) return false;
                
                // ColliderVisualizerFeatureが存在しなければfalseを返す
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return false;

                return feature.IsExistsPass;
            }
        }
        
        /// <summary>
        /// 設定を適用する
        /// </summary>
        public static void ApplySettings(ColliderVisualSettings settings)
        {
            var feature = ColliderVisualizerUtility.GetFeature();
            if (feature == null) return;
            feature.ApplySettings(settings);
        }
        
        /// <summary>
        /// シーンのヒエラルキーを走査して可視化コライダーを更新する
        /// </summary>
        public static void UpdateColliders() => ColliderManager.Instance.FindAndUpdateColliders();

        /// <summary> コライダー可視化が有効化されているか </summary>
        public static bool IsEnabled
        {
            get
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return false;
                var settings = feature.Settings;
                return settings.VisualizeCollisions || settings.VisualizeTriggers;
            }
        }

        /// <summary> コライダー可視化が無効から有効に切り替わったときのイベント </summary>
        public static event Action OnEnabled;
        /// <summary> コライダー可視化が有効から無効に切り替わったときのイベント </summary>
        public static event Action OnDisabled;
        
        // コライダー可視化の有効・無効の切替をチェック
        private static void CheckEnableStateChange(bool wasEnabled)
        {
            bool isEnabledNow = IsEnabled;
            if (wasEnabled != isEnabledNow)
            {
                if (isEnabledNow) OnEnabled?.Invoke();
                else OnDisabled?.Invoke();
            }
        }
        
        /// <summary>
        /// 衝突コライダー（isTrigger == false）を可視化するか
        /// </summary>
        public static bool VisualizeCollisions
        {
            get
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return false;
                return feature.Settings.VisualizeCollisions;
            }
            set
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return;
                
                bool wasEnabled = IsEnabled;
                var newSettings = feature.Settings.WithVisualizeCollisions(value);
                feature.ApplySettings(newSettings);
                CheckEnableStateChange(wasEnabled);
            }
        }
        
        /// <summary>
        /// トリガーコライダー（isTrigger == true）を可視化するか
        /// </summary>
        public static bool VisualizeTriggers
        {
            get
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return false;
                return feature.Settings.VisualizeTriggers;
            }
            set
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return;
                
                bool wasEnabled = IsEnabled;
                var newSettings = feature.Settings.WithVisualizeTriggers(value);
                feature.ApplySettings(newSettings);
                CheckEnableStateChange(wasEnabled);
            }
        }
        
        /// <summary>
        /// 衝突コライダー（isTrigger == false）の色
        /// </summary>
        public static Color CollisionColor
        {
            get
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return default;
                return feature.Settings.CollisionColor;
            }
            set
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return;
                var newSettings = feature.Settings.WithCollisionColor(value);
                feature.ApplySettings(newSettings);
            }
        }
        
        /// <summary>
        /// トリガーコライダー（isTrigger == true）の色
        /// </summary>
        public static Color TriggerColor
        {
            get
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return default;
                return feature.Settings.TriggerColor;
            }
            set
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return;
                var newSettings = feature.Settings.WithTriggerColor(value);
                feature.ApplySettings(newSettings);
            }
        }
        
        /// <summary>
        /// トリガーコライダーの透明度
        /// </summary>
        public static float Alpha
        {
            get
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return 0f;
                return feature.Settings.Alpha;
            }
            set
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return;
                var newSettings = feature.Settings.WithAlpha(value);
                feature.ApplySettings(newSettings);
            }
        }
        
        /// <summary>
        /// メッシュの品質
        /// </summary>
        public static int MeshQuality
        {
            get
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return 0;
                return feature.Settings.MeshQuality;
            }
            set
            {
                var feature = ColliderVisualizerUtility.GetFeature();
                if (feature == null) return;
                var newSettings = feature.Settings.WithMeshQuality(value);
                feature.ApplySettings(newSettings);
            }
        }
    }
}