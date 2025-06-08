using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColliderVisualizer
{
    internal static class ColliderVisualizerUtility
    {
        /// <summary>
        /// ColliderVisualizerFeatureを取得
        /// </summary>
        public static ColliderVisualizerFeature GetFeature()
        {
            var urpAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Debug.LogError("GraphicsSettings.defaultRenderPipeline は UniversalRenderPipelineAsset ではありません。ColliderVisualizerFeature を取得できません。");
                return null;
            }
            if (urpAsset.rendererDataList == null)
            {
                Debug.LogError("UniversalRenderPipelineAsset.rendererDataList が null です。ColliderVisualizerFeature を取得できません。");
                return null;
            }
            
            foreach (var rendererData in urpAsset.rendererDataList)
            {
                if (rendererData == null) continue;
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature is ColliderVisualizerFeature colliderVisualizerFeature)
                    {
                        return colliderVisualizerFeature;
                    }
                }
            }
            return null;
        }
        
        // シェーダー関連
        private const string ColorName = "_Color";
        private const string AlphaName = "_Alpha";
        private static readonly int _colorID = Shader.PropertyToID(ColorName);
        private static readonly int _alphaID = Shader.PropertyToID(AlphaName);
        
        /// <summary>
        /// マテリアルを作成
        /// </summary>
        public static Material CreateMaterial(Color color, float alpha)
        {
            const string ShaderPath = "Hidden/ColliderVisualizer/ColliderRimLight";
            var mat = CoreUtils.CreateEngineMaterial(ShaderPath);
            mat.SetColor(_colorID, color);
            mat.SetFloat(_alphaID, alpha);
            mat.enableInstancing = true;
            return mat;
        }
    }
}