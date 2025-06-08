using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ColliderVisualizer
{
    /// <summary>
    /// 互換性が無い新旧CommandBufferを共通して使えるようにするインターフェース
    /// </summary>
    public interface ICommandBufferAdapter<TCmd> where TCmd : ICommandBufferAdapter<TCmd>
    {
        void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count);
    }

    /// <summary>
    /// レガシーCommandBuffer用
    /// </summary>
    public readonly struct LegacyCommandBufferAdapter : ICommandBufferAdapter<LegacyCommandBufferAdapter>
    {
        private readonly CommandBuffer _cmd;
        public LegacyCommandBufferAdapter(CommandBuffer cmd) => _cmd = cmd;
        public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count) 
            => _cmd.DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count);
    }

    /// <summary>
    /// IRasterCommandBuffer用
    /// </summary>
    public readonly struct RasterCommandBufferAdapter : ICommandBufferAdapter<RasterCommandBufferAdapter>
    {
        private readonly IRasterCommandBuffer _cmd;
        public RasterCommandBufferAdapter(IRasterCommandBuffer cmd)  => _cmd = cmd;
        public void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, int shaderPass, Matrix4x4[] matrices, int count)
            => _cmd.DrawMeshInstanced(mesh, submeshIndex, material, shaderPass, matrices, count);
    }

    /// <summary>
    /// コライダー可視化描画の抽象基底クラス
    /// </summary>
    internal abstract class ColliderVisualizePassBase : ScriptableRenderPass
    {
        private ColliderVisualSettings _currentSettings;
        
        // コライダー描画メッシュ
        protected readonly Mesh CubeMesh;
        protected Mesh SphereMesh;
        protected Mesh HemisphereMesh;
        protected Mesh TubeMesh;

        // コライダー描画マテリアル
        protected Material CollisionMaterial;
        protected Material TriggerMaterial;

        // 可視化するかどうか
        protected bool VisualizeCollisions;
        protected bool VisualizeTriggers;

        // 可視化するコライダー
        protected BoxCollider[] BoxCollidersCollision;
        protected BoxCollider[] BoxCollidersTrigger;
        protected SphereCollider[] SphereCollidersCollision;
        protected SphereCollider[] SphereCollidersTrigger;
        protected CapsuleCollider[] CapsuleCollidersCollision;
        protected CapsuleCollider[] CapsuleCollidersTrigger;
        protected readonly Dictionary<Mesh, List<MeshCollider>> MeshColliderGroupsCollision = new();
        protected readonly Dictionary<Mesh, List<MeshCollider>> MeshColliderGroupsTrigger = new();

        // 最適化用：再利用可能な配列プール
        private static readonly MatrixArrayPool _matrixArrayPool = new();
        
        // 描画処理中にコライダーが破棄されたときに上がるフラグ
        protected static bool ThrownMissingReferenceException;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        protected ColliderVisualizePassBase(ColliderVisualSettings settings)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            
            BoxCollidersCollision = Array.Empty<BoxCollider>();
            BoxCollidersTrigger = Array.Empty<BoxCollider>();
            SphereCollidersCollision = Array.Empty<SphereCollider>();
            SphereCollidersTrigger = Array.Empty<SphereCollider>();
            CapsuleCollidersCollision = Array.Empty<CapsuleCollider>();
            CapsuleCollidersTrigger = Array.Empty<CapsuleCollider>();
            
            CubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            SphereMesh = MeshCreator.CreateSphere(settings.MeshQuality);
            HemisphereMesh = MeshCreator.CreateHemisphere(settings.MeshQuality);
            TubeMesh = MeshCreator.CreateTube(settings.MeshQuality);
            
            VisualizeCollisions = settings.VisualizeCollisions;
            VisualizeTriggers = settings.VisualizeTriggers;
            
            CollisionMaterial = ColliderVisualizerUtility.CreateMaterial(settings.CollisionColor, settings.Alpha);
            TriggerMaterial = ColliderVisualizerUtility.CreateMaterial(settings.TriggerColor, settings.Alpha);
            
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
                Object.DestroyImmediate(SphereMesh);
                Object.DestroyImmediate(HemisphereMesh);
                Object.DestroyImmediate(TubeMesh);
                SphereMesh = MeshCreator.CreateSphere(settings.MeshQuality);
                HemisphereMesh = MeshCreator.CreateHemisphere(settings.MeshQuality);
                TubeMesh = MeshCreator.CreateTube(settings.MeshQuality);
            }

            // 色とアルファの差分をチェック
            var alphaChanged = !Mathf.Approximately(_currentSettings.Alpha, settings.Alpha);
            if (alphaChanged || _currentSettings.CollisionColor != settings.CollisionColor)
            {
                CoreUtils.Destroy(CollisionMaterial);
                CollisionMaterial = ColliderVisualizerUtility.CreateMaterial(settings.CollisionColor, settings.Alpha);
            }
            if (alphaChanged || _currentSettings.TriggerColor != settings.TriggerColor)
            {
                CoreUtils.Destroy(TriggerMaterial);
                TriggerMaterial = ColliderVisualizerUtility.CreateMaterial(settings.TriggerColor, settings.Alpha);
            }
            
            // 可視化設定を更新
            VisualizeCollisions = settings.VisualizeCollisions;
            VisualizeTriggers = settings.VisualizeTriggers;

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
            BoxCollidersCollision = boxCollidersCollision;
            BoxCollidersTrigger = boxCollidersTrigger;
            SphereCollidersCollision = sphereCollidersCollision;
            SphereCollidersTrigger = sphereCollidersTrigger;
            CapsuleCollidersCollision = capsuleCollidersCollision;
            CapsuleCollidersTrigger = capsuleCollidersTrigger;
            GroupMeshColliders(meshCollidersCollision, meshCollidersTrigger);
        }

        // MeshColliderを事前にMesh別にグループ化する
        private void GroupMeshColliders(MeshCollider[] meshCollidersCollision, MeshCollider[] meshCollidersTrigger)
        {
            // コリジョン用MeshColliderのグループ化
            MeshColliderGroupsCollision.Clear();
            for (int i = 0; i < meshCollidersCollision.Length; ++i)
            {
                var collider = meshCollidersCollision[i];
                if (collider == null || collider.sharedMesh == null) continue;
                
                var mesh = collider.sharedMesh;
                if (!MeshColliderGroupsCollision.TryGetValue(mesh, out var list))
                {
                    list = new List<MeshCollider>();
                    MeshColliderGroupsCollision[mesh] = list;
                }
                list.Add(collider);
            }
            
            // トリガー用MeshColliderのグループ化
            MeshColliderGroupsTrigger.Clear();
            for (int i = 0; i < meshCollidersTrigger.Length; ++i)
            {
                var collider = meshCollidersTrigger[i];
                if (collider == null || collider.sharedMesh == null) continue;
                
                var mesh = collider.sharedMesh;
                if (!MeshColliderGroupsTrigger.TryGetValue(mesh, out var list))
                {
                    list = new List<MeshCollider>();
                    MeshColliderGroupsTrigger[mesh] = list;
                }
                list.Add(collider);
            }
        }

        // コライダーリストからMissingを除外する
        protected void FilterMissingColliders()
        {
            BoxCollidersCollision = BoxCollidersCollision.Where(c => c != null).ToArray();
            BoxCollidersTrigger = BoxCollidersTrigger.Where(c => c != null).ToArray();
            SphereCollidersCollision = SphereCollidersCollision.Where(c => c != null).ToArray();
            SphereCollidersTrigger = SphereCollidersTrigger.Where(c => c != null).ToArray();
            CapsuleCollidersCollision = CapsuleCollidersCollision.Where(c => c != null).ToArray();
            CapsuleCollidersTrigger = CapsuleCollidersTrigger.Where(c => c != null).ToArray();
            foreach (var key in MeshColliderGroupsCollision.Keys.ToArray())
            {
                MeshColliderGroupsCollision[key] = MeshColliderGroupsCollision[key].Where(c => c != null).ToList();
            }
            foreach (var key in MeshColliderGroupsTrigger.Keys.ToArray())
            {
                MeshColliderGroupsTrigger[key] = MeshColliderGroupsTrigger[key].Where(c => c != null).ToList();
            }
        }

        private const float BoxMargin = 0.01f;

        /// <summary>
        /// BoxColliderの描画
        /// </summary>
        protected static void DrawBoxColliders<TCmd>(TCmd cmd, Mesh mesh, Material material, BoxCollider[] colliders)  where TCmd : ICommandBufferAdapter<TCmd>
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
        protected static void DrawSphereColliders<TCmd>(TCmd cmd, Mesh mesh, Material material, SphereCollider[] colliders)  where TCmd : ICommandBufferAdapter<TCmd>
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
        protected static void DrawCapsuleColliders<TCmd>(TCmd cmd, Mesh hemisphereMesh, Mesh tubeMesh, Material material, CapsuleCollider[] colliders) where TCmd : ICommandBufferAdapter<TCmd>
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
        protected static void DrawMeshColliders<TCmd>(TCmd cmd, Material material, Dictionary<Mesh, List<MeshCollider>> meshGroups) where TCmd : ICommandBufferAdapter<TCmd>
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