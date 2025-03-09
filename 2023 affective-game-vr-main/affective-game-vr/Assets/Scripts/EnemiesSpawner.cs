using System;
using UnityEngine;

[Serializable]
public class EnemiesSpawner
{
	[SerializeField] private Transform m_spawnPosition = null;
	public Transform SpawnPosition => m_spawnPosition;

	[SerializeField] private Transform[] m_pathToPlayer = null;
	public Transform[] PathToPlayer => m_pathToPlayer;
}