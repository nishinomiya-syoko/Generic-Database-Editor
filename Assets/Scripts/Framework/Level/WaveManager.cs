using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Logic;

// 波次管理器
public class WaveManager : MonoBehaviour{
    public Transform[] enemySpawnPoints;

    public WaveData currentWave;
    public int currentWaveIndex = 0;
    public List<GameObject> spawnedUnits = new List<GameObject>();
    private Coroutine waveCoroutine;

    public void StartWave(WaveData waveData, int waveIndex)
    {
        currentWave = waveData;
        currentWaveIndex = waveIndex;

        if (waveCoroutine != null)
            StopCoroutine(waveCoroutine);

        waveCoroutine = StartCoroutine(WaveProcess());
    }
    private IEnumerator WaveProcess()
    {
        // 等待波次开始延迟
        yield return new WaitForSeconds(currentWave.startDelay);

        // OnWaveStarted?.Invoke(currentWaveIndex);
        var spawnPositionIndex = UnityEngine.Random.Range(0, enemySpawnPoints.Length);
        var spawnPosition = enemySpawnPoints[spawnPositionIndex].position;
        // 生成单位
        foreach (var spawnData in currentWave.unitsToSpawn)
        {
            for (int i = 0; i < spawnData.quantity; i++)
            {
                yield return new WaitForSeconds(spawnData.spawnDelay);
                SpawnUnit(spawnData, spawnPosition);
            }
        }

        // 等待所有单位被消灭
        yield return new WaitUntil(() => spawnedUnits.Count == 0);

        // OnWaveCompleted?.Invoke(currentWaveIndex);
        spawnedUnits.Clear();
    }

    private void SpawnUnit(UnitSpawnData spawnData, Vector3 spawnPosition)
    {
        // Unit unit = GameManager.Instance?.UnitManager?.DeployUnit(spawnData.unitId, spawnData.spawnPosition);
        // GameObject unit = EntityGenerator.Instance.Generate(spawnData.unitId, spawnPosition);
        GameObject unit = GlobalManager.Instance.EntityManager.ShowEntity(spawnData.unitId.ToString(), spawnPosition);
        if (unit != null)
        {
            spawnedUnits.Add(unit);
            // unit.OnUnitDied += (unit) => spawnedUnits.Remove(unit);
        }
    }

    public bool IsWaveComplete()
    {
        return spawnedUnits.Count == 0;
    }

    public void StopAllWaves()
    {
        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        // 清理生成的单位
        foreach (var unit in spawnedUnits)
        {
            if (unit != null)
            {
                Destroy(unit.gameObject);
            }
        }
        spawnedUnits.Clear();
    }
}