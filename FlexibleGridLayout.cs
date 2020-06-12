using UnityEngine;
using UnityEngine.UI;

public class FlexibleGridLayout : LayoutGroup
{
    [SerializeField] private CellPriority cellPriority;
    [SerializeField] private int rows;
    [SerializeField] private int columns;
    [SerializeField] private Vector2 cellSize;
    [SerializeField] private Vector2 spacing;
    [SerializeField] private bool fitX;
    [SerializeField] private bool fitY;

    public enum CellPriority
    {
        Width,
        Height,
        FixedRows,
        FixedColumns
    }

    private float parentWidth => rectTransform.rect.width; //The width of this ui component
    private float parentHeight => rectTransform.rect.height; //The height of this ui component
    private float realWidthAfterPadding => parentWidth - padding.left - padding.right; //The width of this ui component after padding
    private float realHeightAfterPadding => parentHeight - padding.top - padding.bottom; //The height of this ui component after padding
    private float totalWidthOfCells => (columns * cellSize.x) + (spacing.x * (columns - 1)); //The total width of all the cells including spacing
    private float totalHeightOfCells => (rows * cellSize.y) + (spacing.y * (rows - 1)); //The total height of all the cells including spacing

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        float sqrRT = Mathf.Sqrt(transform.childCount);
        if (cellPriority == CellPriority.Width)
        {
            columns = Mathf.RoundToInt(sqrRT);
            rows = Mathf.CeilToInt(sqrRT);
        } else if (cellPriority == CellPriority.Height)
        {
            columns = Mathf.CeilToInt(sqrRT);
            rows = Mathf.RoundToInt(sqrRT);
        } else if (cellPriority == CellPriority.FixedColumns)
        {
            rows = Mathf.CeilToInt((float)transform.childCount / columns);
        } else if (cellPriority == CellPriority.FixedRows)
        {
            columns = Mathf.CeilToInt((float)transform.childCount / rows);
        }

        float fitCellWidth = (realWidthAfterPadding - (spacing.x * (columns - 1)))/columns;
        float fitCellHeight = (realHeightAfterPadding - (spacing.y * (rows - 1)))/rows;

        cellSize.x = fitX ? fitCellWidth : cellSize.x;
        cellSize.y = fitY ? fitCellHeight : cellSize.y;

        for(int i = 0; i < rectChildren.Count; i++)
        {
            int rowPosition = cellPriority == CellPriority.FixedColumns || cellPriority == CellPriority.Width ? i / columns : i % rows;
            int columnPosition = cellPriority == CellPriority.FixedColumns || cellPriority == CellPriority.Width ? i % columns : i / rows;

            RectTransform item = rectChildren[i];
            float xPos = xPosByChildAlignment(cellSize.x,columnPosition);
            float yPos = yPosByChildAlignment(cellSize.y,rowPosition);

            SetChildAlongAxis(item, 0, xPos, cellSize.x);
            SetChildAlongAxis(item, 1, yPos, cellSize.y);
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
        bool isOutOfUIBounds = areCellsGoingToBeOutsideOfUIBounds(Vector2Axis.X);
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
        bool isOutOfUIBounds = areCellsGoingToBeOutsideOfUIBounds(Vector2Axis.Y);
        if (isOutOfUIBounds || fitY || childAlignment == TextAnchor.UpperLeft || childAlignment == TextAnchor.UpperCenter || childAlignment == TextAnchor.UpperRight)
        {
            return (cellHeight * rowPosition) + (spacing.y * rowPosition) + padding.top;
        } else if (childAlignment == TextAnchor.MiddleLeft || childAlignment == TextAnchor.MiddleCenter || childAlignment == TextAnchor.MiddleRight)
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
    /// <summary>
    /// Checks if at least 1 cell will go out of bounds(minus padding) of the parent
    /// </summary>
    /// <param name="axis">The axis in vector2 we want to check</param>
    /// <returns>Returns if at least 1 cell is out of bounds(minus padding) in the specific axis</returns>
    private bool areCellsGoingToBeOutsideOfUIBounds(Vector2Axis axis)
    {
        if (axis == Vector2Axis.X)
        {
            return totalWidthOfCells > realWidthAfterPadding;
        } else if (axis == Vector2Axis.Y)
        {
            return totalHeightOfCells > realHeightAfterPadding;
        }
        throw new System.NotImplementedException("Axis not supported");
    }
    /// <summary>
    /// Represents and axis in vector2
    /// </summary>
    private enum Vector2Axis
    {
        X,
        Y
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
}
