using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;

public class ManageIntroduction : MonoBehaviour {

	public Text textShowed;
    // public Button affectiveButton;
    // public Button nonaffectiveButton;
    public Button polishButton;
    public Button englishButton;
    // public InputField nameInputField;
    // public InputField numberInputField;

    public float timeCalibrationCheck = 0.5f;
    public float timeWait = 1f;

    // F2
    // public float timeLoadTutorial = 2f;
    // float timer = 0f;

    string txtWelecomePl = "Witaj we Freud2.0!\n\n To Twoja kolejna sesja terapeutyczna,\njesteś tu ze względu na problemy z pamięcią.\nNasza terapia opiera się na psychoanalizie,\nktórej autorem jest Zygmunt Freud.\nW jego koncepcji psychika działa na trzech poziomach\n Świadomości, przedświadomości i nieświadomości.\nNajwiększy wpływ na późniejsze życie ma dzieciństwo.\nNo, więc właśnie tam sie udajemy.\nDo dzieciństwa poprzez wszystkie trzy poziomy, ale najpierw tutorial."; //F2

    // F2
    // string txtPreCalibrationPl = "Zaraz rozpocznie się kalibracja.\nUłóż ręce jak do grania i wyprostuj się.";
    // string txtCalibrationPl = "Postaraj się odprężyć.\nOddychaj powoli głębokimi oddechami\ni postaraj się nie ruszać.\nKalibracja potrwa około 30 sekund.";
    // string txtCalibratedPl = "Skalibrowano!";

    string txtWelecomeEng = "Welcome in the Freud2.0!\n\n This is your next therapeutic session,\nyou are here because of problems with memory.\nOur therapy is based on psychoanalysis,\ncreated by Sigmund Freud.\nIn his concept, the psyche works on three levels.\n The consciousness, the preconscious and the unconscious.\nChildhood has great impact on later life.\nNow, that's where we go.\nTo childhood through all three levels, but first the tutorial."; //F2

    // F2
    // string txtPreCalibrationEng = "The calibration will begin soon.\nArrange your hands as if you were playing and straighten up.";
    // string txtCalibrationEng = "Try to relax.\nBreathe deeply and try not to move.\nCalibrations takes around 30 seconds.";
    // string txtCalibratedEng = "Calibrated!";

    string txtWelecome = "";

    // F2
    // // string txtPreCalibration = "";
    // // string txtCalibration = "";
    // // string txtCalibrated = "";

    // string txtAffectiveButtonPl = "Z pętlą afektywną";
    // string txtNonAffectiveButtonPl = "Graj!"; //F2

    // string txtNameInputPl = "Nazwa";
    // string txtNumberInputPl = "Numer";

    // F2
    // string txtAffectiveButtonEng = "With affective loop";
    // string txtNonAffectiveButtonEng = "Play!"; //F2
    // string txtNameInputEng = "Name";
    // string txtNumberInputEng = "Number";

    UserManager.LanguageOption lang;

    public float timeToStart = 30;

	int timeCD;
    
    public Text txt;

    string txtTime = "";

    string txtTimePL = "Gra rozpocznie się za: ";
    string txtTimeEng = "The game will start in: ";


    void Awake()
    {
        timeCD = (int)timeToStart;
	    StartCoroutine("LoseTime");

        // float t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float t2 = Time.time;

        LogManager.logManager.AddEvent(t2, "Scene;Load;" + "ID;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;
        ChangeLanguage();
    }

    void Update() 
    {
        if(Input.GetButtonDown ("Fire2"))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;X");
            lang = UserManager.LanguageOption._English;
            UserManager.lang = lang;
            ChangeLanguage();
        }

        if(Input.GetButtonDown ("Fire3"))
        {
            LogManager.logManager.AddEvent(Time.time, "Key;O");
            lang = UserManager.LanguageOption._Polish;
            UserManager.lang = lang;
            ChangeLanguage();
        }

        if (Input.GetKeyDown(KeyCode.S))
		{
            LogManager.logManager.AddEvent(Time.time, "Key;S");
			LoadLvl0();
            // LoadTutorial();
		}

        if (timeToStart < Time.timeSinceLevelLoad)
        {
            LoadLvl0();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "Key;Escape");
			Application.Quit(); // ignored in UnityEditor
			// EditorApplication.isPlaying = false;
		}

        if (!lang.Equals(UserManager.lang))
        {
            lang = UserManager.lang;
            ChangeLanguage();
        }

        // if(BitalinoController.bitalinoController != null && BitalinoController.bitalinoController.FinishedCalibration)
        // {
        //     textShowed.text = txtCalibrated;
        //     timer += Time.deltaTime;

        //     if (timer > timeLoadTutorial)
        //     {
        //         LoadTutorial();
        //     }
        // }
    }

    void ChangeLanguage()
    {
        switch(lang)
        {
            case UserManager.LanguageOption._English:
            {
                txtWelecome = txtWelecomeEng;
                // F2
                // txtPreCalibration = txtPreCalibrationEng;
                // txtCalibrated = txtCalibratedEng;
                // txtCalibration = txtCalibrationEng;

                textShowed.text = txtWelecome;
                // F2
                // affectiveButton.GetComponentInChildren<Text>().text = txtAffectiveButtonEng;
                // nonaffectiveButton.GetComponentInChildren<Text>().text = txtNonAffectiveButtonEng;
                // nameInputField.GetComponentInChildren<Text>().text = txtNameInputEng;
                // numberInputField.GetComponentInChildren<Text>().text = txtNumberInputEng;

                txtTime = txtTimeEng;
                break;
            }

            case UserManager.LanguageOption._Polish:
            {
                txtWelecome = txtWelecomePl;
                // F2
                // txtPreCalibration = txtPreCalibrationPl;
                // txtCalibrated = txtCalibratedPl;
                // txtCalibration = txtCalibrationPl;
                
                textShowed.text = txtWelecome;
                // F2
                // affectiveButton.GetComponentInChildren<Text>().text = txtAffectiveButtonPl;
                // nonaffectiveButton.GetComponentInChildren<Text>().text = txtNonAffectiveButtonPl;
                // nameInputField.GetComponentInChildren<Text>().text = txtNameInputPl;
                // numberInputField.GetComponentInChildren<Text>().text = txtNumberInputPl;

                txtTime = txtTimePL;
                break;
            }
        }
    }

    // public void MangeNonAffective()
    // {
    //     LogManager.logManager.AddEvent(Time.time, "ButtonClick;WithoutAffectiveLoop");
    //     LoadTutorial();
    // }

	public void LoadTutorial ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 3);
	}

    public void LoadLvl0 ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 2);
	}

    // F2
    // public void LoadCalibration ()
    // {
    //     LogManager.logManager.AddEvent(Time.time, "ButtonClick;WithAffectiveLoop");

    //     TurnOffButtons();
	// 	textShowed.text = txtPreCalibration;
        
    //     StartCoroutine(Calibrate());
	// }

    void TurnOffButtons ()
    {
    //     affectiveButton.gameObject.SetActive(false);
    //     nonaffectiveButton.gameObject.SetActive(false);
        polishButton.gameObject.SetActive(false);
        englishButton.gameObject.SetActive(false);
    //     nameInputField.gameObject.SetActive(false);
    //     numberInputField.gameObject.SetActive(false);
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(timeWait);
    }

    IEnumerator LoseTime()
	{
		while(true)
		{
            string toShow = txtTime + timeCD.ToString() + "s";
            LogManager.logManager.AddEvent(Time.time, "Game;Introduction;CountDown;Text;ChangeTo;" + toShow);
			txt.text = toShow;
			timeCD -= 1;
			yield return new WaitForSeconds(1);
		}
	}

    // F2
    // IEnumerator Calibrate()
    // {
    //     if (!BitalinoController.bitalinoController.FinishedCalibration)
    //     {
    //         yield return new WaitForSeconds(timeWait);
    //         LogManager.logManager.AddEvent(Time.time, "BITalino;StartReading");
    //         StartCoroutine(BitalinoController.bitalinoController.StartReading());
    //         yield return new WaitForSeconds(timeWait);

    //         yield return new WaitForSeconds(BitalinoController.bitalinoController.calibrationPreTime);
    //         textShowed.text = txtCalibration;
            
    //         while (!BitalinoController.bitalinoController.FinishedCalibration)
    //         {
    //             yield return new WaitForSeconds(timeCalibrationCheck);
    //         }

    //         StartCoroutine(Wait());
    //     }
    // }
}