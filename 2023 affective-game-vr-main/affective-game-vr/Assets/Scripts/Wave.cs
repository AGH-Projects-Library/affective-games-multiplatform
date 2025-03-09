using System;
using UnityEngine;

[Serializable]
public class Wave
{
	[SerializeField] private float m_baseTimeBetweenSpawns = 5f;
	public float BaseTimeBetweenSpawns => m_baseTimeBetweenSpawns;

	[SerializeField] private int m_baseNumOfSpawnersInUse = 1;
	public int BaseNumOfSpawnersInUse => m_baseNumOfSpawnersInUse;

	[SerializeField, Tooltip("In seconds")] private float m_duration = 60f;
	public float Duration => m_duration;

	[SerializeField, Tooltip("In seconds")] private float m_break = 5f;
	public float Break => m_break;
}