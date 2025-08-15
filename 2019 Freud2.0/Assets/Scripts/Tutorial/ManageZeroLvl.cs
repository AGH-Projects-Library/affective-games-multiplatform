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

	UserManager.Language lang = UserManager.Instance.language;

	void Awake () 
	{
		timer -= startTime;
		i = 0;

		lang = UserManager.Instance.language;

        if (lang == UserManager.Language.English)
        {
            alerts.Add("Hello, it's us again! \nWe'll explain some things to you.");
            alerts.Add("This element indicates your health.");
            alerts.Add("This one shows your score.");
            alerts.Add("And here you can see your Superpower level.\nBut more on this later.");
            alerts.Add("We now unlock your movement.\nUse the analogue sticks!");
            alerts.Add("Left stick is for movement, \nright is for turning around.");
            alerts.Add("Go on, get moving! \nJust don't break anything!");
            alerts.Add("In order to collect points you can pick up\n yellow shiny stars. \nJust walk through them.");
            alerts.Add("You can also kill the enemies.");
            alerts.Add("Just press \nR2 on the back of the game pad (right trigger). \nTo finish them off, shoot a few times.");
            alerts.Add("Remember, it's all in your head. \nYou'll never run out of ammo.");
            alerts.Add("You get less points for stars than \nfor killing the enemies.");
            alerts.Add("And pay attention to the score. \nIt keeps changing, you know.");
            alerts.Add("To get to the next level, \nyou need to collect required points.");
            alerts.Add("We won't tell you how many exactly, though.");
            alerts.Add("To complete the training, collect all stars \nor kill all enemies.");
            alerts.Add("These ones are harmless. \nBut you better watch out for the real ones!");
            alerts.Add("The darker they are,\nthe stronger they hit you.");
            alerts.Add("Good luck and see you soon!");
        }
        else if (lang == UserManager.Language.Polish)
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
			// if (lang.Equals(UserManager.Language._English))
			// {
			// 	elementsHUD[2].GetComponent<Text>().text = "TRAGIC";
			// }
			// else if (lang.Equals(UserManager.Language._Polish))
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
		if (timer > startTime && !used && UserManager.Instance.language == lang)
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
