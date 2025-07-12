// Copyright (c) 2014, Tokyo University of Science All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met: * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. * Neither the name of the Tokyo Univerity of Science nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using UnityEngine;
using System.Collections;

public class Helicopter : MonoBehaviour {
    public BitalinoReader reader;
    public GUIText text;

    private bool _asStart = false;
    private double Cmax = 0;
    private float rapport = 0;
    private bool movement = false;

	// Use this for initialization
	void Start () 
    {
        StartCoroutine(start());
	}

    /// <summary>
    /// get if the object is ready
    /// </summary>
    public bool asStart
    {
        get{return _asStart;}
    }

    /// <summary>
    /// Initialisation of the object
    /// </summary>
    private IEnumerator start()
    {
        while(!reader.asStart)
        {
            yield return new WaitForSeconds(0.5f);
        }
        text.text = "Calibration";

        foreach(BITalinoFrame frame in reader.getBuffer())
        {
            Cmax += frame.GetAnalogValue(0);
        }
        Cmax = Cmax / reader.BufferSize;
        rapport = (float)(9.0 / Cmax);
        text.text = "Ready ?";
        movement = true;
        while (!(getPos() < 2.5f && getPos() > -2.5f))
            yield return new WaitForSeconds(0.5f);
        text.text = "3";
        yield return new WaitForSeconds(1f);
        text.text = "2";
        yield return new WaitForSeconds(1f);
        text.text = "1";
        yield return new WaitForSeconds(1f);
        text.text = " ";
        _asStart = true;

    }

    /// <summary>
    /// Calculation of the Y position of the object
    /// </summary>
    /// <returns>Return the Y position calculated</returns>
    private float getPos()
    {
        BITalinoFrame[] frames = reader.getBuffer();
        float pos = 0;
        for (int i = 20; i > 0; i--)
            pos += (float)frames[reader.BufferSize - i].GetAnalogValue(0);
        pos = ((pos / 20f) * rapport) - 5f;
        return pos;
    }
	
	/// <summary>
	/// Calcalation of the movement of the object
	/// </summary>
	void Update () {
        if (movement)
        {
            transform.position = new Vector3(-9.0f,getPos(),0.0f);
        }
	}

    /// <summary>
    /// Collision detector
    /// </summary>
    void OnCollisionEnter()
    {
        if (_asStart)
        {
            movement = false;
            _asStart = false;
            text.text = "GameOver";
        }
    }
}
