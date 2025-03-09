// Copyright (c) 2014, Tokyo University of Science All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met: * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer. * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution. * Neither the name of the Tokyo Univerity of Science nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using UnityEngine;
using System.Collections;

public class MissileManager : MonoBehaviour {
    public Helicopter helico;
    public GameObject obstacle;
    public GUIText score;

    private float time = 3f;

	// Use this for initialization
	void Start () {
        StartCoroutine(manager());
	}

    /// <summary>
    /// When the game start, the manager generate obstacles
    /// </summary>
    private IEnumerator manager()
    {
        while (!helico.asStart)
            yield return new WaitForSeconds(0.5f);
        while(helico.asStart)
        {
            float posY = Random.Range(-4f, 4f);
            GameObject go = (GameObject)Instantiate(obstacle, new Vector3(11f, posY, 0f), new Quaternion());
            Missile ob = (Missile)go.GetComponent("Missile");
            ob.helico = this.helico;
            ob.score = this.score;
            yield return new WaitForSeconds(time);
            if (time > 1f)
                time -= 0.1f;
        }
    }
    	
	// Update is called once per frame
	void Update () 
    {
	}
}
