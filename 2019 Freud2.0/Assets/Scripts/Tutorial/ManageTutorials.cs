using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;

public class ManageTutorials : MonoBehaviour 
{

	public Text textShowed;

    public Text textCD;
    
    // Key mappings (editable in inspector)
    public KeyCode keyS = KeyCode.S;
    public KeyCode keyEscape = KeyCode.Escape;
    public KeyCode keyT = KeyCode.T;
    public KeyCode keyU = KeyCode.U;
    
	List<string> texts = new List<string> ();
    int index;

    // bool bitalinoUseFlag = false;     // F2

    UserManager.LanguageOption lang;

    string txtNextButtonPl = "Następny";
    string txtPreviousButtonPl = "Poprzedni";
    string txtPlayButtonPl = "Graj!";
    string txtNextButtonEng = "Next";
    string txtPreviousButtonEng = "Previous";
    string txtPlayButtonEng = "Play!";

    public float timeToStart = 30;

	int timeCD;

    string txtTime = "";
    string txtTimePL = "Czas do rozpoczęcia poziomu: ";

    string txtTimeENG = "Time to level begin: ";



    public void changeText (int x) 
    {
        this.index += x;
		
	}

    void Awake()
    {
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;ID;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;

        timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");
        
        // nextButton.gameObject.SetActive(false);
        // previousButton.gameObject.SetActive(false);
        // playButton.gameObject.SetActive(false);

        // if (lang.Equals(UserManager.LanguageOption._English))
        // {
        //     nextButton.GetComponentInChildren<Text>().text = txtNextButtonEng;
        //     previousButton.GetComponentInChildren<Text>().text = txtPreviousButtonEng;
        //     playButton.GetComponentInChildren<Text>().text = txtPlayButtonEng;
        // }
        // else if (lang.Equals(UserManager.LanguageOption._Polish))
        // {
        //     nextButton.GetComponentInChildren<Text>().text = txtNextButtonPl;
        //     previousButton.GetComponentInChildren<Text>().text = txtPreviousButtonPl;
        //     playButton.GetComponentInChildren<Text>().text = txtPlayButtonPl;
        // }

        this.index = 0;

        // bitalinoUseFlag = BitalinoController.bitalinoController.bitalinoUse;     // F2

        texts = new List<string> ();
        int currentSceneNumber = SceneManager.GetActiveScene().buildIndex;
        // TextsUpdate(bitalinoUseFlag, currentSceneNumber);     // F2
        TextsUpdate(currentSceneNumber);
        textShowed.text = texts[this.index];

        // playButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(keyS))
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		}

        if(timeToStart < Time.timeSinceLevelLoad)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }


        if (Input.GetKeyDown(keyEscape)) // use public binding for Escape
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        // if (this.index == 0)
        // {
        //     nextButton.gameObject.SetActive(true);
        //     previousButton.gameObject.SetActive(false);
        // }

        // else if (this.index == (texts.Count - 2))
        // {
        //     nextButton.gameObject.SetActive(true);
        // }

        // else if (this.index == (texts.Count - 1))
        // {
        //     nextButton.gameObject.SetActive(false);
        //     playButton.gameObject.SetActive(true); // if user gets to the end of tutorial, remain visible
        // }

        // if (this.index >= 1)
        // {
        //     previousButton.gameObject.SetActive(true);
        // }

        // tutorial skip:
        if (Input.GetKeyDown(keyT))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;T");
            StartCoroutine("CountDown");
        }

        else if (Input.GetKeyDown(keyU))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;U");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
        }

        // if (this.index == -1)
        // {
        //     nextButton.gameObject.SetActive(false);
        //     previousButton.gameObject.SetActive(false);
        //     playButton.gameObject.SetActive(false);
        // }
    }

    public void runGame () 
    {   
        // LogManager.logManager.AddEvent(Time.time, "ButtonClick;Play");
        StartCoroutine("CountDown");
	}

    IEnumerator CountDown()
    {
        this.index = -1;

        textShowed.text = "3".ToString();
        yield return new WaitForSeconds(1);

        textShowed.text = "2".ToString();
        yield return new WaitForSeconds(1);

        textShowed.text = "1".ToString();
        yield return new WaitForSeconds(1);
        
        textShowed.text = "0".ToString();
        yield return new WaitForSeconds(0.25f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // F2
    // void TextsUpdate(bool usingBitalino, int lvl)
    void TextsUpdate(int lvl)
    {
        // if (usingBitalino)
        // {
        //     if(lang.Equals(UserManager.LanguageOption._Polish))
        //     {
        //         if (lvl == 1)
        //         {
        //             texts.Add("Witamy na kolejnej sesji.\nCo tak niewyraźnie wyglądasz?\nNie poznajesz nas. Ok, w porządku. Zaczniemy od początku.");
        //             texts.Add("Przyszedłeś tutaj ze względu na problemy z pamięcią, a my jesteśmy Twoimi terapeutami.\nBrzmi znajomo? Nie? No, dobra.");
        //             texts.Add("Tutaj jest nasza umowa, a tutaj Twój podpis. Zaznaczyliśmy najważniejszy fragment.\n\n\"Zgadzam się na niekonwencjonalne metody terapii psychologicznej.\"\n" + UserManager.userManager.GetUserName());
        //             texts.Add("Ok, skoro już wszystko jasne, przypomnimy Ci kilka zagadnień z psychologii.\nNasza terapia opiera się na psychoanalizie, której autorem jest Zygmunt Freud.");
        //             texts.Add("W jego koncepcji psychika działa na trzech poziomach. Świadomości, przedświadomości i nieświadomości.\nNajwiększy wpływ na późniejsze życie ma dzieciństwo.");
        //             texts.Add("No, więc właśnie tam sie udajemy.\nDo dzieciństwa poprzez wszystkie trzy poziomy.\nZakładaj sprzęt i lecimy, nie ma czasu! ");
        //             texts.Add("Tak, tak, te elektrody pozwolą Ci pozostać z nami w kontakcie.\nNie bój się, robiliśmy to miliony razy.");
        //         }

        //         else if (lvl == 3)
        //         {
        //             texts.Add("Witaj ponownie!\nNo, następnym razem lepiej się pospiesz. Wiesz ile te terapie kosztują w dzisiejszych czasach.");
        //             texts.Add("Dobra, chyba nie ma sensu rozwodzić się nad tym, co Cię czeka.\nUdasz się do świadomości. Jest to najbardziej zewnętrzny poziom, oznacza to, że twoja kontrola będzie największa.");
        //             texts.Add("Uważaj na potwory, szczególnie te święcące najciemniejszym światłem.\n Pamiętaj, że przeciwnicy mogą Cię wyczuwać. Im bardziej jesteś wyluzowyany, tym więcej ich po Ciebie przyjdzie. ");
        //             texts.Add("Aha, będą też się szybciej poruszać.\nAle przecież masz pistolet!\nSpokojnie!");
        //             texts.Add("Na tym poziomie znowu spotkasz swoje ulubione zajączki, które tak chętnie dokarmiałeś sałatą.\nNie wiemy, co im się stało.\nTo Twoja głowa.\nW każdym razie zwykłą sałatą na pewno ich nie przekupisz.");
        //         }

        //         else if (lvl == 5)
        //         {
        //             texts.Add("No, no, gratulacje!\nOd razu widać, że bardziej się starasz!\n\"Musimy zejść głębiej!\"\nPamiętasz Incepcję? A, no fakt. Masz probelmy z pamięcią. Musimy to sobie gdzieś zapisać.");
        //             texts.Add("Teraz pora na przedświadomość. Czyli wszystko, co tłumisz.\nKażdy płacz, szukanie pocieszenia w objęciach ulubionego misia.\nTylko, że teraz te misie nie są już takie słodkie.");
        //             texts.Add("Ale spokojnie, spokojnie.\nPrzecież masz Supermoc! Wystarczy, że napniesz swój biceps!\nPrzez kilka sekund, góra pięć. Im dłużej tym więcej wrogów powalisz.");
        //             texts.Add("Możesz też nie powalić żadnego.\nNo cóż, w przedświadomości nie mamy pełnej kontroli.\nAha, moc potrzebuje chwili, żeby rozejść się po wszystkich przeciwnikach. Nie denerwuj się!");
        //             texts.Add("Niestety są też inne minusy.\nUżycie supermocy na pewno Cię trochę zmęczy.\nNie będziesz mieć siły na takie szybkie i mocne strzały.");
        //             texts.Add("Ale dość gadania.\nLecimyyy!");
        //         }

        //         else if (lvl == 7)
        //         {
        //             texts.Add("Wow, to było wspaniałe!\nRobisz postępy!\nChyba pora na nieświadomość.");
        //             texts.Add("Są tutaj wszystkie zdarzenia, które wyparłeś z górnych warstw.\nWśród nich Twoje spotkanie ze słoniami. Dobrze, że nic Ci się wtedy nie stało. Było blisko.");
        //             texts.Add("Słonie trochę zdziczały od tego czasu. Są bardzo niebezpieczne.\nA, jest też druga sprawa.\nTo w końcu nieświadomość.\nMożemy mieć problemy z łącznością.");
        //             texts.Add("Ale nic się nie bój, zostaniemy z Tobą tak długo jak będziemy mogli.\nA poza tym, masz Supermoc.\nSam powiedz, że jest super, a potem wskakuj!");
        //         }

        //         else if (lvl == 9)
        //         {
        //             texts.Add("Eee, sorki za te komplikacje.\nAle poszło Ci świetnie!\n Ktoś tu chce pozbyć się swoich lęków!\nI to nam się podoba!");
        //             texts.Add("Tak, tak! Dobrze rozumiesz, Twoje problemy z pamięcią wiążą się bezpośrednio z Twoimi największym lękami. Wiesz, co to oznacza.");
        //             texts.Add("Pan Koszmarek.");
        //             texts.Add("Siedzi tam już od dłuższego czasu.\nPora wykurzyć go z mieszkania i zaznać trochę spokoju.");
        //             texts.Add("Kiedy będziesz z nim walczyć, pamietaj, że wszystkie potworki popierają jego rządy.\nJeśli go skrzywdzisz, przybędą mu na pomoc.");
        //             texts.Add("To lecimy!\nMy tutaj z bezpiecznej odległości będziemy się przyglądać.\nW razie jakby były jakieś problemy... No, coś wtedy wymyślimy.");
        //         }
        //     }

        //     else if(lang.Equals(UserManager.LanguageOption._English))
        //     {
        //         if (lvl == 1)
        //         {
        //             texts.Add("Welcome to the next session.\nWhy so agitated?\nYou do not recognize us...\nThat is alright.\nWe will start from the beginning.");
        //             texts.Add("You have come here because of your troubles with memory,\nand we are your therapists.\nSounds familiar? No?\nHave a look at this, then.");
        //             texts.Add("Here is our agreement, and this is your signature. We have highlighed the most important part.\n\n\"I hereby agree for\nunconventional methods of\npsychological treatment.\"\n" + UserManager.userManager.GetUserName());
        //             texts.Add("Ok, if everything is clear now,\nlet’s remind you of some\npsychological basics.\nOur therapy is grounded in psychoanalisys, developed by Sigmund Freud");
        //             texts.Add("According to his theory, human psychology works on three levels: conscious, preconscious,\nand unconscious.\nThe greatest influence on later life comes from one’s childhood.");
        //             texts.Add("So, this is where we are heading now.\nTo the childhood,\nthrough all three levels.\nGet your gear and get going,\nthere is no time to waste!");
        //             texts.Add("Yes, yes, those electrodes will allow you to stay in touch with us.\nWorry not, we have been doing this\na million times.");
        //         }

        //         else if (lvl == 3)
        //         {
        //             texts.Add("Hello again!\nWell, hurry up a little next time, will you?\nYou know, how these therapies\nare expensive these times.");
        //             texts.Add("Alright, there is no point in pondering on what is still\nahead of you.\nYou will head to the consciousness.\nIt is the most external level,\nwhich means that your control\nwill be most unrestricted.");
        //             texts.Add("Beware of monsters, especially those emitting the darkest light.\n Remember, that the enemies\ncan sense you.\nThe more relaxed you are,\nthe more of them will come at you.");
        //             texts.Add("Ah, and they will also come faster.\nRelax, you have a gun!");
        //             texts.Add("On this level, you will meet your favorite bunnies - which you have fed with lettuce so eagerly.\nWe do not know, what happened.\nIt is your head.\nAnyway, it seems that\nthey cannot be bribed with lettuce.");
        //         }

        //         else if (lvl == 5)
        //         {
        //             texts.Add("Well, well, congratulations!\nIt is clear that you are\ntrying harder now!\n\"We have to go deper!\"\nDo you remember Inception?\nAh, right,\nyou have troubles with memory.\nWe need to take a note on this.");
        //             texts.Add("Now, it is time for preconsciousness. That is everything,\nthat you suppress.\nEvery weep, every comfort\nthat you seek in your favorite\n teddy’s embrace.\nThe difference is that the teddies\nare not that cute anymore.");
        //             texts.Add("But rest easy.\nYou have your Superpower!\nYou just have to flex your biceps!\nFor a few seconds, up to five.\nThe longer, the more enemies\nyou will knock down.");
        //             texts.Add("Sometimes you will not\nknock down anyone.\nNo wonder, you do not have your\nfull control in the preconscious.\nBy the way, the power needs some time to spread among all of your enemies. Don’t get too frantic!");
        //             texts.Add("Unfortunately,\nthere are also other drawbacks.\nUsing your superpower\nwill tire you a little.\nYou will not be able to attack as fast and as strong as before using it,\nfor a while.");
        //             texts.Add("But enough talking.\nLet’s goo!");
        //         }

        //         else if (lvl == 7)
        //         {
        //             texts.Add("Wow, that was amazing! \nYou are making progress!\nLooks like it is time\nfor the unconscious.");
        //             texts.Add("Here are all the events that you have repressed from the previous levels.\nIncluding encounter with elephants.\nGood thing\nthat you got out unharmed.\nThat was close.");
        //             texts.Add("The elephants have run wild a bit from that time.\nThey are very dangerous.\nAh, there is one more thing.\nThis is the unconscious, after all.\nThere might be some\ncommunication issues.");
        //             texts.Add("But do not worry, we will stay\nwith you as long as we can.\nBesides, you have your Superpower.\nJust admit yourself,\nhow awesome it is,\nand go get them!");
        //         }

        //         else if (lvl == 9)
        //         {
        //             texts.Add("Err, sorry for those disturbances.\nYou did great, though!\nLooks like someone wants\nto get rid of his fears!\n Way to go!");
        //             texts.Add("Yes, yes! You understand correctly! Your troubles with memory\nare directly connected\nwith your biggest fears.\nYou know what that means.");
        //             texts.Add("Mr Nightmare.");
        //             texts.Add("He has been lingering there\nfor quite a long time.\nIt is time to smoke him out\nand find peace again.");
        //             texts.Add("When you will fight him,\nremember that he is supported\nby all of the other monsters.\nIf you harm him,\nthey will come to his aid.");
        //             texts.Add("Let’s go! \nWe will watch you\nfrom a safe distance.\nIf you will get in trouble...\nWell,\nwe will figure something out then.");
        //         }
        //     }
        // }

        // else
        // {
            if(lang.Equals(UserManager.LanguageOption._Polish))
            {
                txtTime = txtTimePL;

                if (lvl == 1)
                {
                    texts.Add("Witamy na klejnej sesji.\nCo tak niewyraźnie wyglądasz?\nNie poznajesz nas. Ok, w porządku. Zaczniemy od początku.");
                    texts.Add("Przyszedłeś tutaj ze względu na problemy z pamięcią, a my jesteśmy Twoimi terapeutami.\nBrzmi znajomo? Nie? No, dobra.");
                    texts.Add("Tutaj jest nasza umowa, a tutaj Twój podpis. Zaznaczyliśmy najważniejszy fragment.\n\n\"Zgadzam się na niekonwencjonalne metody terapii psychologicznej.\"\n" + UserManager.userManager.GetUserName());
                    texts.Add("Ok, skoro już wszystko jasne, przypomnimy Ci kilka zagadnień z psychologii.\nNasza terapia opiera się na psychoanalizie, której autorem jest Zygmunt Freud.");
                    texts.Add("W jego koncepcji psychika działa na trzech poziomach. Świadomości, przedświadomości i nieświadomości.\nNajwiększy wpływ na późniejsze życie ma dzieciństwo.");
                    texts.Add("No, więc właśnie tam sie udajemy.\nDo dzieciństwa poprzez wszystkie trzy poziomy.\nOdpalaj sprzęt i lecimy, nie ma czasu! ");
                    texts.Add("Przez cały czas będziemy w kontakcie.\nNie bój się, robiliśmy to miliony razy.");
                }

                else if (lvl == 3)
                {
                    texts.Add("Witaj ponownie!\nNo, następnym razem lepiej się pospiesz.\nWiesz ile te terapie kosztują w dzisiejszych czasach.\nTym razem udasz się do świadomości.\nJest to najbardziej zewnętrzny poziom.\nOznacza to, że twoja kontrola będzie największa.\nNa tym poziomie znowu spotkasz swoje ulubione zajączki,\nktóre tak chętnie dokarmiałeś sałatą.\nNie wiemy, co im się stało.\nTo Twoja głowa.\nW każdym razie zwykłą sałatą na pewno ich nie przekupisz.\nPamiętaj, że twoje zdrowie się regeneruje!");
                }

                else if (lvl == 5)
                {
                    texts.Add("No, no, gratulacje!\n\"Musimy zejść głębiej!\"\nPamiętasz Incepcję? A, no fakt. Masz probelmy z pamięcią.\nMusimy to sobie gdzieś zapisać.\nTeraz pora na przedświadomość. Czyli wszystko, co tłumisz.\nKażdy płacz, szukanie pocieszenia w objęciach ulubionego misia.\nTylko, że teraz te misie nie są już takie słodkie.\nAle masz Supermoc! Wystarczy, że przytrzymasz przycisk L2 z tyłu pada (lewy trigger)!\nPrzez kilka sekund, maksymalnie pięć.\nIm dłużej, tym więcej wrogów powalisz.\nNiestety, użycie supermocy na pewno Cię trochę zmęczy.\nNie będziesz mieć siły na takie szybkie i mocne strzały.\nPo czasie, wszystko wróci do normy. Zaczynajmy!");
                }

                else if (lvl == 7)
                {
                    texts.Add("Wow, to było wspaniałe! Robisz postępy!\nChyba pora na nieświadomość.\nSą tutaj wszystkie zdarzenia, które wyparłeś z górnych warstw.\nWśród nich Twoje spotkanie ze słoniami.\nDobrze, że nic Ci się wtedy nie stało. Było blisko.\nSłonie trochę zdziczały od tego czasu. Są bardzo niebezpieczne.\nTo w końcu nieświadomość.\nPamiętaj o tym, że Supermoc się regeneruje!\nPowodzenia!");
                }

                else if (lvl == 9)
                {
                    texts.Add("Ktoś tu chce pozbyć się swoich lęków!\nI to nam się podoba!\nTak, tak! Dobrze rozumiesz,\nTwoje problemy z pamięcią wiążą się bezpośrednio\nz Twoimi największym lękami.\nWiesz, co to oznacza.\nPan Koszmarek.\nSiedzi tam już od dłuższego czasu.\nPora wykurzyć go z mieszkania i zaznać trochę spokoju.\nPamietaj, że wszystkie potworki popierają jego rządy.\nJeśli go skrzywdzisz, przybędą mu na pomoc.");
                }
            }

            else if(lang.Equals(UserManager.LanguageOption._English))
            {
                txtTime = txtTimeENG;

                if (lvl == 1)
                {
                    texts.Add("Welcome to the next session.\nWhy so agitated?\nYou do not recognize us...\nThat is alright.\nWe will start from the beginning.");
                    texts.Add("You have come here because of your troubles with memory,\nand we are your therapists.\nSounds familiar? No?\nHave a look at this, then.");
                    texts.Add("Here is our agreement, and this is your signature. We have highlighed the most important part.\n\n\"I hereby agree for\nunconventional methods of\npsychological treatment.\"\n" + UserManager.userManager.GetUserName());
                    texts.Add("Ok, if everything is clear now,\nlet’s remind you of some\npsychological basics.\nOur therapy is grounded in psychoanalisys, developed by Sigmund Freud");
                    texts.Add("According to his theory, human psychology works on three levels: conscious, preconscious,\nand unconscious.\nThe greatest influence on later life comes from one’s childhood.");
                    texts.Add("So, this is where we are heading now.\nTo the childhood,\nthrough all three levels.\nGet your gear and get going,\nthere is no time to waste!");
                    texts.Add("We will be in touch all the time.\nWorry not, we have been doing this\na million times.");
                }

                else if (lvl == 3)
                {
                    texts.Add("Hello again!\nWell, next time better hurry up, will you?\nYou know, these therapies are expensive these days.\nThis time you’ll go to the consciousness.\nIt is the outermost level.\n This means that your control will be least impaired.\nOn this level, you’ll meet your favourite bunnies,\nwhich you fed lettuce so willingly.\nWe don’t know, what happened to them.\nIt’s your head, after all.\nAnyway, regular lettuce won’t work on them now.\nRemember that your health regenerates!");
                }

                else if (lvl == 5)
                {
                    texts.Add("Well, well, congratulations!\n\"We have to go deeper!\"\nRemember that movie, Inception? Ah, right. You have memory issues.\nWe have to write this down.\nNow it’s time for the preconsciousness. This is everything, that you suppress.\nEvery tear, every need of consolation in your favourite teddybear’s embrace.\nBut those teddys aren’t that cute anymore.\nBut you have your Superpower! You just have to press L2 (left trigger)!\n For a few seconds, 5 tops.\nThe longer, the more enemies you’ll strike down.\nSadly, every use of Superpower will tire you for a bit.\nYou won’t be able to shoot that fast and hard.\nIt will go back to normal after a while, though. Let’s begin!");
                }

                else if (lvl == 7)
                {
                    texts.Add("Wow, that was amazing! You’re making progress!\nIt’s time for the unsconsciousness.\nHere lay all the events, that you pushed away from the upper levels.\nIncluding your encounter with elephants.\nGood thing that you got out alright that time. That was close.\nElephants went wild a bit from that time. They’re very dangerous.\nIt’s the unconsciousness, after all.\nRemember that your Superpower regenerates!\nGood luck!");
                }

                else if (lvl == 9)
                {
                    texts.Add("Somebody wants to fight his fears!\nAnd that’s the attitude we like!\nYes, yes! You got it right.\nYour memory troubles are related directly \nto your biggest phobias.\n You know what it means.\nMister Nightmare.\nHe’s been sitting there for a long time.\nIt’s time to drive him out once and for all, and get some peace.\nRemember, all other monsters support his reign.\nIf you hurt him, they will rush to his aid.");
                }
            }
        // }
    }

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;TextLevel;CountDown;Text;ChangeTo;" + txt);
			textCD.text = txt;
			timeCD -= 1;
			yield return new WaitForSeconds(1);
		}
	}
}
