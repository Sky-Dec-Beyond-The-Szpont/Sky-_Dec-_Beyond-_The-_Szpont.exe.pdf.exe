using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [Header("Ruch po œcie¿ce")]
    public float tilesPerSecond = 3f;   // ile kafelków na sekundê

    [Header("Skakanie")]
    public float hopHeight = 0.25f;     // wysokoœæ skoku

    private Coroutine moveRoutine;
    private Action onCompleteCallback;

    /// <summary>
    /// Ruch po liœcie pozycji w œwiecie (œrodki kafelków).
    /// Opcjonalnie wywo³a onComplete po zakoñczeniu.
    /// </summary>
    public void MoveAlongWorldPositions(List<Vector3> worldPositions, Action onComplete = null)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        onCompleteCallback = onComplete;

        if (worldPositions == null || worldPositions.Count == 0)
        {
            onCompleteCallback = null;
            return;
        }

        moveRoutine = StartCoroutine(MoveRoutine(worldPositions));
    }

    private IEnumerator MoveRoutine(List<Vector3> worldPositions)
    {
        if (worldPositions.Count == 0)
            yield break;

        float baseY = transform.position.y;

        Vector3 current = new Vector3(worldPositions[0].x, baseY, worldPositions[0].z);
        transform.position = current;

        for (int i = 1; i < worldPositions.Count; i++)
        {
            Vector3 next = worldPositions[i];
            next.y = baseY;

            Vector3 delta = next - current;
            float distance = delta.magnitude;
            if (distance < 0.0001f)
            {
                current = next;
                transform.position = current;
                continue;
            }

            float duration = 1f / tilesPerSecond;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                if (t > 1f) t = 1f;

                Vector3 pos = Vector3.Lerp(current, next, t);

                // klasyczna parabola 0->hopHeight->0
                float h = 4f * hopHeight * t * (1f - t);
                pos.y = baseY + h;

                transform.position = pos;

                yield return null;
            }

            current = next;
            transform.position = current;
        }

        moveRoutine = null;

        // wywo³aj callback po skoñczeniu ca³ej trasy
        var cb = onCompleteCallback;
        onCompleteCallback = null;
        cb?.Invoke();
    }
}
