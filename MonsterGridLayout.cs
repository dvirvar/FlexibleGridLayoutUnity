using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MonsterGridLayout : LayoutGroup
{
    [SerializeField]
    [HideInInspector]
    private bool allowOutOfBounds;
    [SerializeField]
    [HideInInspector]
    private GridPriority gridPriority;
    [SerializeField]
    [HideInInspector]
    private int rows;
    [SerializeField]
    [HideInInspector]
    private int columns;
    [SerializeField]
    [HideInInspector]
    private bool customCellSize;
    [SerializeField]
    [HideInInspector]
    private ContentSizeFitter.FitMode horizontalSizePreference;
    [SerializeField]
    [HideInInspector]
    private ContentSizeFitter.FitMode verticalSizePreference;
    [SerializeField]
    [HideInInspector]
    private bool rootSpacingFromHighestCellInRow;
    [SerializeField]
    [HideInInspector]
    private bool rootSpacingFromWidestCellInColumn;
    [SerializeField]
    [HideInInspector]
    private Vector2 cellSize;
    [SerializeField]
    [HideInInspector]
    private Vector2 spacing;
    [SerializeField]
    [HideInInspector]
    private bool magnetizeCells;
    [SerializeField]
    [HideInInspector]
    private bool fitX;
    [SerializeField]
    [HideInInspector]
    private bool fitY;

    private Vector2 fitCell;
    private Dictionary<int, List<RectTransform>> cellsInRow = new Dictionary<int, List<RectTransform>>();
    private Dictionary<int, List<RectTransform>> cellsInColumn = new Dictionary<int, List<RectTransform>>();
    private Dictionary<int, float> highestCellInRows = new Dictionary<int, float>();
    private Dictionary<int, float> widestCellInColumns = new Dictionary<int, float>();

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

    /// <summary>
    /// Setups the default values of certain states/behaviours, For example: customCellSize
    /// </summary>
    private void setupDefaultProperties()
    {
        if (customCellSize)
        {
            magnetizeCells = true;
            fitX = false;
            fitY = false;
        }
        else
        {
            rootSpacingFromHighestCellInRow = false;
            rootSpacingFromWidestCellInColumn = false;
        }
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        setupDefaultProperties();

        var rowsAndColumns = calculateRowsAndColumns();
        rows = rowsAndColumns.rows;
        columns = rowsAndColumns.columns;
        if ((rows < 1 || columns < 1) && rectChildren.Count > 0)
        {
            print("Rows/Columns can't be less than 1");
            return;
        }

        List<RectTransform> rects;
        if (customCellSize)
        {
            rects = calculateCustomRectTransformsAndAdditionalData();
        }
        else
        {
            rects = rectChildren;
            fitCell = calculateSameFitCell();
            cellSize.x = fitX ? fitCell.x : cellSize.x;
            cellSize.y = fitY ? fitCell.y : cellSize.y;
            foreach (var rect in rects)
            {
                rect.sizeDelta = cellSize;
            }
        }
        populateCellsInRowAndColumn();

        for (int i = 0; i < rects.Count; i++)
        {
            int rowPosition = calculateRowPosition(i);
            int columnPosition = calculateColumnPosition(i);

            float xPos = xPosForCellsByChildAlignment(columnPosition, rowPosition);
            float yPos = yPosForCellsByChildAlignment(rowPosition, columnPosition);

            RectTransform item = rects[i];

            SetChildAlongAxis(item, 0, xPos, item.sizeDelta.x);
            SetChildAlongAxis(item, 1, yPos, item.sizeDelta.y);
        }
    }

    /// <summary>
    /// Returns how much rows/columns need to be in the grid layout
    /// </summary>
    /// <returns>The number of rows and columns</returns>
    protected virtual (int rows, int columns) calculateRowsAndColumns()
    {
        float sqrRT = Mathf.Sqrt(rectChildren.Count);
        switch (gridPriority)
        {
            case MonsterGridLayout.GridPriority.Rows:
                return (Mathf.CeilToInt(sqrRT), Mathf.RoundToInt(sqrRT));
            case MonsterGridLayout.GridPriority.Columns:
                return (Mathf.RoundToInt(sqrRT), Mathf.CeilToInt(sqrRT));
            case MonsterGridLayout.GridPriority.FixedColumns:
                return (Mathf.CeilToInt((float)rectChildren.Count / columns), columns);
            case MonsterGridLayout.GridPriority.FixedRows:
                return (rows, Mathf.CeilToInt((float)rectChildren.Count / rows));
            default:
                throw new NotImplementedException("Grid priority not implemented");
        }
    }

    /// <summary>
    /// Calculates the row position of a cell by the his index in rect children
    /// </summary>
    /// <param name="cellIndex">The index of the cell in rect children</param>
    /// <returns>The row position of the cell</returns>
    protected virtual int calculateRowPosition(int cellIndex)
    {
        return gridPriority == GridPriority.FixedColumns || gridPriority == GridPriority.Rows ? cellIndex / columns : cellIndex % rows;
    }

    /// <summary>
    /// Calculates the column position of a cell by the his index in rect children
    /// </summary>
    /// <param name="cellIndex">The index of the cell in rect children</param>
    /// <returns>The column position of the cell</returns>
    protected virtual int calculateColumnPosition(int cellIndex)
    {
        return gridPriority == MonsterGridLayout.GridPriority.FixedColumns || gridPriority == MonsterGridLayout.GridPriority.Rows ? cellIndex % columns : cellIndex / rows;
    }

    /// <summary>
    /// Determines where we should place the cell in X position
    /// </summary>
    /// <param name="columnPosition">In specific column</param>
    /// <param name="rowPosition">In specific row</param>
    /// <returns>X position</returns>
    protected virtual float xPosForCellsByChildAlignment(int columnPosition, int rowPosition)
    {
        float totalWidthOfRow = calculateTotalWidthInRow(rowPosition);
        float startOfXPos;
        bool isOutOfUIBounds = allowOutOfBounds ? false : totalWidthOfRow > realWidthAfterPadding;
        if (isOutOfUIBounds || fitX || childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.LowerLeft)
        {
            startOfXPos = 0;
        }
        else if (childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.LowerCenter)
        {
            startOfXPos = (realWidthAfterPadding - totalWidthOfRow) / 2;
        }
        else if (childAlignment == TextAnchor.UpperRight || childAlignment == TextAnchor.MiddleRight || childAlignment == TextAnchor.LowerRight)
        {
            startOfXPos = realWidthAfterPadding - totalWidthOfRow;
        }
        else
        {
            throw new NotImplementedException("Should not go here honestly");
        }
        for (int i = 0; i < columnPosition; i++)
        {
            startOfXPos += (rootSpacingFromWidestCellInColumn ? widestCellInColumns[i] : cellsInRow[rowPosition][i].sizeDelta.x) + spacing.x;
        }
        return startOfXPos + padding.left;
    }

    /// <summary>
    /// Determines where we should place the cell in Y position
    /// </summary>
    /// <param name="rowPosition">In specific row</param>
    /// <param name="columnPosition">In specific column</param>
    /// <returns>Y position</returns>
    protected virtual float yPosForCellsByChildAlignment(int rowPosition, int columnPosition)
    {
        float totalHeightOfColumn = calculateTotalHeightInColumn(columnPosition);
        float startOfYPos;
        bool isOutOfUIBounds = allowOutOfBounds ? false : totalHeightOfColumn > realHeightAfterPadding;
        if (isOutOfUIBounds || fitY || childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.UpperRight)
        {
            startOfYPos = 0;
        }
        else if (childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.MiddleRight)
        {
            startOfYPos = (realHeightAfterPadding - totalHeightOfColumn) / 2;
        }
        else if (childAlignment == TextAnchor.LowerLeft || childAlignment == TextAnchor.LowerCenter || childAlignment == TextAnchor.LowerRight)
        {
            startOfYPos = realHeightAfterPadding - totalHeightOfColumn;
        }
        else
        {
            throw new NotImplementedException("Should not go here honestly");
        }
        for (int i = 0; i < rowPosition; i++)
        {
            startOfYPos += (rootSpacingFromHighestCellInRow ? highestCellInRows[i] : cellsInColumn[columnPosition][i].sizeDelta.y) + spacing.y;
        }
        return startOfYPos + padding.top;
    }

    /// <summary>
    /// Calculates the width of a specific row
    /// </summary>
    /// <param name="rowPosition">The specific row</param>
    /// <returns>The width of a specific row</returns>
    protected float calculateTotalWidthInRow(int rowPosition)
    {
        float totalwidth;
        if (magnetizeCells && (customCellSize || gridPriority == GridPriority.Rows || gridPriority == GridPriority.FixedRows))
        {
            totalwidth = spacing.x * (numberOfCellsInRow(rowPosition) - 1);
            List<RectTransform> allCellsInRow = cellsInRow[rowPosition];

            for (int i = 0; i < allCellsInRow.Count; i++)
            {
                RectTransform rect = allCellsInRow[i];
                totalwidth += rootSpacingFromWidestCellInColumn ? widestCellInColumns[i] : rect.sizeDelta.x;
            }
        }
        else
        {
            totalwidth = spacing.x * (columns - 1);
            totalwidth += columns * cellSize.x;
        }
        return totalwidth;
    }

    /// <summary>
    /// Calculates the height of a specific column
    /// </summary>
    /// <param name="columnPosition">The specific column</param>
    /// <returns>The height of a specific column</returns>
    protected float calculateTotalHeightInColumn(int columnPosition)
    {
        float totalHeight;
        if (magnetizeCells && (customCellSize || gridPriority == GridPriority.Columns || gridPriority == GridPriority.FixedColumns))
        {
            totalHeight = spacing.y * (numberOfCellsInColumn(columnPosition) - 1);
            List<RectTransform> allCellsInColumn = cellsInColumn[columnPosition];
            for (int i = 0; i < allCellsInColumn.Count; i++)
            {
                RectTransform rect = allCellsInColumn[i];
                totalHeight += rootSpacingFromHighestCellInRow ? highestCellInRows[i] : rect.sizeDelta.y;
            }
        }
        else
        {
            totalHeight = spacing.y * (rows - 1);
            totalHeight += rows * cellSize.y;
        }
        return totalHeight;
    }

    /// <summary>
    /// Returns the number of cells in a specific row
    /// </summary>
    /// <param name="rowPosition">The row index/position</param>
    /// <returns>Number of cells in a specific row</returns>
    protected int numberOfCellsInRow(int rowPosition)
    {
        return cellsInRow[rowPosition].Count;
    }

    /// <summary>
    /// Returns the number of cells in a specific column
    /// </summary>
    /// <param name="columnPosition">The column index/position</param>
    /// <returns>Number of cells in a specific column</returns>
    protected int numberOfCellsInColumn(int columnPosition)
    {
        return cellsInColumn[columnPosition].Count;
    }

    /// <summary>
    /// Sets the custom rect transforms by .MinSize/.PreferredSize, Returns the custom rect transforms, And populates highestCellInRows/widestCellInColumns
    //  I made it in 1 function so i wont need to iterate twice :P
    /// </summary>
    /// <returns>The custom rect transforms</returns>
    private List<RectTransform> calculateCustomRectTransformsAndAdditionalData()
    {
        highestCellInRows.Clear();
        widestCellInColumns.Clear();
        for (int i = 0; i < rectChildren.Count; i++)
        {
            int rowPosition = calculateRowPosition(i);
            int columnPosition = calculateColumnPosition(i);

            float highestCellInRow;
            if (!highestCellInRows.TryGetValue(rowPosition, out highestCellInRow))
            {
                highestCellInRow = 0;
                highestCellInRows[rowPosition] = highestCellInRow;
            }

            float widestCellInColumn;
            if (!widestCellInColumns.TryGetValue(columnPosition, out widestCellInColumn))
            {
                widestCellInColumn = 0;
                widestCellInColumns[columnPosition] = widestCellInColumn;
            }

            RectTransform rectChild = rectChildren[i];
            LayoutElement layoutElement = rectChild.GetComponent<LayoutElement>();
            Vector2 cellSize = Vector2.zero;
            if (layoutElement != null)
            {
                if (horizontalSizePreference == ContentSizeFitter.FitMode.MinSize)
                {
                    cellSize.x = layoutElement.minWidth;
                }
                else if (horizontalSizePreference == ContentSizeFitter.FitMode.PreferredSize)
                {
                    cellSize.x = layoutElement.preferredWidth;
                }

                if (verticalSizePreference == ContentSizeFitter.FitMode.MinSize)
                {
                    cellSize.y = layoutElement.minHeight;
                }
                else if (verticalSizePreference == ContentSizeFitter.FitMode.PreferredSize)
                {
                    cellSize.y = layoutElement.preferredHeight;
                }

                if (cellSize.x > widestCellInColumn)
                {
                    widestCellInColumns[columnPosition] = cellSize.x;
                }

                if (cellSize.y > highestCellInRow)
                {
                    highestCellInRows[rowPosition] = cellSize.y;
                }
            }
            rectChild.sizeDelta = cellSize;
        }

        return rectChildren;
    }

    /// <summary>
    /// Populates cellsInRow and cellsInColumn
    /// </summary>
    private void populateCellsInRowAndColumn()
    {
        cellsInRow.Clear();
        cellsInColumn.Clear();
        for (int i = 0; i < rectChildren.Count; i++)
        {
            int rowPosition = calculateRowPosition(i);
            int columnPosition = calculateColumnPosition(i);
            RectTransform rect = rectChildren[i];
            List<RectTransform> rowRects;
            if (!cellsInRow.TryGetValue(rowPosition, out rowRects))
            {
                rowRects = new List<RectTransform>();
            }

            if (!rowRects.Contains(rect))
            {
                rowRects.Add(rect);
                cellsInRow[rowPosition] = rowRects;
            }

            List<RectTransform> columnRects;
            if (!cellsInColumn.TryGetValue(columnPosition, out columnRects))
            {
                columnRects = new List<RectTransform>();
            }

            if (!columnRects.Contains(rect))
            {
                columnRects.Add(rect);
                cellsInColumn[columnPosition] = columnRects;
            }
        }
    }

    /// <summary>
    /// Calculates the size of a cell, In a way that all the cells will fit in screen
    /// </summary>
    /// <returns>The size of the cell</returns>
    private Vector2 calculateSameFitCell()
    {
        Vector2 vector2 = Vector2.zero;
        vector2.x = (realWidthAfterPadding - (spacing.x * (columns - 1))) / columns;
        vector2.y = (realHeightAfterPadding - (spacing.y * (rows - 1))) / rows;
        return vector2;
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
    [CustomEditor(typeof(MonsterGridLayout))]
    [CanEditMultipleObjects]
    public class MonsterGridLayoutEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            MonsterGridLayout monsterGridLayout = (MonsterGridLayout)target;
            monsterGridLayout.allowOutOfBounds = EditorGUILayout.Toggle(new GUIContent("Allow out of bounds", "Ignores the bounds of the screen for child alignment, In other words if not allowing out of bounds it will behave like unity layout, Means that if the cells are out of bounds it will snap to Upper or Left child alignment"), monsterGridLayout.allowOutOfBounds);
            monsterGridLayout.gridPriority = (GridPriority)EditorGUILayout.EnumPopup("Grid Priority", monsterGridLayout.gridPriority);
            monsterGridLayout.rows = EditorGUILayout.IntField("Rows", monsterGridLayout.rows);
            monsterGridLayout.columns = EditorGUILayout.IntField("Columns", monsterGridLayout.columns);
            monsterGridLayout.customCellSize = EditorGUILayout.Toggle(new GUIContent("Custom Cell Size", "When using custom cell size,Use layout element to customize the cells"), monsterGridLayout.customCellSize);

            if (monsterGridLayout.customCellSize)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Horizontal Size Preference");
                monsterGridLayout.horizontalSizePreference = (ContentSizeFitter.FitMode)EditorGUILayout.EnumPopup(monsterGridLayout.horizontalSizePreference, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Vertical Size Preference");
                monsterGridLayout.verticalSizePreference = (ContentSizeFitter.FitMode)EditorGUILayout.EnumPopup(monsterGridLayout.verticalSizePreference, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
                if (monsterGridLayout.verticalSizePreference == ContentSizeFitter.FitMode.Unconstrained || monsterGridLayout.horizontalSizePreference == ContentSizeFitter.FitMode.Unconstrained)
                {
                    EditorGUILayout.HelpBox("Unconstraint is not supported", MessageType.Warning);
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Root Spacing From Highest Cell In Row", "The next row will start from the previous row's highest cell"));
                monsterGridLayout.rootSpacingFromHighestCellInRow = EditorGUILayout.Toggle(monsterGridLayout.rootSpacingFromHighestCellInRow, GUILayout.Width(14));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Root Spacing From Widest Cell In Column", "The next column will start from the previous column's widest cell"));
                monsterGridLayout.rootSpacingFromWidestCellInColumn = EditorGUILayout.Toggle(monsterGridLayout.rootSpacingFromWidestCellInColumn, GUILayout.Width(14));
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cell Size", GUILayout.Width(65));
                EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true), GUILayout.MaxWidth(50));
                GUI.enabled = !monsterGridLayout.fitX;
                EditorGUILayout.LabelField("X", GUILayout.Width(8));
                monsterGridLayout.cellSize.x = EditorGUILayout.FloatField(monsterGridLayout.cellSize.x, GUILayout.MinWidth(55), GUILayout.Width(55), GUILayout.ExpandWidth(true));
                GUI.enabled = !monsterGridLayout.fitY;
                EditorGUILayout.LabelField("Y", GUILayout.Width(8));
                monsterGridLayout.cellSize.y = EditorGUILayout.FloatField(monsterGridLayout.cellSize.y, GUILayout.MinWidth(50), GUILayout.Width(50), GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField("", GUILayout.ExpandWidth(true), GUILayout.MinWidth(60), GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                GUI.enabled = true;
            }

            monsterGridLayout.spacing = EditorGUILayout.Vector2Field("Spacing", monsterGridLayout.spacing);
            if (!monsterGridLayout.customCellSize)
            {
                monsterGridLayout.magnetizeCells = EditorGUILayout.Toggle(new GUIContent("Magnetize Cells", "The Row/Column(depends on the grid priority) that has less cells will snap to child alignment grid"), monsterGridLayout.magnetizeCells);
                monsterGridLayout.fitX = EditorGUILayout.Toggle("Fit X", monsterGridLayout.fitX);
                monsterGridLayout.fitY = EditorGUILayout.Toggle("Fit Y", monsterGridLayout.fitY);
            }

            monsterGridLayout.CalculateLayoutInputHorizontal();
        }
    }
#endif
}