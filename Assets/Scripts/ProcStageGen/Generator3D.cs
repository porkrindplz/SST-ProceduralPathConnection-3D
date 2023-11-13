using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class Generator3D : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] float waitTime = 0.5f;
    [SerializeField] float tileWaitTime = 0.4f;
    [SerializeField] bool waitsActive = true;
    [SerializeField] bool debugMode = false;
    [SerializeField] int allowance;
    [SerializeField] int attempts = 0;
    [SerializeField] int pathAttempts = 0;
    [SerializeField] int drawAttempts = 0;

    [Header("Grid Settings")]
    [Tooltip("The scale of the grid.")]
    public int scale = 1;

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
    public Vector3Int startLocation;
    [Tooltip("The tile object that represents the end of the path.")]
    public Tile3D endTile;
    [Tooltip("The location of the end tile on the grid.")]
    public Vector3Int endLocation;
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
    WaitForSeconds wait;
    WaitForSeconds tileWait;
    int analyzeCount;
    Cell3D lastCollapsed;
    BezierKnot startKnot;
    BezierKnot midKnot;
    BezierKnot endKnot;
    BezierKnot knot;
    Vector3 intersectionPoint;


    private void Awake()
    {
        wait = new WaitForSeconds(waitTime);
        tileWait = new WaitForSeconds(tileWaitTime);
        startLocation *= scale;
        endLocation *= scale;
    }
    private void Start()
    {
        StartCoroutine(GenerationProcess());
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            attempts = 0;
            pathAttempts = 0;
            drawAttempts = 0;
            StartCoroutine(GenerationProcess());
        }
    }
    IEnumerator GenerationProcess()
    {
        attempts++;
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
        }
        yield return generationCoroutine = StartCoroutine(GenerateGridNew());

        if (generateSplinesCoroutine != null)
        {
            StopCoroutine(generateSplinesCoroutine);
        }
        pathAttempts++;
        yield return generateSplinesCoroutine = StartCoroutine(GenerateSplinesNew());
        if (drawCoroutine != null)
        {
            StopCoroutine(drawCoroutine);
        }
        drawAttempts++;
        Debug.Log("Draw Attempts# " + drawAttempts);
        yield return drawCoroutine = StartCoroutine(Draw());
        Debug.Log("Complete!");
        Debug.Log("Attempts: " + attempts);
        Debug.Log("Path Attempts: " + pathAttempts);
        Debug.Log("Draw Attempts: " + drawAttempts);
    }

    IEnumerator GenerateSplinesNew()
    {
        if (splineParent != null) Destroy(splineParent);
        splines = new SplineContainer[UnityEngine.Random.Range(minSplines, maxSplines + 1)];
        splineParent = new GameObject("Spline Parent");
        for (int i = 0; i < splines.Length; i++)//Create splines that attach to Start and End tiles
        {
            Debug.Log("Starting Spline " + i);
            if (waitsActive)
                yield return new WaitForSeconds(waitTime);

            GameObject splineObject = new GameObject("Spline " + i);
            splines[i] = splineObject.AddComponent<SplineContainer>();
            splineObject.transform.parent = splineParent.transform;

            startKnot = new BezierKnot();
            midKnot = new BezierKnot();
            endKnot = new BezierKnot();
            startKnot.Position = new float3(startLocation.x, startLocation.y, startLocation.z);

            splines[i].Spline.Add(startKnot);
            splines[i].Spline.SetTangentMode(0, TangentMode.AutoSmooth, BezierTangent.Out);

            int zDist = endLocation.z - startLocation.z;
            int xLocation = startLocation.x;
            int yLocation = startLocation.y;
            int xDist;
            int yDist;
            int tempX;
            int tempY;
            int closestI;
            int closestJ;
            int neighborI;
            int neighborJ;
            int whileBreaker;
            Cell3D currentCell;
            Cell3D midCell;

            for (int j = 1; j < zDist / scale; j++)
            {
                if (waitsActive)
                    yield return new WaitForSeconds(waitTime);
                intersectionPoint = new Vector3(xLocation, yLocation, startLocation.z + (j * scale));
                knot = new BezierKnot();

                xDist = 0;
                yDist = 0;
                tempX = xLocation;
                tempY = yLocation;

                if (j * scale > zDist / 2)
                {
                    float[,] distances = new float[xDimension, yDimension];
                    for (int k = 0; k < xDimension; k++)
                    {
                        for (int l = 0; l < yDimension; l++)
                        {
                            distances[k, l] = Vector2.Distance(new Vector2(k, l) * scale, new Vector2(endLocation.x, endLocation.y));
                        }
                    }
                    float minDistanceToEnd = float.MaxValue;
                    closestI = xLocation;
                    closestJ = yLocation;

                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (x == 0 && y != 0 || y == 0 && x != 0)
                            {
                                neighborI = xLocation + (x * scale);
                                neighborJ = yLocation + (y * scale);

                                Debug.Log("Neighbor: " + neighborI + " " + neighborJ);
                                Debug.Log("xDimention * scale: " + xDimension * scale);
                                Debug.Log("yDimention * scale: " + yDimension * scale);
                                Debug.Log("xLoc/scale: " + xLocation / scale + " yLoc/scale: " + yLocation / scale);
                                Debug.Log("NeighborI/scale: " + neighborI / scale + " NeighborJ/scale: " + neighborJ / scale);

                                if (neighborI / scale >= 0 && neighborI / scale < xDimension &&
                                        neighborJ / scale >= 0 && neighborJ / scale < yDimension &&
                                        distances[xLocation / scale, yLocation / scale] > distances[neighborI / scale, neighborJ / scale])
                                {
                                    Debug.Log("Triggered IF");
                                    float distanceToEnd = Vector2.Distance(new Vector2(neighborI, neighborJ), new Vector2(endLocation.x, endLocation.y));

                                    if (distanceToEnd < minDistanceToEnd)
                                    {
                                        minDistanceToEnd = distanceToEnd;
                                        closestI = (int)neighborI;
                                        closestJ = (int)neighborJ;
                                    }
                                }
                            }
                        }
                    }
                    tempX = closestI;
                    tempY = closestJ;
                    xDist = tempX - xLocation;
                    yDist = tempY - yLocation;
                    knot.Position = new Vector3(tempX, tempY, intersectionPoint.z);
                    currentCell = cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.y, (int)knot.Position.z, true)];
                    if (xDist == 0 && yDist == 0)
                    {
                        currentCell.IncreasePoints(Cell3D.Direction.Back);
                    }
                    else
                    {
                        midKnot = new BezierKnot
                        {
                            Position = new Vector3(xLocation + Mathf.RoundToInt((xDist / 2) / scale) * scale, yLocation + Mathf.RoundToInt((yDist / 2) / scale) * scale, intersectionPoint.z)
                        };
                        midCell = cellGrid[FindGridIndex((int)midKnot.Position.x, (int)midKnot.Position.y, (int)midKnot.Position.z, true)];
                        midCell.IncreasePoints(Cell3D.Direction.Back);
                        if (xDist > 0)
                        {
                            midCell.IncreasePoints(Cell3D.Direction.Right);
                            currentCell.IncreasePoints(Cell3D.Direction.Left);
                            cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.y, (int)knot.Position.z, true)].IncreasePoints(Cell3D.Direction.Left);
                        }
                        else if (xDist < 0)
                        {
                            midCell.IncreasePoints(Cell3D.Direction.Left);
                            currentCell.IncreasePoints(Cell3D.Direction.Right);
                            cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.y, (int)knot.Position.z, true)].IncreasePoints(Cell3D.Direction.Right);
                        }
                        else if (yDist > 0)
                        {
                            midCell.IncreasePoints(Cell3D.Direction.Up);
                            currentCell.IncreasePoints(Cell3D.Direction.Down);
                            cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.y, (int)knot.Position.z, true)].IncreasePoints(Cell3D.Direction.Down);
                        }
                        else if (yDist < 0)
                        {
                            midCell.IncreasePoints(Cell3D.Direction.Down);
                            currentCell.IncreasePoints(Cell3D.Direction.Up);
                            cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.y, (int)knot.Position.z, true)].IncreasePoints(Cell3D.Direction.Up);
                        }

                        AddToCellDictionary(midCell, midKnot, CellPoint.PathPoint);
                        splines[i].Spline.Add(midKnot);
                        currentCell.IncreasePoints(Cell3D.Direction.Forward);
                        AddToCellDictionary(currentCell, knot, CellPoint.PathPoint);
                    }
                }

                else
                {
                    int rndXY = UnityEngine.Random.Range(0, 2);//Randomly choose whether to move in the x or y direction
                    whileBreaker = 0;

                    if (rndXY == 0)
                    {
                        tempX = xLocation + (UnityEngine.Random.Range(-1, 2) * scale);
                        while (tempX / scale >= xDimension || tempX / scale < 0 || Mathf.Abs(endLocation.x - tempX) >= Mathf.Abs(endLocation.z - intersectionPoint.z))
                        {
                            whileBreaker++;
                            if (whileBreaker > 100) break;

                            tempX = xLocation + (UnityEngine.Random.Range(-1, 2) * scale);
                        }
                    }
                    else
                    {
                        tempY = yLocation + (UnityEngine.Random.Range(-1, 2) * scale);
                        while (tempY / scale >= yDimension || tempY / scale < 0 || Mathf.Abs(endLocation.y - tempY) >= Mathf.Abs(endLocation.z - intersectionPoint.z))
                        {
                            whileBreaker++;
                            if (whileBreaker > 100) break;

                            tempY = yLocation + (UnityEngine.Random.Range(-1, 2) * scale);
                        }
                    }

                    knot.Position = new Vector3(tempX, tempY, intersectionPoint.z);

                    //if the x location is different from the previous x location, add a midknot and increase the cell's left or right points
                    //if the x location is the same as the previous x location, increase the cell's back points
                    float pointDiffX = xLocation - knot.Position.x;
                    float pointDiffY = yLocation - knot.Position.y;
                    currentCell = cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.y, (int)knot.Position.z, true)];

                    if (pointDiffX == 0 && pointDiffY == 0)
                        cellGrid[FindGridIndex((int)knot.Position.x, (int)knot.Position.y, (int)knot.Position.z, true)].IncreasePoints(Cell3D.Direction.Back); //add back path
                    else if (pointDiffX != 0)
                    {
                        midKnot = new BezierKnot();
                        Debug.LogWarning("DiffX");
                        midKnot.Position = new Vector3(xLocation + Mathf.RoundToInt((pointDiffX / 2) / scale) * scale, yLocation, intersectionPoint.z);
                        midCell = cellGrid[FindGridIndex((int)midKnot.Position.x, (int)midKnot.Position.y, (int)midKnot.Position.z, true)];
                        midCell.IncreasePoints(Cell3D.Direction.Back);
                        if (pointDiffX > 0)
                        {
                            midCell.IncreasePoints(Cell3D.Direction.Left);
                            currentCell.IncreasePoints(Cell3D.Direction.Right);
                        }
                        else
                        {
                            midCell.IncreasePoints(Cell3D.Direction.Right);
                            currentCell.IncreasePoints(Cell3D.Direction.Left);
                        }
                        AddToCellDictionary(midCell, midKnot, CellPoint.PathPoint);
                        splines[i].Spline.Add(midKnot);
                    }
                    else if (pointDiffY != 0)
                    {
                        midKnot = new BezierKnot();
                        Debug.LogWarning("DiffY");
                        midKnot.Position = new Vector3(xLocation, yLocation + Mathf.RoundToInt((pointDiffY / 2) / scale) * scale, intersectionPoint.z);
                        midCell = cellGrid[FindGridIndex((int)midKnot.Position.x, (int)midKnot.Position.y, (int)midKnot.Position.z, true)];
                        midCell.IncreasePoints(Cell3D.Direction.Back);
                        if (pointDiffY > 0)
                        {
                            midCell.IncreasePoints(Cell3D.Direction.Down);
                            currentCell.IncreasePoints(Cell3D.Direction.Up);
                        }
                        else
                        {
                            midCell.IncreasePoints(Cell3D.Direction.Up);
                            currentCell.IncreasePoints(Cell3D.Direction.Down);
                        }
                        AddToCellDictionary(midCell, midKnot, CellPoint.PathPoint);
                        splines[i].Spline.Add(midKnot);
                    }

                    currentCell.IncreasePoints(Cell3D.Direction.Forward);
                    AddToCellDictionary(currentCell, knot, CellPoint.PathPoint);

                }
                splines[i].Spline.Add(knot);
                splines[i].Spline.SetTangentMode(j, TangentMode.AutoSmooth, BezierTangent.Out);

                xLocation = (int)knot.Position.x;
                yLocation = (int)knot.Position.y;
            }
            if (waitsActive)
                yield return new WaitForSeconds(waitTime);
            midKnot = new BezierKnot
            {
                Position = new Vector3(xLocation, yLocation, endLocation.z)
            };
            splines[i].Spline.Add(midKnot);

            if (waitsActive)
                yield return new WaitForSeconds(waitTime);
            endKnot = new BezierKnot
            {
                Position = new Vector3(endLocation.x, endLocation.y, endLocation.z)
            };
            splines[i].Spline.Add(endKnot);
        }
        for (int i = 0; i < cellGrid.Count(); i++)
        {
            cellGrid[i].AnalyzeOpeningOptions();
        }
        yield return null;
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
        Cell3D newCell;
        //Create grid of cells
        for (int z = 0; z < zDimension; z++)
        {
            for (int y = 0; y < yDimension; y++)
            {
                for (int x = 0; x < xDimension; x++)
                {
                    //Create a cell
                    newCell = Instantiate(cellPrefab, new Vector3(x, y, z) * scale, Quaternion.identity);
                    newCell.gameObject.name = "Cell3D " + cellCounter;
                    newCell.CreateCell(false, tileObjects);
                    newCell.ResetPoints();
                    cellGrid.Add(newCell);
                    cellCounter++;
                }
            }
        }
        yield return null;
    }

    IEnumerator Draw()
    {
        List<Cell3D> tempGrid;
        Cell3D[] nextGrid;
        Cell3D selectedCell;
        Cell3D forward, right, back, left, up, down;
        List<Tile3D> options;
        Tile3D chosenTile;
        int optionLength;
        int stopIndex;
        int randCell;
        int index;
        string historicalDataPath = "Assets/Resources/Cell3D Data.txt";
        string cellData;

        while (true)
        {
            if (waitsActive)
                yield return tileWait;

            tempGrid = new List<Cell3D>(cellGrid);

            tempGrid.RemoveAll(c => c.collapsed);
            tempGrid.RemoveAll(c => c.SideCount() < 1);

            if (tempGrid.Count == 0) yield break;

            tempGrid.Sort((a, b) => { return a.filteredOptions.Length - b.filteredOptions.Length; });

            if (startTile != null && endTile != null &&
                cellGrid[FindGridIndex(startLocation.x, startLocation.y, startLocation.z, true)].collapsed == false &&
                cellGrid[FindGridIndex(endLocation.x, endLocation.y, endLocation.z, true)].collapsed == false)
            {
                CollapseTargetCell(cellGrid, startTile, FindGridIndex(startLocation.x, startLocation.y, startLocation.z, true));
                cellGrid[FindGridIndex(startLocation.x, startLocation.y, startLocation.z, true)].IncreasePoints(Cell3D.Direction.Forward);
                CollapseTargetCell(cellGrid, endTile, FindGridIndex(endLocation.x, endLocation.y, endLocation.z, true));
                cellGrid[FindGridIndex(endLocation.x, endLocation.y, endLocation.z, true)].IncreasePoints(Cell3D.Direction.Back);
            }
            else
            {
                //Remove all cells with more options than the first cell (the one with least entropy)
                optionLength = tempGrid[0].filteredOptions.Length;
                stopIndex = 0;
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
                randCell = UnityEngine.Random.Range(0, tempGrid.Count);
                selectedCell = tempGrid[randCell];
                selectedCell.collapsed = true;
                //Pick a random tile from the cell's filtered options
                chosenTile = null;
                if (selectedCell.filteredOptions != null && selectedCell.filteredOptions.Length > 0)
                {
                    chosenTile = selectedCell.filteredOptions[UnityEngine.Random.Range(0, selectedCell.filteredOptions.Length)];
                }
                if (chosenTile == null)
                {
                    Debug.Log("No tile found - Restarting Process");
                    cellData = "Attempts: " + attempts + "\n" + "Cell: " + selectedCell.name + "\n" +
                                        "Grid size: " + xDimension + "x" + yDimension + "y" + zDimension + "z" + "\n" +
                                        "Paths: " + splines.Length + "\n" +
                                        "Coordinates: " + selectedCell.transform.position + "\n" +
                                        "Options: " + selectedCell.filteredOptions.Length + "\n" +
                                        "Points: " + "\n" +
                                        "Forward: " + selectedCell.forwardPoints + " Back: " + selectedCell.backPoints +
                                        " Left: " + selectedCell.leftPoints + " Right: " + selectedCell.rightPoints +
                                        " Up: " + selectedCell.upPoints + " Down: " + selectedCell.downPoints + "\n" +
                                        "--------------------------------------------------" + "\n";
                    System.IO.File.AppendAllText(historicalDataPath, cellData);
                    if (debugMode)
                        while (!Input.GetKeyDown(KeyCode.P))
                        {
                            yield return null;
                        }
                    yield return StartCoroutine(GenerationProcess());
                }
                tempGrid[randCell].tileOptions = new Tile3D[] { chosenTile };
                CollapseTargetCell(cellGrid, chosenTile, FindGridIndex((int)tempGrid[randCell].transform.position.x, (int)tempGrid[randCell].transform.position.y, (int)tempGrid[randCell].transform.position.z, true));
            }
            nextGrid = new Cell3D[cellGrid.Count()];
            for (int z = 0; z < zDimension; z++)
            {
                for (int y = 0; y < yDimension; y++)
                {
                    for (int x = 0; x < xDimension; x++)
                    {
                        index = FindGridIndex(x, y, z);

                        Vector3 position = new Vector3(x, y, z) * scale;

                        if (cellGrid[index].collapsed || cellGrid[index].SideCount() < 1 || Mathf.Abs(position.magnitude) - Mathf.Abs(new Vector3(lastCollapsed.transform.position.x, lastCollapsed.transform.position.y, lastCollapsed.transform.position.z).magnitude) > scale)
                        {
                            nextGrid[index] = cellGrid[index];
                        }
                        else
                        {
                            options = tileObjects.ToList();

                            if (z < zDimension)
                            {
                                forward = cellGrid[FindGridIndex(x, y, z + 1)];
                                var validOptions = new List<Tile3D>();

                                if (forward.transform.position.z < zDimension * scale)
                                    foreach (Tile3D option in forward.filteredOptions)
                                    {
                                        if (forward.GetOpenSides(Cell3D.Direction.Back) == cellGrid[index].GetOpenSides(Cell3D.Direction.Forward))
                                        {
                                            var valid = option.validNeighbours.back;
                                            validOptions.AddRange(valid);
                                        }
                                    }
                                else
                                {
                                    validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.forward.Contains(tileObjects[0])));
                                }
                                options = CheckValid(options, validOptions);
                            }

                            if (x < xDimension)
                            {
                                right = cellGrid[FindGridIndex(x + 1, y, z)];
                                var validOptions = new List<Tile3D>();

                                if (right.transform.position.x < xDimension * scale)
                                    foreach (Tile3D option in right.filteredOptions)
                                    {
                                        if (right.GetOpenSides(Cell3D.Direction.Left) == cellGrid[index].GetOpenSides(Cell3D.Direction.Right))
                                        {
                                            var valid = option.validNeighbours.left;
                                            validOptions.AddRange(valid);
                                        }
                                    }
                                else
                                {
                                    validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.right.Contains(tileObjects[0])));
                                }
                                options = CheckValid(options, validOptions);
                            }

                            if (z > -1)
                            {
                                back = cellGrid[FindGridIndex(x, y, z - 1)];
                                var validOptions = new List<Tile3D>();

                                if (back.transform.position.z >= 0)
                                    foreach (Tile3D option in back.filteredOptions)
                                    {
                                        if (back.GetOpenSides(Cell3D.Direction.Forward) == cellGrid[index].GetOpenSides(Cell3D.Direction.Back))
                                        {
                                            var valid = option.validNeighbours.forward;
                                            validOptions.AddRange(valid);
                                        }
                                    }
                                else
                                {
                                    validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.back.Contains(tileObjects[0])));
                                }
                                options = CheckValid(options, validOptions);
                            }

                            if (x > -1)
                            {
                                left = cellGrid[FindGridIndex(x - 1, y, z)];
                                var validOptions = new List<Tile3D>();

                                if (left.transform.position.x >= 0)
                                    foreach (Tile3D option in left.filteredOptions)
                                    {
                                        if (left.GetOpenSides(Cell3D.Direction.Right) == cellGrid[index].GetOpenSides(Cell3D.Direction.Left))
                                        {
                                            var valid = option.validNeighbours.right;
                                            validOptions.AddRange(valid);
                                        }
                                    }
                                else
                                {
                                    validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.left.Contains(tileObjects[0])));
                                }
                                options = CheckValid(options, validOptions);
                            }

                            if (y < yDimension)
                            {
                                up = cellGrid[FindGridIndex(x, y + 1, z)];
                                var validOptions = new List<Tile3D>();

                                if (up.transform.position.y < yDimension * scale)
                                    foreach (Tile3D option in up.filteredOptions)
                                    {
                                        if (up.GetOpenSides(Cell3D.Direction.Down) == cellGrid[index].GetOpenSides(Cell3D.Direction.Up))
                                        {
                                            var valid = option.validNeighbours.down;
                                            validOptions.AddRange(valid);
                                        }
                                    }
                                else
                                {
                                    validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.up.Contains(tileObjects[0])));
                                }
                                options = CheckValid(options, validOptions);
                            }

                            if (y > -1)
                            {
                                down = cellGrid[FindGridIndex(x, y - 1, z)];
                                var validOptions = new List<Tile3D>();

                                if (down.transform.position.y >= 0)
                                    foreach (Tile3D option in down.filteredOptions)
                                    {
                                        if (down.GetOpenSides(Cell3D.Direction.Up) == cellGrid[index].GetOpenSides(Cell3D.Direction.Down))
                                        {
                                            var valid = option.validNeighbours.up;
                                            validOptions.AddRange(valid);
                                        }
                                    }
                                else
                                {
                                    validOptions.AddRange(cellGrid[index].filteredOptions.Where(t => t.validNeighbours.down.Contains(tileObjects[0])));
                                }
                                options = CheckValid(options, validOptions);
                            }

                            nextGrid[index] = cellGrid[index];
                            nextGrid[index].filteredOptions = new Tile3D[options.Count()];
                            nextGrid[index].filteredOptions = options.ToArray();
                        }
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
            var currentElement = options[i];
            if (!valid.Contains(currentElement))
            {
                options.RemoveAt(i);
            }
        }
        return options;
    }

    int FindGridIndex(int x, int y, int z, bool scalingNeeded = false)
    {
        Debug.Log("Finding index of " + x + " " + y + " " + z);
        Debug.Log("Scaling Needed: " + scalingNeeded);
        if (scalingNeeded) Debug.Log(z / scale * xDimension * yDimension + y / scale * xDimension + x / scale);
        else Debug.Log(z * xDimension * yDimension + y * xDimension + x);
        if (scalingNeeded)
            return (int)(z / scale) * xDimension * yDimension + (int)(y / scale) * xDimension + (int)(x / scale);
        else
            return (z) * xDimension * yDimension + (y) * xDimension + (x);
    }

    void CollapseTargetCell(List<Cell3D> tempGrid, Tile3D targetTile, int targetCellIndex)
    {
        Cell3D targetCell = tempGrid[targetCellIndex];
        targetCell.collapsed = true;
        targetCell.filteredOptions = new Tile3D[] { targetTile };
        Tile3D tile = Instantiate(targetTile, targetCell.transform.position, targetTile.transform.rotation, targetCell.transform);
        Debug.Log("Spawned " + targetTile.name + " at " + targetCell.transform.position);
        lastCollapsed = targetCell;
        if (targetCell.transform.position.y == startLocation.y)
            tile.UpdateColor(1);
        else if (targetCell.transform.position.y > startLocation.y)
            tile.UpdateColor(2);
        else if (targetCell.transform.position.y < startLocation.y)
            tile.UpdateColor(0);
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
