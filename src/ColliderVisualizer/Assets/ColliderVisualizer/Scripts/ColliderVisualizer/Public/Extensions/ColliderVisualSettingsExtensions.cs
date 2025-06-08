using UnityEngine;

namespace ColliderVisualizer
{
    public static class ColliderVisualSettingsExtensions
    {
        public static ColliderVisualSettings WithVisualizeCollisions(this ColliderVisualSettings settings, bool value)
        {
            return new ColliderVisualSettings(value, settings.CollisionColor, settings.VisualizeTriggers, settings.TriggerColor, settings.Alpha, settings.MeshQuality);
        }
        
        public static ColliderVisualSettings WithCollisionColor(this ColliderVisualSettings settings, Color value)
        {
            return new ColliderVisualSettings(settings.VisualizeCollisions, value, settings.VisualizeTriggers, settings.TriggerColor, settings.Alpha, settings.MeshQuality);
        }
        
        public static ColliderVisualSettings WithVisualizeTriggers(this ColliderVisualSettings settings, bool value)
        {
            return new ColliderVisualSettings(settings.VisualizeCollisions, settings.CollisionColor, value, settings.TriggerColor, settings.Alpha, settings.MeshQuality);
        }
        
        public static ColliderVisualSettings WithTriggerColor(this ColliderVisualSettings settings, Color value)
        {
            return new ColliderVisualSettings(settings.VisualizeCollisions, settings.CollisionColor, settings.VisualizeTriggers, value, settings.Alpha, settings.MeshQuality);
        }
        
        public static ColliderVisualSettings WithAlpha(this ColliderVisualSettings settings, float value)
        {
            return new ColliderVisualSettings(settings.VisualizeCollisions, settings.CollisionColor, settings.VisualizeTriggers, settings.TriggerColor, Mathf.Clamp(value, 0, 1), settings.MeshQuality);
        }
        
        public static ColliderVisualSettings WithMeshQuality(this ColliderVisualSettings settings, int value)
        {
            return new ColliderVisualSettings(settings.VisualizeCollisions, settings.CollisionColor, settings.VisualizeTriggers, settings.TriggerColor, settings.Alpha, Mathf.Max(0, value));
        }
    }
}