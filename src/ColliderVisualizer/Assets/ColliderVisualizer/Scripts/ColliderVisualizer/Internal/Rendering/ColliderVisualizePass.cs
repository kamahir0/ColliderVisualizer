using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ColliderVisualizer
{
    /// <summary>
    /// コライダー可視化描画の具体的な処理を記述するPassパス
    /// </summary>
    internal class ColliderVisualizePass : ScriptableRenderPass
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ColliderVisualizePass(ColliderVisualSettings settings)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            
            _boxCollidersCollision = Array.Empty<BoxCollider>();
            _boxCollidersTrigger = Array.Empty<BoxCollider>();
            _sphereCollidersCollision = Array.Empty<SphereCollider>();
            _sphereCollidersTrigger = Array.Empty<SphereCollider>();
            _capsuleCollidersCollision = Array.Empty<CapsuleCollider>();
            _capsuleCollidersTrigger = Array.Empty<CapsuleCollider>();
            
            _cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            _sphereMesh = MeshCreator.CreateSphere(settings.MeshQuality);
            _hemisphereMesh = MeshCreator.CreateHemisphere(settings.MeshQuality);
            _tubeMesh = MeshCreator.CreateTube(settings.MeshQuality);
            
            _visualizeCollisions = settings.VisualizeCollisions;
            _visualizeTriggers = settings.VisualizeTriggers;
            
            _collisionMaterial = ColliderVisualizerUtility.CreateMaterial(settings.CollisionColor, settings.Alpha);
            _triggerMaterial = ColliderVisualizerUtility.CreateMaterial(settings.TriggerColor, settings.Alpha);
            
            _currentSettings = settings;
        }
        
        /// <summary>
        /// 設定を適用する
        /// </summary>
        public void ApplySettings(ColliderVisualSettings settings)
        {
            // メッシュ品質の差分をチェック
            if (_currentSettings.MeshQuality != settings.MeshQuality)
            {
                Object.DestroyImmediate(_sphereMesh);
                Object.DestroyImmediate(_hemisphereMesh);
                Object.DestroyImmediate(_tubeMesh);
                _sphereMesh = MeshCreator.CreateSphere(settings.MeshQuality);
                _hemisphereMesh = MeshCreator.CreateHemisphere(settings.MeshQuality);
                _tubeMesh = MeshCreator.CreateTube(settings.MeshQuality);
            }

            // 色とアルファの差分をチェック
            var alphaChanged = !Mathf.Approximately(_currentSettings.Alpha, settings.Alpha);
            if (alphaChanged || _currentSettings.CollisionColor != settings.CollisionColor)
            {
                CoreUtils.Destroy(_collisionMaterial);
                _collisionMaterial = ColliderVisualizerUtility.CreateMaterial(settings.CollisionColor, settings.Alpha);
            }
            if (alphaChanged || _currentSettings.TriggerColor != settings.TriggerColor)
            {
                CoreUtils.Destroy(_triggerMaterial);
                _triggerMaterial = ColliderVisualizerUtility.CreateMaterial(settings.TriggerColor, settings.Alpha);
            }
            
            // 可視化設定を更新
            _visualizeCollisions = settings.VisualizeCollisions;
            _visualizeTriggers = settings.VisualizeTriggers;

            // 現在の設定を更新
            _currentSettings = settings;
        }
        
        /// <summary>
        /// 可視化対象コライダーを更新する
        /// </summary>
        public void UpdateColliders(
            BoxCollider[] boxCollidersCollision, BoxCollider[] boxCollidersTrigger, 
            SphereCollider[] sphereCollidersCollision, SphereCollider[] sphereCollidersTrigger, 
            CapsuleCollider[] capsuleCollidersCollision, CapsuleCollider[] capsuleCollidersTrigger, 
            MeshCollider[] meshCollidersCollision, MeshCollider[] meshCollidersTrigger)
        {
            _boxCollidersCollision = boxCollidersCollision;
            _boxCollidersTrigger = boxCollidersTrigger;
            _sphereCollidersCollision = sphereCollidersCollision;
            _sphereCollidersTrigger = sphereCollidersTrigger;
            _capsuleCollidersCollision = capsuleCollidersCollision;
            _capsuleCollidersTrigger = capsuleCollidersTrigger;
            GroupMeshColliders(meshCollidersCollision, meshCollidersTrigger);
        }

        // MeshColliderを事前にMesh別にグループ化する
        private void GroupMeshColliders(MeshCollider[] meshCollidersCollision, MeshCollider[] meshCollidersTrigger)
        {
            // コリジョン用MeshColliderのグループ化
            _meshColliderGroupsCollision.Clear();
            for (int i = 0; i < meshCollidersCollision.Length; ++i)
            {
                var collider = meshCollidersCollision[i];
                if (collider == null || collider.sharedMesh == null) continue;
                
                var mesh = collider.sharedMesh;
                if (!_meshColliderGroupsCollision.TryGetValue(mesh, out var list))
                {
                    list = new List<MeshCollider>();
                    _meshColliderGroupsCollision[mesh] = list;
                }
                list.Add(collider);
            }
            
            // トリガー用MeshColliderのグループ化
            _meshColliderGroupsTrigger.Clear();
            for (int i = 0; i < meshCollidersTrigger.Length; ++i)
            {
                var collider = meshCollidersTrigger[i];
                if (collider == null || collider.sharedMesh == null) continue;
                
                var mesh = collider.sharedMesh;
                if (!_meshColliderGroupsTrigger.TryGetValue(mesh, out var list))
                {
                    list = new List<MeshCollider>();
                    _meshColliderGroupsTrigger[mesh] = list;
                }
                list.Add(collider);
            }
        }

        // コライダーリストからMissingを除外する
        private void FilterMissingColliders()
        {
            _boxCollidersCollision = _boxCollidersCollision.Where(c => c != null).ToArray();
            _boxCollidersTrigger = _boxCollidersTrigger.Where(c => c != null).ToArray();
            _sphereCollidersCollision = _sphereCollidersCollision.Where(c => c != null).ToArray();
            _sphereCollidersTrigger = _sphereCollidersTrigger.Where(c => c != null).ToArray();
            _capsuleCollidersCollision = _capsuleCollidersCollision.Where(c => c != null).ToArray();
            _capsuleCollidersTrigger = _capsuleCollidersTrigger.Where(c => c != null).ToArray();
            foreach (var key in _meshColliderGroupsCollision.Keys.ToArray())
            {
                _meshColliderGroupsCollision[key] = _meshColliderGroupsCollision[key].Where(c => c != null).ToList();
            }
            foreach (var key in _meshColliderGroupsTrigger.Keys.ToArray())
            {
                _meshColliderGroupsTrigger[key] = _meshColliderGroupsTrigger[key].Where(c => c != null).ToList();
            }
        }

        private ColliderVisualSettings _currentSettings;
        
        // コライダー描画メッシュ
        private readonly Mesh _cubeMesh;
        private Mesh _sphereMesh;
        private Mesh _hemisphereMesh;
        private Mesh _tubeMesh;

        // コライダー描画マテリアル
        private Material _collisionMaterial;
        private Material _triggerMaterial;

        // 可視化するかどうか
        private bool _visualizeCollisions;
        private bool _visualizeTriggers;

        // 可視化するコライダー
        private BoxCollider[] _boxCollidersCollision;
        private BoxCollider[] _boxCollidersTrigger;
        private SphereCollider[] _sphereCollidersCollision;
        private SphereCollider[] _sphereCollidersTrigger;
        private CapsuleCollider[] _capsuleCollidersCollision;
        private CapsuleCollider[] _capsuleCollidersTrigger;
        private readonly Dictionary<Mesh, List<MeshCollider>> _meshColliderGroupsCollision = new();
        private readonly Dictionary<Mesh, List<MeshCollider>> _meshColliderGroupsTrigger = new();

        // 最適化用：再利用可能な配列プール
        private static readonly MatrixArrayPool _matrixArrayPool = new();
        
        // 描画処理中にコライダーが破棄されたときに上がるフラグ
        private static bool _thrownMissingReferenceException;

        private const string CollidersRenderPass = "Colliders Render Pass";

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
                Mesh boxMesh, Mesh sphereMesh, Mesh hemisphereMesh, Mesh tubeMesh, Material collisionMaterial, Material triggerMaterial, 
                BoxCollider[] boxCollidersCollision, BoxCollider[] boxCollidersTrigger, 
                SphereCollider[] sphereCollidersCollision, SphereCollider[] sphereCollidersTrigger, 
                CapsuleCollider[] capsuleCollidersCollision, CapsuleCollider[] capsuleCollidersTrigger, 
                Dictionary<Mesh, List<MeshCollider>> meshColliderGroupsCollision, Dictionary<Mesh, List<MeshCollider>> meshColliderGroupsTrigger)
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
            if (_thrownMissingReferenceException)
            {
                // コライダーリストからMissingを除外する
                FilterMissingColliders();
                _thrownMissingReferenceException = false;
            }

            // frameDataからURP内蔵のリソースデータを取得
            var resourceData = frameData.Get<UniversalResourceData>();

            // 入力とするテクスチャをresourceDataから取得
            var colorTextureHandle = resourceData.activeColorTexture;  // activeColorTextureはカメラが描画したメインのカラーバッファ
            var depthTextureHandle = resourceData.activeDepthTexture;  // activeDepthTextureはカメラの深度テクスチャ

            // 直接カメラのカラーテクスチャに描画するシンプルなパス
            using var builder = renderGraph.AddRasterRenderPass<PassData>(CollidersRenderPass, out var passData);
            
            // PassDataに必要なデータをセット
            passData.Set(_visualizeCollisions, _visualizeTriggers, _cubeMesh, _sphereMesh, _hemisphereMesh, _tubeMesh, _collisionMaterial, _triggerMaterial, _boxCollidersCollision, _boxCollidersTrigger, _sphereCollidersCollision, _sphereCollidersTrigger, _capsuleCollidersCollision, _capsuleCollidersTrigger, _meshColliderGroupsCollision, _meshColliderGroupsTrigger);

            // カメラのカラーテクスチャに直接描画
            builder.SetRenderAttachment(colorTextureHandle, 0, AccessFlags.ReadWrite);
            builder.SetRenderAttachmentDepth(depthTextureHandle, AccessFlags.Read);

            // 実際の描画関数を設定する(static関数が推奨されてる)
            builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
            {
                DrawCollidersInstanced(context.cmd, data);
            });
        }

        /// <summary>
        /// 各種コライダーの描画処理を行う
        /// </summary>
        private static void DrawCollidersInstanced(IRasterCommandBuffer cmd, PassData passData)
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
                    DrawMeshCollidersOptimized(cmd, collisionMaterial, passData.MeshColliderGroupsCollision);
                }
                if (passData.VisualizeTriggers)
                {
                    DrawBoxColliders(cmd, passData.BoxMesh, triggerMaterial, passData.BoxCollidersTrigger);
                    DrawSphereColliders(cmd, passData.SphereMesh, triggerMaterial, passData.SphereCollidersTrigger);
                    DrawCapsuleColliders(cmd, passData.HemisphereMesh, passData.TubeMesh, triggerMaterial, passData.CapsuleCollidersTrigger);
                    DrawMeshCollidersOptimized(cmd, triggerMaterial, passData.MeshColliderGroupsTrigger);
                }
            }
            catch(MissingReferenceException)
            {
                _thrownMissingReferenceException = true;
            }
        }

        private const float BoxMargin = 0.01f;

        /// <summary>
        /// BoxColliderの描画
        /// </summary>
        private static void DrawBoxColliders(IRasterCommandBuffer cmd, Mesh mesh, Material material, BoxCollider[] colliders)
        {
            if (mesh == null || material == null || colliders == null) return;

            var count = colliders.Length;
            if (count == 0) return;

            var matrices = _matrixArrayPool.Rent(count);
            try
            {
                for (int i = 0; i < count; ++i)
                {
                    var collider = colliders[i];
                    var size = collider.size;
                    var marginedSize = size + Vector3.one * BoxMargin;

                    // TransformとColliderパラメータを考慮
                    matrices[i] = collider.transform.localToWorldMatrix * Matrix4x4.TRS(collider.center, Quaternion.identity, marginedSize);
                }
                cmd.DrawMeshInstanced(mesh, 0, material, 0, matrices, count);
            }
            finally
            {
                _matrixArrayPool.Return(matrices);
            }
        }

        private const float SphereMargin = 0.0312f;

        /// <summary>
        /// SphereColliderの描画
        /// </summary>
        private static void DrawSphereColliders(IRasterCommandBuffer cmd, Mesh mesh, Material material, SphereCollider[] colliders)
        {
            if (mesh == null || material == null || colliders == null) return;

            var count = colliders.Length;
            if (count == 0) return;

            var matrices = _matrixArrayPool.Rent(count);
            try
            {
                for (int i = 0; i < count; ++i)
                {
                    var collider = colliders[i];
                    // TransformとColliderパラメータを考慮
                    matrices[i] = collider.transform.localToWorldMatrix * Matrix4x4.TRS(collider.center, Quaternion.identity, Vector3.one * (collider.radius + SphereMargin));
                }
                cmd.DrawMeshInstanced(mesh, 0, material, 0, matrices, count);
            }
            finally
            {
                _matrixArrayPool.Return(matrices);
            }
        }

        private const float CapsuleMargin = 0.0308f;

        /// <summary>
        /// CapsuleColliderの描画
        /// </summary>
        private static void DrawCapsuleColliders(IRasterCommandBuffer cmd, Mesh hemisphereMesh, Mesh tubeMesh, Material material, CapsuleCollider[] colliders)
        {
            if (hemisphereMesh == null || tubeMesh == null || colliders == null) return;

            var count = colliders.Length;
            if (count == 0) return;

            var hemisphereMatrices = _matrixArrayPool.Rent(count * 2);
            var tubeMatrices = _matrixArrayPool.Rent(count);
            
            try
            {
                int hemisphereIndex = 0;
                for (int i = 0; i < count; ++i)
                {
                    var collider = colliders[i];
                    var position = collider.transform.position;
                    var rotation = collider.transform.rotation;
                    var scale = collider.transform.lossyScale;
                    var center = collider.center;
                    center.x *= scale.x;
                    center.y *= scale.y;
                    center.z *= scale.z;
                    var height = collider.height;
                    var radius = collider.radius;

                    float scaledHeight = 0f;
                    Vector3 hemisphereOffsetTop = Vector3.zero;
                    Vector3 hemisphereOffsetBottom = Vector3.zero;
                    Quaternion hemisphereRotationTop = Quaternion.identity;
                    Quaternion hemisphereRotationBottom = Quaternion.identity;
                    Quaternion tubeRotation = Quaternion.identity;

                    switch (collider.direction)
                    {
                        case 0: // X-Axis
                            {
                                radius *= Mathf.Max(scale.y, scale.z);
                                scaledHeight = Mathf.Max(0, height * scale.x - 2 * radius) / 2;
                                hemisphereOffsetTop = rotation * (center + Vector3.right * scaledHeight);
                                hemisphereOffsetBottom = rotation * (center + Vector3.left * scaledHeight);
                                hemisphereRotationTop = Quaternion.Euler(0f, 0f, 90f);
                                hemisphereRotationBottom = Quaternion.Euler(0f, 0f, -90f);
                                tubeRotation = Quaternion.Euler(0f, 0f, 90f);
                                break;
                            }
                        case 1: // Y-Axis
                            {
                                radius *= Mathf.Max(scale.x, scale.z);
                                scaledHeight = Mathf.Max(0, height * scale.y - 2 * radius) / 2;
                                hemisphereOffsetTop = rotation * (center + Vector3.up * scaledHeight);
                                hemisphereOffsetBottom = rotation * (center + Vector3.down * scaledHeight);
                                hemisphereRotationTop = Quaternion.Euler(180f, 0f, 0f);
                                break;
                            }
                        case 2: // Z-Axis
                            {
                                radius *= Mathf.Max(scale.x, scale.y);
                                scaledHeight = Mathf.Max(0, height * scale.z - 2 * radius) / 2;
                                hemisphereOffsetTop = rotation * (center + Vector3.forward * scaledHeight);
                                hemisphereOffsetBottom = rotation * (center + Vector3.back * scaledHeight);
                                hemisphereRotationTop = Quaternion.Euler(-90f, 0f, 0f);
                                hemisphereRotationBottom = Quaternion.Euler(90f, 0f, 0f);
                                tubeRotation = Quaternion.Euler(90f, 0f, 0f);
                                break;
                            }
                    }
                    var marginedRadius = radius + CapsuleMargin;
                    Vector3 hemisphereScale = -Vector3.one * marginedRadius;
                    Vector3 tubeScale = new Vector3(marginedRadius, scaledHeight, marginedRadius) * 2;

                    // Top hemisphere
                    hemisphereMatrices[hemisphereIndex++] = Matrix4x4.TRS(
                        position + hemisphereOffsetTop,
                        rotation * hemisphereRotationTop,
                        hemisphereScale
                    );

                    // Bottom hemisphere
                    hemisphereMatrices[hemisphereIndex++] = Matrix4x4.TRS(
                        position + hemisphereOffsetBottom,
                        rotation * hemisphereRotationBottom,
                        hemisphereScale
                    );

                    // Tube
                    tubeMatrices[i] = Matrix4x4.TRS(
                        position + rotation * center,
                        rotation * tubeRotation,
                        tubeScale
                    );
                }

                cmd.DrawMeshInstanced(hemisphereMesh, 0, material, 0, hemisphereMatrices, count * 2);
                cmd.DrawMeshInstanced(tubeMesh, 0, material, 0, tubeMatrices, count);
            }
            finally
            {
                _matrixArrayPool.Return(hemisphereMatrices);
                _matrixArrayPool.Return(tubeMatrices);
            }
        }

        private const float MeshMargin = 0.01f;
        private const float PlaneMargin = 0.00025f;
        private const float QuadMargin = 0.00025f;

        /// <summary>
        /// MeshColliderの描画
        /// </summary>
        private static void DrawMeshCollidersOptimized(IRasterCommandBuffer cmd, Material material, Dictionary<Mesh, List<MeshCollider>> meshGroups)
        {
            if (material == null || meshGroups == null) return;

            foreach (var kvp in meshGroups)
            {
                var mesh = kvp.Key;
                var colliders = kvp.Value;
                
                if (mesh == null || colliders.Count == 0) continue;

                var matrices = _matrixArrayPool.Rent(colliders.Count);
                try
                {
                    for (int i = 0; i < colliders.Count; ++i)
                    {
                        var collider = colliders[i];
                        var rawMatrix = collider.transform.localToWorldMatrix;
                        var scale = new Vector3(rawMatrix.m00, rawMatrix.m11, rawMatrix.m22) + Vector3.one * MeshMargin;
                        matrices[i] = Matrix4x4.TRS(
                            // Unity標準のPlane・Quadの可視化メッシュが汚く見えるのでズラす。超微量なので敢えて条件分岐とかはしなくてもいいだろうという判断
                            rawMatrix.GetColumn(3) + new Vector4(0, PlaneMargin, -QuadMargin, 0),
                            Quaternion.LookRotation(rawMatrix.GetColumn(2), rawMatrix.GetColumn(1)),
                            scale
                        );
                    }
                    cmd.DrawMeshInstanced(mesh, 0, material, 0, matrices, colliders.Count);
                }
                finally
                {
                    _matrixArrayPool.Return(matrices);
                }
            }
        }
    }
}