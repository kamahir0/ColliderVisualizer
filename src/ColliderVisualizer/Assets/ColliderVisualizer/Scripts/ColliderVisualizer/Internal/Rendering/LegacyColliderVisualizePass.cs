using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColliderVisualizer
{
    /// <summary>
    /// レガシーCommandBuffer対応のコライダー可視化描画パス
    /// </summary>
    internal class LegacyColliderVisualizePass : ColliderVisualizePassBase
    {
        private const string CollidersRenderPass = "Colliders Render Pass (Legacy)";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LegacyColliderVisualizePass(ColliderVisualSettings settings) : base(settings) { }

        /// <inheritdoc />
        [Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // シーンビューカメラとメインカメラ以外はreturn
            var camera = renderingData.cameraData.camera;
            if (camera == null || (camera != Camera.main && camera.cameraType != CameraType.SceneView)) return;

            // 前回の描画でMissingReferenceExceptionが発生していた場合
            if (ThrownMissingReferenceException)
            {
                // コライダーリストからMissingを除外する
                FilterMissingColliders();
                ThrownMissingReferenceException = false;
            }

            // CommandBufferを取得
            var cmd = CommandBufferPool.Get(CollidersRenderPass);
            try
            {
                var cmdAdapter = new LegacyCommandBufferAdapter(cmd);
                DrawCollidersInstanced(cmdAdapter);
                
                // CommandBufferを実行
                context.ExecuteCommandBuffer(cmd);
            }
            finally
            {
                CommandBufferPool.Release(cmd);
            }
        }

        /// <summary>
        /// 各種コライダーの描画処理
        /// </summary>
        private void DrawCollidersInstanced(LegacyCommandBufferAdapter cmd)
        {
            try
            {
                if (VisualizeCollisions)
                {
                    DrawBoxColliders(cmd, CubeMesh, CollisionMaterial, BoxCollidersCollision);
                    DrawSphereColliders(cmd, SphereMesh, CollisionMaterial, SphereCollidersCollision);
                    DrawCapsuleColliders(cmd, HemisphereMesh, TubeMesh, CollisionMaterial, CapsuleCollidersCollision);
                    DrawMeshColliders(cmd, CollisionMaterial, MeshColliderGroupsCollision);
                }
                
                if (VisualizeTriggers)
                {
                    DrawBoxColliders(cmd, CubeMesh, TriggerMaterial, BoxCollidersTrigger);
                    DrawSphereColliders(cmd, SphereMesh, TriggerMaterial, SphereCollidersTrigger);
                    DrawCapsuleColliders(cmd, HemisphereMesh, TubeMesh, TriggerMaterial, CapsuleCollidersTrigger);
                    DrawMeshColliders(cmd, TriggerMaterial, MeshColliderGroupsTrigger);
                }
            }
            catch (MissingReferenceException)
            {
                ThrownMissingReferenceException = true;
            }
        }
    }
}