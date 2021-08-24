using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FlexibleScrollableGridLayout : FlexibleGridLayout
{
    private RectTransform parentRectTransform;

    protected override float rectWidth => parentRectTransform.rect.width;
    protected override float rectHeight => parentRectTransform.rect.height;
    private Vector2 gridSize => new Vector2(totalWidthOfCells + padding.left + padding.right, totalHeightOfCells + padding.top + padding.bottom);


    //So it will work in editor
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        parentRectTransform = rectTransform.parent.GetComponent<RectTransform>();
    }
    #endif

    //So it will work in run time
    protected override void Start()
    {
        base.Start();
        parentRectTransform = rectTransform.parent.GetComponent<RectTransform>();
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        rectTransform.anchorMin = new Vector2(.5f, .5f);
        rectTransform.anchorMax = new Vector2(.5f, .5f);
        rectTransform.sizeDelta = gridSize;//For scroll to work
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(FlexibleScrollableGridLayout))]
    [CanEditMultipleObjects]
    public class FlexibleScrollableGridLayoutEditor : FlexibleGridLayoutEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
#endif
}