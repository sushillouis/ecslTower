﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitMainMenuButton : GameButton
{
    public override void Close()
    {
    }

    public override void Hide()
    {
    }

    public override void PerformAction()
    {
        Application.Quit();
    }

    public override void Show()
    {
	}
}
