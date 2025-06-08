using UnityEngine;

namespace ColliderVisualizer
{
    /// <summary>
    /// 可視化設定をまとめた構造体
    /// </summary>
    public readonly struct ColliderVisualSettings
    {
        /// <summary> 衝突コライダー（isTrigger == false）を可視化するか </summary>
        public readonly bool VisualizeCollisions;
        
        /// <summary> 衝突コライダー（isTrigger == false）の色 </summary>
        public readonly Color CollisionColor;
        
        /// <summary> トリガーコライダー（isTrigger == true）を可視化するか </summary>
        public readonly bool VisualizeTriggers;
        
        /// <summary> トリガーコライダー（isTrigger == true）の色 </summary>
        public readonly Color TriggerColor;
        
        /// <summary> 可視化の透明度（0〜1） </summary>
        public readonly float Alpha;
        
        /// <summary> メッシュの品質（1〜10） </summary>
        public readonly int MeshQuality;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ColliderVisualSettings(
            bool visualizeCollisions, 
            Color collisionColor, 
            bool visualizeTriggers,
            Color triggerColor, 
            float alpha, 
            int meshQuality)
        {
            VisualizeCollisions = visualizeCollisions;
            CollisionColor = collisionColor;
            VisualizeTriggers = visualizeTriggers;
            TriggerColor = triggerColor;
            Alpha = Mathf.Clamp(alpha, 0, 1);
            MeshQuality = Mathf.Max(0, meshQuality);
        }
    }
}