using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

/// <summary>
/// This class holds a tile at a specific location in the grid.
/// </summary>
public class Cell3D : MonoBehaviour
{
    public bool collapsed;
    public Tile3D collapsedTile;
    public Tile3D[] tileOptions;
    public Tile3D[] filteredOptions;

    public int forwardPoints = 0;
    public int rightPoints = 0;
    public int backPoints = 0;
    public int leftPoints = 0;
    public int upPoints = 0;
    public int downPoints = 0;
    public int sideCount = 0;
    public enum Direction { Forward, Right, Back, Left, Up, Down };

    private void Start()
    {
        filteredOptions = tileOptions;
    }

    public void CreateCell(bool collapsedState, Tile3D[] tiles)
    {
        collapsed = collapsedState;
        tileOptions = tiles;
    }
    public void RecreateCell(Tile3D[] tiles)
    {
        tileOptions = tiles;
    }


    #region old
    /// <summary>
    /// This method filters the tile options based on the direction and the value of the tile.
    /// This is used to determine where the path(s) go to and from
    /// </summary>
    /// <param name="direction"></param>
    public void IncreasePoints(Direction direction)
    {
        sideCount++;
        switch (direction)
        {
            case Direction.Forward:
                forwardPoints++;
                break;
            case Direction.Right:
                rightPoints++;
                break;
            case Direction.Back:
                backPoints++;
                break;
            case Direction.Left:
                leftPoints++;
                break;
            case Direction.Up:
                upPoints++;
                break;
            case Direction.Down:
                downPoints++;
                break;
        }
    }
    public void AnalyzeOpeningOptions()
    {
        List<Tile3D> filteredList = new List<Tile3D>();
        for (int i = 0; i < filteredOptions.Length; i++)
        {
            Tile3D tile = filteredOptions[i];
            if ((tile.sides.ForwardOpenings > 0 && forwardPoints > 0) || (tile.sides.ForwardOpenings == 0 && forwardPoints == 0))
            {
                if ((tile.sides.RightOpenings > 0 && rightPoints > 0) || (tile.sides.RightOpenings == 0 && rightPoints == 0))
                {
                    if ((tile.sides.BackOpenings > 0 && backPoints > 0) || (tile.sides.BackOpenings == 0 && backPoints == 0))
                    {
                        if ((tile.sides.LeftOpenings > 0 && leftPoints > 0) || (tile.sides.LeftOpenings == 0 && leftPoints == 0))
                        {
                            if ((tile.sides.UpOpenings > 0 && upPoints > 0) || (tile.sides.UpOpenings == 0 && upPoints == 0))
                            {
                                if ((tile.sides.DownOpenings > 0 && downPoints > 0) || (tile.sides.DownOpenings == 0 && downPoints == 0))
                                {
                                    filteredList.Add(tile);
                                }
                            }
                        }
                    }
                }
            }
        }
        filteredOptions = filteredList.ToArray();
    }
    public void ResetPoints()
    {
        forwardPoints = 0;
        rightPoints = 0;
        backPoints = 0;
        leftPoints = 0;
        upPoints = 0;
        downPoints = 0;
    }
    public int SideCount()
    {
        int count = 0;
        if (forwardPoints > 0) count++;
        if (rightPoints > 0) count++;
        if (backPoints > 0) count++;
        if (leftPoints > 0) count++;
        if (upPoints > 0) count++;
        if (downPoints > 0) count++;
        return count;
    }
    public int GetOpenSides(Direction direction)
    {
        switch (direction)
        {
            case Direction.Forward:
                return forwardPoints > 0 ? 1 : 0;
            case Direction.Right:
                return rightPoints > 0 ? 1 : 0;
            case Direction.Back:
                return backPoints > 0 ? 1 : 0;
            case Direction.Left:
                return leftPoints > 0 ? 1 : 0;
            case Direction.Up:
                return upPoints > 0 ? 1 : 0;
            case Direction.Down:
                return downPoints > 0 ? 1 : 0;
        }
        return 0;
    }
    // public string[] GetSideValues(Direction direction)
    // {
    //     string[] valueList = new string[filteredOptions.Length];
    //     switch (direction)
    //     {
    //         case Direction.Forward:
    //             for (int i = 0; i < filteredOptions.Length; i++)
    //             {
    //                 valueList[i] = filteredOptions[i].GetSideValue(Direction.Forward);
    //             }
    //             return valueList;
    //         case Direction.Right:
    //             for (int i = 0; i < filteredOptions.Length; i++)
    //             {
    //                 valueList[i] = filteredOptions[i].GetSideValue(Direction.Right);
    //             }
    //             return valueList;
    //         case Direction.Back:
    //             for (int i = 0; i < filteredOptions.Length; i++)
    //             {
    //                 valueList[i] = filteredOptions[i].GetSideValue(Direction.Back);
    //             }
    //             return valueList;

    //         case Direction.Left:
    //             for (int i = 0; i < filteredOptions.Length; i++)
    //             {
    //                 valueList[i] = filteredOptions[i].GetSideValue(Direction.Left);
    //             }
    //             return valueList;
    //         case Direction.Up:
    //             for (int i = 0; i < filteredOptions.Length; i++)
    //             {
    //                 valueList[i] = filteredOptions[i].GetSideValue(Direction.Up);
    //             }
    //             return valueList;
    //         case Direction.Down:
    //             for (int i = 0; i < filteredOptions.Length; i++)
    //             {
    //                 valueList[i] = filteredOptions[i].GetSideValue(Direction.Down);
    //             }
    //             return valueList;
    //     }
    //     return null;
    // }
    #endregion
}
