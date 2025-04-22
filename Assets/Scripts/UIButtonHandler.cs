using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles UI button interactions for the BGSNL Dashboard
/// Attach this script to button GameObjects to handle specific functions
/// </summary>
public class UIButtonHandler : MonoBehaviour
{
    public enum ButtonType
    {
        DropdownToggle,
        BGSNLHome,
        CitiesScene,
        CityButton,
        ChartsScene,
        AchievementsScene
    }

    [Header("Button Configuration")]
    [SerializeField] private ButtonType buttonType;
    [SerializeField] private string cityId; // Only used for CityButton type
    [SerializeField] private GameObject dropdownMenu; // Only used for DropdownToggle
    
    [Header("Debugging")]
    [SerializeField] private bool debugMode = true;
    
    private Button button;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[UIButtonHandler] No Button component found on {gameObject.name}");
            return;
        }
        
        // If this is a CityButton but no cityId is set, try to infer from name
        if (buttonType == ButtonType.CityButton && string.IsNullOrEmpty(cityId))
        {
            InferCityIdFromName();
        }
    }
    
    private void InferCityIdFromName()
    {
        // Try to use the button or GameObject name as cityId if not set
        string name = gameObject.name.ToLower();
        
        if (name.StartsWith("citybutton_"))
        {
            cityId = name.Substring(11); // Remove "citybutton_" prefix
        }
        else if (name.EndsWith("button"))
        {
            cityId = name.Substring(0, name.Length - 6); // Remove "button" suffix
        }
        else if (name.Contains("_"))
        {
            cityId = name.Split('_')[0]; // Use part before underscore
        }
        else
        {
            cityId = name; // Just use the name as is
        }
        
        LogDebug($"[UIButtonHandler] Inferred cityId '{cityId}' from name '{gameObject.name}'");
    }
    
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log(message);
        }
    }
    
    private void Start()
    {
        // Setup button click handler based on type
        button.onClick.RemoveAllListeners();
        
        switch (buttonType)
        {
            case ButtonType.DropdownToggle:
                button.onClick.AddListener(ToggleDropdownMenu);
                break;
                
            case ButtonType.BGSNLHome:
                button.onClick.AddListener(GoToBGSNLHome);
                break;
                
            case ButtonType.CitiesScene:
                button.onClick.AddListener(GoToCitiesScene);
                break;
                
            case ButtonType.CityButton:
                button.onClick.AddListener(SelectCityAndGoHome);
                break;
                
            case ButtonType.ChartsScene:
                button.onClick.AddListener(GoToChartsScene);
                break;
                
            case ButtonType.AchievementsScene:
                button.onClick.AddListener(GoToAchievementsScene);
                break;
        }
        
        // Log setup for clarity
        if (buttonType == ButtonType.CityButton)
        {
            LogDebug($"[UIButtonHandler] Setup CityButton '{gameObject.name}' with cityId: '{cityId}'");
        }
    }
    
    private void ToggleDropdownMenu()
    {
        if (dropdownMenu != null)
        {
            bool isActive = dropdownMenu.activeSelf;
            dropdownMenu.SetActive(!isActive);
            LogDebug($"[UIButtonHandler] Toggled dropdown menu: {!isActive}");
        }
        else
        {
            Debug.LogError("[UIButtonHandler] Cannot toggle dropdown menu - reference is null!");
        }
    }
    
    private void GoToBGSNLHome()
    {
        LogDebug("[UIButtonHandler] BGSNL Home button clicked");
        
        // Override forceDefaultCity in PlayerPrefs to make sure it uses BGSNL
        PlayerPrefs.SetInt("ForceDefaultCity", 1);
        PlayerPrefs.SetString("SelectedCityId", "bgsnl");
        PlayerPrefs.Save();
        LogDebug("[UIButtonHandler] Set SelectedCityId to 'bgsnl' in PlayerPrefs");
        
        // Close dropdown if it exists in the scene
        if (dropdownMenu != null)
        {
            dropdownMenu.SetActive(false);
        }
        
        // Try to find UIManager in current scene
        UIManager manager = FindObjectOfType<UIManager>();
        if (manager != null)
        {
            LogDebug("[UIButtonHandler] Found UIManager, calling LoadCity with 'bgsnl'");
            manager.LoadCity("bgsnl");
        }
        
        // Load main scene if not already there
        if (SceneManager.GetActiveScene().name != "HomeScreen")
        {
            LogDebug("[UIButtonHandler] Loading HomeScreen scene");
            SceneManager.LoadScene("HomeScreen");
        }
        else
        {
            // If we're already in the main scene and couldn't find a UIManager, try once more
            if (manager == null)
            {
                LogDebug("[UIButtonHandler] Already in HomeScreen but no UIManager found, trying again");
                manager = FindObjectOfType<UIManager>();
                if (manager != null)
                {
                    manager.LoadCity("bgsnl");
                }
            }
        }
    }
    
    private void GoToCitiesScene()
    {
        // Close dropdown if it exists
        if (dropdownMenu != null)
        {
            dropdownMenu.SetActive(false);
        }
        
        // Load cities scene
        LogDebug("[UIButtonHandler] Loading CitiesScreen scene");
        SceneManager.LoadScene("CitiesScreen");
    }
    
    private void GoToChartsScene()
    {
        // Close dropdown if it exists
        if (dropdownMenu != null)
        {
            dropdownMenu.SetActive(false);
        }
        
        // Load charts scene
        LogDebug("[UIButtonHandler] Loading ChartsScreen scene");
        SceneManager.LoadScene("ChartsScreen");
    }
    
    private void GoToAchievementsScene()
    {
        // Close dropdown if it exists
        if (dropdownMenu != null)
        {
            dropdownMenu.SetActive(false);
        }
        
        // Load achievements scene
        LogDebug("[UIButtonHandler] Loading AchievementsScreen scene");
        SceneManager.LoadScene("AchievementsScreen");
    }
    
    private void SelectCityAndGoHome()
    {
        // Double-check cityId
        if (string.IsNullOrEmpty(cityId))
        {
            InferCityIdFromName();
            
            if (string.IsNullOrEmpty(cityId))
            {
                Debug.LogError($"[UIButtonHandler] Cannot select city - cityId is null or empty for button {gameObject.name}");
                return;
            }
        }
        
        // Tell the system not to force default city
        PlayerPrefs.SetInt("ForceDefaultCity", 0);
        PlayerPrefs.SetString("SelectedCityId", cityId);
        PlayerPrefs.Save();
        LogDebug($"[UIButtonHandler] City button clicked: {gameObject.name} with cityId: {cityId}");
        LogDebug($"[UIButtonHandler] Set SelectedCityId to '{cityId}' in PlayerPrefs");
        
        // Try to find UIManager in current scene
        UIManager manager = FindObjectOfType<UIManager>();
        if (manager != null)
        {
            LogDebug($"[UIButtonHandler] Found UIManager, calling LoadCity with '{cityId}'");
            manager.LoadCity(cityId);
        }
        
        // Go to main scene
        LogDebug("[UIButtonHandler] Loading HomeScreen scene");
        SceneManager.LoadScene("HomeScreen");
    }
    
    // Reset to BGSNL when quitting the application to ensure it starts with BGSNL next time
    private void OnApplicationQuit()
    {
        // Force default city on next startup
        PlayerPrefs.SetInt("ForceDefaultCity", 1);
        PlayerPrefs.SetString("SelectedCityId", "bgsnl");
        PlayerPrefs.Save();
        LogDebug("[UIButtonHandler] Application quitting - Reset preferences to BGSNL for next startup");
    }
} 