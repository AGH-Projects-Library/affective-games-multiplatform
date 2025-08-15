using UnityEngine;

public partial class LocalizationData
{
    [ContextMenu("Populate Default Data")]
    private void PopulateDefaultData()
    {
        entries.Clear();

        // Shared Labels
        AddOrUpdate("tutorial.timeLabel", "Time to level begin: ", "Czas do rozpoczęcia poziomu: ");
        AddOrUpdate("tutorial.button.next", "Next", "Następny");
        AddOrUpdate("tutorial.button.prev", "Previous", "Poprzedni");
        AddOrUpdate("tutorial.button.play", "Play!", "Graj!");

        // Game Over
        AddOrUpdate("gameover.text", "You lost!\nThe level will restart in: ", "Przegrałeś!\nRestart poziomu za: ");

        // AffectiveEnemyManager Alerts
        AddOrUpdate("alert.moreMonsters", "More and more monsters are coming!\nWatch out!", "Nadchodzą kolejne potwory!\nUważaj!");

        // MrNightmareEnemyManager Alerts
        AddOrUpdate("alert.nightmareSupport", "Supporters are coming! Watch out!", "Przyzwano sojuszników! Uważaj!");

        // Scoreboard Close
        AddOrUpdate("scoreboard.close", "<color=red>Your name: {0}.\n Your score: {1}.\n</color> The game will end in: ",
            "<color=red>Twoja nazwa: {0}.\n Twój wynik: {1}.\n</color> Gra zakończy się za: ");
    }
}