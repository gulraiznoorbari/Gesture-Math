using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.HandPose;

public sealed class HandAnimator : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] ImageSource _source = null;
    [Space]
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] bool _useAsyncReadback = true;
    [Space]
    [SerializeField] Mesh _jointMesh = null;
    [SerializeField] Mesh _boneMesh = null;
    [Space]
    [SerializeField] Material _jointMaterial = null;
    [SerializeField] Material _boneMaterial = null;
    [Space]
    [SerializeField] RawImage _monitorUI = null;

    #endregion

    #region Private members

    HandPipeline _pipeline;

    static readonly (int, int)[] BonePairs =
    {
        (0, 1), (1, 2), (1, 2), (2, 3), (3, 4),     // Thumb
        (5, 6), (6, 7), (7, 8),                     // Index finger
        (9, 10), (10, 11), (11, 12),                // Middle finger
        (13, 14), (14, 15), (15, 16),               // Ring finger
        (17, 18), (18, 19), (19, 20),               // Pinky
        (0, 17), (2, 5), (5, 9), (9, 13), (13, 17)  // Palm
    };

    Matrix4x4 CalculateJointXform(Vector3 pos)
      => Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * 0.07f);

    Matrix4x4 CalculateBoneXform(Vector3 p1, Vector3 p2)
    {
        var length = Vector3.Distance(p1, p2) / 2;
        var radius = 0.03f;

        var center = (p1 + p2) / 2;
        var rotation = Quaternion.FromToRotation(Vector3.up, p2 - p1);
        var scale = new Vector3(radius, length, radius);

        return Matrix4x4.TRS(center, rotation, scale);
    }

    int CountExtendedFingers()
    {
        var extendedFingers = 0;

        // Thumb detection
        var thumbTip = _pipeline.GetKeyPoint(4); // Thumb tip
        var thumbIP = _pipeline.GetKeyPoint(3); // Thumb interphalangeal joint
        var thumbMCP = _pipeline.GetKeyPoint(2); // Thumb metacarpophalangeal joint
        var wrist = _pipeline.GetKeyPoint(0); // Wrist (palm base)

        // Calculate the distance between the thumb tip and the wrist
        var thumbTipToWristDistance = Vector3.Distance(thumbTip, wrist);

        // Calculate the distance between the thumb MCP and the wrist
        var thumbMCPToWristDistance = Vector3.Distance(thumbMCP, wrist);

        // If the thumb tip is far enough from the wrist and the MCP, it's extended
        if (thumbTipToWristDistance > thumbMCPToWristDistance * 1.5f) // Adjust threshold as needed
            extendedFingers++;

        // Index finger (key points 5 to 8)
        if (_pipeline.GetKeyPoint(8).y > _pipeline.GetKeyPoint(6).y)
            extendedFingers++;

        // Middle finger (key points 9 to 12)
        if (_pipeline.GetKeyPoint(12).y > _pipeline.GetKeyPoint(10).y)
            extendedFingers++;

        // Ring finger (key points 13 to 16)
        if (_pipeline.GetKeyPoint(16).y > _pipeline.GetKeyPoint(14).y)
            extendedFingers++;

        // Pinky (key points 17 to 20)
        if (_pipeline.GetKeyPoint(20).y > _pipeline.GetKeyPoint(18).y)
            extendedFingers++;

        return extendedFingers;
    }

    #endregion

    #region MonoBehaviour implementation

    private void Start() => _pipeline = new HandPipeline(_resources);

    private void OnDestroy() => _pipeline.Dispose();

    private void LateUpdate()
    {
        // Feed the input image to the Hand pose pipeline.
        _pipeline.UseAsyncReadback = _useAsyncReadback;
        _pipeline.ProcessImage(_source.Texture);

        var layer = gameObject.layer;

        // Joint ballsx
        for (var i = 0; i < HandPipeline.KeyPointCount; i++)
        {
            var xform = CalculateJointXform(_pipeline.GetKeyPoint(i));
            Graphics.DrawMesh(_jointMesh, xform, _jointMaterial, layer);
        }

        // Bones
        foreach (var pair in BonePairs)
        {
            var p1 = _pipeline.GetKeyPoint(pair.Item1);
            var p2 = _pipeline.GetKeyPoint(pair.Item2);
            var xform = CalculateBoneXform(p1, p2);
            Graphics.DrawMesh(_boneMesh, xform, _boneMaterial, layer);
        }

        // UI update
        _monitorUI.texture = _source.Texture;

        var extendedFingers = CountExtendedFingers();
        Debug.Log("Extended Fingers: " + extendedFingers);

        // Use the number of extended fingers to determine the user's input
        if (extendedFingers == 1)
        {
            Debug.Log("User selected addition");
        }
        else if (extendedFingers == 2)
        {
            Debug.Log("User selected subtraction");
        }
        else if (extendedFingers == 3)
        {
            Debug.Log("User selected equals");
        }
    }

    #endregion
}