﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGrid: MonoBehaviour {
    public int rows = 1;
    public int cols = 1;
    public Node nodePrefab;

    private Node[,] m_grid;


    private void Awake() {
        m_grid = new Node[rows, cols];

        for(int i = 0; i < rows; ++i) {
            for(int j = 0; j < cols; ++j) {
                m_grid[i, j] = Instantiate(nodePrefab, transform);
                m_grid[i, j].transform.position = new Vector3(i + 0.5f, 0, j + 0.5f);
                m_grid[i, j].name = "Node (" + i + ", " + j + ")";
            }
        }
    }

    /// <summary>
    /// Raycasts the mouse location to the WorldGrid.
    /// Returns the node it hit, or null if no collision occured.
    /// </summary>
    /// <returns>The hit node if valid, otherwise null</returns>
    public Node getRaycastNode() {
        Ray ray;
        RaycastHit hit;
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(!Physics.Raycast(ray, out hit)) {
            return null;
        }

        Node ret = hit.transform.gameObject.GetComponent<Node>();
        if(ret != null) {
            return ret;
        } else {
            return null;
        }
    }

    private void LateUpdate() {
        Node hit = getRaycastNode();
        if(hit != null) {
            hit.setHovered();
        }
    }





}