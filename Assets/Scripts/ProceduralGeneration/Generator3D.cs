using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;

public class Generator3D : MonoBehaviour
{
    [Tooltip("The number of cells in the x dimension of the grid.")]
    public int xDimension;
    [Tooltip("The number of cells in the y dimension of the grid.")]
    public int yDimension;
    [Tooltip("The number of cells in the z dimension of the grid.")]
    public int zDimension;
    [Tooltip("The minimum number of splines to generate.")]
    public int minSplines;
    [Tooltip("The maximum number of splines to generate.")]
    public int maxSplines;
    [Tooltip("An array of all the tile objects that can be used to generate the grid.")]

    [Separator(0.5f, 5)]
    public Tile3D[] tileObjects;
    [Tooltip("The tile object that represents the start of the path.")]
    public Tile3D startTile;
    [Tooltip("The location of the start tile on the grid.")]
    public Vector2Int startLocation;
    [Tooltip("The tile object that represents the end of the path.")]
    public Tile3D endTile;
    [Tooltip("The location of the end tile on the grid.")]
    public Vector2Int endLocation;
    [Tooltip("The list of cells that make up the grid.")]
    [Separator(0.5f, 5)]
    public List<Cell3D> cellGrid;
    [Tooltip("The default cell configuration used to generate new cells.")]
    public Cell3D cellPrefab;
    [Tooltip("An array of splines that connect the start and end tiles.")]
    public SplineContainer[] splines;

    Coroutine generationCoroutine;
    Coroutine generateSplinesCoroutine;
    Coroutine drawCoroutine;
    Dictionary<Cell3D, List<BezierKnot>> cellMidPoints = new();
    Dictionary<Cell3D, List<BezierKnot>> cellPathPoints = new();
    GameObject splineParent;
    WaitForSeconds wait = new WaitForSeconds(0);
    int analyzeCount;
    Cell3D lastCollapsed;


    private void Start()
    {
        //GenerateGrid();//Generate grid of cells
        StartCoroutine(GenerationProcess());
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(GenerationProcess());
        }
    }
    IEnumerator GenerationProcess()
    {
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
        }
        yield return generationCoroutine = StartCoroutine(GenerateGridNew());

        if (generateSplinesCoroutine != null)
        {
            StopCoroutine(generateSplinesCoroutine);
        }
        yield return generateSplinesCoroutine = StartCoroutine(GenerateSplinesNew());
        if (drawCoroutine != null)
        {
            StopCoroutine(drawCoroutine);
        }
        yield return drawCoroutine = StartCoroutine(Draw());
    }

    IEnumerator GenerateSplinesNew()
    {
        if (splineParent != null) Destroy(splineParent);
        splines = new SplineContainer[UnityEngine.Random.Range(minSplines, maxSplines + 1)];
        splineParent = new GameObject("Spline Parent");
        for (int i = 0; i < splines.Length; i++)//Create splines that attach to Start and End tiles
        {
            //using the start and end locations, create a spline that connects them with points in between that line up with the grid's z axis
            GameObject splineObject = new GameObject("Spline " + i);
            splines[i] = splineObject.AddComponent<SplineContainer>();
            splineObject.transform.parent = splineParent.transform;

            BezierKnot startKnot = new BezierKnot();//Create start knot
            BezierKnot midKnot = new BezierKnot();//Create a knot that indicates the previous knot's direction
            BezierKnot endKnot = new BezierKnot();//Create end knot
            startKnot.Position = new Vector3(startLocation.x, 0, startLocation.y);

            splines[i].Spline.Add(startKnot);//Add start knot to spline
            splines[i].Spline.SetTangentMode(0, TangentMode.AutoSmooth, BezierTangent.Out);

            int numPoints = endLocation.y - startLocation.y;//Number of points between start and end knots
            int xLocation = startLocation.x;//xLocation of most recently added knot

            for (int j = 1; j < numPoints; j++)
            {
                Vector3 intersectionPoint = new Vector3(xLocation, 0, startLocation.y + j);//Point on grid that intersects with spline
                BezierKnot knot = new BezierKnot();//

                int tempX = xLocation + UnityEngine.Random.Range(-1, 2);
                int whileBreaker = 0;
                //while the x location is out of bounds or the x location is too far from the end location, keep generating a new x location
                while (tempX >= xDimension - 1 || tempX < 0 || Mathf.Abs(endLocation.x - tempX) >= Mathf.Abs(endLocation.y - intersectionPoint.z))
                {
                    whileBreaker++;
                    if (whileBreaker > 100) break;
                    tempX = xLocation + UnityEngine.Random.Range(-1, 2);
                }
                knot.Position = new Vector3(tempX, 0, intersectionPoint.z);

                //if the x location is different from the previous x location, add a midknot and increase the cell's left or right points
                //if the x location is the same as the previous x location, increase the cell's back points
                float pointDiff = xLocation - knot.Position.x;
                Cell3D currentCell = cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.z)];

                if (pointDiff == 0)
                    cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.z)].backPoints++; //add back path
                else
                {
                    Cell3D midCell;
                    midKnot = new BezierKnot();

                    if (pointDiff > 0)
                    {
                        midKnot.Position = new Vector3(xLocation + (int)(pointDiff / 2), 0, intersectionPoint.z);
                        midCell = cellGrid[FindGridIndex((int)midKnot.Position.x, (int)midKnot.Position.z)];
                        midCell.IncreasePoints(Cell3D.Direction.Back);
                        midCell.IncreasePoints(Cell3D.Direction.Left);
                        currentCell.IncreasePoints(Cell3D.Direction.Right);
                    }
                    else
                    {
                        midKnot.Position = new Vector3(xLocation - (int)(pointDiff / 2), 0, intersectionPoint.z);
                        midCell = cellGrid[FindGridIndex((int)midKnot.Position.x, (int)midKnot.Position.z)];
                        midCell.IncreasePoints(Cell3D.Direction.Back);
                        midCell.IncreasePoints(Cell3D.Direction.Right);
                        currentCell.IncreasePoints(Cell3D.Direction.Left);
                    }
                    AddToCellDictionary(midCell, midKnot, CellPoint.PathPoint);
                    splines[i].Spline.Add(midKnot);
                }

                currentCell.IncreasePoints(Cell3D.Direction.Forward);
                AddToCellDictionary(currentCell, knot, CellPoint.PathPoint);
                splines[i].Spline.Add(knot);
                splines[i].Spline.SetTangentMode(j, TangentMode.AutoSmooth, BezierTangent.Out);

                xLocation = (int)knot.Position.x;
                // Debug.Log("For Cell3D position: " + knot.Position + "Forward " + cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.z)].forwardPoints + " Back " + cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.z)].backPoints + " Left " + cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.z)].leftPoints + " Right " + cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.z)].rightPoints);
            }

            midKnot = new BezierKnot();
            midKnot.Position = new Vector3(xLocation, 0, endLocation.y);
            splines[i].Spline.Add(midKnot);


            endKnot = new BezierKnot();
            endKnot.Position = new Vector3(endLocation.x, 0, endLocation.y);
            splines[i].Spline.Add(endKnot);
        }
        for (int i = 0; i < cellGrid.Count(); i++)
        {
            cellGrid[i].AnalyzeOpeningOptions();
        }
        yield return wait;
    }

    public IEnumerator GenerateGridNew()
    {
        if (cellGrid != null)
        {
            foreach (Cell3D cell in cellGrid)
            {
                Destroy(cell.gameObject);
            }
        }
        cellGrid = new List<Cell3D>();

        if (analyzeCount < 1)
        {
            Tile3D tile;
            //create adjacency rules based on edges for each cell on the grid
            for (int i = 0; i < tileObjects.Length; i++)
            {
                tile = tileObjects[i];
                tile.UpdateSides();
            }
            for (int i = 0; i < tileObjects.Length; i++)
            {
                tile = tileObjects[i];
                tile.Analyze(tileObjects);
            }
            analyzeCount++;
        }

        int cellCounter = 0;
        //Create grid of cells
        for (int z = 0; z < zDimension; z++)
        {
            for (int x = 0; x < xDimension; x++)
            {
                //Create a cell
                Cell3D newCell = Instantiate(cellPrefab, new Vector3(x, 0, z), Quaternion.identity);
                newCell.gameObject.name = "Cell3D " + cellCounter;
                newCell.CreateCell(false, tileObjects);
                newCell.ResetPoints();
                cellGrid.Add(newCell);
                cellCounter++;
            }
        }
        yield return null;

        //Set start and end tiles
        //CollapseTargetCell(cellGrid, startTile, FindGridIndex(startLocation.x, startLocation.y));
        //CollapseTargetCell(cellGrid, endTile, FindGridIndex(endLocation.x, endLocation.y));
        //StartCoroutine(GenerateSplines());//Generate splines between start and end tiles
    }

    IEnumerator Draw()
    {
        while (true)
        {
            yield return null;

            //Pick Cell3D with least entropy

            //Sort by entropy
            List<Cell3D> tempGrid = new List<Cell3D>(cellGrid);

            tempGrid.RemoveAll(c => c.collapsed);
            tempGrid.RemoveAll(c => c.SideCount() < 1);

            if (tempGrid.Count == 0) yield break;

            tempGrid.Sort((a, b) => { return a.filteredOptions.Length - b.filteredOptions.Length; });

            if (startTile != null && endTile != null &&
                cellGrid[FindGridIndex(startLocation.x, startLocation.y)].collapsed == false &&
                cellGrid[FindGridIndex(endLocation.x, endLocation.y)].collapsed == false)
            {
                CollapseTargetCell(cellGrid, startTile, FindGridIndex(startLocation.x, startLocation.y));
                cellGrid[FindGridIndex(startLocation.x, startLocation.y)].IncreasePoints(Cell3D.Direction.Forward);
                CollapseTargetCell(cellGrid, endTile, FindGridIndex(endLocation.x, endLocation.y));
                cellGrid[FindGridIndex(endLocation.x, endLocation.y)].IncreasePoints(Cell3D.Direction.Back);
            }
            else
            {

                //Remove all cells with more options than the first cell (the one with least entropy)
                int optionLength = tempGrid[0].filteredOptions.Length;
                int stopIndex = 0;
                for (int i = 0; i < tempGrid.Count(); i++)
                {
                    if (tempGrid[i].filteredOptions.Length > optionLength)
                    {
                        stopIndex = i;
                        break;
                    }
                }
                if (stopIndex > 0) tempGrid.RemoveRange(stopIndex, tempGrid.Count - stopIndex);

                //Pick a random cell from the list
                int randCell = UnityEngine.Random.Range(0, tempGrid.Count);
                tempGrid[randCell].collapsed = true;
                //Pick a random tile from the cell's filtered options
                Tile3D chosenTile = null;
                if (tempGrid[randCell].filteredOptions != null && tempGrid[randCell].filteredOptions.Length > 0)
                {
                    chosenTile = tempGrid[randCell].filteredOptions[UnityEngine.Random.Range(0, tempGrid[randCell].filteredOptions.Length)];
                }
                if (chosenTile == null)
                {
                    Debug.Log("No tile selected");
                    yield return StartCoroutine(GenerationProcess());
                }
                tempGrid[randCell].tileOptions = new Tile3D[] { chosenTile };
                CollapseTargetCell(cellGrid, chosenTile, FindGridIndex((int)tempGrid[randCell].transform.position.x, (int)tempGrid[randCell].transform.position.z));
            }
            Cell3D[] nextGrid = new Cell3D[cellGrid.Count()];
            for (int z = 0; z < zDimension; z++)
            {
                for (int x = 0; x < xDimension; x++)
                {
                    int index = FindGridIndex(x, z);
                    Debug.Log("Index: " + index + " beginning of loop. Grid value" + x + z);
                    if (cellGrid[index].collapsed || cellGrid[index].SideCount() < 1 || Mathf.Abs(new Vector2(x, z).magnitude) - Mathf.Abs(new Vector2(lastCollapsed.transform.position.x, lastCollapsed.transform.position.z).magnitude) > 1)
                    {
                        Debug.Log("Cell3D " + index + " is collapsed");
                        nextGrid[index] = cellGrid[index];
                    }
                    else
                    {
                        List<Tile3D> options = tileObjects.ToList();

                        Debug.Log("Options Added: " + options.Count());
                        //look forward
                        if (z < zDimension - 1)
                        {
                            Cell3D forward = cellGrid[FindGridIndex(x, z + 1)];
                            Debug.Log("Cell3D forward: " + forward.name);

                            var validOptions = new List<Tile3D>();

                            if (forward.transform.position.z < zDimension)
                                foreach (Tile3D option in forward.filteredOptions)
                                {
                                    if (forward.GetOpenSides(Cell3D.Direction.Back) == cellGrid[index].GetOpenSides(Cell3D.Direction.Forward))
                                    {
                                        var valid = option.validNeighbours.back; //two is the index of the back direction of the adjacent tile
                                        validOptions.AddRange(valid);
                                    }
                                }
                            else
                            {
                                // the valid options must have blank forward side
                                validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.forward.Contains(tileObjects[0])));
                            }
                            options = CheckValid(options, validOptions);
                            Debug.Log("Look forward options: " + options.Count());
                        }

                        //look right
                        if (x < xDimension - 1)
                        {
                            Cell3D right = cellGrid[FindGridIndex(x + 1, z)];
                            Debug.Log("Cell3D right: " + right.name);
                            var validOptions = new List<Tile3D>();

                            if (right.transform.position.x < xDimension)
                                foreach (Tile3D option in right.filteredOptions)
                                {
                                    if (right.GetOpenSides(Cell3D.Direction.Left) == cellGrid[index].GetOpenSides(Cell3D.Direction.Right))
                                    {
                                        var valid = option.validNeighbours.left; //one is the index of the left direction of the adjacent tile
                                        validOptions.AddRange(valid);
                                    }
                                }
                            else
                            {
                                // the valid options must have blank right side
                                validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.right.Contains(tileObjects[0])));
                            }
                            options = CheckValid(options, validOptions);
                            Debug.Log("Look right options: " + options.Count());
                        }

                        //look back
                        if (z > 0)
                        {
                            Cell3D back = cellGrid[FindGridIndex(x, z - 1)];
                            Debug.Log("Cell3D back: " + back.name);
                            var validOptions = new List<Tile3D>();

                            if (back.transform.position.z >= 0)
                                foreach (Tile3D option in back.filteredOptions)
                                {
                                    if (back.GetOpenSides(Cell3D.Direction.Forward) == cellGrid[index].GetOpenSides(Cell3D.Direction.Back))
                                    {
                                        var valid = option.validNeighbours.forward; //one is the index of the forward direction of the adjacent tile
                                        validOptions.AddRange(valid);
                                    }
                                }
                            else
                            {
                                // the valid options must have blank back side
                                validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.back.Contains(tileObjects[0])));
                            }
                            options = CheckValid(options, validOptions);
                            Debug.Log("Look back options: " + options.Count());
                        }

                        //look left
                        if (x > 0)
                        {
                            Cell3D left = cellGrid[FindGridIndex(x - 1, z)];
                            Debug.Log("Cell3D left: " + left.name);
                            var validOptions = new List<Tile3D>();

                            if (left.transform.position.x >= 0)
                                foreach (Tile3D option in left.filteredOptions)
                                {
                                    if (left.GetOpenSides(Cell3D.Direction.Right) == cellGrid[index].GetOpenSides(Cell3D.Direction.Left))
                                    {
                                        var valid = option.validNeighbours.right; //one is the index of the right direction of the adjacent tile
                                        validOptions.AddRange(valid);
                                    }
                                }
                            else
                            {
                                // the valid options must have blank left side
                                validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.left.Contains(tileObjects[0])));
                            }
                            options = CheckValid(options, validOptions);
                            Debug.Log("Look left options: " + options.Count());
                        }

                        nextGrid[index] = cellGrid[index];
                        nextGrid[index].filteredOptions = new Tile3D[options.Count()];
                        nextGrid[index].filteredOptions = options.ToArray();
                        //nextGrid[index].collapsed = false;
                    }
                }
            }
            cellGrid = nextGrid.ToList();
        }
    }

    List<Tile3D> CheckValid(List<Tile3D> options, List<Tile3D> valid)
    {
        for (int i = options.Count - 1; i >= 0; i--)
        {
            //Valid: [Blank, I]
            //Options: [Blank,I,T,C,Y]
            //Result: Remove T,C,Y
            var currentElement = options[i];
            if (!valid.Contains(currentElement))
            {
                options.RemoveAt(i);
            }
        }
        return options;
    }

    int FindGridIndex(int x, int z)
    {
        return z * xDimension + x;
    }

    void CollapseTargetCell(List<Cell3D> tempGrid, Tile3D targetTile, int targetCellIndex)
    {
        Cell3D targetCell = tempGrid[targetCellIndex];
        targetCell.collapsed = true;
        targetCell.filteredOptions = new Tile3D[] { targetTile };
        Instantiate(targetTile, targetCell.transform.position, targetTile.transform.rotation, targetCell.transform);
        Debug.Log("Spawned " + targetTile.name + " at " + targetCell.transform.position);
        lastCollapsed = targetCell;
    }

    enum CellPoint { MidPoint, PathPoint }
    void AddToCellDictionary(Cell3D cell, BezierKnot knot, CellPoint cellPoint)
    {
        if (cellPoint == CellPoint.MidPoint)
        {
            if (cellMidPoints.ContainsKey(cell))
            {
                cellMidPoints[cell].Add(knot);
            }
            else
            {
                cellMidPoints[cell] = new List<BezierKnot>();
                cellMidPoints[cell].Add(knot);
            }
        }
        else if (cellPoint == CellPoint.PathPoint)
        {
            if (cellPathPoints.ContainsKey(cell))
            {
                cellPathPoints[cell].Add(knot);
            }
            else
            {
                cellPathPoints[cell] = new List<BezierKnot>();
                cellPathPoints[cell].Add(knot);
            }
        }
    }
}
