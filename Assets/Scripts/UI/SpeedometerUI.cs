using System;
using TMPro;
using UnityEngine;

public class SpeedometerUI : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    private TextMeshProUGUI speedometerText;
    private void Awake()
    {
        speedometerText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        string currentSpeed = $"{MathF.Round(controller.CurrentFullSpeed, 1)} km/h";
        speedometerText.text = currentSpeed;
    }
}
