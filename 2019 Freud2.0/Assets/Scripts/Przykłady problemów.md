1. Duplikacja
EnemyHealth (lin. ~45, ~65, ~90): trzy TakeDamage* z podobną logiką.
``` 
    public void TakeDamageSuper (int amount)
    {
        if(isDead)
            return;

        enemyAudio.Play ();

        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;Death;By;SuperPower;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            TypicalDeath ();
            AddScore();
        }
    }

    public void TakeDamageLvlEnd (int amount)
    {
        if(isDead)
            return;

        // enemyAudio.Play (); // F2

        currentHealth -= amount;

        if(currentHealth <= 0)
        {
            LogManager.logManager.AddEvent(Time.time, "Enemy;Death;By;LevelEnd;ID;" + gameObject.GetInstanceID() + ";PositionX;" + gameObject.transform.position.x + ";PositionY;" + gameObject.transform.position.y + ";PositionZ;" + gameObject.transform.position.z + ";RotationX;" + gameObject.transform.rotation.x + ";RotationY;" + gameObject.transform.rotation.y + ";RotationZ;" + gameObject.transform.rotation.z + ";RotationW;" + gameObject.transform.rotation.w);
            Death ();
            // AddScore();
        }
    }

```
4.Naruszenia SOLID
UserManager (lin. ~20–140): Single Responsibility Principle złamane → język, wynik, ścieżki plików, no i oczywiście dane samego użytkownika
```
public class UserManager : MonoBehaviour 
{
	public static UserManager userManager;

    public enum LanguageOption
    {
		_English = 0,
		_Polish = 1
	};

    public static LanguageOption lang = LanguageOption._Polish;

    string userName; 
    string userID;

    string persDataPath;
    string userPath;

    int lvls = 0;
    int [] scoreLvl;

    bool used;

    void Start () 
	{
        persDataPath = Application.persistentDataPath;
        userPath = persDataPath + "\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";

        Cursor.visible = false;
		Screen.fullScreen = true;

        userID = "0000";
        userName = "Zawodnik0000";

        if (File.Exists(persDataPath + "\\currentNumber.txt")) 
        {
            StreamReader readtext = new StreamReader(persDataPath + "\\currentNumber.txt");
            userID = readtext.ReadLine();
            userName = "Zawodnik" + userID;
            readtext.Close();
        }

		LogManager.logManager.AddEvent(Time.time, "UserName;" + userName);
		LogManager.logManager.AddEvent(Time.time, "UserID;" + userID);

        userPath += userID + "\\";
        Directory.CreateDirectory(userPath);


        lvls = SceneManager.sceneCountInBuildSettings;
        scoreLvl = new int[lvls];
        Array.Clear(scoreLvl, 0, scoreLvl.Length);

        used = false;

        MakeThisTheOnlyUserManager();
    }

    void Update()
    {
        if (Time.time > 1200 && !used)
        {   
            used = true;
            ScoreUpdate(SceneManager.GetActiveScene().buildIndex, ScoreManager.score);
            LogManager.logManager.AddEvent(Time.time, "Score;GameEnd;Level;" + SceneManager.GetActiveScene().buildIndex + ";Value;" + ScoreManager.score);
            SceneManager.LoadScene("ScoreBoard");
        }
    }


    void MakeThisTheOnlyUserManager()
    {

        if(userManager == null)
        {
            DontDestroyOnLoad(gameObject);
            userManager = this;
        }

        else
        {
            if(userManager != this)
            {
                Destroy (gameObject);
            }
        }
	}
```

5.Nadmierna złożoność metod
MrNightmareEnemyManger Spawn() z 67-145, zawiera w sobie całą logikę kolejnych fal w zależności od poziomu zdrowia boss'a. Powinna być użyta strategia, czy to w postaci funkcji obsługących konkretne etapy, czy to w postaci klas swego rodzaju maszyny stanów. Jest 1 if i za nim 7 else ifów.

6.Nazewnictwo: 
CameraChange 51-53:
```
	bool usedFirst = false;
	bool usedSecond = false;
	bool usedThird = false;
```
Audiomanager 10 
```
	bool flagChecked = false;
```

LogManager.logManager.AddEvent: pierwsze nazwa klasy, drugie nazwa singletona (publiczne, to nie powinno mieć miejsca), trzecie, logger powinien logować a nie dodawać eventy (najlepiej przez statyczną funkcję)
```
public class LogManager : MonoBehaviour
{
    public static LogManager logManager;

    string fileName = "events.csv";

    double t1 = 0f;

    struct FmOEvent
    {
        float time;
        string eventType;

        public FmOEvent(float t, string eT)
        {
            time = t;
            eventType = eT;
        }

        public float GetTime()
        {
            return time;
        }

        public string GetEventType()
        {
            return eventType;
        }
    }

    List<FmOEvent> events = new List<FmOEvent>();

    void Awake () 
	{
        t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        MakeThisTheOnlyDontDestroyManager();
    }
 
    void MakeThisTheOnlyDontDestroyManager()
	{
        if(logManager == null)
		{
            DontDestroyOnLoad(gameObject);
            logManager = this;
        }

        else
		{
            if(logManager != this)
			{
                Destroy (gameObject);
            }
        }
	}

    void OnDestroy()
    {
        StreamWriter writer = File.AppendText(UserManager.userManager.GetUserPath() + fileName);

        writer.WriteLine(t1.ToString() + ";" + "UnixTime");

        foreach (FmOEvent e in events)
        {
            writer.WriteLine((e.GetTime()) + ";" + e.GetEventType());
        }

        writer.Close();
    }

    public void AddEvent(float time, string eventType)
    {
        FmOEvent newEvent = new FmOEvent((time * 1000), eventType); // *1000 to get time in miliseconds
        events.Add(newEvent);
    }
}
```
7. Zależności
PlayerHealth: powiązany z Animator, AudioSource, PlayerMovement, PlayerShooting, LogManager, SceneManager.
Sama klasa player health powinna wystawiać interfejs do zmiany ilości życia, przez który idzie wysokość zmiany i jak informacja o źródle zmiany, oraz wystawiać akcję Unity action najważniejsze zdarzenia, czyli gdy poziom zdrowia się zmienia, oraz gdy poziom zdrowia spadł do zera. W aktualnym stanie, klasa jest nie ręużywalna na innym projekcie. Przede wszystkim przez to że trzeba przenieść dwa inne dedykowane skrypty z tego projektu, ale też przez fakt że nie podpięte w inspektorze animacje elementy interfejsu oraz efekty audio będą skutkowały błędami brakujących referencji w konsoli, oraz wstrzymają wywołanie wszystkiego co było zaraz za użyciem wyżej wymienionych elementów 

8. Magic numbers
EnemyMovement nav.speed 23-30
```
    void Awake ()
    {
        player = GameObject.FindGameObjectWithTag ("Player").transform;
        playerHealth = player.GetComponent <PlayerHealth> ();
        enemyHealth = GetComponent <EnemyHealth> ();

        nav = GetComponent <UnityEngine.AI.NavMeshAgent> ();

        if (SceneManager.GetActiveScene().buildIndex == zeroLvlIndex)
        {
            nav.speed = 0f; // should not move in the begining of tutorial
        }
        else 
        {
            nav.speed = 3f;
        }
    }
```
ManageTutorials 300-370
Magic numbers to nie tylko liczby ale też enumy i stringi wpisane na sztywno
```
 if(lang.Equals(UserManager.LanguageOption._Polish))
    {
        txtTime = txtTimePL;

        if (lvl == 1)
        {
            texts.Add("Witamy na klejnej sesji.\nCo tak niewyraźnie wyglądasz?\nNie poznajesz nas. Ok, w porządku. Zaczniemy od początku.");
            texts.Add("Przyszedłeś tutaj ze względu na problemy z pamięcią, a my jesteśmy Twoimi terapeutami.\nBrzmi znajomo? Nie? No, dobra.");
        }

        else if (lvl == 3)
        {
            ...
```
Wstawione teksty na sztywno, które powinny być np w pliku z lokalizacjami, typu Scriptable object

9.Ghost Code (wcześniej Komentarze) 
ManageTutorials.cs, mający 384 linie, ma bloki wykomentowanego tekstu, z czego największy jest między liniami 192-298 (ma 106 linii). Sam skrypt posiada sumarycznie 185 linii ghost code komentarzy, oraz 65 pustych linii, 31 linii z tesktem tutoriala do wyświetlenia na ekranie, który powinien być w źródle a nie w kodzie, a także 52 linie z samą klamrą `{` lub `}` (20 uzasadnionych przypadków), co oznacza że ghost code stanowi `185/384=~48%` pliku, a po wyczyszczeniu, plik mógłby się skurczyć do `(384-185-65-31-32)/384=71/384=18%` zawartości z zachowaniem całej logiki, i być może da się zejść jeszcze bardziej z objętością stosując SRP i przenosząc część logiki do bardziej adekwatnych skryptów

CameraChange ma blok komentarza w liniach 92-200. Dodatkowo warto zauważyć że jednocześnie jest to funkcja Update wykomentowana, a wszystkie pozostałe funkcje są prywatne, więc skrypt jest całkowicie zbędny