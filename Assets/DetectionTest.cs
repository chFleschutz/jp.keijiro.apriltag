using UnityEngine;
using System.Linq;
using UI = UnityEngine.UI;
using Klak.TestTools;
using System.Collections.Generic;

sealed class DetectionTest : MonoBehaviour
{
    [SerializeField] ImageSource _source = null;
    [SerializeField] int _decimation = 4;
    [SerializeField] float _tagSize = 0.05f;
    [SerializeField] Material _tagMaterial = null;
    [SerializeField] UI.RawImage _webcamPreview = null;
    [SerializeField] UI.Text _debugText = null;

    [SerializeField] GameObject _tagPrefab = null;
    Dictionary<int, GameObject> _tagObjects = new Dictionary<int, GameObject>();

    AprilTag.TagDetector _detector;
    TagDrawer _drawer;

    void Start()
    {
        var dims = _source.OutputResolution;
        _detector = new AprilTag.TagDetector(dims.x, dims.y, _decimation);
        _drawer = new TagDrawer(_tagMaterial);
    }

    void OnDestroy()
    {
        _detector.Dispose();
        _drawer.Dispose();
    }

    void LateUpdate()
    {
        _webcamPreview.texture = _source.Texture;

        // Source image acquisition
        var image = _source.Texture.AsSpan();
        if (image.IsEmpty) return;

        // AprilTag detection
        var fov = Camera.main.fieldOfView * Mathf.Deg2Rad;
        _detector.ProcessImage(image, fov, _tagSize);

        // Start with all tag objects disabled
        foreach (var obj in _tagObjects.Values)
            obj.SetActive(false);

        // Detected tag visualization
        foreach (var tag in _detector.DetectedTags)
        {
            if (!_tagObjects.ContainsKey(tag.ID))
                _tagObjects[tag.ID] = Instantiate(_tagPrefab);

            var obj = _tagObjects[tag.ID];
            obj.transform.position = tag.Position;
            obj.transform.rotation = tag.Rotation;
            obj.transform.localScale = Vector3.one * _tagSize;
            obj.SetActive(true);

            _drawer.Draw(tag.ID, tag.Position, tag.Rotation, _tagSize);
        }

        // Profile data output (with 30 frame interval)
        if (Time.frameCount % 30 == 0)
            _debugText.text = _detector.ProfileData.Aggregate
              ("Profile (usec)", (c, n) => $"{c}\n{n.name} : {n.time}");
    }
}
