﻿using UnityEngine;
using System.Collections;

public class Crosshairs : MonoBehaviour {

    bool devMode;

    public LayerMask targetMask;
    public SpriteRenderer dot;
    public Color dotHighlightColor;
    Color originalDotColor;

    void Start () {
        devMode = FindObjectOfType<ControlPanel>().devMode;
        if(!devMode) Cursor.visible = false;
        originalDotColor = dot.color;
    }

	void Update () {
        transform.Rotate(Vector3.forward * -40 * Time.deltaTime);
	}

    public void DetectTargets(Ray ray) {
        if (Physics.Raycast(ray, 100, targetMask)) {
            dot.color = dotHighlightColor;
        } else {
            dot.color = originalDotColor;
        }
    }
}
