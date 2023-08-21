using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private float respawnTime;
    
    private float respawnTimeStart;
    
    private bool respawn;
    
    private CinemachineVirtualCamera vcam;

    private void Start()
    {
        vcam = GameObject.Find("Player Camera").GetComponent<CinemachineVirtualCamera>();
    }

    private void Update()
    {
        CheckRespawn();
    }
    
    public void Respawn()
    {
        respawnTimeStart = Time.time;
        respawn = true;
    }

    private void CheckRespawn()
    {
        if (Time.time >= respawnTimeStart + respawnTime && respawn)
        {
            // 플레이어 생성 및 카메라 할당
            var playerTemp = Instantiate(playerPrefab, respawnPoint.position, respawnPoint.rotation);
            vcam.m_Follow = playerTemp.transform;
            
            respawn = false;
        }
    }
}
