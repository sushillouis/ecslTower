﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyStack : MonoBehaviour
{

    public GameObject coinPrefab;
    public int coinPerCash = 50;
    public int scoreOffset = 0;
    public float updateTime = 0.1f;
    public float moveUpBy = 10f;

    public static bool Modifying = false;

    void Start()
    {
        if (coinPrefab.tag != "Coin")
        {
            Debug.LogError("Please place a valid Coin object in this script. (Must have the 'Coin' tag)");
            return;
        }

        Modifying = false;

    }

    private void Update()
    {
        if (Modifying || Score.Money < 0)
        {
            return;
        }
        int desireValue = (Score.Money - scoreOffset) / coinPerCash;
        if(desireValue < 0)
        {
            desireValue = 0;
        }
        int toModify = desireValue - (transform.childCount);
        if (Mathf.Abs(toModify) > 0)
        {
            StartCoroutine(CreateCoins(toModify));
        }
    }

    private void CreateCoin()
    {
        GameObject coin = Instantiate(coinPrefab, transform.parent);
        transform.position += Vector3.up * moveUpBy;
        coin.transform.SetParent(transform);
        coin.transform.SetAsFirstSibling();
    }

    private void DeleteCoin()
    {
        transform.position += Vector3.up * -moveUpBy;
        Destroy(transform.GetChild(0).gameObject);
    }

    IEnumerator CreateCoins(int count)
    {
        Modifying = true;
        int absCount = Mathf.Abs(count);
        for (int i = 0; i < absCount; i++)
        {
            if (count > 0)
            {
                CreateCoin();
            } else
            {
                DeleteCoin();
            }
            yield return new WaitForSeconds(updateTime);
        }
        Modifying = false;
    }



}
