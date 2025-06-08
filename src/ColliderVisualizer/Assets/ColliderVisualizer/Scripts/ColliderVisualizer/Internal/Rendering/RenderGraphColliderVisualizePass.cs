using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ColliderVisualizer
{
    /// <summary>
    /// RenderGraph対応のコライダー可視化描画パス
    /// </summary>
    internal class RenderGraphColliderVisualizePass : ColliderVisualizePassBase
    {
        private const string CollidersRenderPass = "Colliders Render Pass (Render Graph)";

        /// <summary> コンストラクタ </summary>
        public RenderGraphColliderVisualizePass(ColliderVisualSettings settings) : base(settings) { }

        private class PassData
        {
            public bool VisualizeCollisions;
            public bool VisualizeTriggers;

            public Mesh BoxMesh;
            public Mesh SphereMesh;
            public Mesh HemisphereMesh;
            public Mesh TubeMesh;
            public Material CollisionMaterial;
            public Material TriggerMaterial;

            public BoxCollider[] BoxCollidersCollision;
            public BoxCollider[] BoxCollidersTrigger;
            public SphereCollider[] SphereCollidersCollision;
            public SphereCollider[] SphereCollidersTrigger;
            public CapsuleCollider[] CapsuleCollidersCollision;
            public CapsuleCollider[] CapsuleCollidersTrigger;
            public Dictionary<Mesh, List<MeshCollider>> MeshColliderGroupsCollision;
            public Dictionary<Mesh, List<MeshCollider>> MeshColliderGroupsTrigger;

            public void Set(bool visualizeCollisions, bool visualizeTriggers,
                Mesh boxMesh, Mesh sphereMesh, Mesh hemisphereMesh, Mesh tubeMesh, Material collisionMaterial,
                Material triggerMaterial,
                BoxCollider[] boxCollidersCollision, BoxCollider[] boxCollidersTrigger,
                SphereCollider[] sphereCollidersCollision, SphereCollider[] sphereCollidersTrigger,
                CapsuleCollider[] capsuleCollidersCollision, CapsuleCollider[] capsuleCollidersTrigger,
                Dictionary<Mesh, List<MeshCollider>> meshColliderGroupsCollision,
                Dictionary<Mesh, List<MeshCollider>> meshColliderGroupsTrigger)
            {
                VisualizeCollisions = visualizeCollisions;
                VisualizeTriggers = visualizeTriggers;
                BoxMesh = boxMesh;
                SphereMesh = sphereMesh;
                HemisphereMesh = hemisphereMesh;
                TubeMesh = tubeMesh;
                CollisionMaterial = collisionMaterial;
                TriggerMaterial = triggerMaterial;
                BoxCollidersCollision = boxCollidersCollision;
                BoxCollidersTrigger = boxCollidersTrigger;
                SphereCollidersCollision = sphereCollidersCollision;
                SphereCollidersTrigger = sphereCollidersTrigger;
                CapsuleCollidersCollision = capsuleCollidersCollision;
                CapsuleCollidersTrigger = capsuleCollidersTrigger;
                MeshColliderGroupsCollision = meshColliderGroupsCollision;
                MeshColliderGroupsTrigger = meshColliderGroupsTrigger;
            }
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // シーンビューカメラとメインカメラ以外はreturn
            var camera = frameData.Get<UniversalCameraData>().camera;
            if (camera == null || (camera != Camera.main && camera.cameraType != CameraType.SceneView)) return;

            // 前回の描画でMissingReferenceExceptionが発生していた場合
            if (ThrownMissingReferenceException)
            {
                // コライダーリストからMissingを除外する
                FilterMissingColliders();
                ThrownMissingReferenceException = false;
            }

            // frameDataからURP内蔵のリソースデータを取得
            var resourceData = frameData.Get<UniversalResourceData>();

            // 入力とするテクスチャをresourceDataから取得
            var colorTextureHandle = resourceData.activeColorTexture; // activeColorTextureはカメラが描画したメインのカラーバッファ
            var depthTextureHandle = resourceData.activeDepthTexture; // activeDepthTextureはカメラの深度テクスチャ

            // 直接カメラのカラーテクスチャに描画するシンプルなパス
            using var builder = renderGraph.AddRasterRenderPass<PassData>(CollidersRenderPass, out var passData);

            // PassDataに必要なデータをセット
            passData.Set(VisualizeCollisions, VisualizeTriggers, CubeMesh, SphereMesh, HemisphereMesh, TubeMesh,
                CollisionMaterial, TriggerMaterial, BoxCollidersCollision, BoxCollidersTrigger,
                SphereCollidersCollision, SphereCollidersTrigger, CapsuleCollidersCollision,
                CapsuleCollidersTrigger, MeshColliderGroupsCollision, MeshColliderGroupsTrigger);

            // カメラのカラーテクスチャに直接描画
            builder.SetRenderAttachment(colorTextureHandle, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(depthTextureHandle, AccessFlags.Read);

            // 実際の描画関数を設定する(static関数が推奨されてる)
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                var cmd = new RasterCommandBufferAdapter(context.cmd);
                DrawCollidersInstanced(cmd, data);
            });
        }
        
        // 各種コライダーの描画処理
        private static void DrawCollidersInstanced(RasterCommandBufferAdapter cmd, PassData passData)
        {
            try
            {
                var collisionMaterial = passData.CollisionMaterial;
                var triggerMaterial = passData.TriggerMaterial;
                if (passData.VisualizeCollisions)
                {
                    DrawBoxColliders(cmd, passData.BoxMesh, collisionMaterial, passData.BoxCollidersCollision);
                    DrawSphereColliders(cmd, passData.SphereMesh, collisionMaterial, passData.SphereCollidersCollision);
                    DrawCapsuleColliders(cmd, passData.HemisphereMesh, passData.TubeMesh, collisionMaterial, passData.CapsuleCollidersCollision);
                    DrawMeshColliders(cmd, collisionMaterial, passData.MeshColliderGroupsCollision);
                }
                if (passData.VisualizeTriggers)
                {
                    DrawBoxColliders(cmd, passData.BoxMesh, triggerMaterial, passData.BoxCollidersTrigger);
                    DrawSphereColliders(cmd, passData.SphereMesh, triggerMaterial, passData.SphereCollidersTrigger);
                    DrawCapsuleColliders(cmd, passData.HemisphereMesh, passData.TubeMesh, triggerMaterial, passData.CapsuleCollidersTrigger);
                    DrawMeshColliders(cmd, triggerMaterial, passData.MeshColliderGroupsTrigger);
                }
            }
            catch(MissingReferenceException)
            {
                ThrownMissingReferenceException = true;
            }
        }
    }
}
