using UnityEngine;
using System.Collections;
using Valve.VR;

public class AmbientSound : MonoBehaviour {
	AudioSource s;

	public float fadeintime;

	float t;

	public bool fadeblack = false;

	float vol;

	// Use this for initialization
	void Start () {
		AudioListener.volume = 1;
		s = GetComponent<AudioSource> ();
		s.time = Random.Range (0, s.clip.length);
		if (fadeintime > 0)
			t = 0;

		vol = s.volume;

		SteamVR_Fade.Start(Color.black, 0);
		SteamVR_Fade.Start(Color.clear, 7);
	}
	
	// Update is called once per frame
	void Update () {
		if (fadeintime > 0&&t<1) {
			t += Time.deltaTime / fadeintime;
			s.volume = t * vol;
		}
	
	}
}
