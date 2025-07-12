public class EventStrings
{
    public static string GetWaveEndString(int waveNum, int enemiesSpawned, int spawnPoints, int damageReceived)
    {
        return $"Wave: {waveNum}; SpawnedEnemies: {enemiesSpawned}; SpawnPoints: {spawnPoints}; DamageReceived: {damageReceived};";
    }

    public static string GetEnemiesSpeedString(float speed)
    {
        return $"EnemiesSpeed: {speed};";
    }
}
