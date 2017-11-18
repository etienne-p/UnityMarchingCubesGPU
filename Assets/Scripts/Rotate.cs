using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour 
{
	[SerializeField] Vector3 eulerAngles;
	[SerializeField] float animationSpeed;

	void Update () 
	{
		transform.Rotate (eulerAngles * Time.deltaTime * animationSpeed);
	}
}
