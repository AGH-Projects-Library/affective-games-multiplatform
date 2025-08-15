using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace OldScripts
{
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ManageZeroLvl : MonoBehaviour 
{

	public PlayerMovement playerMovement;
	public CameraFollow cameraFollow;
	public PlayerShooting playerShooting;

	float startTime = 0.5f;
	float endTime = 6f;

	float timer = 0f;

	bool used = false;

	int i;
	List<string> alerts = new List<string> ();

	string tagTurnOff = "TurnOff";
	GameObject [] elementsHUD;

	UserManager.LanguageOption lang;

	void Awake () 
	{
		timer -= startTime;
		i = 0;

		lang = UserManager.lang;

        if (lang.Equals(UserManager.LanguageOption._English))
        {
            alerts.Add("Hello, it’s us again! \nWe’ll explain some things to you.");
            alerts.Add("This element indicates your health.");
            alerts.Add("This one shows your score.");
            alerts.Add("And here you can see your Superpower level.\nBut more on this later.");
            alerts.Add("We now unlock your movement.\nUse the analogue sticks!");
            alerts.Add("Left stick is for movement, \nright is for turning around.");
            alerts.Add("Go on, get moving! \nJust don’t break anything!");
            alerts.Add("In order to collect points you can pick up\n yellow shiny stars. \nJust walk through them.");
            alerts.Add("You can also kill the enemies.");
            alerts.Add("Just press \nR2 on the back of the game pad (right trigger). \nTo finish them off, shoot a few times.");
            alerts.Add("Remember, it’s all in your head. \nYou’ll never run out of ammo.");
            alerts.Add("You get less points for stars than \nfor killing the enemies.");
            alerts.Add("And pay attention to the score. \nIt keeps changing, you know.");
            alerts.Add("To get to the next level, \nyou need to collect required points.");
            alerts.Add("We won’t tell you how many exactly, though.");
            alerts.Add("To complete the training, collect all stars \nor kill all enemies.");
            alerts.Add("These ones are harmless. \nBut you better watch out for the real ones!");
            alerts.Add("The darker they are,\nthe stronger they hit you.");
            alerts.Add("Good luck and see you soon!");
        }
        else if (lang.Equals(UserManager.LanguageOption._Polish))
        {
            alerts.Add("Cześć, to znowu my!\nWyjaśnimy Ci kilka kwestii.");
			alerts.Add("Ten element wskazuje Twoje życie.");
			alerts.Add("Ten pokaże Ci Twój wynik.");
			alerts.Add("A tutaj widzisz SuperMoc.\nAle o tym będzie później.");
			alerts.Add("Odblokowujemy możliwość poruszania się.\nUżyj joysticków kontrolera!");
			alerts.Add("Lewy joystick to ruch,\nprawy joystick to obrót.");
			alerts.Add("No, śmiało, ruszaj się!\nTylko niczego nie zepsuj!");
			alerts.Add("Aby zdobywać punkty możesz zbierać\n żółte, świecące gwiazdki.\nPo prostu w nie wejdź.");
			alerts.Add("Możesz też zabijać przeciwników.");
			alerts.Add("Po prostu naciśnij\nprzycisk R2 z tyłu pada (prawy trigger).\nAby ich wykończyć, strzel kilka razy.");
			alerts.Add("Pamiętaj, że to Twoja głowa.\nAmunicja nie skończy się nigdy.");
			alerts.Add("Za gwiazdki dostaniesz mniej punktów niż\nza eliminowanie przeciwników.");
			alerts.Add("I zwróć uwagę na informację o punktach.\nBo wiesz, ona się zmienia.");
			alerts.Add("Aby awansować na kolejne poziomy\nmusisz zebrać wystarczającą liczbę punktów.");
			alerts.Add("Ale nie powiemy Ci ile.");
			alerts.Add("Aby zakończyć szkolenie zbierz wszystkie gwiazdki\nlub zniszcz wszystkich przeciwników.");
			alerts.Add("Ci tutaj są niegroźni.\nAle na prawdziwych lepiej uważaj!");
			alerts.Add("Im są ciemniejsi,\ntym są silniejsi.");
			alerts.Add("Powodzenia i do zobaczenia niedługo!");
        }

		elementsHUD = GameObject.FindGameObjectsWithTag(tagTurnOff);
		foreach (var el in elementsHUD)
		{
			el.gameObject.SetActive(false);
		}
	}
	
	void Update () 
	{
		timer += Time.deltaTime;
		
		if(i < alerts.Count)
		{
			ShowAlerts(endTime - 1.0f, alerts[i]);
		}

		if (i == 2)
		{
			elementsHUD[1].gameObject.SetActive(true);
		}

		else if (i == 3)
		{
			elementsHUD[2].gameObject.SetActive(true);
			elementsHUD[2].GetComponent<Text>().text = "0";

			// F2
			// if (lang.Equals(UserManager.LanguageOption._English))
			// {
			// 	elementsHUD[2].GetComponent<Text>().text = "TRAGIC";
			// }
			// else if (lang.Equals(UserManager.LanguageOption._Polish))
			// {
			// 	elementsHUD[2].GetComponent<Text>().text = "TRAGICZNIE";
			// }
		}

		else if (i == 4)
		{
			elementsHUD[0].gameObject.SetActive(true);
		}

		else if(i == 5)
		{
			playerMovement.enabled = true;
			cameraFollow.enabled = true;
		}

		else if (i == 10)
		{
			playerShooting.enabled = true;	
		}

		else if (i == 14)
		{
			GameObject [] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
				enemy.GetComponent<EnemyMovement>().SetSpeed(1);
            }
		}
	}

	void ShowAlerts(float time, string alert)
	{
		if (timer > startTime && !used)
		{
			LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + time + ";Content;" + alert.Replace("\n", ""));
			StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(time, alert));
			i++;
			used = !used;
		}

		if (timer > startTime + time)
		{
			used = !used;
			timer = 0f;
		}
	}
}

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;

public class ManageScoreBoard : MonoBehaviour 
{
	public Text textBestPlayers;

	public Text closeTxt;
	// Key mappings (editable in inspector)
	public KeyCode keyEscape = KeyCode.Escape;
	public KeyCode keyR = KeyCode.R;
	public KeyCode keyS = KeyCode.S;
	
	string pathScores;
	Dictionary<int, List<string>> bestPlayers = new Dictionary<int, List<string>> ();

	string tagIndestructible = "DontDestroyObject";
	string tagBITalino = "BITalino";

	int indexIntroduction = 0;
	int indexFirstLevel = 3;

	public float timeToStart = 15;

	float timeCD;

	string closeText = "";

	string persDataPath;

	void Awake()
	{
		persDataPath = Application.persistentDataPath;
		pathScores = persDataPath + "\\Scores.csv";
		
		timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");

		string namePlayer = UserManager.userManager.GetUserName();
		int scorePlayer = UserManager.userManager.GetCumulatedScore();

		if(UserManager.lang == UserManager.LanguageOption._English)
		{
			closeText = String.Format("<color=red>Your name: {0}.\n Your score: {1}.\n</color> The game will end in: ", namePlayer, scorePlayer);
		}

		else if(UserManager.lang == UserManager.LanguageOption._Polish)
		{
			closeText = String.Format("<color=red>Twoja nazwa: {0}.\n Twój wynik: {1}.\n</color> Gra zakończy się za: ", namePlayer, scorePlayer);
		}

		ReadBestPlayers();
		AddNewPlayer(namePlayer, scorePlayer);
		PrintBestPlayers();
		UserManager.userManager.ScoreZero();
		LogManager.logManager.AddEvent(Time.time, "ScoreBoard;Show");
		WriteBestPlayers();

		
	}

	void Update()
	{
		if (timeCD < 0)
        {
            Application.Quit();
        }

		if(Input.GetKeyDown(keyEscape))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
        }

		else if(Input.GetKeyDown(keyR))
        {
			LogManager.logManager.AddEvent(Time.time, "Key;R");
			DestroyIndestructible();
	        SceneManager.LoadScene(indexIntroduction);
        }

		else if(Input.GetKeyDown(keyS))
        {
			LogManager.logManager.AddEvent(Time.time, "Key;S");
	        SceneManager.LoadScene(indexFirstLevel);
        }
	}

	void DestroyIndestructible()
	{
		GameObject [] indestructible = GameObject.FindGameObjectsWithTag(tagIndestructible);
		foreach (GameObject i in indestructible)
		{
			Destroy(i);
		}

		Destroy(GameObject.FindWithTag(tagBITalino));
	}

	void ReadBestPlayers()
	{
		if (File.Exists(pathScores))
		{
			StreamReader reader = new StreamReader(pathScores);
			string line;
			while((line = reader.ReadLine()) != null)  
			{
				string[] elements = line.Split(';');

				int score = 0;
				Int32.TryParse(elements[0], out score);

				List<string> names = new List<string> ();
				for (int i = 1; i < (elements.Length - 1); i++)
				{
					names.Add(elements[i]);
				}

				bestPlayers.Add(score, names);
			}

			reader.Close();
		}
	}


	void AddNewPlayer(string cN, int cS)
	{
	    // F2
		// bool bitalinoUse = BitalinoController.bitalinoController.bitalinoUse;
		// string use = bitalinoUse ? "1" : "0";
		string currentName = cN; // + " (" + use + ")";     // F2
		int currentScore = cS;

		if (!bestPlayers.ContainsKey(currentScore))
		{
			List<string> players = new List<string> ();
			players.Add(currentName);
			bestPlayers.Add(currentScore, players);
		}

		else
		{
			List<string> players = new List<string> ();
			players = bestPlayers[currentScore];
			bestPlayers.Remove(currentScore);
			players.Add(currentName);
			bestPlayers.Add(currentScore, players);
		}
	}


	void PrintBestPlayers()
	{
		string bests = "";
		int i = 0;

		List<int> keyList = bestPlayers.Keys.ToList();
		keyList.Sort();
		keyList.Reverse();
		
		foreach (int key in keyList)
		{
			foreach (string el in bestPlayers[key])
			{
				i++;
				bests += String.Format("{0}. {1} {2}\n" , i, el, key); 
			}
		}

		textBestPlayers.text = bests;
	}


	void WriteBestPlayers()
	{
		if(File.Exists(pathScores))
		{
			File.Delete(pathScores);
		}

		StreamWriter writer = new StreamWriter(pathScores);

		List<int> keyList = bestPlayers.Keys.ToList();
		foreach (int key in keyList)
		{
			string bests = "";
			foreach (string el in bestPlayers[key])
			{
				bests += String.Format("{0};", el);
			}

			string line = String.Format("{0};", key);
			line += bests;
			
			writer.WriteLine(line);
		}

		writer.Close();
	}

	IEnumerator LoseTime()
	{
		while(true)
		{
			string txt = closeText + timeCD.ToString() + "s";
			LogManager.logManager.AddEvent(Time.time, "Game;ScoreBoard;CountDown;Text;ChangeTo;" + txt);

            yield return new WaitForSeconds(1);
			closeTxt.text = txt;
			timeCD -= 1;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ManageIntroduction : MonoBehaviour {

	// UI
	public Text textShowed;

	// Public UI hooks for editor wiring
	public UnityEvent OnIntroductionFinished;
	public UnityEvent OnLanguageChanged;
	// Configurable input mappings (inspector-friendly)
	public string fireButton2 = "Fire2"; // mapped to English welcome progression
	public string fireButton3 = "Fire3"; // mapped to Polish progression
	public KeyCode keyS = KeyCode.S;     // quick-load level 0 shortcut

	// Language-specific welcome text
	[Tooltip("Welcome text for English")]
	public string welcomeTextEng = "Welcome in the Freud2.0!";
	[Tooltip("Welcome text for Polish")]
	public string welcomeTextPl = "Witaj w Freud2.0!";

	// Language-aware strings
	[Tooltip("Time label when the game will start (English)")]
	public string timeLabelENG = "Time to level begin: ";
	[Tooltip("Time label when the game will start (Polish)")]
	public string timeLabelPL = "Czas do rozpoczęcia poziomu: ";

	// Timing controls
	public float timeToStart = 30;
	public float timeWait = 1f;
	int timeCD;
	string txtTime = "";
	// language tracking
	UserManager.LanguageOption lang;
 
	[SerializeField] public LanguageOption currentLang;
	[System.Serializable]
	public enum LanguageOption { English, Polish }
	// Expose a couple of helper for editor wiring
	public UnityEvent OnCalibrationSkip;

	[SerializeField] public string txtTimePL;
	[SerializeField] public string txtTimeENG;

	[Tooltip("Welcome text shown on startup")]
	public string welcomeTextLabelENG = "Welcome in the Freud2.0!";
	[Tooltip("Welcome text shown on startup (Polish)")]
	public string welcomeTextLabelPL = "Witaj w Freud2.0!";

	// A small reference to a language manager if you have one
	// (we'll keep existing behavior and just expose hooks)

	public float timeCalibrationCheck = 0.5f;

	void Awake()
    {
        timeCD = (int)timeToStart;
	    StartCoroutine(LoseTime());

        // Log start load
        LogManager.logManager.AddEvent(Time.time, "Scene;Load;" + SceneManager.GetActiveScene().buildIndex);

        // initial language
        lang = UserManager.lang;
        ChangeLanguage();
    }

    void Update() 
    {
        if(Input.GetButtonDown (fireButton2)) // use public mapping for Fire2
        {
            LogManager.logManager.AddEvent(Time.time, "Key;X");
            lang = UserManager.LanguageOption._English;
            UserManager.lang = lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }

        if(Input.GetButtonDown (fireButton3)) // use public mapping for Fire3
        {
            LogManager.logManager.AddEvent(Time.time, "Key;O");
            lang = UserManager.LanguageOption._Polish;
            UserManager.lang = lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }

        if (Input.GetKeyDown(keyS)) // use public key binding for S
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			LoadLvl0();
		}

        // Auto-load into lvl 0 after countdown
        if (CheckIfTimeToStartElapsed()) LoadLvl0();

        if (Input.GetKeyDown(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        if (lang != UserManager.lang)
        {
            lang = UserManager.lang;
            ChangeLanguage();
            OnLanguageChanged?.Invoke();
        }
    }

    void ChangeLanguage()
    {
        switch(lang)
        {
            case UserManager.LanguageOption._English:
            {
                textShowed.text = welcomeTextLabelENG;
                currentLang = UserManager.LanguageOption._English;
                currentLang = UserManager.LanguageOption._English;
                currentTimeLabel = timeLabelENG;
                txtTimeENG = timeLabelENG;
                // time label for countdown
                // If you want to update any other UI texts, set them here
                break;
            }

            case UserManager.LanguageOption._Polish:
            {
                textShowed.text = welcomeTextLabelPL;
                currentLang = UserManager.LanguageOption._Polish;
                currentTimeLabel = timeLabelPL;
                txtTimePL = timeLabelPL;
                break;
            }
        }
    }

	// Call to advance to the gameplay after intro
	public void LoadTutorial ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());
        OnIntroductionFinished?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 3);
	}

	public void LoadLvl0 ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());
        OnIntroductionFinished?.Invoke();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
	}

	// Helpers
    bool CheckIfTimeToStartElapsed() { return timeToStart < Time.timeSinceLevelLoad; }
    void TurnOffButtons () { /* keep; minimal cleanup placeholder for now */ }
    IEnumerator Wait() { yield return new WaitForSeconds(timeWait); }

	// Coroutine for countdown display
	IEnumerator LoseTime()
	{
		while(true)
		{
            string toShow = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Introduction;CountDown;Text;ChangeTo;" + toShow);
			textShowed.text = toShow;
			timeCD -= 1;
			yield return new WaitForSeconds(1);
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class ManageEnd : MonoBehaviour {
    [Header("UI")]

	public Text textCD;
	// Key mappings
	public KeyCode keyEscape = KeyCode.Escape;
	public KeyCode keySkipToScoreBoard = KeyCode.T;
    
	// Editor-wirable events
	public UnityEvent OnScoreBoardRequested = new UnityEvent();

	// Language/time display customization
	[Tooltip("Text label shown before the countdown (English)")]
	public string timeLabelENG = "Time to show results: ";
	[Tooltip("Text label shown before the countdown (Polish)")]
	public string timeLabelPL = "Czas za jaki pokażemy Ci wyniki: ";

	// Publicly editable timing and scene-wiring
	[SerializeField] private float timeToStart = 15f;
	[SerializeField] private string nameScoreBoard = "ScoreBoard";

	private int timeCD;
	private UserManager.LanguageOption currentLang;

	// Backing for displaying countdown
	string currentTimeLabel;

	 void Awake()
	{
		// Init language/text labels
		currentLang = UserManager.lang;
		currentTimeLabel = (currentLang == UserManager.LanguageOption._English) ? timeLabelENG : timeLabelPL;

		// Countdown
		timeCD = (int)timeToStart;
		StartCoroutine(LoseTime());

		LogManager.logManager.AddEvent(Time.time, "Load;Scene;ID" + SceneManager.GetActiveScene().buildIndex);
	}

    void Update()
    {
        if (CheckIfTimeToScoreBoard()) TriggerScoreBoard();
        if (CheckIfEscapePressed()) Application.Quit();
        if (CheckIfSkipToScoreBoard()) TriggerScoreBoard();
    }

    private bool CheckIfTimeToScoreBoard() => timeCD < 0;
    private bool CheckIfEscapePressed() => Input.GetKey(keyEscape);
    private bool CheckIfSkipToScoreBoard() => Input.GetKeyDown(keySkipToScoreBoard);

    public void ShowScoreBoard() 
    {   
        // Fire event for editor wiring
        OnScoreBoardRequested?.Invoke();
        if (currentLang != UserManager.lang)
        {
            UpdateLanguage();
        }
        // Fallback if no listeners wired: load directly
        if (OnScoreBoardRequested == null || OnScoreBoardRequested.GetPersistentEventCount() == 0)
        {
            SceneManager.LoadScene(nameScoreBoard);
        }
	}

    private void UpdateLanguage()
    {
        currentLang = UserManager.lang;
        currentTimeLabel = (currentLang == UserManager.LanguageOption._English) ? timeLabelENG : timeLabelPL;
        textCD.text = currentTimeLabel + timeCD + "s";
    }

    IEnumerator LoseTime()
	{
		while(true)
		{
            string txt = currentTimeLabel.Replace("Time to show results:", "Time:") + timeCD + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;End;CountDown;Text;ChangeTo;" + txt);
            yield return new WaitForSeconds(1);
			textCD.text = txt;
			timeCD -= 1;
		}
	}
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    [SerializeField] private UnityEngine.UI.Text textGO;
    float timeCD = 3.0f;
    
    [SerializeField] private string textGOvPL = "Przegrałeś!\nRestart poziomu za: ";
    [SerializeField] private string textGOvENG = "You lost!\nThe level will restart in: ";
    [SerializeField] private UserManager.LanguageOption lang;

    Animator anim;
    [SerializeField] private string animationName = "GameOver";
    
    string nameScoreBoard = "ScoreBoard";

    bool flagUsed = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        textGO.text = GetGameOverText();
    }

    private string GetGameOverText()
    {
        return lang == UserManager.LanguageOption._Polish ? textGOvPL : textGOvENG;
    }


    void Update()
    {
        if (playerHealth.currentHealth <= 0 && timeCD <= 0) UpdateScoreAndRestartLevel();
        else if (playerHealth.currentHealth <= 0 && !flagUsed) HandleGameOver();
        else if (Input.GetKey(KeyCode.B)) HandleScoreboardLoad();
    }

    private bool IsPlayerDead() => playerHealth.currentHealth <= 0;
    private bool IsTimeUp() => timeCD <= 0;

    private void UpdateScoreAndRestartLevel()
    {
        UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
        LogManager.logManager.AddEvent(Time.time, "Score;PlayerDeath;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleGameOver()
    {
        flagUsed = true;
        anim.SetTrigger(animationName);
        LogManager.logManager.AddEvent(Time.time, "Game;Over;Animation;PlayerDeath");
        timeCD = 4.0f;
        StartCoroutine(LoseTime());
    }
    private void HandleScoreboardLoad()
    {
        UserManager.userManager.ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
        LogManager.logManager.AddEvent(Time.time, "Key;B");
        SceneManager.LoadScene(nameScoreBoard);
    }

    private IEnumerator LoseTime() {
        while (true) {
            string txt = GetGameOverText() + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Over;CountDown;Text;ChangeTo;" + txt.Replace("\n", ""));
            textGO.text = txt;
            yield return new WaitForSeconds(1);
            timeCD -= 1;
        }
    }
}

using System.Collections;
using UnityEngine;

public class AffectiveEnemyManager : MonoBehaviour 
{
    [SerializeField] private int affectiveSpawnTimeMedium = 60;
    [SerializeField] private int affectiveSpawnTimeMediumMax = 80;
    [SerializeField] private int affectiveSpawnTimeHard = 90;
    [SerializeField] private int affectiveSpawnTimeHardMax = 180;
    [SerializeField] private int preparationTime = 5;
    [SerializeField] private float spawnTime = 3f;
    [SerializeField] private float invokeTime = 5f;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject[] enemy;
    [SerializeField] private Transform[] spawnPoints;

    private bool checkedMedium = false;
    private bool checkedHard = false;
    private float timerSpawnMax = 0f;
    private float remainTime = 5f;
    private float alertTime = 5f;
    private string alertMessageNonAffectivePl = "Nadchodzą kolejne potwory!\nUważaj!";
    private string alertMessageNonAffectiveEng = "More and more monsters are coming!\nWatch out!";
    private string alertMessageNonAffective = "";

    private void Awake()
    {
        alertMessageNonAffective = UserManager.lang.Equals(UserManager.LanguageOption._English) ? alertMessageNonAffectiveEng : alertMessageNonAffectivePl;
    }

    private void Start() => InvokeRepeating("Spawn", invokeTime, spawnTime);

    private void Update()
    {
        if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - remainTime)) timerSpawnMax += Time.deltaTime;
        if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeMedium - preparationTime) && !checkedMedium) ShowAlert(alertMessageNonAffective, checkedMedium = true);
        else if (Time.timeSinceLevelLoad >= (affectiveSpawnTimeHard - preparationTime) && !checkedHard) ShowAlert(alertMessageNonAffective, checkedHard = true);
    }

    private void Spawn()
    {
        if (playerHealth.currentHealth <= 0f || GameObject.FindGameObjectsWithTag("Enemy").Length >= EnemyManager.maxEnemies) return;
        int enemyIndex = Random.Range(0, enemy.Length);
        if (Time.timeSinceLevelLoad >= affectiveSpawnTimeMedium) SpawnEnemy(enemyIndex, Random.Range(0, (int)(spawnPoints.Length / Random.Range(1, 3))), "AdditionalMediumSpawn");
        if (Time.timeSinceLevelLoad >= affectiveSpawnTimeHard) SpawnEnemy(enemyIndex, Random.Range(0, (int)(spawnPoints.Length / Random.Range(1, 2))), "AdditionalHardSpawn");
    }

    private void SpawnEnemy(int enemyIndex, int spawnPointIndex, string mechanic)
    {
        Instantiate(enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
        LogManager.logManager.AddEvent(Time.time, $"Enemy;Spawn;Type;{enemyIndex};ID;{gameObject.GetInstanceID()};SpawnPoint;{spawnPoints[spawnPointIndex].name};Mechanic;{mechanic}");
    }

    private void ShowAlert(string message, bool checkedFlag)
    {
        LogManager.logManager.AddEvent(Time.time, $"Alert;Show;Time;{alertTime};Content;{message.Replace("\n", "")}");
        StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(alertTime, message));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MrNightmareEnemyManager : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemyHealth mrNightmareHealth;
    [SerializeField] private AffectiveEnemyManager affectiveEnemyManager;
    [SerializeField] private GameObject[] enemy;
    public Transform[] spawnPoints;

    [SerializeField] private int[] healthLevels = { 900, 600, 300, 30 };
    [SerializeField] private bool[] usedLevels = new bool[4];

    [SerializeField] private int[] helpNumbers = { 5, 7, 9, 11 };
    [SerializeField] private List<float> helpWaits = new List<float> { 3f, 2f, 4f, 1f };

    [field: SerializeField] public float AlertTime { get; private set; } = 5f;
    [SerializeField] private string[] alertMessages = { "Przyzwano sojuszników! Uważaj!", "Supporters are coming! Watch out!" };
    private string alertMessage;

    [SerializeField] private List<float> timers = new List<float> { 0f, 0f, 0f, 0f };

    [SerializeField] private bool enemySpeedFlag, affectiveSpawnMediumFlag, affectiveSpawnHardFlag;

    private bool ShouldSpawnEnemy()
    {
        return playerHealth.currentHealth > 0f && mrNightmareHealth.currentHealth <= healthLevels[0] && mrNightmareHealth.currentHealth > 0f && (
            (usedLevels[3] && timers[3] > (helpWaits[3] + 1f) && !affectiveSpawnHardFlag) ||
            (!usedLevels[3] && mrNightmareHealth.currentHealth <= healthLevels[3]) ||
            (usedLevels[2] && timers[2] > (helpWaits[2] + 1f) && !enemySpeedFlag) ||
            (!usedLevels[2] && mrNightmareHealth.currentHealth <= healthLevels[2]) ||
            (usedLevels[1] && timers[1] > (helpWaits[1] + 1f) && !affectiveSpawnMediumFlag) ||
            (!usedLevels[1] && mrNightmareHealth.currentHealth <= healthLevels[1]) ||
            (!usedLevels[0] && mrNightmareHealth.currentHealth <= healthLevels[0])
        );
    }

    private void SpawnEnemies()
    {
        if (ShouldSpawnEnemy())
        {
            UpdateTimers();
            if (ShouldSpawnHardEnemies()) { SpawnHardEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel3()) { SpawnEnemiesAtLevel3(); }
            else if (ShouldSpeedUpEnemies()) { SpeedUpEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel2()) { SpawnEnemiesAtLevel2(); }
            else if (ShouldSpawnMediumEnemies()) { SpawnMediumEnemies(); }
            else if (ShouldSpawnEnemiesAtLevel1()) { SpawnEnemiesAtLevel1(); }
            else if (ShouldSpawnEnemiesAtLevel0()) { SpawnEnemiesAtLevel0(); }
        }
    }

    private void SpawnEnemiesAtLevel(int level)
    {
        usedLevels[level] = true;
        StartCoroutine(CallForHelp(helpNumbers[level], helpWaits[level], "MrNightmareSpawn"));
        LogManager.logManager.AddEvent(Time.time, "Alert;Show;Time;" + AlertTime + ";Content;" + alertMessage.Replace("\n", ""));
        StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp(AlertTime, alertMessage));
    }

    private IEnumerator CallForHelp(int number, float waitTime, string mechanic)
    {
        for (int i = 0; i < number; i++)
        {
            int enemyIndex = Random.Range(0, enemy.Length);
            int spawnPointIndex = Random.Range(0, spawnPoints.Length);
            Instantiate(enemy[enemyIndex], spawnPoints[spawnPointIndex].position, spawnPoints[spawnPointIndex].rotation);
            LogManager.logManager.AddEvent(Time.time, "Enemy;Spawn;Type;" + enemyIndex + ";ID;" + gameObject.GetInstanceID() + ";SpawnPoint;" + spawnPoints[spawnPointIndex].name + ";Mechanic;" + mechanic);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void Awake() => alertMessage = UserManager.lang.Equals(UserManager.LanguageOption._English) ? alertMessages[1] : alertMessages[0];

    private bool ShouldSpawnHardEnemies()
    {
        return usedLevels[3] && timers[3] > (helpWaits[3] + 1f) && !affectiveSpawnHardFlag;
    }

    private void SpawnHardEnemies()
    {
        affectiveSpawnHardFlag = true;
        affectiveEnemyManager.invokeTime = 0f;
        affectiveEnemyManager.affectiveSpawnTimeHard = (int)Time.timeSinceLevelLoad;
        affectiveEnemyManager.affectiveSpawnTimeHardMax = (int)Time.timeSinceLevelLoad + 20;
    }

    private bool ShouldSpawnEnemiesAtLevel3()
    {
        return !usedLevels[3] && mrNightmareHealth.currentHealth <= healthLevels[3];
    }

    private void SpawnEnemiesAtLevel3()
    {
        SpawnEnemiesAtLevel(3);
    }

    private bool ShouldSpeedUpEnemies()
    {
        return usedLevels[2] && timers[2] > (helpWaits[2] + 1f) && !enemySpeedFlag;
    }

    private void SpeedUpEnemies()
    {
        enemySpeedFlag = true;
    }

    private bool ShouldSpawnEnemiesAtLevel2()
    {
        return !usedLevels[2] && mrNightmareHealth.currentHealth <= healthLevels[2];
    }

    private void SpawnEnemiesAtLevel2()
    {
        timers[2] = 0f;
        SpawnEnemiesAtLevel(2);
    }

    private bool ShouldSpawnMediumEnemies() => usedLevels[1] && timers[1] > (helpWaits[1] + 1f) && !affectiveSpawnMediumFlag;
    private void SpawnMediumEnemies()
    {
        affectiveSpawnMediumFlag = true;
        affectiveEnemyManager.invokeTime = 0f;
        affectiveEnemyManager.affectiveSpawnTimeMedium = (int)Time.timeSinceLevelLoad;
        affectiveEnemyManager.affectiveSpawnTimeMediumMax = (int)Time.timeSinceLevelLoad + 20;
    }

    private bool ShouldSpawnEnemiesAtLevel1()
    {
        return !usedLevels[1] && mrNightmareHealth.currentHealth <= healthLevels[1];
    }

    private void SpawnEnemiesAtLevel1()
    {
        SpawnEnemiesAtLevel(1);
    }

    private bool ShouldSpawnEnemiesAtLevel0()
    {
        return !usedLevels[0] && mrNightmareHealth.currentHealth <= healthLevels[0];
    }

    private void SpawnEnemiesAtLevel0()
    {
        SpawnEnemiesAtLevel(0);
    }

    private void UpdateTimers()
    {
        for (int i = 0; i < usedLevels.Length; i++)
        {
            if (usedLevels[i])
                this.timers[i] += Time.deltaTime;
        }
    }
}
}