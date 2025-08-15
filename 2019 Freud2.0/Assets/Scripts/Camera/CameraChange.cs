using System.Collections;
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

	string firstAlertFPS, secondAlertFPS, thirdAlertFPS, firstAlert, secondAlert, thirdAlert;

	public GameObject[] elementsHUD;
	string turnOffTag = "TurnOff";
	float turnOffTime = 0.5f;

	float timer;
	bool fps;

	int currentSceneNumber;
	int thirdLvlIndex = 8;

	bool usedFirst, usedSecond, usedThird;
	bool dontChange;

	void Awake() {
		SetLanguageStrings(UserManager.Instance.language);
		elementsHUD = GameObject.FindGameObjectsWithTag(turnOffTag);
		currentSceneNumber = SceneManager.GetActiveScene().buildIndex;
		timer = cameraTime - waitBeginTime;
		fps = false;
		playerHealth = player.GetComponent<PlayerHealth>();
	}

	void Update() {
		// Commented out code removed
	}

	IEnumerator Wait() {
		yield return new WaitForSeconds(turnOffTime);
	}

	void changeCam() {
		string camerName = fps ? "FPS" : "Izomorphic";
		usedFirst = usedSecond = usedThird = false;

		foreach (GameObject element in elementsHUD)
			element.gameObject.SetActive(!fps);

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

	void SetLanguageStrings(UserManager.Language lang) {
		if (lang == UserManager.Language.English) {
			firstAlertFPS = firstAlertFPSEng;
			secondAlertFPS = secondAlertFPSEng;
			thirdAlertFPS = thirdAlertFPSEng;
			firstAlert = firstAlertEng;
			secondAlert = secondAlertEng;
			thirdAlert = thirdAlertEng;
		}
		else if (lang == UserManager.Language.Polish) {
			firstAlertFPS = firstAlertFPSPl;
			secondAlertFPS = secondAlertFPSPl;
			thirdAlertFPS = thirdAlertFPSPl;
			firstAlert = firstAlertPl;
			secondAlert = secondAlertPl;
			thirdAlert = thirdAlertPl;
		}
	}

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
}
