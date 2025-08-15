using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManageTutorials : MonoBehaviour 
{

	public Text textShowed;

    public Text textCD;
    
    // Key mappings (editable in inspector)
    public KeyCode skipLevelKey = KeyCode.T;
    public KeyCode skipTutorialKey = KeyCode.U;
    public KeyCode quitKey = KeyCode.Escape;
    
    [SerializeField] private List<string> texts = new List<string> ();
    int index;

    [SerializeField] private UserManager.LanguageOption lang;

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

    private void Awake()
    {
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;ID;" + SceneManager.GetActiveScene().buildIndex);
        lang = UserManager.lang;

        timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");
        
        this.index = 0;

        texts = new List<string> ();
        int currentSceneNumber = SceneManager.GetActiveScene().buildIndex;
        UpdateTexts(currentSceneNumber);
        textShowed.text = texts[this.index];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		}

        if (IsTimeToStartNextLevel())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        if (Input.GetKeyDown(quitKey))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Quit");
			Application.Quit(); // ignored in UnityEditor
		}

        if (Input.GetKeyDown(skipLevelKey))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;SkipLevel");
            StartCoroutine("CountDown");
        }

        else if (Input.GetKeyDown(skipTutorialKey))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;SkipTutorial");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
        }
    }

    public void RunGame() 
    {   
        StartCoroutine(CountDown());
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

    private void UpdateTexts(int lvl)
    {
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
    }

    private bool IsTimeToStartNextLevel()
    {
        return timeToStart < Time.timeSinceLevelLoad;
    }

    private IEnumerator LoseTime()
    {
        while (true) {
            string txt = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;TextLevel;CountDown;Text;ChangeTo;" + txt);
            textCD.text = txt;
            timeCD -= 1;
            yield return new WaitForSeconds(1);
        }
    }
}
