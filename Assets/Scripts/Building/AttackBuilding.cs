﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackBuilding : Building {

    private void Start() {
        radius = 2;
        initLineRenderer();
        location.width = 3;
        location.height = 4;

        //placeOnMap( new GridArea(new Vector2Int(3, 2), 3, 4) );
    }


    void Update () {
        handleMouse();
        showRadius();
        
    }

    private void FixedUpdate() {
        updateAction();
    }

    protected override void updateAction() {

    }
}
