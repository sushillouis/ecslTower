﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapDisplay : MonoBehaviour
{

    #region public variables

    public Map currentMap;

    /// <summary>
    /// How much the arrow will overlay on the grid
    /// </summary>
    /// <remarks>
    /// Default works fine
    /// </remarks>
    public float arrowOffset = 0.1f;

    public List<WavePath> WavePathList
    {
        get
        {
            return new List<WavePath>(wavePathList);
        }
    }

    /// <summary>
    /// Manatory prefab so an Arrow can be drawn and placed
    /// </summary>
    public Arrow arrowPrefab;

    public EndArea endAreaPrefab;

    /// <summary>
    /// Contains the Arrows and allows creation and deletion of the path of Arrows
    /// </summary>
    public ArrowContainer arrowContainer;

    public static bool mapLoaded = false;

    #endregion


    #region protected variables

    ///<summary>
    /// Reference to the WorldGrid object
    /// </summary>
    /// <remarks>
    /// Necessary for raycasting to Nodes
    /// </remarks>
    protected WorldGrid worldGrid;

    ///<summary>
    /// List of WavePaths recieved by the ArrowContainer or the SerializedWavePaths to be used by the Agents
    /// </summary>
    protected List<WavePath> wavePathList = new List<WavePath>();

    /// <summary>
    /// The Level that stores the current WavePaths in the inspector and the file
    /// </summary>
    private SerializableLevel currentLevel;

    #endregion

    int selectedPathIndex = -1;
    public WavePath selectedPath = null;

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        /*

        if(Input.GetKeyDown(KeyCode.L))
        {
            if (selectedPathIndex >= 0 && selectedPathIndex < wavePathList.Count)
            {
                HighLightWavePath(wavePathList[selectedPathIndex], false);
            }
            selectedPathIndex++;
            selectedPathIndex %= wavePathList.Count;
            selectedPath = wavePathList[selectedPathIndex];
            HighLightWavePath(selectedPath, true); 
        }

        */

        Node selectedNode = null;
        if(Player.localPlayer != null && Player.localPlayer.PlayerType == PlayerType.Attacker)
        {
            selectedNode = worldGrid.getRaycastNode();
        }
        
        if (selectedNode != null)
        {
            WavePath localSelectedPath = null;
            List<WavePath> selectedList = new List<WavePath>();
            foreach (WavePath currentPath in wavePathList)
            {
                if (currentPath.Contains(selectedNode))
                {
                    selectedList.Add(currentPath);
                    //localSelectedPath = currentPath;
                    //break;
                }
            }

            if (selectedList.Count > 0 && selectedList.Contains(selectedPath))
            {
                if(Input.GetKeyDown(KeyCode.Space))
                {
                    localSelectedPath = selectedList[(selectedList.IndexOf(selectedPath) + 1) % selectedList.Count];
                } else
                {
                    localSelectedPath = selectedPath;
                }
                
            }
            else if (selectedList.Count > 0)
            {
                localSelectedPath = selectedList[0];
            }

            if (localSelectedPath != null)
            {
                if (localSelectedPath != selectedPath)
                {
                    if (selectedPath != null) // if there is a hovered path and a selectedPath and they are not the same, unhighlight
                    {
                        HighLightWavePath(selectedPath, false);
                    }
                    selectedPath = localSelectedPath; // select a new path and highlight it
                    HighLightWavePath(selectedPath, true);
                }
            }
            else if (selectedPath != null) // if there is a selectedPath and no hovered path, unhighlight
            {
                HighLightWavePath(selectedPath, false);
                selectedPath = null;
            }
        }
        else if (selectedPath != null) // if there is a selectedPath and no selectedNode, unhighlight
        {
            HighLightWavePath(selectedPath, false);
            selectedPath = null;
        }
    }

    public void HighLightWavePath(WavePath wavePath, bool highlight)
    {
        List<Arrow> highlightArrows = GetArrowsInPath(wavePath);
        if (highlightArrows == null)
        {
            return;
        }
        foreach (Arrow arrow in highlightArrows)
        {
            arrow.GetComponent<Renderer>().material.SetColor("_EmissionColor", highlight ? Color.white : Color.black);
            arrow.transform.position = new Vector3(arrow.transform.position.x, arrowOffset * (highlight ? 2f : 1f), arrow.transform.position.z);
        }
    }

    private List<Arrow> GetArrowsInPath(WavePath wavePath)
    {
        List<Node> nodeList = wavePath.NodeList;
        if (nodeList == null)
        {
            //Debug.LogError("WavePath must be valid!");
            return null;
        }
        List<Arrow> arrowList = new List<Arrow>();

        foreach (List<Arrow> containerList in arrowContainer.ArrowLists)
        {
            if (containerList != null && containerList.Count > 0 && ( /*containerList[containerList.Count - 1].Origin == wavePath.StartNode || */ containerList[0].Destination == wavePath.EndNode))
            {
                arrowList = containerList;
                break;
            }
        }

        //check to make sure this is the right list by checking each arrow with the Node

        return arrowList;
    }

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        GetWorldGrid();
        currentMap = FindMap();
        if (currentMap == null)
        {
            if (LevelLookup.levelName == LevelLookup.defaultLevelName)
            {
                Debug.LogError("Could not find map!");
                return;
            }
            else
            {
                currentMap = LoadMapFromName(LevelLookup.levelName);
                if (currentMap == null)
                {
                    return;
                }
            }
        }
        else if (currentMap.name != LevelLookup.levelName && LevelLookup.levelName != LevelLookup.defaultLevelName)
        {
            Destroy(currentMap.gameObject);
            currentMap = LoadMapFromName(LevelLookup.levelName);
        }

        currentMap.BuildWorldGrid(worldGrid);

        currentMap.SetLookup();

        currentLevel = currentMap.loadLevel;
        UseLevel(currentLevel);

        mapLoaded = true;

        DerivedAwake();
    }

    private Map LoadMapFromName(string name)
    {
        Map newMap = Resources.Load<Map>("Maps/" + name);
        if (newMap == null)
        {
            Debug.LogError("LevelLookup does not have a valid level name!");
            return null;
        }
        else
        {
            newMap = Instantiate(newMap);
            return newMap;
        }
    }

    protected virtual void DerivedAwake() { }

    protected void GetWorldGrid()
    {
        worldGrid = GameObject.FindWithTag("WorldGrid").GetComponent<WorldGrid>();
        if (worldGrid == null)
        {
            Debug.LogError("Could not find WorldGrid object in the scene. Either the tag was changed or the object is missing.");
        }
    }

    protected Map FindMap()
    {
        GameObject mapObject = GameObject.FindGameObjectWithTag("Map");
        if (mapObject == null)
        {
            //Debug.LogError("Cannot find an object with the Map tag!");
            return null;
        }
        Map map = mapObject.GetComponent<Map>();
        if (map == null)
        {
            //m_Debug.LogError("Object tagged Map does not have a Map script attached!");
            return null;
        }
        return map;
    }

    /// <summary>
    /// Populates wavePathList based on a List of SerializableWavePaths
    /// </summary>
    /// <param name="paths">List to be converted</param>
    protected void UseLevel(SerializableLevel level)
    {
        //get the arrow color
        LevelLookup.arrowColor = "#" + ColorUtility.ToHtmlStringRGB(currentMap.arrowColor);
        SetArrowContainerAreas(level.endAreas);
        wavePathList = new List<WavePath>();
        foreach (SerializableWavePath path in level.wavePaths)
        {
            wavePathList.Add(new WavePath(path, worldGrid));
        }

        DisplayPath(wavePathList);
    }

    protected void SetArrowContainerAreas(List<SerializableEndArea> endAreas)
    {
        Transform markerHolder = transform.Find("MarkerHolder").transform;
        if (markerHolder == null)
        {
            Debug.LogError("Cannot find marker holder! Perhaps it was moved or renamed?");
            return;
        }
        foreach (SerializableEndArea area in endAreas)
        {
            EndArea endArea = Instantiate(endAreaPrefab, markerHolder);
            endArea.endSetting = area.Sink ? endOptions.sink : endOptions.source;
            endArea.SetColor();
            GridArea tempArea = new GridArea(area);
            endArea.Place(worldGrid.getAt(tempArea.bottomLeft.x, tempArea.bottomLeft.y), worldGrid.getAt(tempArea.bottomLeft.x + tempArea.width - 1, tempArea.bottomLeft.y + tempArea.height - 1));
            HandleEndArea(endArea);
        }
    }

    protected virtual void HandleEndArea(EndArea endArea, SerializableEndArea area = null)
    {
        endArea.AddToArrowContainer(this);
        endArea.enabled = false;
    }

    /// <summary>
    /// When finished placing an Arrow, the Arrow will be placed and adhere to the Grid
    /// </summary>
    /// <param name="start">The Node in which the base of the Arrow is in</param>
    /// <param name="end">The Node in which the tip of the Arrow is in</param>
    /// <returns>success</returns>
    protected bool SetPathSegment(Node start, Node end, Arrow toSet = null)
    {

        if (start == null || end == null)
        {
            Debug.LogError("Passed invalid nodes to create segment!");
            return false;
        }

        if (toSet == null)
        {
            toSet = Instantiate(arrowPrefab, transform) as Arrow;
        }

        //toSet.PlaceArrow(start, end, arrowOffset);
        toSet.PlaceArrow(start, end, arrowOffset);

        Arrow addedArrow = arrowContainer.AddArrowToContainer(toSet);
        if (addedArrow == null)
        {
            Destroy(toSet.gameObject); //if you want players to be able to highlight paths remove this
            //Debug.Log("Arrow could not be place or was a duplicate!");
            //return false?
        }
        else
        {
            SetNodeOccupation(addedArrow, Node.nodeStates.navigation);
        }

        return true;
    }

    /// <summary>
    /// Creates the visual Arrow path
    /// </summary>
    /// <param name="wavePaths">Will create the path based on this value</param>
    protected void DisplayPath(List<WavePath> wavePaths)
    {
        foreach (WavePath path in wavePaths)
        {
            WavePath temp = new WavePath(path);
            Node current = null;
            Node previous = null;
            while ((current = temp.GetNextNode()) != null)
            {
                if (previous == null)
                {
                    previous = current;
                    continue;
                }
                if (SetPathSegment(previous, current))
                {
                    previous = current;
                }
                else
                {
                    Debug.LogError("DisplayPath failed, passed a null Node!");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Sets all Nodes between Arrow endpoint to a certain nodeState
    /// </summary>
    /// <param name="arrow">Arrow to be set</param>
    /// <param name="state">State that the Node will be set to (navigational or emmpty)</param>
    public void SetNodeOccupation(Arrow arrow, Node.nodeStates state)
    {
        bool vertical = arrow.Origin.Coordinate.x == arrow.Destination.Coordinate.x;
        if (vertical == (arrow.Origin.Coordinate.y == arrow.Destination.Coordinate.y))
        {
            Debug.LogError("Error marking an arrow occupied! The Arrow could be invalid!");
            return;
        }
        int start = vertical ? arrow.Origin.Coordinate.y : arrow.Origin.Coordinate.x;
        int end = vertical ? arrow.Destination.Coordinate.y : arrow.Destination.Coordinate.x;
        int constCoord = vertical ? arrow.Origin.Coordinate.x : arrow.Origin.Coordinate.y;
        if (start > end)
        {
            int temp = start;
            start = end;
            end = temp;
        }


        for (int i = start; i <= end; i++)
        {
            if (vertical)
            {
                worldGrid.getAt(constCoord, i).Occupied = state;
            }
            else
            {
                worldGrid.getAt(i, constCoord).Occupied = state;
            }
        }
    }

}
