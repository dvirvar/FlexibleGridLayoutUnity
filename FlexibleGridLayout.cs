using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FlexibleGridLayout : LayoutGroup
{
    private bool allowOutOfBounds;
    private GridPriority gridPriority;
    private int rows;
    private int columns;
    private Vector2 cellSize;
    private Vector2 spacing;
    private bool fitX;
    private bool fitY;

    public enum GridPriority
    {
        Rows,
        Columns,
        FixedRows,
        FixedColumns
    }

    private float rectWidth => rectTransform.rect.width; //The width of this ui component
    private float rectHeight => rectTransform.rect.height; //The height of this ui component
    private float realWidthAfterPadding => rectWidth - padding.left - padding.right; //The width of this ui component after padding
    private float realHeightAfterPadding => rectHeight - padding.top - padding.bottom; //The height of this ui component after padding
    private float totalWidthOfCells => (columns * cellSize.x) + (spacing.x * (columns - 1)); //The total width of all the cells including spacing
    private float totalHeightOfCells => (rows * cellSize.y) + (spacing.y * (rows - 1)); //The total height of all the cells including spacing
    private float fitCellWidth => (realWidthAfterPadding - (spacing.x * (columns - 1))) / columns; //The width of of a cell, In a way that all the cells will fit in screen
    private float fitCellHeight => (realHeightAfterPadding - (spacing.y * (rows - 1))) / rows; //The height of of a cell, In a way that all the cells will fit in screen

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        var rowsAndColumns = calculateRowsAndColumns();
        rows = rowsAndColumns.rows;
        columns = rowsAndColumns.columns;
        if ((rows < 1 || columns < 1) && rectChildren.Count > 0)
        {
            print("Rows/Columns can't be less than 1");
            return;
        }

        cellSize.x = fitX ? fitCellWidth : cellSize.x;
        cellSize.y = fitY ? fitCellHeight : cellSize.y;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int rowPosition = gridPriority == GridPriority.FixedColumns || gridPriority == GridPriority.Rows ? i / columns : i % rows;
            int columnPosition = gridPriority == GridPriority.FixedColumns || gridPriority == GridPriority.Rows ? i % columns : i / rows;

            RectTransform item = rectChildren[i];
            float xPos = xPosByChildAlignment(cellSize.x, columnPosition);
            float yPos = yPosByChildAlignment(cellSize.y, rowPosition);

            SetChildAlongAxis(item, 0, xPos, cellSize.x);
            SetChildAlongAxis(item, 1, yPos, cellSize.y);
        }
    }

    /// <summary>
    /// Returns how much rows/columns need to be in the grid layout
    /// </summary>
    /// <returns>The number of rows and columns</returns>
    protected (int rows, int columns) calculateRowsAndColumns()
    {
        float sqrRT = Mathf.Sqrt(rectChildren.Count);
        switch (gridPriority)
        {
            case FlexibleGridLayout.GridPriority.Rows:
                return (Mathf.CeilToInt(sqrRT), Mathf.RoundToInt(sqrRT));
            case FlexibleGridLayout.GridPriority.Columns:
                return (Mathf.RoundToInt(sqrRT), Mathf.CeilToInt(sqrRT));
            case FlexibleGridLayout.GridPriority.FixedColumns:
                return (Mathf.CeilToInt((float)rectChildren.Count / columns), columns);
            case FlexibleGridLayout.GridPriority.FixedRows:
                return (rows, Mathf.CeilToInt((float)rectChildren.Count / rows));
            default:
                throw new NotImplementedException("Grid priority not implemented");
        }
    }

    /// <summary>
    /// Returns the xPos of the cell in a specific row
    /// </summary>
    /// <param name="cellWidth">The width of the cell</param>
    /// <param name="columnPosition">The column poisition</param>
    /// <returns>The xPos of the cell in a specific row</returns>
    private float xPosByChildAlignment(float cellWidth, int columnPosition)
    {
        bool isOutOfUIBounds = allowOutOfBounds ? false : totalWidthOfCells > realWidthAfterPadding;
        if (isOutOfUIBounds || fitX || childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.LowerLeft)
        {
            return (cellWidth * columnPosition) + (spacing.x * columnPosition) + padding.left;
        }
        else if (childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.LowerCenter)
        {
            float startOfXPos = (realWidthAfterPadding - totalWidthOfCells) / 2;
            float xPosOfChild = startOfXPos + (columnPosition * (cellWidth + spacing.x));
            return xPosOfChild + padding.left;
        }
        else if (childAlignment == TextAnchor.UpperRight || childAlignment == TextAnchor.MiddleRight || childAlignment == TextAnchor.LowerRight)
        {
            float startOfXPos = realWidthAfterPadding - totalWidthOfCells;
            float xPosOfChild = startOfXPos + (columnPosition * (cellWidth + spacing.x));
            return xPosOfChild + padding.left;
        }
        throw new System.NotImplementedException("Should not go here honestly");
    }
    /// <summary>
    /// Returns the yPos of the cell in a specific row
    /// </summary>
    /// <param name="cellHeight">The height of the cell</param>
    /// <param name="rowPosition">The row poisition</param>
    /// <returns>The yPos of the cell in a specific row</returns>
    private float yPosByChildAlignment(float cellHeight, int rowPosition)
    {
        bool isOutOfUIBounds = allowOutOfBounds ? false : totalHeightOfCells > realHeightAfterPadding;
        if (isOutOfUIBounds || fitY || childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.UpperRight)
        {
            return (cellHeight * rowPosition) + (spacing.y * rowPosition) + padding.top;
        }
        else if (childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.MiddleRight)
        {
            float startOfYPos = (realHeightAfterPadding - totalHeightOfCells) / 2;
            float yPosOfChild = startOfYPos + (rowPosition * (cellHeight + spacing.y));
            return yPosOfChild + padding.top;
        }
        else if (childAlignment == TextAnchor.LowerLeft || childAlignment == TextAnchor.LowerCenter || childAlignment == TextAnchor.LowerRight)
        {
            float startOfYPos = realHeightAfterPadding - totalHeightOfCells;
            float yPosOfChild = startOfYPos + (rowPosition * (cellHeight + spacing.y));
            return yPosOfChild + padding.top;
        }
        throw new System.NotImplementedException("Should not go here honestly");
    }

    public override void CalculateLayoutInputVertical()
    {

    }

    public override void SetLayoutHorizontal()
    {

    }

    public override void SetLayoutVertical()
    {

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FlexibleGridLayout))]
    [CanEditMultipleObjects]
    public class FlexibleGridLayoutEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            FlexibleGridLayout flexibleGridLayout = (FlexibleGridLayout)target;
            flexibleGridLayout.allowOutOfBounds = EditorGUILayout.Toggle(new GUIContent("Allow out of bounds", "Ignores the bounds of the screen for child alignment, In other words if not allowing out of bounds it will behave like unity layout, Means that if the cells are out of bounds it will snap to Upper or Left child alignment"), flexibleGridLayout.allowOutOfBounds);
            flexibleGridLayout.gridPriority = (GridPriority)EditorGUILayout.EnumPopup("Grid Priority", flexibleGridLayout.gridPriority);
            flexibleGridLayout.rows = EditorGUILayout.IntField("Rows", flexibleGridLayout.rows);
            flexibleGridLayout.columns = EditorGUILayout.IntField("Columns", flexibleGridLayout.columns);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cell Size", GUILayout.Width(65));
            EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true), GUILayout.MaxWidth(50));
            GUI.enabled = !flexibleGridLayout.fitX;
            EditorGUILayout.LabelField("X", GUILayout.Width(8));
            flexibleGridLayout.cellSize.x = EditorGUILayout.FloatField(flexibleGridLayout.cellSize.x, GUILayout.MinWidth(55), GUILayout.Width(55), GUILayout.ExpandWidth(true));
            GUI.enabled = !flexibleGridLayout.fitY;
            EditorGUILayout.LabelField("Y", GUILayout.Width(8));
            flexibleGridLayout.cellSize.y = EditorGUILayout.FloatField(flexibleGridLayout.cellSize.y, GUILayout.MinWidth(50), GUILayout.Width(50), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true), GUILayout.MinWidth(60), GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            flexibleGridLayout.spacing = EditorGUILayout.Vector2Field("Spacing", flexibleGridLayout.spacing);
            flexibleGridLayout.fitX = EditorGUILayout.Toggle("Fit X", flexibleGridLayout.fitX);
            flexibleGridLayout.fitY = EditorGUILayout.Toggle("Fit Y", flexibleGridLayout.fitY);

            flexibleGridLayout.CalculateLayoutInputHorizontal();
        }
    }
#endif
}
