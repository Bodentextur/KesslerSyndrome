using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Put on an empty "layer" object holding background sprites (stars, satellites, a planet).
/// parallaxFactor: 0 = glued to the camera (infinitely far, never appears to move),
///                 1 = fixed in the world (moves exactly like the foreground).
/// </summary>
[DefaultExecutionOrder(100)] // run after CameraFollow's LateUpdate so the layer never lags a frame behind
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Transform cam;
    [SerializeField, Range(0f, 1f)] private float parallaxFactor = 0.1f;

    [Header("Infinite tiling (leave at 0 for a single object like a planet)")]
    [SerializeField] private Vector2 tileSize;      // world size of one repeating tile; must be larger than the camera view
    [SerializeField] private bool autoTile3x3 = true; // duplicates this layer's children into the 8 neighbouring tiles

    [Header("Ambient drift")]
    [SerializeField] private Vector2 drift;         // constant slow movement, e.g. for passing satellites

    private Vector2 origin;
    private Vector2 camStart;
    private Vector2 driftOffset;

    private void Start()
    {
        if (cam == null) cam = Camera.main.transform;

        origin = transform.position;
        camStart = cam.position;

        if (autoTile3x3 && tileSize.x > 0f && tileSize.y > 0f)
            BuildNeighbourTiles();
    }

    private void LateUpdate()
    {
        driftOffset += drift * Time.deltaTime; // stops while paused

        Vector2 camPos = cam.position;
        Vector2 pos = origin + driftOffset + (camPos - camStart) * (1f - parallaxFactor);

        // Snap the layer in whole tiles toward the camera: the repeating pattern makes the jump invisible,
        // so the background never runs out no matter how far the player flies.
        if (tileSize.x > 0f) pos.x += Mathf.Round((camPos.x - pos.x) / tileSize.x) * tileSize.x;
        if (tileSize.y > 0f) pos.y += Mathf.Round((camPos.y - pos.y) / tileSize.y) * tileSize.y;

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    private void BuildNeighbourTiles()
    {
        // Copy the list first, because we add children while looping
        var originals = new List<Transform>();
        foreach (Transform child in transform) originals.Add(child);

        foreach (Transform original in originals)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    GameObject copy = Instantiate(original.gameObject, transform);
                    copy.transform.localPosition = original.localPosition + new Vector3(dx * tileSize.x, dy * tileSize.y, 0f);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (tileSize.x <= 0f || tileSize.y <= 0f) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, tileSize); // shows one tile in the Scene view
    }
}
