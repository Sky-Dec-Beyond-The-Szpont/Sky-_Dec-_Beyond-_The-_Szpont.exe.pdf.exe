using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GambleResultDetector : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform viewport;
    public List<RectTransform> items;

    public RectTransform GetItemAtCenter()
    {
        float viewportCenterX =
            viewport.TransformPoint(new Vector3(viewport.rect.width / 2f, 0, 0)).x;

        RectTransform closestItem = null;
        float closestDistance = float.MaxValue;

        foreach (var item in items)
        {
            float itemX = item.TransformPoint(Vector3.zero).x;
            float distance = Mathf.Abs(itemX - viewportCenterX);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestItem = item;
            }
        }

        return closestItem;
    }
}