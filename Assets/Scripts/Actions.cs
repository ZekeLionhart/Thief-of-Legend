using System;

public static class Actions
{
    public static Action OnPlayerDeath;
    public static Action<int> DamagePlayer;
    public static Action RespawnPlayer;
    public static Action<int> HealPlayer;
    public static Action OnPowerUp;
    public static Action PauseGame;
    //public static Action ScorePoint;
    public static Action<int> ChangeCoins;
    public static Action<int> SetUpHealth;
    public static Action<int> OnHealthChange;
    public static Action OnSaveSettings;
}
