using System.Collections;
using UnityEngine;
using ColliderVisualizer;
using UnityEngine.UI;
using TMPro;

public class TestView : MonoBehaviour
{
    [SerializeField] private Toggle _visualizeCollisionsToggle;
    [SerializeField] private Toggle _visualizeTriggersToggle;
    [SerializeField] private TextMeshProUGUI _alphaText;
    [SerializeField] private Slider _alphaSlider;
    [SerializeField] private TextMeshProUGUI _meshQualityText;
    [SerializeField] private Slider _meshQualitySlider;
    [SerializeField] private Button _updateCollidersButton;
    
    private void Start()
    {
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        yield return new WaitUntil(() => ColliderVisualize.IsRuntimeInitialized);
        
        _visualizeCollisionsToggle.isOn = ColliderVisualize.VisualizeCollisions;
        _visualizeTriggersToggle.isOn = ColliderVisualize.VisualizeTriggers;
        _alphaSlider.value = ColliderVisualize.Alpha;
        _meshQualitySlider.value = ColliderVisualize.MeshQuality;

        _visualizeCollisionsToggle.onValueChanged.AddListener(value => ColliderVisualize.VisualizeCollisions = value);
        _visualizeTriggersToggle.onValueChanged.AddListener(value => ColliderVisualize.VisualizeTriggers = value);
        _alphaSlider.onValueChanged.AddListener(value => ColliderVisualize.Alpha = value);
        _alphaSlider.onValueChanged.AddListener(value => _alphaText.text = value.ToString("F2"));
        _meshQualitySlider.onValueChanged.AddListener(value => ColliderVisualize.MeshQuality = (int)value);
        _meshQualitySlider.onValueChanged.AddListener(value => _meshQualityText.text = value.ToString("F0"));
        _updateCollidersButton.onClick.AddListener(ColliderVisualize.UpdateColliders);
    }
}
