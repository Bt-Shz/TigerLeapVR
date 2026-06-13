using UnityEngine;
using UnityEngine.UI; // Important for Sliders

public class FoodeChooseGameManagerr : MonoBehaviour
{
    public static FoodeChooseGameManagerr Instance; // Singleton so we can call it from anywhere

    [Header("UI References")]
    public Slider oilSlider;
    public Slider sugarSlider;

    private float currentOil = 0;
    private float currentSugar = 0;

    void Awake()
    {
        Instance = this;
    }

    public void AddFoodStats(float oilToAdd, float sugarToAdd)
    {
        currentOil += oilToAdd;
        currentSugar += sugarToAdd;

        // Update UI
        oilSlider.value = currentOil;
        sugarSlider.value = currentSugar;
    }
}
