﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Networking;

public class Tutorial : PreWaveCreator
{
    public static Tutorial instance;

    public List<TutorialArrow.Spawn> spawnPoints = new List<TutorialArrow.Spawn>();

    private List<TutorialArrow> spawnedArrows = new List<TutorialArrow>();

    public TutorialArrow tutorialArrowPrefab;
    public TutorialArrow canvasTutorialArrowPrefab;
    public TutorialTips tutorialTips;
    public Button repairButton;
    public Button shopButton;
    //public Button buyRouterButton;

    private float bufferTime;

    List<PreAgent> firstPreWave = new List<PreAgent>();
    int firstInitialCount;
    List<PreAgent> secondPreWave = new List<PreAgent>();
    int secondInitialCount;

    AgentAttribute[] infectedAttributes = new AgentAttribute[3];

    Wave currentWave;

    private bool dismissed = false;

    private static int callFunction = -1;

    private void Start()
    {
        instance = this;

        bufferTime = mapDisplay.currentMap.spawnRate / 2f;

        //Wave 1

        infectedAttributes[0] = GenerateAttribute();

        //1
        AddBenignAgent(15, firstPreWave, 0);
        //2
        AddMaliciousAgent(2, firstPreWave, 0, 0);
        //7 change to random later
        AddBenignAgent(4, firstPreWave, 0);
        AddMaliciousAgent(1, firstPreWave, 0, 0);
        AddBenignAgent(4, firstPreWave, 0);
        AddMaliciousAgent(1, firstPreWave, 0, 0);
        AddBenignAgent(4, firstPreWave, 0);
        AddMaliciousAgent(1, firstPreWave, 0, 0);
        //8
        AddMaliciousAgent(4, firstPreWave, 0, 1);
        //9
        AddBenignAgent(15, firstPreWave);
        //10 change to random later
        AddBenignAgent(4, firstPreWave, 0);
        AddMaliciousAgent(1, firstPreWave, 0, 0);
        AddBenignAgent(4, firstPreWave, 0);
        AddMaliciousAgent(1, firstPreWave, 0, 0);

        firstInitialCount = firstPreWave.Count;

        //Wave2
        do
        {
            int _bogey;
            infectedAttributes[1] = MutateAttribute(infectedAttributes[0], out _bogey);
        } while (infectedAttributes[1].Color == infectedAttributes[0].Color && infectedAttributes[1].Size == infectedAttributes[0].Size);
        do
        {
            infectedAttributes[2] = GenerateAttribute();
        } while (infectedAttributes[2].Equals(infectedAttributes[1]));

        //13
        AddMaliciousAgent(1, secondPreWave, 1, 0);
        //15
        AddMaliciousAgent(1, secondPreWave, 1, 0);
        AddMaliciousAgent(1, secondPreWave, 2, 0);
        //16 change to random later
        AddBenignAgent(4, secondPreWave);
        AddMaliciousAgent(1, secondPreWave, 1);
        AddBenignAgent(4, secondPreWave);
        AddMaliciousAgent(1, secondPreWave, 2);
        AddBenignAgent(4, secondPreWave);
        AddMaliciousAgent(1, secondPreWave, 1);
        AddBenignAgent(4, secondPreWave);
        AddMaliciousAgent(1, secondPreWave, 2);

        secondInitialCount = secondPreWave.Count;

        CmdSpawnWave(firstPreWave.Select(x => new SerializablePreAgent(x)).ToArray());

        StartCoroutine(RunTutorial());


    }

    [Command]
    protected void CmdSpawnWave(SerializablePreAgent[] preAgents)
    {
        List<PreAgent> preAgentList = preAgents.Select(x => x.ToPreAgent()).ToList();
        currentWave = Instantiate(wavePrefab, transform);
        currentWave.CreateWaveWithList(preAgentList);

        NetworkServer.SpawnWithClientAuthority(currentWave.gameObject, connectionToClient);

    }

    IEnumerator RunTutorial()
    {
        //shopButton.interactable = false;
        foreach (Arrow arrow in mapDisplay.arrowContainer.arrowStacks[1])
        {
            mapDisplay.SetNodeOccupation(arrow, Node.nodeStates.empty);
        }
        currentWave.PauseSpawning();

        yield return new WaitForSeconds(bufferTime);

        //find a way to set this false at the beginning
            //currently must wait a little bit before setting it
        shopButton.interactable = false;

        //0
        tutorialTips.Show("Welcome to NETWORKING!");
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        //1
        tutorialTips.Show("Use the WASD or Arrow keys to move the Camera.\nUse the scroll wheel to zoom in/out.");
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        //1
        tutorialTips.Show("Benign packets give you money when they reach their destination.");
        CreateArrow("MoneyPointer", canvasTutorialArrowPrefab);
        currentWave.PauseSpawning(false);
        yield return new WaitUntil(() => currentWave.AgentsRemaining == firstInitialCount - 16);
        yield return new WaitForSeconds(bufferTime);
        currentWave.PauseSpawning();
        yield return new WaitUntil(() => WaitForFunction(0));
        currentWave.Pause();

        //2
        tutorialTips.Show("Malicious packets damage your health.");
        DeleteArrow("MoneyPointer");
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        //3
        tutorialTips.Show("Spend money to repair your health to full.", false);

        CreateArrow("RepairButton", canvasTutorialArrowPrefab);
        yield return new WaitUntil(() => Score.Health == Score.MaxHealth);
        DeleteArrow("RepairButton");
        tutorialTips.ShowDismiss();
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);
        currentWave.PauseSpawning(false);
        currentWave.Pause(false);
        yield return new WaitUntil(() => currentWave.AgentsRemaining == firstInitialCount - 17);
        currentWave.PauseSpawning();
        currentWave.Pause();

        //4
        tutorialTips.Show("The way to stop bad packets is by placing a router.");
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);
        tutorialTips.Show("Place a Router on the path to filter out the malicious packet.", false);
        shopButton.interactable = true;
        CreateArrow("RouterBuy", canvasTutorialArrowPrefab);
        yield return new WaitUntil(() => FindObjectOfType<RouterBuilding>() != null);
        DeleteArrow("RouterBuy");
        CreateArrow("PlaceFirstRouter", tutorialArrowPrefab);
        shopButton.interactable = false;
        RouterBuilding routerBuilding = FindObjectOfType<RouterBuilding>();
        routerBuilding.routingOptions.DisableSelection(2);
        yield return new WaitUntil(() => WaitForFunction(1));
        DeleteArrow("PlaceFirstRouter");
        

        tutorialTips.ShowDismiss();
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        //5
        tutorialTips.Show("Set the first two color and size filters to filter out the malicious packet.", false);
        CreateArrow("RouterSelection", canvasTutorialArrowPrefab, routerBuilding.UIOverlay.transform);
        CreateArrow("FirstEnemy", tutorialArrowPrefab);
        CreateArrow("RouterPointer", tutorialArrowPrefab, transform, routerBuilding.transform, 30);
        AgentAttribute attr0 = infectedAttributes[0];
        attr0.Speed = AgentAttribute.PossibleSpeeds.dontCare;
        yield return new WaitUntil(() => RouterSetCorrectly(attr0));
        DeleteArrow("RouterSelection");
        DeleteArrow("FirstEnemy");
        DeleteArrow("RouterPointer");
        routerBuilding.routingOptions.DisableSelection(-1);
        tutorialTips.ShowDismiss();
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        //6
        tutorialTips.Show("The ring packet on top of the router and in the HUD shows what packet combination you are filtering.");
        CreateArrow("DisplayAgent", tutorialArrowPrefab, transform, routerBuilding.worldSpaceDisplayAgent.transform, 45);
        CreateArrow("AgentHUD", canvasTutorialArrowPrefab);
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);
        DeleteArrow("DisplayAgent");
        DeleteArrow("AgentHUD");

        //7
        currentWave.PauseSpawning(false);
        currentWave.Pause(false);
        yield return new WaitUntil(() => currentWave.AgentsRemaining == firstInitialCount - 32);

        //8
        LevelLookup.markMalicious = false;
        yield return new WaitUntil(() => currentWave.AgentsRemaining == firstInitialCount - 36);
        currentWave.PauseSpawning();
        yield return new WaitUntil(() => WaitForFunction(0));
        repairButton.interactable = false;
        yield return new WaitUntil(() => Score.Health == 0);
        currentWave.Pause();
        tutorialTips.Show("Not all levels will mark bad packets.");
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        //9
        tutorialTips.Show("When servers are down you won't take damage, but you can't make money.");
        CreateArrow("NoHealth", canvasTutorialArrowPrefab);
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        //10
        tutorialTips.Show("Rebuild your servers to keep making money", false);
        repairButton.interactable = true;
        CreateArrow("RepairButton", canvasTutorialArrowPrefab);
        yield return new WaitUntil(() => WaitForFunction(2));
        DeleteArrow("RepairButton");
        tutorialTips.ShowDismiss();
        yield return new WaitUntil(() => DismissCheck());
        DeleteArrow("NoHealth");
        currentWave.PauseSpawning(false);
        currentWave.Pause(false);
        tutorialTips.Show("Your servers will be online once your health returns to full.");
        yield return new WaitUntil(() => currentWave == null);
        yield return new WaitForSeconds(bufferTime);

        //11
        tutorialTips.Show("When the wave is over you have time to rest.");
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        //Wave 2

        //12
        AudioManager.Play("PowerUp");
        yield return new WaitForSeconds(AudioManager.GetLength("PowerUp"));
        tutorialTips.Show("But the malicious packets will be mutated to a new combination.");
        CmdSpawnWave(secondPreWave.Select(x => new SerializablePreAgent(x)).ToArray());
        yield return new WaitUntil(() => currentWave.AgentsRemaining == secondInitialCount - 1);
        currentWave.PauseSpawning();
        yield return new WaitUntil(() => WaitForFunction(0));

        //13
        tutorialTips.Show("Change your filter to adapt to the new wave.\n(" + infectedAttributes[1].Color + ", " + infectedAttributes[1].Size + ", " + infectedAttributes[1].Speed + ")", false);
        CreateArrow("RouterPointer", tutorialArrowPrefab, transform, routerBuilding.transform, 30);
        routerBuilding.routingOptions.DisableSelection(-1, false);
        yield return new WaitUntil(() => RouterSetCorrectly(infectedAttributes[1]));
        routerBuilding.routingOptions.DisableSelection(-1);
        DeleteArrow("RouterPointer");
        tutorialTips.ShowDismiss();
        yield return new WaitUntil(() => DismissCheck());
        currentWave.PauseSpawning(false);
        yield return new WaitUntil(() => currentWave.AgentsRemaining == secondInitialCount - 3);
        currentWave.PauseSpawning();
        yield return new WaitUntil(() => WaitForFunction(0));

        //15a
        tutorialTips.Show("There will be more than one combination to filter.");
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);

        tutorialTips.Show("Place another Router to filter out the other packet.", false);
        CreateArrow("PlaceSecondRouter", tutorialArrowPrefab);
        shopButton.interactable = true;
        yield return new WaitUntil(() => FindObjectsOfType<RouterBuilding>().Length >= 2);
        shopButton.interactable = false;
        yield return new WaitUntil(() => WaitForFunction(1));
        DeleteArrow("PlaceSecondRouter");
        tutorialTips.ShowDismiss();
        yield return new WaitUntil(() => DismissCheck());
        tutorialTips.Show("Change the filter to filter out the other packet.\n(" + infectedAttributes[2].Color + ", " + infectedAttributes[2].Size + ", " + infectedAttributes[2].Speed + ")");
        yield return new WaitUntil(() => DismissCheck());

        //14
        tutorialTips.Show("The more specific the filter is, the more money you'll make from benign packets.");
        yield return new WaitUntil(() => DismissCheck());

        //15b
        tutorialTips.Show("Now place two more routers on the other path and filter to mirror these.", false);
        CreateArrow("PlaceThirdRouter", tutorialArrowPrefab);
        CreateArrow("PlaceFourthRouter", tutorialArrowPrefab);
        shopButton.interactable = true;
        foreach (Arrow arrow in mapDisplay.arrowContainer.arrowStacks[1])
        {
            mapDisplay.SetNodeOccupation(arrow, Node.nodeStates.navigation);
        }
        foreach (Arrow arrow in mapDisplay.arrowContainer.arrowStacks[0])
        {
            mapDisplay.SetNodeOccupation(arrow, Node.nodeStates.empty);
        }
        yield return new WaitUntil(() => WaitForFunction(1));
        yield return new WaitUntil(() => WaitForFunction(1));
        DeleteArrow("PlaceThirdRouter");
        DeleteArrow("PlaceFourthRouter");
        tutorialTips.ShowDismiss();
        yield return new WaitUntil(() => DismissCheck());
        yield return new WaitForSeconds(bufferTime);
        currentWave.PauseSpawning(false);

        //16
        tutorialTips.Show("Once you are filtering the most specific malicious combinations, you can sit back and relax.");
        yield return new WaitUntil(() => currentWave == null);

        //17
        tutorialTips.Show("But watch out for the next wave!");
        yield return new WaitUntil(() => DismissCheck());

        //18
        AudioManager.Play("PowerUp");
        yield return new WaitForSeconds(AudioManager.GetLength("PowerUp"));

        //19
        SceneLoader.ExitGame();

    }

    private void CreateArrow(string _name, TutorialArrow prefab, Transform parent = null, Transform tracker = null, float angle = 0)
    {
        if(parent == null)
        {
            parent = transform;
        }
        TutorialArrow arrow = Instantiate(prefab, parent);
        if (tracker == null)
        {
            arrow.PlaceArrow(spawnPoints, _name);
            spawnedArrows.Add(arrow);
        } else
        {
            arrow.TrackTransform(tracker, angle);
            arrow.name = _name;
            spawnedArrows.Add(arrow);
        }
        
        
    }

    private void DeleteArrow(string _name)
    {
        TutorialArrow delArrow = spawnedArrows.Find(x => x.name == _name);
        if(delArrow == null)
        {
            Debug.LogError("Could not find TutorialArrow with that name!");
            return;
        }
        spawnedArrows.Remove(delArrow);
        delArrow.DeleteArrow();
    }

    private void Update()
    {
        //not the most efficient but works for now
        Sell[] sellButtons = GameObject.FindObjectsOfType<Sell>();
        foreach(Sell s in sellButtons)
        {
            s.GetButton.interactable = false;
        }
    }

    private bool WaitForFunction(int index)
    {
        //0: Malicious packet reaches destination
        //1: Building is placed
        //2: Rebuilding of the server starts

        if (index == callFunction && index != -1 && callFunction != -1)
        {
            callFunction = -1;
            return true;
        }
        else
        {
            return false;
        }
    }

    public static void CallFunction(int index)
    {
        callFunction = index;
    }

    private bool RouterSetCorrectly(AgentAttribute attribute)
    {
        RouterBuilding router = FindObjectOfType<RouterBuilding>();
        if (router == null)
        {
            return false;
        }

        AgentAttribute routerAttribute = router.filter;
        return routerAttribute.Color == attribute.Color && routerAttribute.Size == attribute.Size && routerAttribute.Speed == attribute.Speed;

    }


    private bool DismissCheck()
    {
        if (dismissed)
        {
            dismissed = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Dismiss(bool _dismissed)
    {
        dismissed = _dismissed;
    }

    private void AddBenignAgent(int count, List<PreAgent> preWave, int pathNumber = -1)
    {
        WavePath wavePath = null;
        if (pathNumber > -1)
        {
            wavePath = mapDisplay.WavePathList[pathNumber];
        }
        for (int i = 0; i < count; i++)
        {
            AgentAttribute attr;
            do
            {
                attr = GenerateAttribute();
            } while (System.Array.Exists(infectedAttributes, a => a.Equals(attr)));
            if (pathNumber <= -1)
            {
                wavePath = GetRandomWavePath(mapDisplay);
            }
            preWave.Add(new PreAgent(benignAgent, wavePath, attr));
        }
    }

    private void AddMaliciousAgent(int count, List<PreAgent> preWave, int attr, int pathNumber = -1)
    {
        WavePath wavePath = null;
        if (pathNumber > -1)
        {
            wavePath = mapDisplay.WavePathList[pathNumber];
        }
        for (int i = 0; i < count; i++)
        {
            if (pathNumber <= -1)
            {
                wavePath = GetRandomWavePath(mapDisplay);
            }
            preWave.Add(new PreAgent(maliciousAgent, wavePath, infectedAttributes[attr]));
        }
    }
}
