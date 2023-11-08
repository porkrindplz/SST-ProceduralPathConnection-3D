using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ConnectorSize { Standard, Wide, };
/// <summary>
/// This script creates connecting points for the tiles to connect to each other.
/// This is for the purpose of precedural map generation.
/// It also draws the gizmos for the connecting points.
/// The connecting points are used to determine if a tile can be placed in a certain location.
/// </summary>
public class Connector : MonoBehaviour
{
    Vector2 size = Vector2.one * 4f;
    public ConnectorSize connectorSize = ConnectorSize.Standard;
    public bool isConnected = false;

    [SerializeField] bool beginningConnector = false;
    public bool IsBeginningConnector => beginningConnector;

    public Vector2 standardSize = Vector2.one * 4f;
    public Vector2 wideSize = new Vector2(4, 20);
    public float midPointLength = .5f;

    bool isPlaying;

    private void Start()
    {
        isPlaying = true;
    }
    void OnDrawGizmos()
    {
        if (connectorSize == ConnectorSize.Wide)
        {
            size = wideSize * transform.parent.localScale.x;
        }
        else
        {
            size = standardSize * transform.parent.localScale.x;
        }
        Gizmos.color = isConnected ? Color.green : Color.red;
        if (!isPlaying) Gizmos.color = Color.cyan;
        Vector2 halfSize = size * 0.5f;
        Vector3 offset = transform.position + transform.up * halfSize.y;
        Gizmos.DrawLine(offset, offset + transform.forward * midPointLength);

        //define top & side vectors
        Vector3 top = transform.up * size.y;
        Vector3 side = transform.right * halfSize.x;

        //define corner vectors
        Vector3 topRight = transform.position + top + side;
        Vector3 topLeft = transform.position + top - side;
        Vector3 botRight = transform.position + side;
        Vector3 botLeft = transform.position - side;

        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, botLeft);
        Gizmos.DrawLine(botLeft, botRight);
        Gizmos.DrawLine(botRight, topRight);

        //draw diagonal lines
        Gizmos.color *= 0.5f;
        Gizmos.DrawLine(topRight, offset);
        Gizmos.DrawLine(topLeft, offset);
        Gizmos.DrawLine(botLeft, offset);
        Gizmos.DrawLine(botRight, offset);

    }
}
