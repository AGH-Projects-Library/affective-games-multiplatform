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
            alerts.Add("Hello, it is us again!\nWe will explain a few things to you.");
			alerts.Add("This part indicates your health level.");
			alerts.Add("This one shows your score.\nIn a way. You will figure it out.");
			alerts.Add("And here you can see your SuperPower charge.");
			alerts.Add("But we will speak more on this later.");
			alerts.Add("We now unlock your ability to move.");
			alerts.Add("WSAD, rings a bell?\nW - up. S - down. A - left. D - right.");
			alerts.Add("Go on, get moving!\nJust do not touch anything!");
			alerts.Add("Ok, good. Now, let’s fight.\nEliminate those sweet bunnies.");
			alerts.Add("Just press the left mouse button.\nYou have to shoot a few times.");
			alerts.Add("Remember, this is your head.\nYou will never run out of amunition.");
			alerts.Add("Pay attention to the information on your score.\nBecause, you know, it changes.");
			alerts.Add("Time for a challenge.\nDefeat all 5 enemies.");
			alerts.Add("These ones are harmless.\nBut better watch out for the real ones!");
			alerts.Add("The darker light color they emit,\nthe stronger they are.");
			alerts.Add("Okaaay, so we will get going.\nSee you later... We guess?");
			alerts.Add("Yeees, that is a good farewell.\nYou can see us, after all.");
        }
        else if (lang.Equals(UserManager.LanguageOption._Polish))
        {
            alerts.Add("Cześć, to znowu my!\nWyjaśnimy Ci kilka kwestii.");
			alerts.Add("Ten element wskazuje Twoje życie.");
			alerts.Add("Ten pokaże Ci Twój wynik.\nNo, tak jakby. Jakoś sobie poradzisz.");
			alerts.Add("A tutaj widzisz SuperMoc.");
			alerts.Add("Ale o tym będzie później.");
			alerts.Add("Odblokowujemy możliwość poruszania się.");
			alerts.Add("WSAD, mówi Ci to coś?\nW - w górę. S - w dół. A - w lewo. D - w prawo.");
			alerts.Add("No, śmiało, ruszaj się!\nTylko niczego nie zabieraj!");
			alerts.Add("No, dobra, dobra. To teraz walka.\nZniszcz te słodkie zajączki.");
			alerts.Add("Po prostu naciśnij lewy przycisk myszy.\nMusisz strzelić kilka razy.");
			alerts.Add("Pamiętaj, że to Twoja głowa.\nAmunicja nie skończy się nigdy.");
			alerts.Add("I zwróć uwagę na informację o punktach.\nBo wiesz, ona się zmienia.");
			alerts.Add("A teraz wyzwanie.\nPokonaj wszystkich 5 przeciwników.");
			alerts.Add("Ci tutaj są niegroźni.\nAle na prowadziwych lepiej uważaj!");
			alerts.Add("Im ciemniej świecą,\ntym są mocniejsi.");
			alerts.Add("Eee, to my się zmywamy.\nDo zobaczenia?");
			alerts.Add("Taaak, to dobre pożegnanie.\nW sumie to nas widzisz.");
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
			ShowAlerts(endTime, alerts[i]);
		}

		if (i == 2)
		{
			elementsHUD[1].gameObject.SetActive(true);
		}

		else if (i == 3)
		{
			elementsHUD[2].gameObject.SetActive(true);
			if (lang.Equals(UserManager.LanguageOption._English))
			{
				elementsHUD[2].GetComponent<Text>().text = "TRAGIC";
			}
			else if (lang.Equals(UserManager.LanguageOption._Polish))
			{
				elementsHUD[2].GetComponent<Text>().text = "TRAGICZNIE";
			}
		}

		else if (i == 4)
		{
			elementsHUD[0].gameObject.SetActive(true);
		}

		else if(i == 7)
		{
			playerMovement.enabled = true;
			cameraFollow.enabled = true;
		}

		else if (i == 10)
		{
			playerShooting.enabled = true;	
		}

		else if (i == 13)
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
			LogManager.logManager.AddEvent(Time.time, "Alert;Lvl0Index;" + i);
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
