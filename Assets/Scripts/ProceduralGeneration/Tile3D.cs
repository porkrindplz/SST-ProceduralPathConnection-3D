using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using System.IO;



public enum Side3D { Forward = 0, Right = 1, Back = 2, Left = 3, Up = 4, Down = 5 };
public enum SideStyle3D
{
    Blank,
    Wide,
    Tall,
    Center,
    Two,
    Unknown
}
public class Sides3D
{
    private SideStyle3D forward = SideStyle3D.Blank;
    private SideStyle3D right = SideStyle3D.Blank;
    private SideStyle3D back = SideStyle3D.Blank;
    private SideStyle3D left = SideStyle3D.Blank;
    private SideStyle3D up = SideStyle3D.Blank;
    private SideStyle3D down = SideStyle3D.Blank;

    private int forwardOpenings = 0;
    private int rightOpenings = 0;
    private int backOpenings = 0;
    private int leftOpenings = 0;
    private int upOpenings = 0;
    private int downOpenings = 0;

    public SideStyle3D Forward => forward;
    public SideStyle3D Right => right;
    public SideStyle3D Back => back;
    public SideStyle3D Left => left;
    public SideStyle3D Up => up;
    public SideStyle3D Down => down;

    public int ForwardOpenings => forwardOpenings;
    public int RightOpenings => rightOpenings;
    public int BackOpenings => backOpenings;
    public int LeftOpenings => leftOpenings;
    public int UpOpenings => upOpenings;
    public int DownOpenings => downOpenings;

    public void SetSideStyle(Side3D side, SideStyle3D newStyle, int openingsCount)
    {
        switch (side)
        {
            case Side3D.Forward:
                forward = newStyle;
                forwardOpenings = openingsCount;
                break;
            case Side3D.Right:
                right = newStyle;
                rightOpenings = openingsCount;
                break;
            case Side3D.Back:
                back = newStyle;
                backOpenings = openingsCount;
                break;
            case Side3D.Left:
                left = newStyle;
                leftOpenings = openingsCount;
                break;
            case Side3D.Up:
                up = newStyle;
                upOpenings = openingsCount;
                break;
            case Side3D.Down:
                down = newStyle;
                downOpenings = openingsCount;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(side), side, null);
        }
    }
}

/// <summary>
/// This class identifies the tiles that can be a neighbour to this tile.
/// </summary>
public class Tile3D : MonoBehaviour
{
    public bool rotX90 = true;
    public bool rotX180 = true;
    public bool rotX270 = true;
    public bool rotZ90 = true;
    public bool rotZ180 = true;

    public Sides3D sides = new Sides3D();

    public SideStyle3D forward;
    public SideStyle3D right;
    public SideStyle3D back;
    public SideStyle3D left;
    public SideStyle3D up;
    public SideStyle3D down;

    [Header("Color Options")]
    Material[] materials;
    Renderer ren;

    public ValidNeighbours3D validNeighbours = new ValidNeighbours3D();

    /// <summary>
    /// This class identifies the tiles that can be a neighbour to this tile.
    /// </summary>
    public class ValidNeighbours3D
    {
        public ValidNeighbours3D()
        {
            forward = new List<Tile3D>();
            right = new List<Tile3D>();
            back = new List<Tile3D>();
            left = new List<Tile3D>();
            up = new List<Tile3D>();
            down = new List<Tile3D>();
        }

        public List<Tile3D> forward { get; }
        public List<Tile3D> right { get; }
        public List<Tile3D> back { get; }
        public List<Tile3D> left { get; }
        public List<Tile3D> up { get; }
        public List<Tile3D> down { get; }

        public void Add(Tile3D tile, Side3D side)
        {
            switch (side)
            {
                case Side3D.Forward:
                    forward.Add(tile);
                    break;
                case Side3D.Right:
                    right.Add(tile);
                    break;
                case Side3D.Back:
                    back.Add(tile);
                    break;
                case Side3D.Left:
                    left.Add(tile);
                    break;
                case Side3D.Up:
                    up.Add(tile);
                    break;
                case Side3D.Down:
                    down.Add(tile);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }
    }

    /// <summary>
    /// Analyzes the tiles to find valid neighbours.
    /// This is done by comparing the sides of this tile with the sides of the other tiles.
    /// If the sides match, the tile is added to the valid neighbours list.
    /// </summary>
    /// <param name="tiles"></param>
    public void Analyze(Tile3D[] tiles)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i].sides.Back == sides.Forward)
            {
                validNeighbours.Add(tiles[i], Side3D.Forward);
            }
            if (tiles[i].sides.Left == sides.Right)
            {
                validNeighbours.Add(tiles[i], Side3D.Right);
            }
            if (tiles[i].sides.Forward == sides.Back)
            {
                validNeighbours.Add(tiles[i], Side3D.Back);
            }
            if (tiles[i].sides.Right == sides.Left)
            {
                validNeighbours.Add(tiles[i], Side3D.Left);
            }
            if (tiles[i].sides.Up == sides.Down)
            {
                validNeighbours.Add(tiles[i], Side3D.Down);
            }
            if (tiles[i].sides.Down == sides.Up)
            {
                validNeighbours.Add(tiles[i], Side3D.Up);
            }
        }
    }

    private void Awake()
    {

    }
    public void UpdateColor(int colorIndex)
    {
        ren = GetComponentInChildren<Renderer>();
        materials = Resources.LoadAll<Material>("Materials");
        Debug.Log("Material 0: " + materials[0].name + " Material 1: " + materials[1].name + " Material 2: " + materials[2].name);
        Material currentMaterial = ren.material;
        materials = materials.Where(x => x.name != currentMaterial.name).ToArray();
        ren.material = materials[colorIndex];
    }
    public int GetSideCount(Cell3D.Direction direction)
    {
        switch (direction)
        {
            case Cell3D.Direction.Forward:
                return sides.ForwardOpenings;
            case Cell3D.Direction.Right:
                return sides.RightOpenings;
            case Cell3D.Direction.Back:
                return sides.BackOpenings;
            case Cell3D.Direction.Left:
                return sides.LeftOpenings;
            default:
                throw new ArgumentException("Invalid direction");
        }
    }

    void SetConnectorSides(Vector3 forwardDir, Vector3 upDir, Vector3 rightDir)
    {
        float extent = 2.5f;
        Connector[] connectors = GetComponentsInChildren<Connector>();

        List<Connector> forwardConnectors = new();
        List<Connector> rightConnectors = new();
        List<Connector> backConnectors = new();
        List<Connector> leftConnectors = new();
        List<Connector> upConnectors = new();
        List<Connector> downConnectors = new();

        if (forwardDir == Vector3.forward)
        {
            forwardConnectors = connectors.Where(x => x.transform.localPosition.z == extent).ToList();
            backConnectors = connectors.Where(x => x.transform.localPosition.z == -extent).ToList();
        }
        else if (forwardDir == Vector3.right)
        {
            rightConnectors = connectors.Where(x => x.transform.localPosition.z == extent).ToList();
            leftConnectors = connectors.Where(x => x.transform.localPosition.z == -extent).ToList();
        }
        else if (forwardDir == Vector3.back)
        {
            forwardConnectors = connectors.Where(x => x.transform.localPosition.z == -extent).ToList();
            backConnectors = connectors.Where(x => x.transform.localPosition.z == extent).ToList();
        }
        else if (forwardDir == Vector3.left)
        {
            rightConnectors = connectors.Where(x => x.transform.localPosition.z == -extent).ToList();
            leftConnectors = connectors.Where(x => x.transform.localPosition.z == extent).ToList();
        }
        else if (forwardDir == Vector3.up)
        {
            upConnectors = connectors.Where(x => x.transform.localPosition.z == extent).ToList();
            downConnectors = connectors.Where(x => x.transform.localPosition.z == -extent).ToList();
        }
        else if (forwardDir == Vector3.down)
        {
            upConnectors = connectors.Where(x => x.transform.localPosition.z == -extent).ToList();
            downConnectors = connectors.Where(x => x.transform.localPosition.z == extent).ToList();
        }

        if (upDir == Vector3.up)
        {
            upConnectors = connectors.Where(x => x.transform.localPosition.y == extent).ToList();
            downConnectors = connectors.Where(x => x.transform.localPosition.y == -extent).ToList();
        }
        else if (upDir == Vector3.down)
        {
            upConnectors = connectors.Where(x => x.transform.localPosition.y == -extent).ToList();
            downConnectors = connectors.Where(x => x.transform.localPosition.y == extent).ToList();
        }
        else if (upDir == Vector3.forward)
        {
            forwardConnectors = connectors.Where(x => x.transform.localPosition.y == extent).ToList();
            backConnectors = connectors.Where(x => x.transform.localPosition.y == -extent).ToList();
        }
        else if (upDir == Vector3.back)
        {
            forwardConnectors = connectors.Where(x => x.transform.localPosition.y == -extent).ToList();
            backConnectors = connectors.Where(x => x.transform.localPosition.y == extent).ToList();
        }
        else if (upDir == Vector3.right)
        {
            rightConnectors = connectors.Where(x => x.transform.localPosition.y == extent).ToList();
            leftConnectors = connectors.Where(x => x.transform.localPosition.y == -extent).ToList();
        }
        else if (upDir == Vector3.left)
        {
            rightConnectors = connectors.Where(x => x.transform.localPosition.y == -extent).ToList();
            leftConnectors = connectors.Where(x => x.transform.localPosition.y == extent).ToList();
        }

        if (rightDir == Vector3.right)
        {
            rightConnectors = connectors.Where(x => x.transform.localPosition.x == extent).ToList();
            leftConnectors = connectors.Where(x => x.transform.localPosition.x == -extent).ToList();
        }
        else if (rightDir == Vector3.left)
        {
            rightConnectors = connectors.Where(x => x.transform.localPosition.x == -extent).ToList();
            leftConnectors = connectors.Where(x => x.transform.localPosition.x == extent).ToList();
        }
        else if (rightDir == Vector3.forward)
        {
            forwardConnectors = connectors.Where(x => x.transform.localPosition.x == extent).ToList();
            backConnectors = connectors.Where(x => x.transform.localPosition.x == -extent).ToList();
        }
        else if (rightDir == Vector3.back)
        {
            forwardConnectors = connectors.Where(x => x.transform.localPosition.x == -extent).ToList();
            backConnectors = connectors.Where(x => x.transform.localPosition.x == extent).ToList();
        }
        else if (rightDir == Vector3.up)
        {
            upConnectors = connectors.Where(x => x.transform.localPosition.x == extent).ToList();
            downConnectors = connectors.Where(x => x.transform.localPosition.x == -extent).ToList();
        }
        else if (rightDir == Vector3.down)
        {
            upConnectors = connectors.Where(x => x.transform.localPosition.x == -extent).ToList();
            downConnectors = connectors.Where(x => x.transform.localPosition.x == extent).ToList();
        }

        forward = CheckSide(forwardConnectors);
        right = CheckSide(rightConnectors);
        back = CheckSide(backConnectors);
        left = CheckSide(leftConnectors);
        up = CheckSide(upConnectors);
        down = CheckSide(downConnectors);

        sides.SetSideStyle(Side3D.Forward, forward, forwardConnectors.Count());
        sides.SetSideStyle(Side3D.Right, right, rightConnectors.Count());
        sides.SetSideStyle(Side3D.Back, back, backConnectors.Count());
        sides.SetSideStyle(Side3D.Left, left, leftConnectors.Count());
        sides.SetSideStyle(Side3D.Up, up, upConnectors.Count());
        sides.SetSideStyle(Side3D.Down, down, downConnectors.Count());

    }

    public void UpdateSides()
    {
        SetConnectorSides(transform.forward, transform.up, transform.right);
    }

    SideStyle3D CheckSide(List<Connector> connectors)
    {
        if (connectors.Count() == 0)
            return SideStyle3D.Blank;
        else if (connectors.Count() == 1)
        {
            if (connectors[0].connectorSize == ConnectorSize.Wide)
            {
                if (transform.up == Vector3.right && transform.forward == Vector3.forward || transform.up == Vector3.left && transform.forward == Vector3.forward ||
                    transform.up == Vector3.forward && transform.forward == Vector3.right || transform.up == Vector3.forward && transform.forward == Vector3.left ||
                    transform.up == Vector3.right && transform.forward == Vector3.back || transform.up == Vector3.left && transform.forward == Vector3.back ||
                    transform.up == Vector3.back && transform.forward == Vector3.right || transform.up == Vector3.back && transform.forward == Vector3.left)
                {
                    return SideStyle3D.Tall;
                }
                return SideStyle3D.Wide;
            }
            else
            {
                return SideStyle3D.Center;
            }
        }
        else if (connectors.Count() == 2)
            return SideStyle3D.Two;
        else
        {
            Debug.LogError("Invalid number of forward connectors: " + connectors.Count());
            return SideStyle3D.Unknown;
        }
    }

    #region Editor
    public void GenerateTiles()
    {
        //find all objects in "Assets/Resources/Tiles3D" folder


        string[] prefabPaths = Directory.GetFiles("Assets/Resources/Tiles3D", "*.prefab");

        //if the name of the object does not contain a - add to list
        List<GameObject> validPrefabs = new List<GameObject>();
        foreach (string prefabPath in prefabPaths)
        {
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            if (!prefabName.Contains("-"))
            {
                GameObject prefab = Resources.Load<GameObject>("Tiles3D/" + prefabName);
                validPrefabs.Add(prefab);
            }
        }

        //for each object in list do the following
        foreach (GameObject prefab in validPrefabs)
        {
            Tile3D tile = prefab.GetComponent<Tile3D>();
            tile.GenerateRotatedTiles();
        }
    }
    public void GenerateRotatedTiles()
    {
        GameObject newTile;
        Tile3D tile;
        string prefabPath;
        if (rotX90)
        {
            CreateNewTile(Vector3.right, Vector3.up, "-90");

        }
        if (rotX180)
        {
            CreateNewTile(Vector3.back, Vector3.up, "-180");
        }
        if (rotX270)
        {
            CreateNewTile(Vector3.left, Vector3.up, "-270");
        }
        if (rotZ90)
        {
            CreateNewTile(Vector3.forward, Vector3.right, "-Z90");

            CreateNewTile(Vector3.right, Vector3.back, "-Z90X90");

            CreateNewTile(Vector3.back, Vector3.left, "-Z90X180");

            CreateNewTile(Vector3.left, Vector3.forward, "-Z90X270");

            CreateNewTile(Vector3.forward, Vector3.left, "-Z270");

            CreateNewTile(Vector3.right, Vector3.forward, "-Z270X90");

            CreateNewTile(Vector3.back, Vector3.right, "-Z90X180");

            CreateNewTile(Vector3.left, Vector3.back, "-Z270X270");

        }
        if (rotZ180)
        {
            CreateNewTile(Vector3.forward, Vector3.down, "-Z180");

            if (rotX180)
            {
                CreateNewTile(Vector3.back, Vector3.down, "-ZX180");
            }
        }
    }

    void CreateNewTile(Vector3 forwardDir, Vector3 upDir, string suffix)
    {
        GameObject newTile = Instantiate(gameObject, transform.position, Quaternion.LookRotation(forwardDir, upDir));
        newTile.name = name + suffix;
        Tile3D tile = newTile.GetComponent<Tile3D>();

        string prefabPath = "Assets/Resources/Tiles3D/" + newTile.name + ".prefab";
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(newTile, prefabPath);
        DestroyImmediate(newTile);
    }
    // public void RotateScannedColors(RotationType3D rotation)
    // {
    //     string[] newForward = new string[18];
    //     string[] newRight = new string[18];
    //     string[] newBack = new string[18];
    //     string[] newLeft = new string[18];
    //     string[] newUp = new string[18];
    //     string[] newDown = new string[18];

    //     switch (rotation)
    //     {
    //         case RotationType3D.Rot90:
    //             for (int i = 0; i < 18; i++)
    //             {
    //                 newRight[i] = forward[i];
    //                 newBack[i] = right[i];
    //                 newLeft[i] = back[i];
    //                 newForward[i] = left[i];
    //             }
    //             Array.Reverse(newBack);
    //             Array.Reverse(newForward);
    //             break;
    //         case RotationType3D.Rot180:
    //             for (int i = 0; i < 18; i++)
    //             {
    //                 newBack[i] = forward[i];
    //                 newLeft[i] = right[i];
    //                 newForward[i] = back[i];
    //                 newRight[i] = left[i];
    //             }
    //             Array.Reverse(newBack);
    //             Array.Reverse(newLeft);
    //             Array.Reverse(newForward);
    //             Array.Reverse(newRight);
    //             break;
    //         case RotationType3D.Rot270:
    //             for (int i = 0; i < 18; i++)
    //             {
    //                 newLeft[i] = forward[i];
    //                 newForward[i] = right[i];
    //                 newRight[i] = back[i];
    //                 newBack[i] = left[i];
    //             }
    //             Array.Reverse(newLeft);
    //             Array.Reverse(newRight);
    //             break;
    //         case RotationType3D.RotZ180:
    //             for (int i = 0; i < 18; i++)
    //             {
    //                 newUp[i] = forward[i];
    //                 newRight[i] = right[i];
    //                 newDown[i] = back[i];
    //                 newLeft[i] = left[i];
    //             }
    //             Array.Reverse(newUp);
    //             Array.Reverse(newRight);
    //             Array.Reverse(newDown);
    //             Array.Reverse(newLeft);
    //             break;
    //         default:
    //             // No rotation needed
    //             return;
    //     }

    //     forward = newForward;
    //     right = newRight;
    //     back = newBack;
    //     left = newLeft;
    //     up = newUp;
    //     down = newDown;
    // }

    public Vector2Int GetCoordinates(int count, int itemsPerRow)
    {
        int x = count % itemsPerRow;
        int y = count / itemsPerRow;
        return new Vector2Int(x, y);
    }
    #endregion
}