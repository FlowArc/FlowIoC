#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FlowIoC.Editor.UI
{
    public class RectTransformContextMenu
    {
        [MenuItem("CONTEXT/RectTransform/Set Anchors To Rect", false, 151)]
        static void SetEasyAnchors()
        {
            var objs = Selection.gameObjects;

            foreach (var o in objs)
            {
                if (o != null && o.GetComponent<RectTransform>() != null)
                {
                    var r = o.GetComponent<RectTransform>();
                    var p = o.transform.parent.GetComponent<RectTransform>();

                    var offsetMin = r.offsetMin;
                    var offsetMax = r.offsetMax;
                    var anchorMin = r.anchorMin;
                    var anchorMax = r.anchorMax;

                    var parentWidth = p.rect.width;
                    var parentHeight = p.rect.height;

                    var fixedAnchorMin = new Vector2(anchorMin.x + (offsetMin.x / parentWidth), anchorMin.y + (offsetMin.y / parentHeight));
                    var fixedAnchorMax = new Vector2(anchorMax.x + (offsetMax.x / parentWidth), anchorMax.y + (offsetMax.y / parentHeight));

                    r.anchorMin = fixedAnchorMin;
                    r.anchorMax = fixedAnchorMax;

                    r.offsetMin = new Vector2(0, 0);
                    r.offsetMax = new Vector2(0, 0);
                    r.pivot = new Vector2(0.5f, 0.5f);
                }
            }
        }

        [MenuItem("CONTEXT/RectTransform/Set Anchors To Pivot", false, 151)]
        static void SetAnchorsToPivot()
        {
            var objs = Selection.gameObjects;

            foreach (var o in objs)
            {
                if (o != null && o.GetComponent<RectTransform>() != null)
                {
                    var r = o.GetComponent<RectTransform>();
                    var p = o.transform.parent.GetComponent<RectTransform>();

                    Rect rect = r.rect;

                    var offsetMin = r.offsetMin;
                    var offsetMax = r.offsetMax;
                    var anchorMin = r.anchorMin;
                    var anchorMax = r.anchorMax;
                    var sizeDelta = r.sizeDelta;

                    var parentWidth = p.rect.width;
                    var parentHeight = p.rect.height;

                    var fixedAnchorMin = new Vector2((r.localPosition.x + parentWidth / 2) / parentWidth, (r.localPosition.y + parentHeight / 2) / parentHeight);
                    var fixedAnchorMax = new Vector2((r.localPosition.x + parentWidth / 2) / parentWidth, (r.localPosition.y + parentHeight / 2) / parentHeight);

                    r.anchorMin = fixedAnchorMin;
                    r.anchorMax = fixedAnchorMax;

                    r.anchoredPosition = Vector3.zero;
                    r.sizeDelta = new Vector2(rect.width, rect.height);
                }
            }
        }
    }
}
#endif