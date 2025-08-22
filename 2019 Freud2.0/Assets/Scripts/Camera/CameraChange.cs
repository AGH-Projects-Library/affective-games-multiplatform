using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CameraChange : MonoBehaviour {

	public Camera cameraMain;
	public GameObject player;
	public GameObject mainLight;
	PlayerHealth playerHealth;

	public Image imageCrossHair;
	public float cameraTime = 200f;
	public float waitBeginTime = 100000f;


	string firstAlertPl = "Utracono połączenie.";
	string secondAlertPl = "Wyłączanie interfejsu.";
	string thirdAlertPl = "Trzymaj się!";
	string firstAlertEng = "Connection lost.";
	string secondAlertEng = "Turning off the interface.";
	string thirdAlertEng = "Hold on!";

	string firstAlertFPSPl = "Przywrócono połączenie.";
	string secondAlertFPSPl = "Przywracanie interfejsu.";
	string thirdAlertFPSPl = "Witaj ponownie!";
	string firstAlertFPSEng = "Connection restored.";
	string secondAlertFPSEng = "Turning on the interface.";
	string thirdAlertFPSEng = "Hello again!";

	string firstAlertFPS = "";
	string secondAlertFPS = "";
	string thirdAlertFPS = "";
	string firstAlert = "";
	string secondAlert = "";
	string thirdAlert = "";


	public GameObject [] elementsHUD;
	string turnOffTag = "TurnOff";
	float turnOffTime = 0.5f;

	float timer;
	bool fps;

	int currentSceneNumber;
	int thirdLvlIndex = 8;

	bool usedFirst = false;
	bool usedSecond = false;
	bool usedThird = false;

	bool dontChange = false;

	void Awake()
	{	
		if(UserManager.lang.Equals(UserManager.LanguageOption._English))
		{
			firstAlertFPS = firstAlertFPSEng;
			secondAlertFPS = secondAlertFPSEng;
			thirdAlertFPS = thirdAlertFPSEng;

			firstAlert = firstAlertEng;
			secondAlert = secondAlertEng;
			thirdAlert = thirdAlertEng;
		}

		else if(UserManager.lang.Equals(UserManager.LanguageOption._Polish))
		{
			firstAlertFPS = firstAlertFPSPl;
			secondAlertFPS = secondAlertFPSPl;
			thirdAlertFPS = thirdAlertFPSPl;

			firstAlert = firstAlertPl;
			secondAlert = secondAlertPl;
			thirdAlert = thirdAlertPl;
		}

		elementsHUD = GameObject.FindGameObjectsWithTag(turnOffTag);
		currentSceneNumber = SceneManager.GetActiveScene().buildIndex;
		timer = cameraTime - waitBeginTime;
		fps = false;

		playerHealth = player.GetComponent<PlayerHealth>();
	}

	// Update is called once per frame
	void Update ()
	{
		// if (playerHealth.isDead)
		// {
		// 	player.GetComponent<PlayerMovement>().enabled = false;
		// 	player.GetComponent<CharacterController>().enabled = false;

		// 	player.transform.Find("Player").gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;
		// 	player.transform.Find("Gun").gameObject.GetComponent<SkinnedMeshRenderer>().enabled = true;

		// 	mainLight.gameObject.SetActive(true);
		// 	player.GetComponent<Light>().enabled = false;

		// 	cameraMain.gameObject.SetActive(true);
		// 	player.transform.Find("MainCameraFPS").gameObject.SetActive(false);

		// 	imageCrossHair.gameObject.GetComponent<Image>().enabled = false;	
		// }

		// timer += Time.deltaTime;

		// if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - waitBeginTime)) && (timer <= (cameraTime - (2 * waitBeginTime / 3))) && !fps && !usedFirst)
		// {
		// 	usedFirst = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;1");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), firstAlert));
		// 	elementsHUD[0].gameObject.SetActive(fps);
		// }

		// else if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - (2 * waitBeginTime / 3))) && (timer <= (cameraTime - (1 * waitBeginTime / 3))) && !fps && !usedSecond)
		// {
		// 	usedSecond = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;2");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), secondAlert));
		// 	elementsHUD[1].gameObject.SetActive(fps);
		// }

		// else if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - (1 * waitBeginTime / 3))) && (timer <= (cameraTime)) && !fps && !usedThird)
		// {
		// 	usedThird = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;3");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), thirdAlert));
		// 	elementsHUD[2].gameObject.SetActive(fps);

		// 	StartCoroutine(Wait());
		// }

		// if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - waitBeginTime)) && (timer <= (cameraTime - (2 * waitBeginTime / 3))) && fps && !usedFirst)
		// {
		// 	usedFirst = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;FPS1");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), firstAlertFPS));
		// 	elementsHUD[0].gameObject.SetActive(fps);
		// }

		// else if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - (2 * waitBeginTime / 3))) && (timer <= (cameraTime - (1 * waitBeginTime / 3))) && fps && !usedSecond)
		// {
		// 	usedSecond = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;FPS2");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), secondAlertFPS));
		// 	elementsHUD[1].gameObject.SetActive(fps);
		// }

		// else if((currentSceneNumber == thirdLvlIndex) && (timer >= (cameraTime - (1 * waitBeginTime / 3))) && (timer <= (cameraTime)) && fps && !usedThird)
		// {
		// 	usedThird = true;
		// 	LogManager.logManager.AddEvent(Time.time, "Alert;CameraChange;FPS3");
		// 	StartCoroutine(ImportantAlertManager.importantAlertManager.ShowAlertAndLerp((waitBeginTime / 3), thirdAlertFPS));
		// 	elementsHUD[2].gameObject.SetActive(fps);

		// 	StartCoroutine(Wait());
		// }

		// if (Input.GetKeyUp(KeyCode.M) && !playerHealth.isDead) 
		// {

		// 	LogManager.logManager.AddEvent(Time.time, "KeyPress;M");
			
		// 	fps = !fps;
		// 	changeCam();

		// 	timer = 0f;
		// }

		// if ((currentSceneNumber == thirdLvlIndex) && (timer >= cameraTime) && !playerHealth.isDead)
		// {
		// 	fps = !fps;
		// 	changeCam();

		// 	timer = 0f;
		// }

		// if (Input.GetKeyDown(KeyCode.I))
		// {
		// 	LogManager.logManager.AddEvent(Time.time, "KeyPress;I");
		// 	Cursor.lockState = CursorLockMode.None;
		// 	dontChange = true;
		// }

		// else if(!fps || playerHealth.isDead && !dontChange)
		// {
		// 	Cursor.lockState = CursorLockMode.None;
		// }

		// else if(fps && !dontChange)
		// {
		// 	Cursor.lockState = CursorLockMode.Locked;
		// }
	}

	IEnumerator Wait()
	{
		yield return new WaitForSeconds(turnOffTime);
	}

	void changeCam()
	{
		string camerName = fps ? "FPS" : "Izomorphic";

		// LogManager.logManager.AddEvent(Time.time, "Camera;ChangeTo;" + camerName);
	
		usedFirst = false;
		usedSecond = false;
		usedThird = false;

		foreach(GameObject element in elementsHUD)
		{
			element.gameObject.SetActive(!fps);
		}

		player.GetComponent<PlayerMovement>().enabled = !fps;
		player.GetComponent<CharacterController>().enabled = fps;

		player.transform.Find("Player").gameObject.GetComponent<SkinnedMeshRenderer>().enabled = !fps;
		player.transform.Find("Gun").gameObject.GetComponent<SkinnedMeshRenderer>().enabled = !fps;

		mainLight.gameObject.SetActive(!fps);
		player.GetComponent<Light>().enabled = fps;

		cameraMain.gameObject.SetActive(!fps);
		player.transform.Find("MainCameraFPS").gameObject.SetActive(fps);

		imageCrossHair.gameObject.GetComponent<Image>().enabled = fps;
	}
}