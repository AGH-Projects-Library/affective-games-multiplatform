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
    public Button affectiveButton;
    public Button nonaffectiveButton;
    public Button polishButton;
    public Button englishButton;
    public InputField nameInputField;
    public InputField numberInputField;

    public float timeCalibrationCheck = 0.5f;
    public float timeWait = 1f;

    public float timeLoadTutorial = 2f;
    float timer = 0f;

    string txtWelecomePl = "Witaj we Freud Me Out!\nWprowadź potrzebne dane\na następnie wybierz wersję gry do uruchomienia.";
    string txtPreCalibrationPl = "Zaraz rozpocznie się kalibracja.\nUłóż ręce jak do grania i wyprostuj się.";
    string txtCalibrationPl = "Postaraj się odprężyć.\nOddychaj powoli głębokimi oddechami\ni postaraj się nie ruszać.\nKalibracja potrwa około 30 sekund.";
    string txtCalibratedPl = "Skalibrowano!";

    string txtWelecomeEng = "Welcome in the Freud Me Out!\nInput your data\nand then choose game version.";
    string txtPreCalibrationEng = "The calibration will begin soon.\nArrange your hands as if you were playing and straighten up.";
    string txtCalibrationEng = "Try to relax.\nBreathe deeply and try not to move.\nCalibrations takes around 30 seconds.";
    string txtCalibratedEng = "Calibrated!";

    string txtWelecome = "";
    string txtPreCalibration = "";
    string txtCalibration = "";
    string txtCalibrated = "";

    string txtAffectiveButtonPl = "Z pętlą afektywną";
    string txtNonAffectiveButtonPl = "Bez pętli afektywnej";

    string txtNameInputPl = "Nazwa";
    string txtNumberInputPl = "Numer";

    string txtAffectiveButtonEng = "With affective loop";
    string txtNonAffectiveButtonEng = "Without affective loop";
    string txtNameInputEng = "Name";
    string txtNumberInputEng = "Number";

    UserManager.LanguageOption lang;


    void Awake()
    {
        float t1 = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float t2 = Time.time;

        LogManager.logManager.AddEvent(t1, "UnixTime");
        LogManager.logManager.AddEvent(t2, "Scene;Load;" + SceneManager.GetActiveScene().buildIndex);

        lang = UserManager.lang;
        ChangeLanguage();
    }

    void Update() 
    {
        if (Input.GetKey(KeyCode.Escape))
		{
			LogManager.logManager.AddEvent(Time.time, "KeyPress;Esc");
			Application.Quit(); // ignored in UnityEditor
			EditorApplication.isPlaying = false;
		}

        if (!lang.Equals(UserManager.lang))
        {
            lang = UserManager.lang;
            ChangeLanguage();
        }

        if(BitalinoController.bitalinoController != null && BitalinoController.bitalinoController.FinishedCalibration)
        {
            textShowed.text = txtCalibrated;
            timer += Time.deltaTime;

            if (timer > timeLoadTutorial)
            {
                LoadTutorial();
            }
        }
    }

    void ChangeLanguage()
    {
        switch(lang)
        {
            case UserManager.LanguageOption._English:
            {
                txtWelecome = txtWelecomeEng;
                txtPreCalibration = txtPreCalibrationEng;
                txtCalibrated = txtCalibratedEng;
                txtCalibration = txtCalibrationEng;

                textShowed.text = txtWelecome;
                affectiveButton.GetComponentInChildren<Text>().text = txtAffectiveButtonEng;
                nonaffectiveButton.GetComponentInChildren<Text>().text = txtNonAffectiveButtonEng;
                nameInputField.GetComponentInChildren<Text>().text = txtNameInputEng;
                numberInputField.GetComponentInChildren<Text>().text = txtNumberInputEng;
                break;
            }

            case UserManager.LanguageOption._Polish:
            {
                txtWelecome = txtWelecomePl;
                txtPreCalibration = txtPreCalibrationPl;
                txtCalibrated = txtCalibratedPl;
                txtCalibration = txtCalibrationPl;
                
                textShowed.text = txtWelecome;
                affectiveButton.GetComponentInChildren<Text>().text = txtAffectiveButtonPl;
                nonaffectiveButton.GetComponentInChildren<Text>().text = txtNonAffectiveButtonPl;
                nameInputField.GetComponentInChildren<Text>().text = txtNameInputPl;
                numberInputField.GetComponentInChildren<Text>().text = txtNumberInputPl;
                break;
            }
        }
    }

    public void MangeNonAffective()
    {
        LogManager.logManager.AddEvent(Time.time, "ButtonClick;WithoutAffectiveLoop");
        LoadTutorial();
    }

	public void LoadTutorial ()
    {
        TurnOffButtons();
        StartCoroutine(Wait());

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
	}

    public void LoadCalibration ()
    {
        LogManager.logManager.AddEvent(Time.time, "ButtonClick;WithAffectiveLoop");

        TurnOffButtons();
		textShowed.text = txtPreCalibration;
        
        StartCoroutine(Calibrate());
	}

    void TurnOffButtons ()
    {
        affectiveButton.gameObject.SetActive(false);
        nonaffectiveButton.gameObject.SetActive(false);
        polishButton.gameObject.SetActive(false);
        englishButton.gameObject.SetActive(false);
        nameInputField.gameObject.SetActive(false);
        numberInputField.gameObject.SetActive(false);
    }

    IEnumerator Wait()
    {
        yield return new WaitForSeconds(timeWait);
    }

    IEnumerator Calibrate()
    {
        if (!BitalinoController.bitalinoController.FinishedCalibration)
        {
            yield return new WaitForSeconds(timeWait);
            LogManager.logManager.AddEvent(Time.time, "BITalino;StartReading");
            StartCoroutine(BitalinoController.bitalinoController.StartReading());
            yield return new WaitForSeconds(timeWait);

            yield return new WaitForSeconds(BitalinoController.bitalinoController.calibrationPreTime);
            textShowed.text = txtCalibration;
            
            while (!BitalinoController.bitalinoController.FinishedCalibration)
            {
                yield return new WaitForSeconds(timeCalibrationCheck);
            }

            StartCoroutine(Wait());
        }
    }
}