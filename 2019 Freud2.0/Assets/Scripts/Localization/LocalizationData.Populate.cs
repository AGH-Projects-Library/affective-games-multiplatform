using UnityEngine;

public partial class LocalizationData
{
    [ContextMenu("Populate Default Data")]
    private void PopulateDefaultData()
    {
        entries.Clear();

        // Shared Labels
        AddOrUpdate("tutorial.timeLabel", "Czas do rozpoczęcia poziomu: ", "Time to level begin: "); 
        AddOrUpdate("tutorial.button.next", "Następny", "Next");
        AddOrUpdate("tutorial.button.prev", "Poprzedni", "Previous");
        AddOrUpdate("tutorial.button.play", "Graj!", "Play!");
        
        // Tutorial Labels (ManageTutorials.cs)
        AddOrUpdate("tutorial.level1.line0", "Witamy na kolejnej sesji.\nCo tak niewyraźnie wyglądasz?\nNie poznajesz nas. Ok, w porządku. Zaczniemy od początku.", "Welcome to the next session.\nWhy so agitated?\nYou do not recognize us...\nThat is alright.\nWe will start from the beginning.");
        AddOrUpdate("tutorial.level1.line1", "Przyszedłeś tutaj ze względu na problemy z pamięcią, a my jesteśmy Twoimi terapeutami.\nBrzmi znajomo? Nie? No, dobra.", "You have come here because of your troubles with memory,\nand we are your therapists.\nSounds familiar? No?\nHave a look at this, then.");
        AddOrUpdate("tutorial.level1.line2", "Tutaj jest nasza umowa, a tutaj Twój podpis. Zaznaczyliśmy najważniejszy fragment.\n\n\"Zgadzam się na niekonwencjonalne metody terapii psychologicznej.", "Here is our agreement, and this is your signature. We have highlighed the most important part.\n\n\"I hereby agree for\nunconventional methods of\npsychological treatment.");
        AddOrUpdate("tutorial.level1.line3", "Ok, skoro już wszystko jasne, przypomnimy Ci kilka zagadnień z psychologii.\nNasza terapia opiera się na psychoanalizie, której autorem jest Zygmunt Freud.", "Ok, if everything is clear now,\nlet's remind you of some\npsychological basics.\nOur therapy is grounded in psychoanalisys, developed by Sigmund Freud");
        AddOrUpdate("tutorial.level1.line4", "W jego koncepcji psychika działa na trzech poziomach. Świadomości, przedświadomości i nieświadomości.\nNajwiększy wpływ na późniejsze życie ma dzieciństwo.", "According to his theory, human psychology works on three levels: conscious, preconscious,\nand unconscious.\nThe greatest influence on later life comes from one's childhood.");
        AddOrUpdate("tutorial.level1.line5", "No, więc właśnie tam sie udajemy.\nDo dzieciństwa poprzez wszystkie trzy poziomy.\nOdpalaj sprzęt i lecimy, nie ma czasu!", "So, this is where we are heading now.\nTo the childhood,\nthrough all three levels.\nGet your gear and get going,\nthere is no time to waste!");
        AddOrUpdate("tutorial.level1.line6", "Przez cały czas będziemy w kontakcie.\nNie bój się, robiliśmy to miliony razy.", "We will be in touch all the time.\nWorry not, we have been doing this\na million times.");

        AddOrUpdate("tutorial.level3.line0", "Witaj ponownie!\nNo, następnym razem lepiej się pospiesz.\nWiesz ile te terapie kosztują w dzisiejszych czasach.\nTym razem udasz się do świadomości.\nJest to najbardziej zewnętrzny poziom.\nOznacza to, że twoja kontrola będzie największa.\nNa tym poziomie znowu spotkasz swoje ulubione zajączki,\nktóre tak chętnie dokarmiałeś sałatą.\nNie wiemy, co im się stało.\nTo Twoja głowa.\nW każdym razie zwykłą sałatą na pewno ich nie przekupisz.\nPamiętaj, że twoje zdrowie się regeneruje!", "Hello again!\nWell, next time better hurry up, will you?\nYou know, these therapies are expensive these days.\nThis time you'll go to the consciousness.\nIt is the outermost level.\nThis means that your control will be least impaired.\nOn this level, you'll meet your favourite bunnies,\nwhich you fed lettuce so willingly.\nWe don't know, what happened to them.\nIt's your head, after all.\nAnyway, regular lettuce won't work on them now.\nRemember that your health regenerates!");
        AddOrUpdate("tutorial.level5.line0", "No, no, gratulacje!\n\"Musimy zejść głębiej!\"\nPamiętasz Incepcję? A, no fakt. Masz probelmy z pamięcią.\nMusimy to sobie gdzieś zapisać.\nTeraz pora na przedświadomość. Czyli wszystko, co tłumisz.\nKażdy płacz, szukanie pocieszenia w objęciach ulubionego misia.\nTylko, że teraz te misie nie są już takie słodkie.\nAle masz Supermoc! Wystarczy, że przytrzymasz przycisk L2 z tyłu pada (lewy trigger)!\nPrzez kilka sekund, maksymalnie pięć.\nIm dłużej, tym więcej wrogów powalisz.\nNiestety, użycie supermocy na pewno Cię trochę zmęczy.\nNie będziesz mieć siły na takie szybkie i mocne strzały.\nPo czasie, wszystko wróci do normy. Zaczynajmy!", "Well, well, congratulations!\n\"We have to go deeper!\"\nRemember that movie, Inception? Ah, right. You have memory issues.\nWe have to write this down.\nNow it's time for the preconsciousness. This is everything, that you suppress.\nEvery tear, every need of consolation in your favourite teddybear's embrace.\nBut those teddys aren't that cute anymore.\nBut you have your Superpower! You just have to press L2 (left trigger)!\nFor a few seconds, 5 tops.\nThe longer, the more enemies you'll strike down.\nSadly, every use of Superpower will tire you for a bit.\nYou won't be able to shoot that fast and hard.\nIt will go back to normal after a while, though. Let's begin!");
        AddOrUpdate("tutorial.level7.line0", "Wow, to było wspaniałe! Robisz postępy!\nChyba pora na nieświadomość.\nSą tutaj wszystkie zdarzenia, które wyparłeś z górnych warstw.\nWśród nich Twoje spotkanie ze słoniami.\nDobrze, że nic Ci się wtedy nie stało. Było blisko.\nSłonie trochę zdziczały od tego czasu. Są bardzo niebezpieczne.\nTo w końcu nieświadomość.\nPamiętaj o tym, że Supermoc się regeneruje!\nPowodzenia!", "Wow, that was amazing! You're making progress!\nIt's time for the unsconsciousness.\nHere lay all the events, that you pushed away from the upper levels.\nIncluding your encounter with elephants.\nGood thing that you got out alright that time. That was close.\nElephants went wild a bit from that time. They're very dangerous.\nIt's the unconsciousness, after all.\nRemember that your Superpower regenerates!\nGood luck!");
        AddOrUpdate("tutorial.level9.line0", "Ktoś tu chce pozbyć się swoich lęków!\nI to nam się podoba!\nTak, tak! Dobrze rozumiesz,\nTwoje problemy z pamięcią wiążą się bezpośrednio\nz Twoimi największym lękami.\nWiesz, co to oznacza.\nPan Koszmarek.\nSiedzi tam już od dłuższego czasu.\nPora wykurzyć go z mieszkania i zaznać trochę spokoju.\nPamietaj, że wszystkie potworki popierają jego rządy.\nJeśli go skrzywdzisz, przybędą mu na pomoc.", "Somebody wants to fight his fears!\nAnd that's the attitude we like!\nYes, yes! You got it right.\nYour memory troubles are related directly \nto your biggest phobias.\nYou know what it means.\nMister Nightmare.\nHe's been sitting there for a long time.\nIt's time to drive him out once and for all, and get some peace.\nRemember, all other monsters support his reign.\nIf you hurt him, they will rush to his aid.");
        
        // Tutorial Labels (ManageZeroLvl.cs)
        AddOrUpdate("zeroLvl.alert1", "Cześć, to znowu my!\nWyjaśnimy Ci kilka kwestii.", "Hello, it's us again! \nWe'll explain some things to you.");
        AddOrUpdate("zeroLvl.alert2", "Ten element wskazuje Twoje życie.", "This element indicates your health.");
        AddOrUpdate("zeroLvl.alert3", "Ten pokaże Ci Twój wynik.", "This one shows your score.");
        AddOrUpdate("zeroLvl.alert4", "A tutaj widzisz SuperMoc.\nAle o tym będzie później.", "And here you can see your Superpower level.\nBut more on this later.");
        AddOrUpdate("zeroLvl.alert5", "Odblokowujemy możliwość poruszania się.\nUżyj joysticków kontrolera!", "We now unlock your movement.\nUse the analogue sticks!");
        AddOrUpdate("zeroLvl.alert6", "Lewy joystick to ruch,\nprawy joystick to obrót.", "Left stick is for movement, \nright is for turning around.");
        AddOrUpdate("zeroLvl.alert7", "No, śmiało, ruszaj się!\nTylko niczego nie zepsuj!", "Go on, get moving! \nJust don't break anything!");
        AddOrUpdate("zeroLvl.alert8", "Aby zdobywać punkty możesz zbierać\n żółte, świecące gwiazdki.\nPo prostu w nie wejdź.", "In order to collect points you can pick up\n yellow shiny stars. \nJust walk through them.");
        AddOrUpdate("zeroLvl.alert9", "Możesz też zabijać przeciwników.", "You can also kill the enemies.");
        AddOrUpdate("zeroLvl.alert10", "Po prostu naciśnij\nprzycisk R2 z tyłu pada (prawy trigger).\nAby ich wykończyć, strzel kilka razy.", "Just press \nR2 on the back of the game pad (right trigger). \nTo finish them off, shoot a few times.");
        AddOrUpdate("zeroLvl.alert11", "Pamiętaj, że to Twoja głowa.\nAmunicja nie skończy się nigdy.", "Remember, it's all in your head. \nYou'll never run out of ammo.");
        AddOrUpdate("zeroLvl.alert12", "Za gwiazdki dostaniesz mniej punktów niż\nza eliminowanie przeciwników.", "You get less points for stars than \nfor killing the enemies.");
        AddOrUpdate("zeroLvl.alert13", "I zwróć uwagę na informację o punktach.\nBo wiesz, ona się zmienia.", "And pay attention to the score. \nIt keeps changing, you know.");
        AddOrUpdate("zeroLvl.alert14", "Aby awansować na kolejne poziomy\nmusisz zebrać wystarczającą liczbę punktów.", "To get to the next level, \nyou need to collect required points.");
        AddOrUpdate("zeroLvl.alert15", "Ale nie powiemy Ci ile.", "We won't tell you how many exactly, though.");
        AddOrUpdate("zeroLvl.alert16", "Aby zakończyć szkolenie zbierz wszystkie gwiazdki\nlub zniszcz wszystkich przeciwników.", "To complete the training, collect all stars \nor kill all enemies.");
        AddOrUpdate("zeroLvl.alert17", "Ci tutaj są niegroźni.\nAle na prawdziwych lepiej uważaj!", "These ones are harmless. \nBut you better watch out for the real ones!");
        AddOrUpdate("zeroLvl.alert18", "Im są ciemniejsi,\ntym są silniejsi.", "The darker they are,\nthe stronger they hit you.");
        AddOrUpdate("zeroLvl.alert19", "Powodzenia i do zobaczenia niedługo!", "Good luck and see you soon!");
        
        // Intro Labels (ManageIntroduction.cs)
        AddOrUpdate("intro.welcome", "Witaj w Freud2.0!", "Welcome in the Freud2.0!");
        AddOrUpdate("intro.timeLabel", "Czas do rozpoczęcia poziomu: ", "Time to level begin: ");
        
        // Level Labels (ManageEnd.cs)
        AddOrUpdate("end.timeLabel", "Czas za jaki pokażemy Ci wyniki: ", "Time to show results: ");
        
        // Game Over
        AddOrUpdate("gameover.text", "Przegrałeś!\nRestart poziomu za: ", "You lost!\nThe level will restart in: ");

        // AffectiveEnemyManager Alerts
        AddOrUpdate("alert.moreMonsters", "Nadchodzą kolejne potwory!\nUważaj!", "More and more monsters are coming!\nWatch out!");

        // MrNightmareEnemyManager Alerts
        AddOrUpdate("alert.nightmareSupport", "Przyzwano sojuszników! Uważaj!", "Supporters are coming! Watch out!");

        // Scoreboard Close
        AddOrUpdate("scoreboard.close", "<color=red>Twoja nazwa: {0}.\n Twój wynik: {1}.\n</color> Gra zakończy się za: ",
            "<color=red>Your name: {0}.\n Your score: {1}.\n</color> The game will end in: ");
    }
}